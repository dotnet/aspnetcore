using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.PlatformAbstractions;

namespace MusicStore
{
    internal class Platform
    {
        // Defined in winnt.h
        private const int PRODUCT_NANO_SERVER = 0x0000006D;
        private const int PRODUCT_DATACENTER_NANO_SERVER = 0x0000008F;
        private const int PRODUCT_STANDARD_NANO_SERVER = 0x00000090;

        [DllImport("api-ms-win-core-sysinfo-l1-2-1.dll", SetLastError = false)]
        private static extern bool GetProductInfo(
              int dwOSMajorVersion,
              int dwOSMinorVersion,
              int dwSpMajorVersion,
              int dwSpMinorVersion,
              out int pdwReturnedProductType);

        private bool? _isNano;
        private bool? _isMono;
        private bool? _isWindows;

        public bool IsRunningOnWindows
        {
            get
            {
                if (_isWindows == null)
                {
                    _isWindows = PlatformServices.Default.Runtime.OperatingSystem.Equals(
                        "Windows", StringComparison.OrdinalIgnoreCase);
                }

                return _isWindows.Value;
            }
        }

        public bool IsRunningOnMono
        {
            get
            {
                if (_isMono == null)
                {
                    _isMono = string.Equals(
                        PlatformServices.Default.Runtime.RuntimeType,
                        "Mono",
                        StringComparison.OrdinalIgnoreCase);
                }

                return _isMono.Value;
            }
        }

        public bool IsRunningOnNanoServer
        {
            get
            {
                if (_isNano == null)
                {
                    var osVersion = new Version(PlatformServices.Default.Runtime.OperatingSystemVersion ?? "");

                    try
                    {
                        int productType;
                        if (GetProductInfo(osVersion.Major, osVersion.Minor, 0, 0, out productType))
                        {
                            _isNano = productType == PRODUCT_NANO_SERVER ||
                                productType == PRODUCT_DATACENTER_NANO_SERVER ||
                                productType == PRODUCT_STANDARD_NANO_SERVER;
                        }
                        else
                        {
                            _isNano = false;
                        }
                    }
                    catch
                    {
                        // If the API call fails, the API set is not there which means
                        // that we are definetely not running on Nano
                        _isNano = false;
                    }
                }

                return _isNano.Value;
            }
        }
    }
}
