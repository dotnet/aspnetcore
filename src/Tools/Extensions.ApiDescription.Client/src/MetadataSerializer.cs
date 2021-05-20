// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Extensions.ApiDescription.Client
{
    /// <summary>
    /// Utility methods to serialize and deserialize <see cref="ITaskItem"/> metadata.
    /// </summary>
    /// <remarks>
    /// Based on and uses the same escaping as
    /// https://github.com/Microsoft/msbuild/blob/e70a3159d64f9ed6ec3b60253ef863fa883a99b1/src/Shared/EscapingUtilities.cs
    /// </remarks>
    public static class MetadataSerializer
    {
        private static readonly char[] CharsToEscape = { '%', '*', '?', '@', '$', '(', ')', ';', '\'' };
        private static readonly HashSet<char> CharsToEscapeHash = new HashSet<char>(CharsToEscape);

        /// <summary>
        /// Add the given <paramref name="key"/> and <paramref name="value"/> to the <paramref name="item"/>. Or,
        /// modify existing value to be <paramref name="value"/>.
        /// </summary>
        /// <param name="item">The <see cref="ITaskItem"/> to update.</param>
        /// <param name="key">The name of the new metadata.</param>
        /// <param name="value">The value of the new metadata. Assumed to be unescaped.</param>
        /// <remarks>Uses same hex-encoded format as MSBuild's EscapeUtilities.</remarks>
        public static void SetMetadata(ITaskItem item, string key, string value)
        {
            if (item is ITaskItem2 item2)
            {
                item2.SetMetadataValueLiteral(key, value);
                return;
            }

            if (value.IndexOfAny(CharsToEscape) == -1)
            {
                item.SetMetadata(key, value);
                return;
            }

            var builder = new StringBuilder();
            EscapeValue(value, builder);
            item.SetMetadata(key, builder.ToString());
        }

        /// <summary>
        /// Serialize metadata for use as a property value passed into an inner build.
        /// </summary>
        /// <param name="item">The item to serialize.</param>
        /// <returns>A <see cref="string"/> containing the serialized metadata.</returns>
        /// <remarks>Uses same hex-encoded format as MSBuild's EscapeUtilities.</remarks>
        public static string SerializeMetadata(ITaskItem item)
        {
            var builder = new StringBuilder();
            if (item is ITaskItem2 item2)
            {
                builder.Append($"Identity={item2.EvaluatedIncludeEscaped}");
                var metadata = item2.CloneCustomMetadataEscaped();
                foreach (var key in metadata.Keys)
                {
                    var value = metadata[key];
                    builder.Append($"|{key.ToString()}={value.ToString()}");
                }
            }
            else
            {
                builder.Append($"Identity=");
                EscapeValue(item.ItemSpec, builder);

                var metadata = item.CloneCustomMetadata();
                foreach (var key in metadata.Keys)
                {
                    builder.Append($"|{key.ToString()}=");

                    var value = metadata[key];
                    EscapeValue(value.ToString(), builder);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Recreate an <see cref="ITaskItem"/> with metadata encoded in given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The serialized metadata.</param>
        /// <returns>The deserialized <see cref="ITaskItem"/>.</returns>
        public static ITaskItem DeserializeMetadata(string value)
        {
            var metadata = value.Split('|');
            var item = new TaskItem();

            // TaskItem implements ITaskITem2 explicitly and ITaskItem implicitly.
            var item2 = (ITaskItem2)item;
            foreach (var segment in metadata)
            {
                var keyAndValue = segment.Split(new[] { '=' }, count: 2);
                if (string.Equals("Identity", keyAndValue[0]))
                {
                    item2.EvaluatedIncludeEscaped = keyAndValue[1];
                    continue;
                }

                item2.SetMetadata(keyAndValue[0], keyAndValue[1]);
            }

            return item;
        }

        private static void EscapeValue(string value, StringBuilder builder)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.IndexOfAny(CharsToEscape) == -1)
            {
                builder.Append(value);
                return;
            }

            foreach (var @char in value)
            {
                if (CharsToEscapeHash.Contains(@char))
                {
                    builder.Append('%');
                    builder.Append(HexDigitChar(@char / 0x10));
                    builder.Append(HexDigitChar(@char & 0x0F));
                    continue;
                }

                builder.Append(@char);
            }
        }

        private static char HexDigitChar(int x)
        {
            return (char)(x + (x < 10 ? '0' : ('a' - 10)));
        }
    }
}
