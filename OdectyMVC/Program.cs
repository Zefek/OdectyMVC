using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using OdectyMVC;
using OdectyMVC.Application;
using OdectyMVC.Contracts;
using OdectyMVC.DataLayer;
using OdectyMVC.HealthChecks;
using OdectyMVC.Middleware;
using OdectyMVC.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "OdectyMVC";
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry(opts =>
    {
        opts.Endpoint = otlpEndpoint;
        opts.Protocol = OtlpProtocol.Grpc;
        opts.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = serviceName
        };
    }));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("RabbitMQ.Client.Publisher", "RabbitMQ.Client.Subscriber")
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQSettings"));
builder.Services.Configure<GaugeImageLocation>(builder.Configuration.GetSection("GaugeImageLocation"));
builder.Services.Configure<BasicAuthentication>(builder.Configuration.GetSection("BasicAuthentication"));
builder.Services.AddScoped<IGaugeService, GaugeService>();
builder.Services.AddScoped<IGaugeContext, GaugeContext>();
builder.Services.AddScoped<GaugeDbContext>();

builder.Services.AddScoped<IGaugeRepository, GaugeRepository>();
builder.Services.AddScoped<IGaugeListModelRepository, GaugeListModelRepository>();
builder.Services.AddSingleton<IMessageQueue, MessageQueue>();
builder.Services.AddSingleton<RabbitMQProvider>();
builder.Services.AddHostedService<IncomeMessageBackgroundService>();

builder.Services.AddHealthChecks()
    .AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: new[] { "ready" })
    .AddCheck<GaugeFileHealthCheck>("gauge-file", tags: new[] { "ready" });

#if !DEBUG
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("basic", null)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    var allowedEmails = builder.Configuration
        .GetSection("Authentication:AllowedEmails")
        .Get<List<string>>()
        ?.Select(e => e.ToLowerInvariant())
        ?.ToHashSet() ?? new HashSet<string>();

    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            var email = context.Principal.FindFirst("preferred_username")?.Value
                        ?? context.Principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email) || !allowedEmails.Contains(email.ToLowerInvariant()))
            {
                context.Fail("Unauthorized access");
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = "basic",
                        Type = ReferenceType.SecurityScheme
                    }
                },
                Array.Empty<string>()
            }
    });
});
#else
builder.Services.AddSwaggerGen();
#endif


var app = builder.Build();
app.UseMiddleware<RequestLogMiddleware>();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
#if DEBUG
app.UseSwagger();
app.UseSwaggerUI();
#endif

app.UseStaticFiles();

app.UseRouting();

#if !DEBUG
app.UseAuthentication();
app.UseAuthorization();
#endif

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{value?}");
app.MapControllers();
app.Run();
