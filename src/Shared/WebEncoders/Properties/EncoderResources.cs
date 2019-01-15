// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;

namespace Microsoft.Extensions.WebEncoders.Sources
{
    // TODO using a resx file. project.json, unfortunately, fails to embed resx files when there are also compile items
    // in the contentFiles section. Revisit once we convert repos to MSBuild
    internal static class EncoderResources
    {
        /// <summary>
        /// Invalid {0}, {1} or {2} length.
        /// </summary>
        internal static readonly string WebEncoders_InvalidCountOffsetOrLength = "Invalid {0}, {1} or {2} length.";

        /// <summary>
        /// Malformed input: {0} is an invalid input length.
        /// </summary>
        internal static readonly string WebEncoders_MalformedInput = "Malformed input: {0} is an invalid input length.";

        /// <summary>
        /// Invalid {0}, {1} or {2} length.
        /// </summary>
        internal static string FormatWebEncoders_InvalidCountOffsetOrLength(object p0, object p1, object p2)
        {
            return string.Format(CultureInfo.CurrentCulture, WebEncoders_InvalidCountOffsetOrLength, p0, p1, p2);
        }

        /// <summary>
        /// Malformed input: {0} is an invalid input length.
        /// </summary>
        internal static string FormatWebEncoders_MalformedInput(object p0)
        {
            return string.Format(CultureInfo.CurrentCulture, WebEncoders_MalformedInput, p0);
        }
    }
}
