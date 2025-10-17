using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using OdectyMVC;
using OdectyMVC.Application;
using OdectyMVC.Contracts;
using OdectyMVC.DataLayer;
using OdectyMVC.Middleware;
using OdectyMVC.Options;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
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
builder.Logging.AddEventLog(conf => conf.SourceName = "OdectyMVC");

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{value?}");
app.MapControllers();
app.Run();
