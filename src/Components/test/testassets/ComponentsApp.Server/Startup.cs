// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ComponentsApp.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorComponents<App.Startup>();
            services.AddSingleton<WeatherForecastService, DefaultWeatherForecastService>();
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddRouting(options => options.ConstraintMap.Add("clientRoute", typeof(ClientRouteConstraint)));

            services.AddScoped<RemoteUriHelper>();
            services.AddScoped<ContextBasedUriHelper>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IUriHelper>(sp =>
            {
                var contextBased = sp.GetRequiredService<ContextBasedUriHelper>();
                var remoteBased = sp.GetRequiredService<RemoteUriHelper>();

                if (contextBased.CanHandle())
                {
                    return contextBased;
                }
                else
                {
                    return remoteBased;
                }
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRazorComponents<App.Startup>();
            app.UseMvc();
        }
    }

    internal class ClientRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (!(httpContext.Request.Path.Equals("/counter", StringComparison.OrdinalIgnoreCase) ||
                httpContext.Request.Path.Equals("/fetch-data", StringComparison.OrdinalIgnoreCase) ||
                httpContext.Request.Path.Equals("/index", StringComparison.OrdinalIgnoreCase) ||
                httpContext.Request.Path.Equals("/", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }
    }

    internal class ContextBasedUriHelper : UriHelperBase
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private HttpContext _context = null;
        public ContextBasedUriHelper(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public HttpContext Context
        {
            get
            {
                if (_context == null)
                {
                    _context = _contextAccessor.HttpContext;
                }
                if (_context == null)
                {
                    throw new InvalidOperationException("Couldn't retrieve the HttpContext");
                }

                return _context;
            }
        }

        protected override void InitializeState()
        {
            SetAbsoluteUri(GetAbsoluteUriFromContext());
            SetAbsoluteBaseUri(GetBaseUriFromContext());
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            SetAbsoluteUri(uri);
        }

        internal bool CanHandle() => (_context != null || _contextAccessor.HttpContext != null) &&
            _contextAccessor.HttpContext?.WebSockets?.IsWebSocketRequest == false;

        private string GetAbsoluteUriFromContext()
        {
            var request = Context.Request;
            var absoluteUri = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
            return absoluteUri;
        }

        private string GetBaseUriFromContext()
        {
            var request = Context.Request;
            return $"{request.Scheme}://{request.Host}{request.PathBase}/";
        }
    }
}
