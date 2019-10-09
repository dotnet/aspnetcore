// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Areas.Identity.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.UI
{
    internal class IdentityDefaultUIConfigureOptions<TUser> :
        IPostConfigureOptions<RazorPagesOptions>,
        IConfigureNamedOptions<CookieAuthenticationOptions> where TUser : class
    {
        private const string IdentityUIDefaultAreaName = "Identity";

        public IdentityDefaultUIConfigureOptions(
            IWebHostEnvironment environment)        {
            Environment = environment;
        }

        public IWebHostEnvironment Environment { get; }

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

        public void Configure(CookieAuthenticationOptions options) {
            // Nothing to do here as Configure(string name, CookieAuthenticationOptions options) is hte one setting things up.
        }

        public void Configure(string name, CookieAuthenticationOptions options)
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
