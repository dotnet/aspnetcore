using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace StaticFilesAuth
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }

        public IHostingEnvironment HostingEnvironment { get; }

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
                        var userPath = Path.Combine(usersPath, userName);
                        if (context.Resource is IFileInfo file)
                        {
                            var path = Path.GetDirectoryName(file.PhysicalPath);
                            return string.Equals(path, basePath, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(path, usersPath, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(path, userPath, StringComparison.OrdinalIgnoreCase)
                                || path.StartsWith(userPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                        }
                        else if (context.Resource is IDirectoryContents dir)
                        {
                            // https://github.com/aspnet/Home/issues/3073
                            // This won't work right if the directory is empty
                            var path = Path.GetDirectoryName(dir.First().PhysicalPath);
                            return string.Equals(path, basePath, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(path, usersPath, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(path, userPath, StringComparison.OrdinalIgnoreCase)
                                || path.StartsWith(userPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                        }

                        throw new NotImplementedException($"Unknown resource type '{context.Resource.GetType()}'");
                    });
                });
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IAuthorizationService authorizationService)
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
                MapAuthenticatedFiles(branch, files);
            });

            app.Map("/MapImperativeFiles", branch =>
            {
                MapImperativeFiles(authorizationService, branch, files);
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        // Blanket authorization, any authenticated user is allowed access to these resources.
        private static void MapAuthenticatedFiles(IApplicationBuilder branch, PhysicalFileProvider files)
        {
            branch.Use(async (context, next) =>
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    await context.ChallengeAsync(new AuthenticationProperties()
                    {
                        // https://github.com/aspnet/Security/issues/1730
                        // Return here after authenticating
                        RedirectUri = context.Request.PathBase + context.Request.Path + context.Request.QueryString
                    });
                    return;
                }

                await next();
            });
            branch.UseFileServer(new FileServerOptions()
            {
                EnableDirectoryBrowsing = true,
                FileProvider = files
            });
        }

        // Policy based authorization, requests must meet the policy criteria to be get access to the resources.
        private static void MapImperativeFiles(IAuthorizationService authorizationService, IApplicationBuilder branch, PhysicalFileProvider files)
        {
            branch.Use(async (context, next) =>
            {
                var fileInfo = files.GetFileInfo(context.Request.Path);
                AuthorizationResult result = null;
                if (fileInfo.Exists)
                {
                    result = await authorizationService.AuthorizeAsync(context.User, fileInfo, "files");
                }
                else
                {
                    // https://github.com/aspnet/Home/issues/2537
                    var dir = files.GetDirectoryContents(context.Request.Path);
                    if (dir.Exists)
                    {
                        result = await authorizationService.AuthorizeAsync(context.User, dir, "files");
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                }

                if (!result.Succeeded)
                {
                    if (!context.User.Identity.IsAuthenticated)
                    {
                        await context.ChallengeAsync(new AuthenticationProperties()
                        {
                            // https://github.com/aspnet/Security/issues/1730
                            // Return here after authenticating
                            RedirectUri = context.Request.PathBase + context.Request.Path + context.Request.QueryString
                        });
                        return;
                    }
                    // Authenticated but not authorized
                    await context.ForbidAsync();
                    return;
                }

                await next();
            });
            branch.UseFileServer(new FileServerOptions()
            {
                EnableDirectoryBrowsing = true,
                FileProvider = files
            });
        }
    }
}
