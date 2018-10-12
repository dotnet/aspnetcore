// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Web.Utility.PInvoke.Fusion
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")]
    internal interface IAssemblyName
    {
        [PreserveSig()]
		int SetProperty(
				int PropertyId,
				IntPtr pvProperty,
				int cbProperty);

		[PreserveSig()]
		int GetProperty(
				int PropertyId,
				IntPtr pvProperty,
				ref int pcbProperty);

		[PreserveSig()]
		int Finalize();

		[PreserveSig()]
		int GetDisplayName(
				StringBuilder pDisplayName,
				ref int pccDisplayName,
				int displayFlags);

		[PreserveSig()]
		int Reserved(ref Guid guid,
			Object obj1,
			Object obj2,
			String string1,
			Int64 llFlags,
			IntPtr pvReserved,
			int cbReserved,
			out IntPtr ppv);

		[PreserveSig()]
		int GetName(
				ref int pccBuffer,
				StringBuilder pwzName);

		[PreserveSig()]
		int GetVersion(
				out int versionHi,
				out int versionLow);
		[PreserveSig()]
		int IsEqual(
				IAssemblyName pAsmName,
				int cmpFlags);

		[PreserveSig()]
		int Clone(out IAssemblyName pAsmName);
	}// IAssemblyName

	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
	internal interface IAssemblyCache
	{
		[PreserveSig()]
		int UninstallAssembly(
			int flags,
			[MarshalAs(UnmanagedType.LPWStr)]
		string assemblyName,
			IntPtr refData,
			out int disposition);

		[PreserveSig()]
		int QueryAssemblyInfo(
			int flags,
			[MarshalAs(UnmanagedType.LPWStr)]
		string assemblyName,
			ref AssemblyInfo assemblyInfo);
		[PreserveSig()]
		int Reserved(
			int flags,
			IntPtr pvReserved,
			out object ppAsmItem,
			[MarshalAs(UnmanagedType.LPWStr)]
		string assemblyName);
		[PreserveSig()]
		int Reserved(out object ppAsmScavenger);

		[PreserveSig()]
		int InstallAssembly(
			int flags,
			[MarshalAs(UnmanagedType.LPWStr)]
		string assemblyFilePath,
			IntPtr refData);
	}// IAssemblyCache

	[StructLayout(LayoutKind.Sequential)]
	internal struct AssemblyInfo
	{
		public int cbAssemblyInfo; // size of this structure for future expansion
		public int assemblyFlags;
		public long assemblySizeInKB;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string currentAssemblyPath;
		public int cchBuf; // size of path buf.
	}

    [Flags]
    internal enum AssemblyCacheFlags
    {
        GAC = 2,
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
    internal interface IAssemblyEnum
    {
        [PreserveSig()]
        int GetNextAssembly(
                IntPtr pvReserved,
                out IAssemblyName ppName,
                int flags);

        [PreserveSig()]
        int Reset();
        [PreserveSig()]
        int Clone(out IAssemblyEnum ppEnum);
    }

    [Flags]
    internal enum AssemblyNameDisplayFlags
    {
        VERSION = 0x01,
        CULTURE = 0x02,
        PUBLIC_KEY_TOKEN = 0x04,
        PROCESSORARCHITECTURE = 0x20,
        RETARGETABLE = 0x80,

        // This enum might change in the future to include
        // more attributes.
        ALL =
            VERSION
            | CULTURE
            | PUBLIC_KEY_TOKEN
            | PROCESSORARCHITECTURE
            | RETARGETABLE
    }

    internal enum CreateAssemblyNameObjectFlags
    {
        CANOF_DEFAULT = 0,
        CANOF_PARSE_DISPLAY_NAME = 1,
    }

    internal static class NativeMethods
    {
        [DllImport("fusion.dll")]
        public static extern int CreateAssemblyCache(
                out IAssemblyCache ppAsmCache,
                int reserved);

        [DllImport("fusion.dll")]
        public static extern int CreateAssemblyEnum(
                out IAssemblyEnum ppEnum,
                IntPtr pUnkReserved,
                IAssemblyName pName,
                AssemblyCacheFlags flags,
                IntPtr pvReserved);

        [DllImport("fusion.dll")]
        public static extern int CreateAssemblyNameObject(
                out IAssemblyName ppAssemblyNameObj,
                [MarshalAs(UnmanagedType.LPWStr)]
                String szAssemblyName,
                CreateAssemblyNameObjectFlags flags,
                IntPtr pvReserved);
    }
}
