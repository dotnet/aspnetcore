// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Serialization
{
    internal class SerializedRazorExtension : RazorExtension
    {
        public SerializedRazorExtension(string extensionName)
        {
            if (extensionName == null)
            {
                throw new ArgumentNullException(nameof(extensionName));
            }

            ExtensionName = extensionName;
        }

        public override string ExtensionName { get; }
    }
}
