// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A <see cref="IValueProviderFactory"/> for <see cref="FormValueProvider"/>.
/// </summary>
public class FormValueProviderFactory : IValueProviderFactory
{
    /// <inheritdoc />
    public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.ActionContext.HttpContext.Request;
        if (request.HasFormContentType)
        {
            RegisterValueProvider(context);

            // Allocating a Task only when the body is form data.
            //return AddValueProviderAsync(context);
        }

        return Task.CompletedTask;
    }

    private static void RegisterValueProvider(ValueProviderFactoryContext context)
    {
        var valueProvider = new FormValueProvider(
            BindingSource.Form,
            new FormCollection(fields: null, files: null),
            CultureInfo.CurrentCulture);

        context.ValueProviders.Add(valueProvider);
    }

#pragma warning disable IDE0051 // Remove unused private members
    private static async Task AddValueProviderAsync(ValueProviderFactoryContext context)
#pragma warning restore IDE0051 // Remove unused private members
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
