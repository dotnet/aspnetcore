// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request services when a model 
    /// has the binding source <see cref="BindingSource.Services"/>/
    /// </summary>
    public class ServicesModelBinder : BindingSourceModelBinder
    {
        /// <summary>
        /// Creates a new <see cref="ServicesModelBinder"/>.
        /// </summary>
        public ServicesModelBinder()
            : base(BindingSource.Services)
        {
        }

        /// <inheritdoc />
        protected override Task BindModelCoreAsync([NotNull] ModelBindingContext bindingContext)
        {
            var requestServices = bindingContext.OperationBindingContext.HttpContext.RequestServices;
            bindingContext.Model = requestServices.GetRequiredService(bindingContext.ModelType);
            return Task.FromResult(true);
        }
    }
}
