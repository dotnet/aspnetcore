// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which understands <see cref="IServiceActivatorBinderMetadata"/>
    /// and activates a given model using <see cref="IServiceProvider"/>.
    /// </summary>
    public class ServicesModelBinder : MetadataAwareBinder<IServiceActivatorBinderMetadata>
    {
        /// <inheritdoc />
        protected override Task<bool> BindAsync(
            [NotNull] ModelBindingContext bindingContext,
            [NotNull] IServiceActivatorBinderMetadata metadata)
        {
            var requestServices = bindingContext.OperationBindingContext.HttpContext.RequestServices;
            bindingContext.Model = requestServices.GetRequiredService(bindingContext.ModelType);
            return Task.FromResult(true);
        }
    }
}
