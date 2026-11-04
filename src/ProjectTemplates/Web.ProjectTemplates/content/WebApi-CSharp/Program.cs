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
#endif
#if (OrganizationalAuth || IndividualB2CAuth || GenerateGraph || WindowsAuth)

#endif
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#if (OrganizationalAuth)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
#if (GenerateApiOrGraph)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi()
#if (GenerateApi)
            .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
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
            .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
            .AddInMemoryTokenCaches();
#else
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));
#endif
#endif

builder.Services.AddControllers();
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

app.UseAuthorization();

app.MapControllers();

app.Run();
