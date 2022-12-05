using System.Globalization;
using BlazorUnitedApp.Data;
using BlazorUnitedApp.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddRazorComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapRazorComponents();

app.MapPost("/counter", (HttpContext httpContext) =>
{
    var count = int.Parse(httpContext.Request.Form["currentCount"]!, CultureInfo.InvariantCulture);
    return new RazorComponentResult(typeof(Counter)).WithParameter(nameof(Counter.CurrentCount), count + 1);
});

app.Run();
