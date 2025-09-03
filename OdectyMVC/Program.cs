using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using OdectyMVC.Application;
using OdectyMVC.Contracts;
using OdectyMVC.DataLayer;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQSettings"));
builder.Services.AddScoped<IGaugeService, GaugeService>();
builder.Services.AddScoped<IGaugeContext, GaugeContext>();
builder.Services.AddScoped<GaugeDbContext>();

builder.Services.AddScoped<IGaugeRepository, GaugeRepository>();
builder.Services.AddScoped<IGaugeListModelRepository, GaugeListModelRepository>();
builder.Services.AddSingleton<IMessageQueue, MessageQueue>();
builder.Services.AddSingleton<RabbitMQProvider>();
builder.Services.AddHostedService<IncomeMessageBackgroundService>();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All
});
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{value?}");

app.Run();
