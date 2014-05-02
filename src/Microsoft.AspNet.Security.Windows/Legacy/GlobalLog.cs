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

// -----------------------------------------------------------------------
// <copyright file="GlobalLog.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.AspNet.Security.Windows
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.Runtime.ConstrainedExecution;
    using System.Security.Permissions;
    using System.Threading;

    /// <internalonly/>
    /// <devdoc>
    /// </devdoc>
    internal static class GlobalLog
    {
        // Logging Initalization - I need to disable Logging code, and limit
        //  the effect it has when it is dissabled, so I use a bool here.
        //
        //  This can only be set when the logging code is built and enabled.
        //  By specifing the "CSC_DEFINES=/D:TRAVE" in the build environment,
        //  this code will be built and then checks against an enviroment variable
        //  and a BooleanSwitch to see if any of the two have enabled logging.

        private static BaseLoggingObject Logobject = GlobalLog.LoggingInitialize();
#if TRAVE
        internal static LocalDataStoreSlot s_ThreadIdSlot;
        internal static bool s_UseThreadId;
        internal static bool s_UseTimeSpan;
        internal static bool s_DumpWebData;
        internal static bool s_UsePerfCounter;
        internal static bool s_DebugCallNesting;
        internal static bool s_DumpToConsole;
        internal static int s_MaxDumpSize;
        internal static string s_RootDirectory;

        //
        // Logging Config Variables -  below are list of consts that can be used to config
        //  the logging,
        //

        // Max number of lines written into a buffer, before a save is invoked
        // s_DumpToConsole disables.
        public const int MaxLinesBeforeSave = 0;

#endif
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        private static BaseLoggingObject LoggingInitialize()
        {
#if DEBUG
            if (GetSwitchValue("SystemNetLogging", "System.Net logging module", false) &&
                GetSwitchValue("SystemNetLog_ConnectionMonitor", "System.Net connection monitor thread", false))
            {
                InitConnectionMonitor();
            }
#endif // DEBUG
#if TRAVE
            // by default we'll log to c:\temp\ so that non interactive services (like w3wp.exe) that don't have environment
            // variables can easily be debugged, note that the ACLs of the directory might need to be adjusted
            if (!GetSwitchValue("SystemNetLog_OverrideDefaults", "System.Net log override default settings", false)) {
                s_ThreadIdSlot = Thread.AllocateDataSlot();
                s_UseThreadId = true;
                s_UseTimeSpan = true;
                s_DumpWebData = true;
                s_MaxDumpSize = 256;
                s_UsePerfCounter = true;
                s_DebugCallNesting = true;
                s_DumpToConsole = false;
                s_RootDirectory = "C:\\Temp\\";
                return new LoggingObject();
            }
            if (GetSwitchValue("SystemNetLogging", "System.Net logging module", false)) {
                s_ThreadIdSlot = Thread.AllocateDataSlot();
                s_UseThreadId = GetSwitchValue("SystemNetLog_UseThreadId", "System.Net log display system thread id", false);
                s_UseTimeSpan = GetSwitchValue("SystemNetLog_UseTimeSpan", "System.Net log display ticks as TimeSpan", false);
                s_DumpWebData = GetSwitchValue("SystemNetLog_DumpWebData", "System.Net log display HTTP send/receive data", false);
                s_MaxDumpSize = GetSwitchValue("SystemNetLog_MaxDumpSize", "System.Net log max size of display data", 256);
                s_UsePerfCounter = GetSwitchValue("SystemNetLog_UsePerfCounter", "System.Net log use QueryPerformanceCounter() to get ticks ", false);
                s_DebugCallNesting = GetSwitchValue("SystemNetLog_DebugCallNesting", "System.Net used to debug call nesting", false);
                s_DumpToConsole = GetSwitchValue("SystemNetLog_DumpToConsole", "System.Net log to console", false);
                s_RootDirectory = GetSwitchValue("SystemNetLog_RootDirectory", "System.Net root directory of log file", string.Empty);
                return new LoggingObject();
            }
#endif // TRAVE
            return new BaseLoggingObject();
        }

#if TRAVE 
        private static string GetSwitchValue(string switchName, string switchDescription, string defaultValue) {
            new EnvironmentPermission(PermissionState.Unrestricted).Assert();
            try {
                defaultValue = Environment.GetEnvironmentVariable(switchName);
            }
            finally {
                EnvironmentPermission.RevertAssert();
            }
            return defaultValue;
        }

        private static int GetSwitchValue(string switchName, string switchDescription, int defaultValue) {
            IntegerSwitch theSwitch = new IntegerSwitch(switchName, switchDescription);
            if (theSwitch.Enabled) {
                return theSwitch.Value;
            }
            new EnvironmentPermission(PermissionState.Unrestricted).Assert();
            try {
                string environmentVar = Environment.GetEnvironmentVariable(switchName);
                if (environmentVar!=null) {
                    defaultValue = Int32.Parse(environmentVar.Trim(), CultureInfo.InvariantCulture);
                }
            }
            finally {
                EnvironmentPermission.RevertAssert();
            }
            return defaultValue;
        }

#endif

#if TRAVE || DEBUG
        private static bool GetSwitchValue(string switchName, string switchDescription, bool defaultValue)
        {
            BooleanSwitch theSwitch = new BooleanSwitch(switchName, switchDescription);
            new EnvironmentPermission(PermissionState.Unrestricted).Assert();
            try
            {
                if (theSwitch.Enabled)
                {
                    return true;
                }
                string environmentVar = Environment.GetEnvironmentVariable(switchName);
                defaultValue = environmentVar != null && environmentVar.Trim() == "1";
            }
            catch (ConfigurationException)
            {
            }
            finally
            {
                EnvironmentPermission.RevertAssert();
            }
            return defaultValue;
        }
#endif // TRAVE || DEBUG

        // Enables thread tracing, detects mis-use of threads.
#if DEBUG
        [ThreadStatic]
        private static Stack<ThreadKinds> t_ThreadKindStack;

        private static Stack<ThreadKinds> ThreadKindStack
        {
            get
            {
                if (t_ThreadKindStack == null)
                {
                    t_ThreadKindStack = new Stack<ThreadKinds>();
                }
                return t_ThreadKindStack;
            }
        }
#endif

        internal static ThreadKinds CurrentThreadKind
        {
            get
            {
#if DEBUG
                return ThreadKindStack.Count > 0 ? ThreadKindStack.Peek() : ThreadKinds.Other;
#else
                return ThreadKinds.Unknown;
#endif
            }
        }

        private static bool HasShutdownStarted
        {
            get
            {
                return Environment.HasShutdownStarted || AppDomain.CurrentDomain.IsFinalizingForUnload();
            }
        }

#if DEBUG
        // ifdef'd instead of conditional since people are forced to handle the return value.
        // [Conditional("DEBUG")]
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        internal static IDisposable SetThreadKind(ThreadKinds kind)
        {
            if ((kind & ThreadKinds.SourceMask) != ThreadKinds.Unknown)
            {
                throw new InvalidOperationException();
            }

            // Ignore during shutdown.
            if (HasShutdownStarted)
            {
                return null;
            }

            ThreadKinds threadKind = CurrentThreadKind;
            ThreadKinds source = threadKind & ThreadKinds.SourceMask;

#if TRAVE
            // Special warnings when doing dangerous things on a thread.
            if ((threadKind & ThreadKinds.User) != 0 && (kind & ThreadKinds.System) != 0)
            {
                Print("WARNING: Thread changed from User to System; user's thread shouldn't be hijacked.");
            }

            if ((threadKind & ThreadKinds.Async) != 0 && (kind & ThreadKinds.Sync) != 0)
            {
                Print("WARNING: Thread changed from Async to Sync, may block an Async thread.");
            }
            else if ((threadKind & (ThreadKinds.Other | ThreadKinds.CompletionPort)) == 0 && (kind & ThreadKinds.Sync) != 0)
            {
                Print("WARNING: Thread from a limited resource changed to Sync, may deadlock or bottleneck.");
            }
#endif

            ThreadKindStack.Push(
                (((kind & ThreadKinds.OwnerMask) == 0 ? threadKind : kind) & ThreadKinds.OwnerMask) |
                (((kind & ThreadKinds.SyncMask) == 0 ? threadKind : kind) & ThreadKinds.SyncMask) |
                (kind & ~(ThreadKinds.OwnerMask | ThreadKinds.SyncMask)) |
                source);

#if TRAVE
            if (CurrentThreadKind != threadKind)
            {
                Print("Thread becomes:(" + CurrentThreadKind.ToString() + ")");
            }
#endif

            return new ThreadKindFrame();
        }

        private class ThreadKindFrame : IDisposable
        {
            private int m_FrameNumber;

            internal ThreadKindFrame()
            {
                m_FrameNumber = ThreadKindStack.Count;
            }

            void IDisposable.Dispose()
            {
                // Ignore during shutdown.
                if (GlobalLog.HasShutdownStarted)
                {
                    return;
                }

                if (m_FrameNumber != ThreadKindStack.Count)
                {
                    throw new InvalidOperationException();
                }

                ThreadKinds previous = ThreadKindStack.Pop();

#if TRAVE
                if (CurrentThreadKind != previous)
                {
                    Print("Thread reverts:(" + CurrentThreadKind.ToString() + ")");
                }
#endif
            }
        }
#endif

        [Conditional("DEBUG")]
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        internal static void SetThreadSource(ThreadKinds source)
        {
#if DEBUG
            if ((source & ThreadKinds.SourceMask) != source || source == ThreadKinds.Unknown)
            {
                throw new ArgumentException("Must specify the thread source.", "source");
            }

            if (ThreadKindStack.Count == 0)
            {
                ThreadKindStack.Push(source);
                return;
            }

            if (ThreadKindStack.Count > 1)
            {
                Print("WARNING: SetThreadSource must be called at the base of the stack, or the stack has been corrupted.");
                while (ThreadKindStack.Count > 1)
                {
                    ThreadKindStack.Pop();
                }
            }

            if (ThreadKindStack.Peek() != source)
            {
                // SQL can fail to clean up the stack, leaving the default Other at the bottom.  Replace it.
                Print("WARNING: The stack has been corrupted.");
                ThreadKinds last = ThreadKindStack.Pop() & ThreadKinds.SourceMask;
                Assert(last == source || last == ThreadKinds.Other, "Thread source changed.|Was:({0}) Now:({1})", last, source);
                ThreadKindStack.Push(source);
            }
#endif
        }

        [Conditional("DEBUG")]
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        internal static void ThreadContract(ThreadKinds kind, string errorMsg)
        {
            ThreadContract(kind, ThreadKinds.SafeSources, errorMsg);
        }

        [Conditional("DEBUG")]
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        internal static void ThreadContract(ThreadKinds kind, ThreadKinds allowedSources, string errorMsg)
        {
            if ((kind & ThreadKinds.SourceMask) != ThreadKinds.Unknown || (allowedSources & ThreadKinds.SourceMask) != allowedSources)
            {
                throw new InvalidOperationException();
            }

            ThreadKinds threadKind = CurrentThreadKind;
            Assert((threadKind & allowedSources) != 0, errorMsg, "Thread Contract Violation.|Expected source:({0}) Actual source:({1})", allowedSources, threadKind & ThreadKinds.SourceMask);
            Assert((threadKind & kind) == kind, errorMsg, "Thread Contract Violation.|Expected kind:({0}) Actual kind:({1})", kind, threadKind & ~ThreadKinds.SourceMask);
        }

#if DEBUG
        // Enables auto-hang detection, which will "snap" a log on hang
        internal static bool EnableMonitorThread = false;

        // Default value for hang timer
#if FEATURE_PAL // ROTORTODO - after speedups (like real JIT and GC) remove this
        public const int DefaultTickValue = 1000*60*5; // 5 minutes
#else
        public const int DefaultTickValue = 1000 * 60; // 60 secs
#endif // FEATURE_PAL
#endif // DEBUG

        [System.Diagnostics.Conditional("TRAVE")]
        public static void AddToArray(string msg)
        {
#if TRAVE
            GlobalLog.Logobject.PrintLine(msg);
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Ignore(object msg)
        {
        }

        [System.Diagnostics.Conditional("TRAVE")]
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        public static void Print(string msg)
        {
#if TRAVE
            GlobalLog.Logobject.PrintLine(msg);
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void PrintHex(string msg, object value)
        {
#if TRAVE
            GlobalLog.Logobject.PrintLine(msg+TraveHelper.ToHex(value));
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Enter(string func)
        {
#if TRAVE
            GlobalLog.Logobject.EnterFunc(func + "(*none*)");
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Enter(string func, string parms)
        {
#if TRAVE
            GlobalLog.Logobject.EnterFunc(func + "(" + parms + ")");
#endif
        }

        [Conditional("DEBUG")]
        [Conditional("_FORCE_ASSERTS")]
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        public static void Assert(bool condition, string messageFormat, params object[] data)
        {
            if (!condition)
            {
                string fullMessage = string.Format(CultureInfo.InvariantCulture, messageFormat, data);
                int pipeIndex = fullMessage.IndexOf('|');
                if (pipeIndex == -1)
                {
                    Assert(fullMessage);
                }
                else
                {
                    int detailLength = fullMessage.Length - pipeIndex - 1;
                    Assert(fullMessage.Substring(0, pipeIndex), detailLength > 0 ? fullMessage.Substring(pipeIndex + 1, detailLength) : null);
                }
            }
        }

        [Conditional("DEBUG")]
        [Conditional("_FORCE_ASSERTS")]
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        public static void Assert(string message)
        {
            Assert(message, null);
        }

        [Conditional("DEBUG")]
        [Conditional("_FORCE_ASSERTS")]
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        public static void Assert(string message, string detailMessage)
        {
            try
            {
                Print("Assert: " + message + (!string.IsNullOrEmpty(detailMessage) ? ": " + detailMessage : string.Empty));
                Print("*******");
                Logobject.DumpArray(false);
            }
            finally
            {
#if DEBUG && !STRESS
                Debug.Assert(false, message, detailMessage);
#endif
            }
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void LeaveException(string func, Exception exception)
        {
#if TRAVE
            GlobalLog.Logobject.LeaveFunc(func + " exception " + ((exception!=null) ? exception.Message : String.Empty));
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Leave(string func)
        {
#if TRAVE
            GlobalLog.Logobject.LeaveFunc(func + " returns ");
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Leave(string func, string result)
        {
#if TRAVE
            GlobalLog.Logobject.LeaveFunc(func + " returns " + result);
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Leave(string func, int returnval)
        {
#if TRAVE
            GlobalLog.Logobject.LeaveFunc(func + " returns " + returnval.ToString());
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Leave(string func, bool returnval)
        {
#if TRAVE
            GlobalLog.Logobject.LeaveFunc(func + " returns " + returnval.ToString());
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void DumpArray()
        {
#if TRAVE
            GlobalLog.Logobject.DumpArray(true);
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Dump(byte[] buffer)
        {
#if TRAVE
            Logobject.Dump(buffer, 0, buffer!=null ? buffer.Length : -1);
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Dump(byte[] buffer, int length)
        {
#if TRAVE
            Logobject.Dump(buffer, 0, length);
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Dump(byte[] buffer, int offset, int length)
        {
#if TRAVE
            Logobject.Dump(buffer, offset, length);
#endif
        }

        [System.Diagnostics.Conditional("TRAVE")]
        public static void Dump(IntPtr buffer, int offset, int length)
        {
#if TRAVE
            Logobject.Dump(buffer, offset, length);
#endif
        }

#if DEBUG
        private class HttpWebRequestComparer : IComparer
        {
            public int Compare(
                   object x1,
                   object y1)
            {
                HttpWebRequest x = (HttpWebRequest)x1;
                HttpWebRequest y = (HttpWebRequest)y1;

                if (x.GetHashCode() == y.GetHashCode())
                {
                    return 0;
                }
                else if (x.GetHashCode() < y.GetHashCode())
                {
                    return -1;
                }
                else if (x.GetHashCode() > y.GetHashCode())
                {
                    return 1;
                }

                return 0;
            }
        }
        /*
        private class ConnectionMonitorEntry {
            public HttpWebRequest m_Request;
            public int m_Flags;
            public DateTime m_TimeAdded;
            public Connection m_Connection;

            public ConnectionMonitorEntry(HttpWebRequest request, Connection connection, int flags) {
                m_Request = request;
                m_Connection = connection;
                m_Flags = flags;
                m_TimeAdded = DateTime.Now;
            }
        }
        */
        private static volatile ManualResetEvent s_ShutdownEvent;
        private static volatile SortedList s_RequestList;

        internal const int WaitingForReadDoneFlag = 0x1;
#endif
        /*
#if DEBUG
        private static void ConnectionMonitor() {
            while(! s_ShutdownEvent.WaitOne(DefaultTickValue, false)) {
                if (GlobalLog.EnableMonitorThread) {
#if TRAVE
                    GlobalLog.Logobject.LoggingMonitorTick();
#endif
                }

                int hungCount = 0;
                lock (s_RequestList) {
                    DateTime dateNow = DateTime.Now;
                    DateTime dateExpired = dateNow.AddSeconds(-DefaultTickValue);
                    foreach (ConnectionMonitorEntry monitorEntry in s_RequestList.GetValueList() ) {
                        if (monitorEntry != null &&
                            (dateExpired > monitorEntry.m_TimeAdded))
                        {
                            hungCount++;
#if TRAVE
                            GlobalLog.Print("delay:" + (dateNow - monitorEntry.m_TimeAdded).TotalSeconds +
                                " req#" + monitorEntry.m_Request.GetHashCode() +
                                " cnt#" + monitorEntry.m_Connection.GetHashCode() +
                                " flags:" + monitorEntry.m_Flags);

#endif
                            monitorEntry.m_Connection.Debug(monitorEntry.m_Request.GetHashCode());
                        }
                    }
                }
                Assert(hungCount == 0, "Warning: Hang Detected on Connection(s) of greater than {0} ms.  {1} request(s) hung.|Please Dump System.Net.GlobalLog.s_RequestList for pending requests, make sure your streams are calling Close(), and that your destination server is up.", DefaultTickValue, hungCount);
            }
        }
#endif // DEBUG
        **/
#if DEBUG
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
        internal static void AppDomainUnloadEvent(object sender, EventArgs e)
        {
            s_ShutdownEvent.Set();
        }
#endif

#if DEBUG
        [System.Diagnostics.Conditional("DEBUG")]
        private static void InitConnectionMonitor()
        {
            s_RequestList = new SortedList(new HttpWebRequestComparer(), 10);
            s_ShutdownEvent = new ManualResetEvent(false);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(AppDomainUnloadEvent);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(AppDomainUnloadEvent);
            // Thread threadMonitor = new Thread(new ThreadStart(ConnectionMonitor));
            // threadMonitor.IsBackground = true;
            // threadMonitor.Start();
        }
#endif
        /*
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void DebugAddRequest(HttpWebRequest request, Connection connection, int flags) {
#if DEBUG
            // null if the connection monitor is off
            if(s_RequestList == null)
                return;

            lock(s_RequestList) {
                Assert(!s_RequestList.ContainsKey(request), "s_RequestList.ContainsKey(request)|A HttpWebRequest should not be submitted twice.");

                ConnectionMonitorEntry requestEntry =
                    new ConnectionMonitorEntry(request, connection, flags);

                try {
                    s_RequestList.Add(request, requestEntry);
                } catch {
                }
            }
#endif
        }
*/
        /*
              [System.Diagnostics.Conditional("DEBUG")]
              internal static void DebugRemoveRequest(HttpWebRequest request) {
      #if DEBUG
                  // null if the connection monitor is off
                  if(s_RequestList == null)
                      return;

                  lock(s_RequestList) {
                      Assert(s_RequestList.ContainsKey(request), "!s_RequestList.ContainsKey(request)|A HttpWebRequest should not be removed twice.");

                      try {
                          s_RequestList.Remove(request);
                      } catch {
                      }
                  }
      #endif
              }
              */
        /*
      [System.Diagnostics.Conditional("DEBUG")]
      internal static void DebugUpdateRequest(HttpWebRequest request, Connection connection, int flags) {
#if DEBUG
          // null if the connection monitor is off
          if(s_RequestList == null)
              return;

          lock(s_RequestList) {
              if(!s_RequestList.ContainsKey(request)) {
                  return;
              }

              ConnectionMonitorEntry requestEntry =
                  new ConnectionMonitorEntry(request, connection, flags);

              try {
                  s_RequestList.Remove(request);
                  s_RequestList.Add(request, requestEntry);
              } catch {
              }
          }
#endif
      }*/
    }
}
