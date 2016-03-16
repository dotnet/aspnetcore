// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    /// <summary>
    /// <see cref="IModelBinderProvider"/> implementation to bind models of type <see cref="HttpRequestMessage"/>.
    /// </summary>
    public class HttpRequestMessageModelBinderProvider : IModelBinderProvider
    {
        /// <inheritdoc />
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(HttpRequestMessage))
            {
                return new HttpRequestMessageModelBinder();
            }

            return null;
        }
    }
}
