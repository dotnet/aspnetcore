using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
#endif
#if (OrganizationalAuth)
#if (MultiOrgAuth)
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
#endif
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
#if (IndividualLocalAuth)
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
#endif
using Microsoft.AspNetCore.Hosting;
#if (OrganizationalAuth)
using Microsoft.AspNetCore.Mvc.Authorization;
#endif
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if (GenerateGraph)
using Microsoft.Graph;
#endif
#if(MultiOrgAuth)
using Microsoft.IdentityModel.Tokens;
#endif
#if (IndividualLocalAuth)
using BlazorServerWeb_CSharp.Areas.Identity;
#endif
using BlazorServerWeb_CSharp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#if (IndividualLocalAuth)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
#if (UseLocalDB)
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
#else
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));
#endif
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
#elif (OrganizationalAuth)
#if (GenerateApiOrGraph)
var initialScopes = builder.Configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');

#endif
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
#if (GenerateApiOrGraph)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
#if (GenerateApi)
            .AddDownstreamWebApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
#endif
#if (GenerateGraph)
            .AddMicrosoftGraph(builder.Configuration.GetSection("DownstreamApi"))
#endif
            .AddInMemoryTokenCaches();
#else
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
#endif
#elif (IndividualB2CAuth)
#if (GenerateApi)
var initialScopes = builder.Configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');

#endif
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
#if (GenerateApi)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C"))
        .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
            .AddDownstreamWebApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
            .AddInMemoryTokenCaches();
#else
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C"));
#endif
#endif
#if (OrganizationalAuth || IndividualB2CAuth)
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});

#endif
builder.Services.AddRazorPages();
#if (OrganizationalAuth || IndividualB2CAuth)
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();
#else
builder.Services.AddServerSideBlazor();
#endif
#if (IndividualLocalAuth)
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
#endif
builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
#if (IndividualLocalAuth)
    app.UseMigrationsEndPoint();
#endif
}
else
{
    app.UseExceptionHandler("/Error");
#if (RequiresHttps)
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
#else
}

#endif

app.UseStaticFiles();

#if (OrganizationalAuth || IndividualAuth)
app.UseAuthentication();
app.UseAuthorization();

#endif

#if (OrganizationalAuth || IndividualAuth)
app.MapControllers();
#endif
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
