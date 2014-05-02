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
    ///     Represents a linked login for a user (i.e. a local username/password or a facebook/google account
    /// </summary>
    public sealed class UserLoginInfo
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="loginProvider"></param>
        /// <param name="providerKey"></param>
        public UserLoginInfo(string loginProvider, string providerKey)
        {
            LoginProvider = loginProvider;
            ProviderKey = providerKey;
        }

        /// <summary>
        ///     Provider for the linked login, i.e. Local, Facebook, Google, etc.
        /// </summary>
        public string LoginProvider { get; set; }

        /// <summary>
        ///     Key for the linked login at the provider
        /// </summary>
        public string ProviderKey { get; set; }
    }
}