// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Internal
{
    internal static class LinkerFlags
    {
        /// <summary>
        /// Flags for a member that is JSON (de)serialized.
        /// </summary>
        public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;

        /// <summary>
        /// Flags for a component
        /// </summary>
        public const DynamicallyAccessedMemberTypes Component = DynamicallyAccessedMemberTypes.All;
    }
}
