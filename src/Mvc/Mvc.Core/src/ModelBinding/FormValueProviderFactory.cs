// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="IValueProviderFactory"/> for <see cref="FormValueProvider"/>.
    /// </summary>
    public class FormValueProviderFactory : IValueProviderFactory
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
            IFormCollection form;

            try
            {
                form = await request.ReadFormAsync();
            }
            catch (InvalidDataException ex)
            {
                // ReadFormAsync can throw InvalidDataException if the form content is malformed.
                // Wrap it in a ValueProviderException that the CompositeValueProvider special cases.
                throw new ValueProviderException(Resources.FormatFailedToReadRequestForm(ex.Message), ex);
            }
            catch (IOException ex)
            {
                // ReadFormAsync can throw IOException if the client disconnects.
                // Wrap it in a ValueProviderException that the CompositeValueProvider special cases.
                throw new ValueProviderException(Resources.FormatFailedToReadRequestForm(ex.Message), ex);
            }

            var valueProvider = new FormValueProvider(
                BindingSource.Form,
                form,
                CultureInfo.CurrentCulture);

            context.ValueProviders.Add(valueProvider);
        }
    }
}
