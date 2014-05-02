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

namespace Microsoft.AspNet.Security.DataProtection
{
    internal unsafe sealed class DataProtectionProviderImpl : IDataProtectionProvider
    {
        private readonly byte[] _protectedKdk;

        public DataProtectionProviderImpl(byte[] protectedKdk)
        {
            _protectedKdk = protectedKdk;
        }

        public IDataProtector CreateProtector(string purpose)
        {
            BCryptKeyHandle newAesKeyHandle;
            BCryptHashHandle newHmacHashHandle;
            byte[] newProtectedKdfSubkey;

            BCryptUtil.DeriveKeysSP800108(_protectedKdk, purpose, Algorithms.AESAlgorithmHandle, out newAesKeyHandle, Algorithms.HMACSHA256AlgorithmHandle, out newHmacHashHandle, out newProtectedKdfSubkey);
            return new DataProtectorImpl(newAesKeyHandle, newHmacHashHandle, newProtectedKdfSubkey);
        }

        public void Dispose()
        {
            // no-op: we hold no protected resources
        }
    }
}
