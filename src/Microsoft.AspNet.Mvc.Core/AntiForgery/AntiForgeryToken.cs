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

namespace Microsoft.AspNet.Mvc
{
    internal sealed class AntiForgeryToken
    {
        internal const int SecurityTokenBitLength = 128;
        internal const int ClaimUidBitLength = 256;

        private string _additionalData = string.Empty;
        private string _username = string.Empty;
        private BinaryBlob _securityToken;

        public string AdditionalData
        {
            get { return _additionalData; }
            set
            {
                _additionalData = value ?? string.Empty;
            }
        }

        public BinaryBlob ClaimUid { get; set; }

        public bool IsSessionToken { get; set; }

        public BinaryBlob SecurityToken
        {
            get
            {
                if (_securityToken == null)
                {
                    _securityToken = new BinaryBlob(SecurityTokenBitLength);
                }
                return _securityToken;
            }
            set
            {
                _securityToken = value;
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                _username = value ?? string.Empty;
            }
        }
    }
}