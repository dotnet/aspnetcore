// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RazorIRPass
    {
        /// <summary>
        /// The default implementation of the <see cref="IRazorEngineFeature"/>s that run in a
        /// <see cref="IRazorEnginePhase"/> will use this value for its Order property.
        /// </summary>
        /// <remarks>
        /// This value is chosen in such a way that the default implementation runs after the other
        /// custom <see cref="IRazorEngineFeature"/> implementations for a particular <see cref="IRazorEnginePhase"/>.
        /// </remarks>
        public static readonly int DefaultFeatureOrder = 1000;
    }
}
