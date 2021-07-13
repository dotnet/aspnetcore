#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
#endif
#if (IndividualLocalAuth)
using Microsoft.AspNetCore.Identity;
#endif
#if (OrganizationalAuth)
using Microsoft.AspNetCore.Mvc.Authorization;
#endif
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
#endif
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
#endif
#if (MultiOrgAuth)
using Microsoft.IdentityModel.Tokens;
#endif
#if (GenerateGraph)
using Graph = Microsoft.Graph;
#endif
#if (IndividualLocalAuth)
using Company.WebApplication1.Data;
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
#if (OrganizationalAuth)

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();
#elif (IndividualB2CAuth)
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();
#else
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

app.UseRouting();

#if (OrganizationalAuth || IndividualAuth)
app.UseAuthentication();
#endif
app.UseAuthorization();

app.MapRazorPages();
#if (IndividualB2CAuth || OrganizationalAuth)
app.MapControllers();
#endif

app.Run();
