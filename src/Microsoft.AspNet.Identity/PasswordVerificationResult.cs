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

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Return result for IPasswordHasher
    /// </summary>
    public enum PasswordVerificationResult
    {
        /// <summary>
        ///     Password verification failed
        /// </summary>
        Failed = 0,

        /// <summary>
        ///     Success
        /// </summary>
        Success = 1,

        /// <summary>
        ///     Success but should update and rehash the password
        /// </summary>
        SuccessRehashNeeded = 2
    }
}