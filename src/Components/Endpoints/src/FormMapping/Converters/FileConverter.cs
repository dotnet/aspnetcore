// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class FileConverter<T>(HttpContext? httpContext) : FormDataConverter<T>, ISingleValueConverter
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal override bool TryRead(ref FormDataReader reader, Type type, FormDataMapperOptions options, out T? result, out bool found)
    {
        if (httpContext == null)
        {
            result = default;
            found = false;
            return true;
        }

        if (typeof(T) == typeof(IFormFileCollection))
        {
            result = (T)httpContext.Request.Form.Files;
            found = true;
            return true;
        }

        var formFileCollection = httpContext.Request.Form.Files;
        if (formFileCollection.Count == 0)
        {
            result = default;
            found = false;
            return true;
        }

        var file = formFileCollection.GetFile(reader.CurrentPrefix.ToString());
        if (file != null)
        {
            result = (T)file;
            found = true;
            return true;
        }

        result = default;
        found = false;
        return true;
    }
}
