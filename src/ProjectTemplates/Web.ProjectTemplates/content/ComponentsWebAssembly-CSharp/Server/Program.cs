using System.Linq;
#if (OrganizationalAuth || IndividualB2CAuth || IndividualLocalAuth)
using Microsoft.AspNetCore.Authentication;
#endif
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication.JwtBearer;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if (GenerateGraph)
using Microsoft.Graph;
#endif
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.Identity.Web;
#endif
#if (IndividualLocalAuth)
using ComponentsWebAssembly_CSharp.Server.Data;
using ComponentsWebAssembly_CSharp.Server.Models;
#endif

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

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

builder.Services.AddAuthentication()
    .AddIdentityServerJwt();
#endif
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

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
#if (IndividualLocalAuth)
    app.UseMigrationsEndPoint();
#endif
    app.UseWebAssemblyDebugging();
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

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

#if (IndividualLocalAuth)
app.UseIdentityServer();
#endif
#if (OrganizationalAuth || IndividualAuth)
app.UseAuthentication();
#endif
#if (!NoAuth)
app.UseAuthorization();

#endif

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
