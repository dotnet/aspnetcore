using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
                        if (context.Resource is Endpoint endpoint)
                        {
                            var userPath = Path.Combine(usersPath, userName);

                            var file = endpoint.Metadata.GetMetadata<IFileInfo>();
                            if (file != null)
                            {
                                var path = Path.GetDirectoryName(file.PhysicalPath);
                                return string.Equals(path, basePath, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(path, usersPath, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(path, userPath, StringComparison.OrdinalIgnoreCase)
                                    || path.StartsWith(userPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                            }

                            var dir = endpoint.Metadata.GetMetadata<IDirectoryContents>();
                            if (dir != null)
                            {
                                // https://github.com/aspnet/Home/issues/3073
                                // This won't work right if the directory is empty
                                var path = Path.GetDirectoryName(dir.First().PhysicalPath);
                                return string.Equals(path, basePath, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(path, usersPath, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(path, userPath, StringComparison.OrdinalIgnoreCase)
                                    || path.StartsWith(userPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                            }

                            throw new InvalidOperationException($"Missing file system metadata.");
                        }

                        throw new InvalidOperationException($"Unknown resource type '{context.Resource.GetType()}'");
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

            app.Use(async (context, next) =>
            {
                // Set an endpoint for files inside PrivateFiles to run authorization
                PathString remaining;
                if (context.Request.Path.StartsWithSegments("/MapImperativeFiles", out remaining))
                {
                    SetFileEndpoint(context, files, remaining, "files");
                }
                else if (context.Request.Path.StartsWithSegments("/MapAuthenticatedFiles", out remaining))
                {
                    SetFileEndpoint(context, files, remaining, null);
                }

                await next();
            });

            app.UseAuthorization();

            app.Map("/MapAuthenticatedFiles", branch => SetupFileServer(branch, files));
            app.Map("/MapImperativeFiles", branch => SetupFileServer(branch, files));

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
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

        private static void SetFileEndpoint(HttpContext context, PhysicalFileProvider files, PathString filePath, string policy)
        {
            var fileSystemInfo = GetFileSystemInfo(files, filePath);
            if (fileSystemInfo != null)
            {
                var metadata = new List<object>();
                metadata.Add(fileSystemInfo);
                metadata.Add(new AuthorizeAttribute(policy));

                var endpoint = new Endpoint(
                    c => Task.CompletedTask,
                    new EndpointMetadataCollection(metadata),
                    context.Request.Path);

                context.Features.Set<IEndpointFeature>(new EndpointFeature(endpoint));
            }
        }

        private static object GetFileSystemInfo(PhysicalFileProvider files, string path)
        {
            var fileInfo = files.GetFileInfo(path);
            if (fileInfo.Exists)
            {
                return fileInfo;
            }
            else
            {
                // https://github.com/aspnet/Home/issues/2537
                var dir = files.GetDirectoryContents(path);
                if (dir.Exists)
                {
                    return dir;
                }
            }

            return null;
        }

        private class EndpointFeature : IEndpointFeature
        {
            public Endpoint Endpoint { get; set; }

            public EndpointFeature(Endpoint endpoint)
            {
                Endpoint = endpoint;
            }
        }
    }
}
