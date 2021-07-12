// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
