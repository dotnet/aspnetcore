// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace Microsoft.Web.Utility
{
    // need to include <oob>\common\Managed\NativeMethods\Fusion.cs in your project to use this class
    internal static class GACManagedAccess
    {
        public static List<string> GetAssemblyList(string assemblyName)
        {
            return GetAssemblyList(assemblyName, true);
        }

        public static List<string> GetAssemblyList(string assemblyName, bool getPhysicalPath)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                throw new ArgumentNullException("assemblyName");
            }

            List<string> assemblyList = new List<string>();
            using (GacAssembly gacAssembly = new GacAssembly(assemblyName))
            {
                while (true)
                {
                    if (gacAssembly.GetNextAssembly())
                    {
                        if (getPhysicalPath)
                        {
                            using (GACAssemblyCache gacAssemblyCache = new GACAssemblyCache(assemblyName, gacAssembly.FullAssemblyName))
                            {
                                assemblyList.Add(gacAssemblyCache.AssemblyPath);
                            }
                        }
                        else
                        {
                            assemblyList.Add(gacAssembly.FullAssemblyName);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return assemblyList;
        }
    }

    internal class GacAssembly : IDisposable
    {
        internal GacAssembly(string assemblyName)
        {
            _assemblyName = assemblyName;
            int hResult = PInvoke.Fusion.NativeMethods.CreateAssemblyNameObject(
                    out _fusionName,
                    _assemblyName,
                    PInvoke.Fusion.CreateAssemblyNameObjectFlags.CANOF_PARSE_DISPLAY_NAME,
                    IntPtr.Zero);

            if (hResult >= 0)
            {
                hResult = PInvoke.Fusion.NativeMethods.CreateAssemblyEnum(
                    out _assemblyEnum,
                    IntPtr.Zero,
                    _fusionName,
                    PInvoke.Fusion.AssemblyCacheFlags.GAC,
                    IntPtr.Zero);
            }

            if (hResult < 0 || _assemblyEnum == null)
            {
                throw Marshal.GetExceptionForHR(hResult);
            }
        }

        internal bool GetNextAssembly()
        {
            int hResult = _assemblyEnum.GetNextAssembly((IntPtr)0, out _fusionName, 0);

            if (hResult < 0 || _fusionName == null)
            {
                return false;
            }

            return true;
        }

        internal string FullAssemblyName
        {
            get
            {
                StringBuilder sDisplayName = new StringBuilder(1024);
                int iLen = 1024;

                int hrLocal = _fusionName.GetDisplayName(
                    sDisplayName,
                    ref iLen,
                    (int)PInvoke.Fusion.AssemblyNameDisplayFlags.ALL);

                if (hrLocal < 0)
                {
                    throw Marshal.GetExceptionForHR(hrLocal);
                }

                return sDisplayName.ToString();
            }
        }

        internal PInvoke.Fusion.IAssemblyName FusionName
        {
            get
            {
                return _fusionName;
            }
        }

        public void Dispose()
        {
            PInvoke.Fusion.IAssemblyName tempName = _fusionName;
            if (tempName != null)
            {
                _fusionName = null;
                Marshal.ReleaseComObject(tempName);
            }

            PInvoke.Fusion.IAssemblyEnum tempEnum = _assemblyEnum;
            if (tempEnum != null)
            {
                _assemblyEnum = null;
                Marshal.ReleaseComObject(tempEnum);
            }
        }

        private string _assemblyName;
        private PInvoke.Fusion.IAssemblyEnum _assemblyEnum;
        private PInvoke.Fusion.IAssemblyName _fusionName;
    }

    internal class GACAssemblyCache : IDisposable
    {
        internal GACAssemblyCache(string assemblyName, string fullAssemblyName)
        {
            PInvoke.Fusion.AssemblyInfo aInfo = new PInvoke.Fusion.AssemblyInfo();
            aInfo.cchBuf = 1024;
            aInfo.currentAssemblyPath = new string('\0', aInfo.cchBuf);

            int hResult = PInvoke.Fusion.NativeMethods.CreateAssemblyCache(out _assemblyCache, 0);

            if (hResult == 0)
            {
                hResult = _assemblyCache.QueryAssemblyInfo(0, fullAssemblyName, ref aInfo);
            }

            if (hResult != 0)
            {
                Marshal.GetExceptionForHR(hResult);
            }

            _assemblyPath = aInfo.currentAssemblyPath;
        }

        internal string AssemblyPath
        {
            get
            {
                return _assemblyPath;
            }
        }

        public void Dispose()
        {
            PInvoke.Fusion.IAssemblyCache temp = _assemblyCache;
            if (temp != null)
            {
                _assemblyCache = null;
                Marshal.ReleaseComObject(temp);
            }
        }

        private string _assemblyPath;
        private PInvoke.Fusion.IAssemblyCache _assemblyCache;
    }
}