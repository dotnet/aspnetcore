// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Sets up options for <see cref="MvcJsonOptions"/>.
    /// </summary>
    internal class MvcJsonOptionsSetup : IConfigureOptions<MvcNewtonsoftJsonOptions>
    {
        private readonly MvcJsonOptions _jsonOptions;

        public MvcJsonOptionsSetup(IOptions<MvcJsonOptions> jsonOptions)
        {
            if (jsonOptions == null)
            {
                throw new ArgumentNullException(nameof(jsonOptions));
            }

            _jsonOptions = jsonOptions.Value;
        }

        public void Configure(MvcNewtonsoftJsonOptions options)
        {
            // Allow MvcJsonOptions to proxy MvcNewtonsoftJsonOptions for back-compat.
            // See https://github.com/aspnet/AspNetCore/issues/8254
            _jsonOptions.Proxy = options;
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
