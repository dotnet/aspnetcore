using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if (GenerateGraph)
using Microsoft.Graph;
#endif
#if (EnableOpenAPI)
using Microsoft.OpenApi.Models;
#endif

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

builder.Services.AddControllers();
#if (EnableOpenAPI)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Company.WebApplication1", Version = "v1" });
});
#endif

var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    #if (EnableOpenAPI)
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Company.WebApplication1 v1"));
    #endif
}
#if (RequiresHttps)

app.UseHttpsRedirection();
#endif

#if (OrganizationalAuth || IndividualAuth)
app.UseAuthentication();
#endif
app.UseAuthorization();

app.MapControllers();

app.Run();
