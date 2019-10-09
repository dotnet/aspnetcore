// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// <see cref="IPageActivatorProvider"/> that uses type activation to create Razor Page instances.
    /// </summary>
    public class ServiceBasedPageModelActivatorProvider : IPageModelActivatorProvider
    {
        public Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var modelType = descriptor.ModelTypeInfo?.AsType();
            if (modelType == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(descriptor.ModelTypeInfo),
                    nameof(descriptor)),
                    nameof(descriptor));
            }

            return context =>
            {
                return context.HttpContext.RequestServices.GetRequiredService(modelType);
            };
        }

        public Action<PageContext, object> CreateReleaser(CompiledPageActionDescriptor descriptor)
        {
            return null;
        }
    }
}

