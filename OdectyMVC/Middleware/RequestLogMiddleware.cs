using System.Text;

namespace OdectyMVC.Middleware;

public class RequestLogMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestLogMiddleware> logger;

    public RequestLogMiddleware(RequestDelegate next, ILogger<RequestLogMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var method = request.Method;
        var path = request.Path;
        var queryString = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
        context.Request.EnableBuffering();

        string requestBody = string.Empty;
        if (request.ContentLength > 0 && request.ContentType?.Contains("multipart") != true)
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        logger.LogInformation("Incoming Request: {Method} {Path}{QueryString} {ContentType} {ContentLength}", method, path, queryString, request.ContentType, request.ContentLength);
        if (!string.IsNullOrWhiteSpace(requestBody))
        {
            logger.LogInformation("Request Body:\n{Body}", requestBody);
        }
        else
        {
            logger.LogInformation("Request Body: [skipped or empty]");
        }

        var originalBodyStream = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await next(context);

        memStream.Position = 0;
        var responseBody = await new StreamReader(memStream).ReadToEndAsync();
        memStream.Position = 0;
        await memStream.CopyToAsync(originalBodyStream);

        logger.LogInformation("Outgoing Response: {StatusCode}", context.Response.StatusCode);
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            logger.LogInformation("Response Body:\n{Body}", responseBody);
        }
        else
        {
            logger.LogInformation("Response Body: [empty]");
        }
    }
}
