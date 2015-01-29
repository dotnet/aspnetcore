// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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