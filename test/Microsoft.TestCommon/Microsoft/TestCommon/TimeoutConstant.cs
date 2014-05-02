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

namespace Microsoft.TestCommon
{
    /// <summary>
    /// MSTest timeout constants for use with the <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.TimeoutAttribute"/>.
    /// </summary>
    public class TimeoutConstant
    {
        private const int seconds = 1000;

        /// <summary>
        /// The default timeout for test methods.
        /// </summary>
        public const int DefaultTimeout = 30 * seconds;

        /// <summary>
        /// An extended timeout for longer running test methods.
        /// </summary>
        public const int ExtendedTimeout = 240 * seconds;
    }
}
