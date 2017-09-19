// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Text
{
    internal static class TextBufferExtensions
    {
        public static bool IsRazorBuffer(this ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            return textBuffer.ContentType.IsOfType(RazorLanguage.ContentType);
        }
    }
}
