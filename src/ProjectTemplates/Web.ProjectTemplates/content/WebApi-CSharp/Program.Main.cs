#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
#endif
#if (WindowsAuth)
using Microsoft.AspNetCore.Authentication.Negotiate;
#endif
#if (GenerateGraph)
using Graph = Microsoft.Graph;
#endif
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
#endif
#if (OrganizationalAuth || IndividualB2CAuth || GenerateGraph || WindowsAuth || EnableOpenAPI)

#endif
namespace Company.WebApplication1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        #if (OrganizationalAuth)
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        #if (GenerateApiOrGraph)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi()
        #if (GenerateApi)
                    .AddDownstreamWebApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
        #endif
        #if (GenerateGraph)
                    .AddMicrosoftGraph(builder.Configuration.GetSection("DownstreamApi"))
        #endif
                    .AddInMemoryTokenCaches();
        #else
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
        #endif
        #elif (IndividualB2CAuth)
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        #if (GenerateApi)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"))
                .EnableTokenAcquisitionToCallDownstreamApi()
                    .AddDownstreamWebApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
                    .AddInMemoryTokenCaches();
        #else
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));
        #endif
        #endif
        #if (UseMinimalAPIs)
        builder.Services.AddAuthorization();
        #endif

        #if (UseControllers)
        builder.Services.AddControllers();
        #endif
        #if (EnableOpenAPI)
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
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
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        #endif
        #if (HasHttpsProfile)

        app.UseHttpsRedirection();
        #endif

        app.UseAuthorization();

        #if (UseMinimalAPIs)
        #if (OrganizationalAuth || IndividualB2CAuth)
        var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? "";
        #endif
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        #if (GenerateApi)
        app.MapGet("/weatherforecast", async (HttpContext httpContext, IDownstreamWebApi downstreamWebApi) =>
        {
            httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

            using var response = await downstreamWebApi.CallWebApiForUserAsync("DownstreamApi").ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var apiResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                // Do something
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
            }

            var forecast =  Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                })
                .ToArray();

            return forecast;
        #elif (GenerateGraph)
        app.MapGet("/weatherforecast", async (HttpContext httpContext, Graph.GraphServiceClient graphServiceClient) =>
        {
            httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

            var user = await graphServiceClient.Me.Request().GetAsync();

            var forecast =  Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                })
                .ToArray();

            return forecast;
        #else
        app.MapGet("/weatherforecast", (HttpContext httpContext) =>
        {
            #if (OrganizationalAuth || IndividualB2CAuth)
            httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

            #endif
            var forecast =  Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                })
                .ToArray();
            return forecast;
        #endif
        #if (EnableOpenAPI && !NoAuth)
        })
        .WithName("GetWeatherForecast")
        .WithOpenApi()
        .RequireAuthorization();
        #elif (EnableOpenAPI && NoAuth)
        })
        .WithName("GetWeatherForecast")
        .WithOpenApi();
        #elif (!EnableOpenAPI && !NoAuth)
        })
        .RequireAuthorization();
        #else
        });
        #endif
        #endif
        #if (UseControllers)

        app.MapControllers();
        #endif

        app.Run();
    }
}
