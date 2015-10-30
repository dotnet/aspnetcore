// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Methods for encoding <see cref="IEnumerable{string}"/> for use as a JavaScript array literal.
    /// </summary>
    public static class JavaScriptStringArrayEncoder
    {
        /// <summary>
        /// Encodes a .NET string array for safe use as a JavaScript array literal, including inline in an HTML file.
        /// </summary>
        public static string Encode(JavaScriptEncoder encoder, IEnumerable<string> values)
        {
            var writer = new StringWriter();

            var firstAdded = false;

            writer.Write('[');

            foreach (var value in values)
            {
                if (firstAdded)
                {
                    writer.Write(',');
                }
                writer.Write('"');
                encoder.Encode(writer, value);
                writer.Write('"');
                firstAdded = true;
            }

            writer.Write(']');

            return writer.ToString();
        }
    }
}