using System;
using System.Diagnostics;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal sealed class DpapiDataProtectionProviderImpl : IDataProtectionProvider
    {
        private readonly byte[] _entropy;

        public DpapiDataProtectionProviderImpl(byte[] entropy)
        {
            Debug.Assert(entropy != null);
            _entropy = entropy;
        }

        public IDataProtector CreateProtector(string purpose)
        {
            return new DpapiDataProtectorImpl(BCryptUtil.GenerateDpapiSubkey(_entropy, purpose));
        }

        public void Dispose()
        {
            // no-op; no unmanaged resources to dispose
        }
    }
}