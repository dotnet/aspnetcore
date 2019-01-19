// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Areas.Identity.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.UI
{
    internal class IdentityDefaultUIConfigureOptions<TUser> :
        IPostConfigureOptions<RazorPagesOptions>,
        IPostConfigureOptions<StaticFileOptions>,
        IPostConfigureOptions<CookieAuthenticationOptions> where TUser : class
    {
        private const string IdentityUIDefaultAreaName = "Identity";

        public IdentityDefaultUIConfigureOptions(
            IHostingEnvironment environment,
            IOptions<DefaultUIOptions> uiOptions)
        {
            Environment = environment;
            UiOptions = uiOptions;
        }

        public IHostingEnvironment Environment { get; }
        public IOptions<DefaultUIOptions> UiOptions { get; }

        public void PostConfigure(string name, RazorPagesOptions options)
        {
            name = name ?? throw new ArgumentNullException(nameof(name));
            options = options ?? throw new ArgumentNullException(nameof(options));

            options.Conventions.AuthorizeAreaFolder(IdentityUIDefaultAreaName, "/Account/Manage");
            options.Conventions.AuthorizeAreaPage(IdentityUIDefaultAreaName, "/Account/Logout");
            var convention = new IdentityPageModelConvention<TUser>();
            options.Conventions.AddAreaFolderApplicationModelConvention(
                IdentityUIDefaultAreaName,
                "/",
                pam => convention.Apply(pam));
            options.Conventions.AddAreaFolderApplicationModelConvention(
                IdentityUIDefaultAreaName,
                "/Account/Manage",
                pam => pam.Filters.Add(new ExternalLoginsPageFilter<TUser>()));
        }

        public void PostConfigure(string name, StaticFileOptions options)
        {
            name = name ?? throw new ArgumentNullException(nameof(name));
            options = options ?? throw new ArgumentNullException(nameof(options));

            // Basic initialization in case the options weren't initialized by any other component
            options.ContentTypeProvider = options.ContentTypeProvider ?? new FileExtensionContentTypeProvider();
            if (options.FileProvider == null && Environment.WebRootFileProvider == null)
            {
                throw new InvalidOperationException("Missing FileProvider.");
            }

            options.FileProvider = options.FileProvider ?? Environment.WebRootFileProvider;

            var basePath = UiOptions.Value.UIFramework == UIFramework.Bootstrap3 ? "wwwroot/V3" :
                "wwwroot/V4";

            // Add our provider
            var filesProvider = new ManifestEmbeddedFileProvider(GetType().Assembly, basePath);
            options.FileProvider = new CompositeFileProvider(options.FileProvider, filesProvider);
        }

        public void PostConfigure(string name, CookieAuthenticationOptions options)
        {
            name = name ?? throw new ArgumentNullException(nameof(name));
            options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.Equals(IdentityConstants.ApplicationScheme, name, StringComparison.Ordinal))
            {
                options.LoginPath = $"/{IdentityUIDefaultAreaName}/Account/Login";
                options.LogoutPath = $"/{IdentityUIDefaultAreaName}/Account/Logout";
                options.AccessDeniedPath = $"/{IdentityUIDefaultAreaName}/Account/AccessDenied";
            }
        }
    }
}
