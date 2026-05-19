#if (WindowsAuth)
using Microsoft.AspNetCore.Authentication.Negotiate;

#endif
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#if (EnableOpenAPI)
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
#endif
#if (WindowsAuth)
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});
#endif

var app = builder.Build();

// Configure the HTTP request pipeline.
#if (EnableOpenAPI)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
#endif
#if (HasHttpsProfile)

app.UseHttpsRedirection();
#endif

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
#if (EnableOpenAPI)
})
.WithName("GetWeatherForecast");
#else
});
#endif

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
