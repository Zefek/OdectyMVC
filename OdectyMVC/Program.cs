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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{value?}");

app.Run();
