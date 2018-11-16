using System;
using System.Runtime.InteropServices;

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
        private bool? _isWindows;

        public bool IsRunningOnWindows
        {
            get
            {
                if (_isWindows == null)
                {
                    _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                }

                return _isWindows.Value;
            }
        }

        public bool IsRunningOnNanoServer
        {
            get
            {
                if (_isNano == null)
                {
                    var osVersion = new Version(RtlGetVersion() ?? string.Empty);

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

        // Sql client not available on mono, non-windows, or nano
        public bool UseInMemoryStore
        {
            get
            {
                return !IsRunningOnWindows || IsRunningOnNanoServer;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }

        // This call avoids the shimming Windows does to report old versions
        [DllImport("ntdll")]
        private static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

        internal static string RtlGetVersion()
        {
            RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
            osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
            if (RtlGetVersion(out osvi) == 0)
            {
                return $"{osvi.dwMajorVersion}.{osvi.dwMinorVersion}.{osvi.dwBuildNumber}";
            }
            else
            {
                return null;
            }
        }
    }
}
