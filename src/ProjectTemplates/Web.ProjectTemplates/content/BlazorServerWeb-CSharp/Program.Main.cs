#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
#endif
#if (WindowsAuth)
using Microsoft.AspNetCore.Authentication.Negotiate;
#endif
#if (OrganizationalAuth)
#if (MultiOrgAuth)
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
#endif
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
#if (IndividualLocalAuth)
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
#endif
#if (OrganizationalAuth)
using Microsoft.AspNetCore.Mvc.Authorization;
#endif
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
#endif
#if (GenerateGraph)
using Graph = Microsoft.Graph;
#endif
#if(MultiOrgAuth)
using Microsoft.IdentityModel.Tokens;
#endif
#if (IndividualLocalAuth)
using BlazorServerWeb_CSharp.Areas.Identity;
#endif
using BlazorServerWeb_CSharp.Data;

namespace BlazorServerWeb_CSharp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        #if (IndividualLocalAuth)
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        #if (UseLocalDB)
            options.UseSqlServer(connectionString));
        #else
            options.UseSqlite(connectionString));
        #endif
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApplicationDbContext>();
        #elif (OrganizationalAuth)
        #if (GenerateApiOrGraph)
        var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ');

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
        var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ');

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

        #elif (WindowsAuth)
        builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();

        builder.Services.AddAuthorization(options =>
        {
            // By default, all incoming requests will be authorized according to the default policy.
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
        #if (IndividualLocalAuth)
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        #else
        if (!app.Environment.IsDevelopment())
        #endif
        {
            app.UseExceptionHandler("/Error");
        #if (HasHttpsProfile)
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        #else
        }

        #endif

        app.UseStaticFiles();

        app.UseRouting();

        #if (IndividualAuth)
        app.UseAuthorization();

        #endif
        #if (OrganizationalAuth || IndividualAuth)
        app.MapControllers();
        #endif
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}
