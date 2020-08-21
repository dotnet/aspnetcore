// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IValueProviderFactory"/> for <see cref="JQueryFormValueProvider"/>.
    /// </summary>
    public class JQueryFormValueProviderFactory : IValueProviderFactory
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
                // Allocating a Task only when the body is form data.
                return AddValueProviderAsync(context);
            }

            return Task.CompletedTask;
        }

        private static async Task AddValueProviderAsync(ValueProviderFactoryContext context)
        {
            var request = context.ActionContext.HttpContext.Request;

            IFormCollection formCollection;
            try
            {
                formCollection = await request.ReadFormAsync();
            }
            catch (InvalidDataException ex)
            {
                throw new ValueProviderException(Resources.FormatFailedToReadRequestForm(ex.Message), ex);
            }
            catch (IOException ex)
            {
                throw new ValueProviderException(Resources.FormatFailedToReadRequestForm(ex.Message), ex);
            }

            var valueProvider = new JQueryFormValueProvider(
                BindingSource.Form,
                JQueryKeyValuePairNormalizer.GetValues(formCollection, formCollection.Count),
                CultureInfo.CurrentCulture);

            context.ValueProviders.Add(valueProvider);
        }
    }
}
