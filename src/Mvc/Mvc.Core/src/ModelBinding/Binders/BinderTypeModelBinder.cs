// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinder"/> for models which specify an <see cref="IModelBinder"/> using
    /// <see cref="BindingInfo.BinderType"/>.
    /// </summary>
    public class BinderTypeModelBinder : IModelBinder
    {
        private readonly ObjectFactory _factory;

        /// <summary>
        /// Creates a new <see cref="BinderTypeModelBinder"/>.
        /// </summary>
        /// <param name="binderType">The <see cref="Type"/> of the <see cref="IModelBinder"/>.</param>
        public BinderTypeModelBinder(Type binderType)
        {
            if (binderType == null)
            {
                throw new ArgumentNullException(nameof(binderType));
            }

            if (!typeof(IModelBinder).IsAssignableFrom(binderType))
            {
                throw new ArgumentException(
                    Resources.FormatBinderType_MustBeIModelBinder(
                        binderType.FullName,
                        typeof(IModelBinder).FullName),
                    nameof(binderType));
            }

            _factory = ActivatorUtilities.CreateFactory(binderType, Type.EmptyTypes);
        }

        /// <inheritdoc />
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var requestServices = bindingContext.HttpContext.RequestServices;
            var binder = (IModelBinder)_factory(requestServices, arguments: null);

            await binder.BindModelAsync(bindingContext);
        }
    }
}
