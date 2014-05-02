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
using System.Diagnostics;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal sealed class DpapiDataProtectionProviderImpl : IDataProtectionProvider
    {
        private readonly byte[] _entropy;
        private readonly bool _protectToLocalMachine;

        public DpapiDataProtectionProviderImpl(byte[] entropy, bool protectToLocalMachine)
        {
            Debug.Assert(entropy != null);
            _entropy = entropy;
            _protectToLocalMachine = protectToLocalMachine;
        }

        public IDataProtector CreateProtector(string purpose)
        {
            return new DpapiDataProtectorImpl(BCryptUtil.GenerateDpapiSubkey(_entropy, purpose), _protectToLocalMachine);
        }

        public void Dispose()
        {
            // no-op; no unmanaged resources to dispose
        }
    }
}
