// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="IValueProviderFactory"/> for <see cref="FormValueProvider"/>.
    /// </summary>
    public sealed class FormFileValueProviderFactory : IValueProviderFactory
    {
        /// <inheritdoc />
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.ActionContext.HttpContext.Request;
            if (request.HasFormContentType)
            {
                // Allocating a Task only when the body is multipart form.
                return AddValueProviderAsync(context, request);
            }

            return Task.CompletedTask;
        }

        private static async Task AddValueProviderAsync(ValueProviderFactoryContext context, HttpRequest request)
        {
            var formCollection = await request.ReadFormAsync();
            if (formCollection.Files.Count > 0)
            {
                var valueProvider = new FormFileValueProvider(formCollection.Files);
                context.ValueProviders.Add(valueProvider);
            }
        }
    }
}
