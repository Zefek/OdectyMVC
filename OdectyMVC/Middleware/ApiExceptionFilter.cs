using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace OdectyMVC.Middleware;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ProblemDetailsFactory problemDetailsFactory;
    private readonly ILogger<ApiExceptionFilter> logger;

    public ApiExceptionFilter(ProblemDetailsFactory problemDetailsFactory, ILogger<ApiExceptionFilter> logger)
    {
        this.problemDetailsFactory = problemDetailsFactory;
        this.logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled || !context.ActionDescriptor.EndpointMetadata.OfType<ApiControllerAttribute>().Any())
        {
            return;
        }
        var httpContext = context.HttpContext;
        var statusCode = context.Exception switch
        {
            HttpRequestException => StatusCodes.Status502BadGateway,
            TaskCanceledException when !httpContext.RequestAborted.IsCancellationRequested => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError,
        };
        var sanitizedPath = (httpContext.Request.Path.Value ?? string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
        logger.LogError(context.Exception, "API request {Method} {Path} failed with {StatusCode}", httpContext.Request.Method, sanitizedPath, statusCode);
        var problem = problemDetailsFactory.CreateProblemDetails(httpContext, statusCode);
        context.Result = new ObjectResult(problem) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
