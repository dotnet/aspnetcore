// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
#if COMPONENTS
using Microsoft.AspNetCore.Components.Forms;
#endif
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class FileConverter<T> : FormDataConverter<T>
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal override bool TryRead(ref FormDataReader reader, Type type, FormDataMapperOptions options, out T? result, out bool found)
    {
        if (reader.FormFileCollection == null)
        {
            result = default;
            found = false;
            return true;
        }

#if COMPONENTS
        if (typeof(T) == typeof(IBrowserFile))
        {
            var targetFile = reader.FormFileCollection.GetFile(reader.CurrentPrefix.ToString());
            if (targetFile != null)
            {
                var browserFile = new BrowserFileFromFormFile(targetFile);
                result = (T)(IBrowserFile)browserFile;
                found = true;
                return true;
            }
        }

        if (typeof(T) == typeof(IReadOnlyList<IBrowserFile>))
        {
            var targetFiles = reader.FormFileCollection.GetFiles(reader.CurrentPrefix.ToString());
            var buffer = ReadOnlyCollectionBufferAdapter<IBrowserFile>.CreateBuffer();
            for (var i = 0; i < targetFiles.Count; i++)
            {
                buffer = ReadOnlyCollectionBufferAdapter<IBrowserFile>.Add(ref buffer, new BrowserFileFromFormFile(targetFiles[i]));
            }
            result = (T)(IReadOnlyList<IBrowserFile>)ReadOnlyCollectionBufferAdapter<IBrowserFile>.ToResult(buffer);
            found = true;
            return true;
        }
#endif

        if (typeof(T) == typeof(IReadOnlyList<IFormFile>))
        {
            result = (T)reader.FormFileCollection.GetFiles(reader.CurrentPrefix.ToString());
            found = true;
            return true;
        }

        if (typeof(T) == typeof(IFormFileCollection))
        {
            result = (T)reader.FormFileCollection;
            found = true;
            return true;
        }

        var formFileCollection = reader.FormFileCollection;
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
