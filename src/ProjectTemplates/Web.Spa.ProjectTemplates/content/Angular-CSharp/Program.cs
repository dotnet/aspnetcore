using System;
#if (IndividualLocalAuth)
using Microsoft.AspNetCore.Authentication;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#if (IndividualLocalAuth)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
#endif
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if (IndividualLocalAuth)
using Company.WebApplication1.Data;
using Company.WebApplication1.Models;
#endif

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#if (IndividualLocalAuth)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
#if (UseLocalDB)
    options.UseSqlServer(connectionString));
#else
    options.UseSqlite(connectionString));
#endif
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

builder.Services.AddAuthentication()
    .AddIdentityServerJwt();
#endif

builder.Services.AddControllersWithViews();
#if (IndividualLocalAuth)
builder.Services.AddRazorPages();
#endif

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
#if (RequiresHttps)
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
#else
}

#endif
app.UseStaticFiles();
app.UseRouting();

#if (IndividualLocalAuth)
app.UseAuthentication();
app.UseIdentityServer();
#endif
#if (!NoAuth)
app.UseAuthorization();
#endif

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");
#if (IndividualLocalAuth)
app.MapRazorPages();
#endif

app.MapFallbackToFile("index.html");;

app.Run();
