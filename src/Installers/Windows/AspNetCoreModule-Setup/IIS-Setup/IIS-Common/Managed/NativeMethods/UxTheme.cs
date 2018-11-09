// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace Microsoft.Web.Management.PInvoke.UxTheme
{
    internal static class NativeMethods
    {
        [DllImport("UxTheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public extern static void SetWindowTheme(IntPtr hWnd, string textSubAppName, string textSubIdList);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int GetCurrentThemeName(StringBuilder pszThemeFileName, int dwMaxNameChars, StringBuilder pszColorBuff, int dwMaxColorChars, StringBuilder pszSizeBuff, int cchMaxSizeChars);

        public static bool TryGetCurrentThemeName(out string themeName, out string color, out string size)
        {
            StringBuilder nameBuilder = new StringBuilder(512);
            StringBuilder colorBuilder = new StringBuilder(512);
            StringBuilder sizeBuilder = new StringBuilder(512);
            int hr = GetCurrentThemeName(nameBuilder, nameBuilder.Capacity, colorBuilder, colorBuilder.Capacity, sizeBuilder, sizeBuilder.Capacity);
            if (hr == 0)
            {
                themeName = nameBuilder.ToString();
                color = colorBuilder.ToString();
                size = sizeBuilder.ToString();
                return true;
            }
            else
            {
                themeName = null;
                color = null;
                size = null;
                if (hr != Extension.AsHRESULT(Win32ErrorCode.ELEMENT_NOT_FOUND))
                {
                    Debug.Fail("GetCurrentThemeName returned: " + hr.ToString());
                }

                return false;
            }
        }
    }
}