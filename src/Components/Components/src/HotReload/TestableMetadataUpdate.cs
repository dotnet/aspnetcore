// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace Microsoft.AspNetCore.Components.HotReload
{
    internal sealed class TestableMetadataUpdate
    {
        public static bool TestIsSupported { private get; set; }

        /// <summary>
        /// A proxy for <see cref="MetadataUpdater.IsSupported" /> that is testable.
        /// </summary>
        public static bool IsSupported => MetadataUpdater.IsSupported || TestIsSupported;
    }
}
