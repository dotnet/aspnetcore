// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// An enumeration of known platforms that the unit test might be running under.
    /// </summary>
    [Flags]
    public enum Platform
    {
        /// <summary>
        /// A special value used to indicate that the test is valid on all known platforms.
        /// </summary>
        All = 0xFFFFFF,

        /// <summary>
        /// Indicates that the test wants to run on .NET 4 (when used with
        /// <see cref="FactAttribute.Platforms"/> and/or <see cref="TheoryAttribute.Platforms"/>),
        /// or that the current platform that the test is running on is .NET 4 (when used with the
        /// <see cref="PlatformInfo.Platform"/>, <see cref="FactAttribute.Platform"/>, and/or
        /// <see cref="TheoryAttribute.Platform"/>).
        /// </summary>
        Net40 = 0x01,

        /// <summary>
        /// Indicates that the test wants to run on .NET 4.5 (when used with
        /// <see cref="FactAttribute.Platforms"/> and/or <see cref="TheoryAttribute.Platforms"/>),
        /// or that the current platform that the test is running on is .NET 4.5 (when used with the
        /// <see cref="PlatformInfo.Platform"/>, <see cref="FactAttribute.Platform"/>, and/or
        /// <see cref="TheoryAttribute.Platform"/>).
        /// </summary>
        Net45 = 0x02,
    }
}
