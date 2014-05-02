// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
