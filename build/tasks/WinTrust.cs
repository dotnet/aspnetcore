using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RepoTasks
{
    // See http://msdn.microsoft.com/en-us/library/aa388205(v=VS.85).aspx
    internal class WinTrust
    {
        // The GUID action ID for using the AuthentiCode policy provider (see softpub.h)
        private const string WINTRUST_ACTION_GENERIC_VERIFY_V2 = "{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}";

        // See wintrust.h
        enum UIChoice
        {
            WTD_UI_ALL = 1,
            WTD_UI_NONE,
            WTD_UI_NOBAD,
            WTD_UI_NOGOOD
        }

        enum RevocationChecks
        {
            WTD_REVOKE_NONE,
            WTD_REVOKE_WHOLECHAIN
        }

        enum UnionChoice
        {
            WTD_CHOICE_FILE = 1,
            WTD_CHOICE_CATALOG,
            WTD_CHOICE_BLOB,
            WTD_CHOICE_SIGNER,
            WTD_CHOICE_CERT
        }

        enum StateAction
        {
            WTD_STATEACTION_IGNORE,
            WTD_STATEACTION_VERIFY,
            WTD_STATEACTION_CLOSE,
            WTD_STATEACTION_AUTO_CACHE,
            WTD_STATEACTION_AUTO_CACHE_FLUSH
        }

        enum Provider
        {
            WTD_USE_IE4_TRUST_FLAG = 0x00000001,
            WTD_NO_IE4_CHAIN_FLAG = 0x00000002,
            WTD_NO_POLICY_USAGE_FLAG = 0x00000004,
            WTD_REVOCATION_CHECK_NONE = 0x00000010,
            WTD_REVOCATION_CHECK_END_CERT = 0x00000020,
            WTD_REVOCATION_CHECK_CHAIN = 0x00000040,
            WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x00000080,
            WTD_SAFER_FLAG = 0x00000100,
            WTD_HASH_ONLY_FLAG = 0x00000200,
            WTD_USE_DEFAULT_OSVER_CHECK = 0x00000400,
            WTD_LIFETIME_SIGNING_FLAG = 0x00000800,
            WTD_CACHE_ONLY_URL_RETRIEVAL = 0x00001000
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WinTrustData
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile; // We're not interested in other union members
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WinTrustFileInfo
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        public static bool IsAuthenticodeSigned(string path)
        {
            var fileInfo = new WinTrustFileInfo
            {
                cbStruct = (uint)Marshal.SizeOf<WinTrustFileInfo>(),

                pcwszFilePath = Path.GetFullPath(path),
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            };

            var data = new WinTrustData
            {
                cbStruct = (uint)Marshal.SizeOf<WinTrustData>(),
                dwProvFlags = Convert.ToUInt32(Provider.WTD_SAFER_FLAG),
                dwStateAction = Convert.ToUInt32(StateAction.WTD_STATEACTION_IGNORE),
                dwUIChoice = Convert.ToUInt32(UIChoice.WTD_UI_NONE),
                dwUIContext = 0,
                dwUnionChoice = Convert.ToUInt32(UnionChoice.WTD_CHOICE_FILE),
                fdwRevocationChecks = Convert.ToUInt32(RevocationChecks.WTD_REVOKE_NONE),
                hWVTStateData = IntPtr.Zero,
                pFile = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>()),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                pwszURLReference = IntPtr.Zero
            };

            // TODO: Potential memory leak. Need to invetigate
            Marshal.StructureToPtr(fileInfo, data.pFile, false);

            var pGuid = Marshal.AllocHGlobal(Marshal.SizeOf<Guid>());
            var pData = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustData>());
            Marshal.StructureToPtr(data, pData, true);
            Marshal.StructureToPtr(new Guid(WINTRUST_ACTION_GENERIC_VERIFY_V2), pGuid, true);

            var result = WinVerifyTrust(IntPtr.Zero, pGuid, pData);

            Marshal.FreeHGlobal(pGuid);
            Marshal.FreeHGlobal(pData);

            return result == 0;
        }

        [DllImport("wintrust.dll", SetLastError = true)]
        internal static extern uint WinVerifyTrust(IntPtr hWnd, IntPtr pgActionID, IntPtr pWinTrustData);
    }
}