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

#if NET45
using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal class ProtectedDataProtectionProvider : IDataProtectionProvider
    {
        private readonly DataProtectionScope _scope;

        public ProtectedDataProtectionProvider(DataProtectionScope scope)
        {
            _scope = scope;
        }

        public IDataProtector CreateProtector(string purpose)
        {
            return new ProtectedDataProtector(_scope, purpose);
        }

        public void Dispose()
        {

        }

        private class ProtectedDataProtector : IDataProtector
        {
            private readonly DataProtectionScope _scope;
            private readonly byte[] _entropy;

            public ProtectedDataProtector(DataProtectionScope scope, string purpose)
            {
                _scope = scope;
                _entropy = Encoding.UTF8.GetBytes(purpose);
            }

            private ProtectedDataProtector(DataProtectionScope scope, byte[] entropy)
            {
                _scope = scope;
                _entropy = entropy;
            }

            public IDataProtector CreateSubProtector(string purpose)
            {
                var purposeBytes = Encoding.UTF8.GetBytes(purpose);
                var subProtectorEntropy = new byte[_entropy.Length + purposeBytes.Length];

                Buffer.BlockCopy(_entropy, 0, subProtectorEntropy, 0, _entropy.Length);
                Buffer.BlockCopy(purposeBytes, 0, subProtectorEntropy, _entropy.Length, purposeBytes.Length);

                return new ProtectedDataProtector(_scope, subProtectorEntropy);
            }

            public byte[] Protect(byte[] unprotectedData)
            {
                return ProtectedData.Protect(unprotectedData, _entropy, _scope);
            }

            public byte[] Unprotect(byte[] protectedData)
            {
                return ProtectedData.Unprotect(protectedData, _entropy, _scope);
            }

            public void Dispose()
            {

            }
        }
    }
}
#endif
