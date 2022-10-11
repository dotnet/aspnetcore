// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;

namespace StaticFilesAuth;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        Configuration = configuration;
        HostingEnvironment = hostingEnvironment;
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment HostingEnvironment { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();

        services.AddAuthorization(options =>
        {
            var basePath = Path.Combine(HostingEnvironment.ContentRootPath, "PrivateFiles");
            var usersPath = Path.Combine(basePath, "Users");

            // When using this policy users are only authorized to access the base directory, the Users directory,
            // and their own directory under Users.
            options.AddPolicy("files", builder =>
            {
                builder.RequireAuthenticatedUser().RequireAssertion(context =>
                {
                    var userName = context.User.Identity.Name;
                    userName = userName?.Split('@').FirstOrDefault();
                    if (userName == null)
                    {
                        return false;
                    }
                    if (context.Resource is HttpContext httpContext && httpContext.GetEndpoint() is Endpoint endpoint)
                    {
                        var userPath = Path.Combine(usersPath, userName);

                        var directory = endpoint.Metadata.GetMetadata<DirectoryInfo>();
                        if (directory != null)
                        {
                            return string.Equals(directory.FullName, basePath, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(directory.FullName, usersPath, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(directory.FullName, userPath, StringComparison.OrdinalIgnoreCase)
                                || directory.FullName.StartsWith(userPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                        }

                        throw new InvalidOperationException($"Missing file system metadata.");
                    }

                    throw new InvalidOperationException($"Unknown resource type '{context.Resource.GetType()}'");
                });
            });
        });

        services.AddMvc();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IAuthorizationService authorizationService)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        // Serve files from wwwroot without authentication or authorization.
        app.UseStaticFiles();

        app.UseAuthentication();

        var files = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "PrivateFiles"));

        app.Map("/MapAuthenticatedFiles", branch =>
        {
            branch.Use((context, next) => { SetFileEndpoint(context, files, null); return next(context); });
            branch.UseAuthorization();
            SetupFileServer(branch, files);
        });
        app.Map("/MapImperativeFiles", branch =>
        {
            branch.Use((context, next) => { SetFileEndpoint(context, files, "files"); return next(context); });
            branch.UseAuthorization();
            SetupFileServer(branch, files);
        });

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }

    private void SetupFileServer(IApplicationBuilder builder, IFileProvider files)
    {
        builder.UseFileServer(new FileServerOptions()
        {
            EnableDirectoryBrowsing = true,
            FileProvider = files
        });
    }

    private static void SetFileEndpoint(HttpContext context, PhysicalFileProvider files, string policy)
    {
        var fileSystemPath = GetFileSystemPath(files, context.Request.Path);
        if (fileSystemPath != null)
        {
            var metadata = new List<object>
            {
                new DirectoryInfo(Path.GetDirectoryName(fileSystemPath)),
                new AuthorizeAttribute(policy)
            };

            var endpoint = new Endpoint(
                requestDelegate: null,
                new EndpointMetadataCollection(metadata),
                context.Request.Path);

            context.SetEndpoint(endpoint);
        }
    }

    private static string GetFileSystemPath(PhysicalFileProvider files, string path)
    {
        var fileInfo = files.GetFileInfo(path);
        if (fileInfo.Exists)
        {
            return Path.Join(files.Root, path);
        }
        else
        {
            // https://github.com/aspnet/Home/issues/2537
            var dir = files.GetDirectoryContents(path);
            if (dir.Exists)
            {
                return Path.Join(files.Root, path);
            }
        }

        return null;
    }
}
