// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class JsonConverterCollectionExtensions
    {
        public static void RegisterRazorConverters(this JsonConverterCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            collection.Add(TagHelperDescriptorJsonConverter.Instance);
            collection.Add(RazorDiagnosticJsonConverter.Instance);
            collection.Add(RazorExtensionJsonConverter.Instance);
            collection.Add(RazorConfigurationJsonConverter.Instance);
            collection.Add(ProjectSnapshotJsonConverter.Instance);
            collection.Add(ProjectSnapshotHandleJsonConverter.Instance);
        }
    }
}
