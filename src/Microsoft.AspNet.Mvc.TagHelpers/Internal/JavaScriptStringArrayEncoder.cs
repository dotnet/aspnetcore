// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Methods for encoding <c>string[]</c>s for use as a JavaScript array literal.
    /// </summary>
    public static class JavaScriptStringArrayEncoder
    {
        /// <summary>
        /// Encodes a <c>string[]</c> for safe use as a JavaScript array literal in many contexts, including
        /// inline in an HTML file.
        /// </summary>
        public static string Encode(JavaScriptEncoder encoder, string[] values)
        {
            var writer = new StringWriter();
            writer.Write('[');

            // Perf: Avoid allocating enumerator
            var firstAdded = false;
            for (var i = 0; i < values.Length; i++)
            {
                if (firstAdded)
                {
                    writer.Write(',');
                }

                writer.Write('"');
                encoder.Encode(writer, values[i]);
                writer.Write('"');
                firstAdded = true;
            }

            writer.Write(']');

            return writer.ToString();
        }
    }
}