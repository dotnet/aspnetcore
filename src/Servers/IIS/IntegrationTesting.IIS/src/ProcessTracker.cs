// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS;

// Uses Windows Job Objects to ensure external processes are killed if the current process is terminated non-gracefully.
internal static partial class ProcessTracker
{
    private static readonly IntPtr _jobHandle = IntiailizeProcessTracker();

    private static IntPtr IntiailizeProcessTracker()
    {
        // Requires Win8 or later
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || Environment.OSVersion.Version < new Version(6, 2))
        {
            return IntPtr.Zero;
        }

        // Job name is optional but helps with diagnostics.  Job name must be unique if non-null.
        var jobHandle = CreateJobObject(IntPtr.Zero, name: $"ProcessTracker{Environment.ProcessId}");

        var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOBOBJECTLIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        var length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        var extendedInfoPtr = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

            if (!SetInformationJobObject(jobHandle, JobObjectInfoType.ExtendedLimitInformation,
                extendedInfoPtr, (uint)length))
            {
                throw new Win32Exception();
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
        }

        return jobHandle;
    }

    public static void Add(Process process)
    {
        if (_jobHandle != IntPtr.Zero)
        {
            var success = AssignProcessToJobObject(_jobHandle, process.Handle);
            if (!success && !process.HasExited)
            {
                throw new Win32Exception();
            }
        }
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateJobObjectW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateJobObject(IntPtr lpJobAttributes, string name);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetInformationJobObject(IntPtr job, JobObjectInfoType infoType,
        IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    private enum JobObjectInfoType
    {
        AssociateCompletionPortInformation = 7,
        BasicLimitInformation = 2,
        BasicUIRestrictions = 4,
        EndOfJobTimeInformation = 6,
        ExtendedLimitInformation = 9,
        SecurityLimitInformation = 5,
        GroupInformation = 11
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public Int64 PerProcessUserTimeLimit;
        public Int64 PerJobUserTimeLimit;
        public JOBOBJECTLIMIT LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public UInt32 ActiveProcessLimit;
        public Int64 Affinity;
        public UInt32 PriorityClass;
        public UInt32 SchedulingClass;
    }

    [Flags]
    private enum JOBOBJECTLIMIT : uint
    {
        JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public UInt64 ReadOperationCount;
        public UInt64 WriteOperationCount;
        public UInt64 OtherOperationCount;
        public UInt64 ReadTransferCount;
        public UInt64 WriteTransferCount;
        public UInt64 OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}
