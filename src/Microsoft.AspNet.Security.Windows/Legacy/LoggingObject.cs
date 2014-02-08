//------------------------------------------------------------------------------
// <copyright file="_LoggingObject.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

// We have function based stack and thread based logging of basic behavior.  We
//  also now have the ability to run a "watch thread" which does basic hang detection
//  and error-event based logging.   The logging code buffers the callstack/picture
//  of all COMNET threads, and upon error from an assert or a hang, it will open a file
//  and dump the snapsnot.  Future work will allow this to be configed by registry and
//  to use Runtime based logging.  We'd also like to have different levels of logging.

namespace Microsoft.AspNet.Security.Windows
{
    using System;

    // BaseLoggingObject - used to disable logging,
    //  this is just a base class that does nothing.
    
    [Flags]
    internal enum ThreadKinds
    {
        Unknown = 0x0000,

        // Mutually exclusive.
        User = 0x0001,     // Thread has entered via an API.
        System = 0x0002,     // Thread has entered via a system callback (e.g. completion port) or is our own thread.

        // Mutually exclusive.
        Sync = 0x0004,     // Thread should block.
        Async = 0x0008,     // Thread should not block.

        // Mutually exclusive, not always known for a user thread.  Never changes.
        Timer = 0x0010,     // Thread is the timer thread.  (Can't call user code.)
        CompletionPort = 0x0020,     // Thread is a ThreadPool completion-port thread.
        Worker = 0x0040,     // Thread is a ThreadPool worker thread.
        Finalization = 0x0080,     // Thread is the finalization thread.
        Other = 0x0100,     // Unknown source.

        OwnerMask = User | System,
        SyncMask = Sync | Async,
        SourceMask = Timer | CompletionPort | Worker | Finalization | Other,

        // Useful "macros"
        SafeSources = SourceMask & ~(Timer | Finalization),  // Methods that "unsafe" sources can call must be explicitly marked.
        ThreadPool = CompletionPort | Worker,               // Like Thread.CurrentThread.IsThreadPoolThread
    }

    internal class BaseLoggingObject
    {
        internal BaseLoggingObject()
        {
        }

        internal virtual void EnterFunc(string funcname)
        {
        }

        internal virtual void LeaveFunc(string funcname)
        {
        }

        internal virtual void DumpArrayToConsole()
        {
        }

        internal virtual void PrintLine(string msg)
        {
        }

        internal virtual void DumpArray(bool shouldClose)
        {
        }

        internal virtual void DumpArrayToFile(bool shouldClose)
        {
        }

        internal virtual void Flush()
        {
        }

        internal virtual void Flush(bool close)
        {
        }

        internal virtual void LoggingMonitorTick()
        {
        }

        internal virtual void Dump(byte[] buffer)
        {
        }

        internal virtual void Dump(byte[] buffer, int length)
        {
        }

        internal virtual void Dump(byte[] buffer, int offset, int length)
        {
        }

        internal virtual void Dump(IntPtr pBuffer, int offset, int length)
        {
        }
    } // class BaseLoggingObject

#if TRAVE
    /// <internalonly/>
    /// <devdoc>
    /// </devdoc>
    internal class LoggingObject : BaseLoggingObject {
        public ArrayList _Logarray;
        private Hashtable _ThreadNesting;
        private int _AddCount;
        private StreamWriter _Stream;
        private int _IamAlive;
        private int _LastIamAlive;
        private bool _Finalized = false;
        private double _NanosecondsPerTick;
        private int _StartMilliseconds;
        private long _StartTicks;

        internal LoggingObject() : base() {
            _Logarray      = new ArrayList();
            _ThreadNesting = new Hashtable();
            _AddCount      = 0;
            _IamAlive      = 0;
            _LastIamAlive  = -1;

            if (GlobalLog.s_UsePerfCounter) {
                long ticksPerSecond;
                SafeNativeMethods.QueryPerformanceFrequency(out ticksPerSecond);
                _NanosecondsPerTick = 10000000.0/(double)ticksPerSecond;
                SafeNativeMethods.QueryPerformanceCounter(out _StartTicks);
            } else {
                _StartMilliseconds = Environment.TickCount;
            }
        }

        //
        // LoggingMonitorTick - this function is run from the monitor thread,
        //  and used to check to see if there any hangs, ie no logging
        //  activitity
        //

        internal override void LoggingMonitorTick() {
            if ( _LastIamAlive == _IamAlive ) {
                PrintLine("================= Error TIMEOUT - HANG DETECTED =================");
                DumpArray(true);
            }
            _LastIamAlive = _IamAlive;
        }

        internal override void EnterFunc(string funcname) {
            if (_Finalized) {
                return;
            }
            IncNestingCount();
            ValidatePush(funcname);
            PrintLine(funcname);
        }

        internal override void LeaveFunc(string funcname) {
            if (_Finalized) {
                return;
            }
            PrintLine(funcname);
            DecNestingCount();
            ValidatePop(funcname);
        }

        internal override void DumpArrayToConsole() {
            for (int i=0; i < _Logarray.Count; i++) {
                Console.WriteLine((string) _Logarray[i]);
            }
        }

        internal override void PrintLine(string msg) {
            if (_Finalized) {
                return;
            }
            string spc = "";

            _IamAlive++;

            spc = GetNestingString();

            string tickString = "";

            if (GlobalLog.s_UsePerfCounter) {
                long nowTicks;
                SafeNativeMethods.QueryPerformanceCounter(out nowTicks);
                if (_StartTicks>nowTicks) { // counter reset, restart from 0
                    _StartTicks = nowTicks;
                }
                nowTicks -= _StartTicks;
                if (GlobalLog.s_UseTimeSpan) {
                    tickString = new TimeSpan((long)(nowTicks*_NanosecondsPerTick)).ToString();
                    // note: TimeSpan().ToString() doesn't return the uSec part
                    // if its 0. .ToString() returns [H*]HH:MM:SS:uuuuuuu, hence 16
                    if (tickString.Length < 16) {
                        tickString += ".0000000";
                    }
                }
                else {
                    tickString = ((double)nowTicks*_NanosecondsPerTick/10000).ToString("f3");
                }
            }
            else {
                int nowMilliseconds = Environment.TickCount;
                if (_StartMilliseconds>nowMilliseconds) {
                    _StartMilliseconds = nowMilliseconds;
                }
                nowMilliseconds -= _StartMilliseconds;
                if (GlobalLog.s_UseTimeSpan) {
                    tickString = new TimeSpan(nowMilliseconds*10000).ToString();
                    // note: TimeSpan().ToString() doesn't return the uSec part
                    // if its 0. .ToString() returns [H*]HH:MM:SS:uuuuuuu, hence 16
                    if (tickString.Length < 16) {
                        tickString += ".0000000";
                    }
                }
                else {
                    tickString = nowMilliseconds.ToString();
                }
            }

            uint threadId = 0;

            if (GlobalLog.s_UseThreadId) {
                try {
                    object threadData = Thread.GetData(GlobalLog.s_ThreadIdSlot);
                    if (threadData!= null) {
                        threadId = (uint)threadData;
                    }

                }
                catch(Exception exception) {
                    if (exception is ThreadAbortException || exception is StackOverflowException || exception is OutOfMemoryException) {
                        throw;
                    }
                }
                if (threadId == 0) {
                    threadId = UnsafeNclNativeMethods.GetCurrentThreadId();
                    Thread.SetData(GlobalLog.s_ThreadIdSlot, threadId);
                }
            }
            if (threadId == 0) {
                threadId = (uint)Thread.CurrentThread.GetHashCode();
            }

            string str = "[" + threadId.ToString("x8") + "]"  + " (" +tickString+  ") " + spc + msg;

            lock(this) {
                _AddCount++;
                _Logarray.Add(str);
                int MaxLines = GlobalLog.s_DumpToConsole ? 0 : GlobalLog.MaxLinesBeforeSave;
                if (_AddCount > MaxLines) {
                    _AddCount = 0;
                    DumpArray(false);
                    _Logarray = new ArrayList();
                }
            }
        }

        internal override void DumpArray(bool shouldClose) {
            if ( GlobalLog.s_DumpToConsole ) {
                DumpArrayToConsole();
            } else {
                DumpArrayToFile(shouldClose);
            }
        }

        internal unsafe override void Dump(byte[] buffer, int offset, int length) {
            //if (!GlobalLog.s_DumpWebData) {
            //    return;
            //}
            if (buffer==null) {
                PrintLine("(null)");
                return;
            }
            if (offset > buffer.Length) {
                PrintLine("(offset out of range)");
                return;
            }
            if (length > GlobalLog.s_MaxDumpSize) {
                PrintLine("(printing " + GlobalLog.s_MaxDumpSize.ToString() + " out of " + length.ToString() + ")");
                length = GlobalLog.s_MaxDumpSize;
            }
            if ((length < 0) || (length > buffer.Length - offset)) {
                length = buffer.Length - offset;
            }
            fixed (byte* pBuffer = buffer) {
                Dump((IntPtr)pBuffer, offset, length);
            }
        }

        internal unsafe override void Dump(IntPtr pBuffer, int offset, int length) {
            //if (!GlobalLog.s_DumpWebData) {
            //    return;
            //}
            if (pBuffer==IntPtr.Zero || length<0) {
                PrintLine("(null)");
                return;
            }
            if (length > GlobalLog.s_MaxDumpSize) {
                PrintLine("(printing " + GlobalLog.s_MaxDumpSize.ToString() + " out of " + length.ToString() + ")");
                length = GlobalLog.s_MaxDumpSize;
            }
            byte* buffer = (byte*)pBuffer + offset;
            Dump(buffer, length);
        }

        unsafe void Dump(byte* buffer, int length) {
            do {
                int offset = 0;
                int n = Math.Min(length, 16);
                string disp = ((IntPtr)buffer).ToString("X8") + " : " + offset.ToString("X8") + " : ";
                byte current;
                for (int i = 0; i < n; ++i) {
                    current = *(buffer + i);
                    disp += current.ToString("X2") + ((i == 7) ? '-' : ' ');
                }
                for (int i = n; i < 16; ++i) {
                    disp += "   ";
                }
                disp += ": ";
                for (int i = 0; i < n; ++i) {
                    current = *(buffer + i);
                    disp += ((current < 0x20) || (current > 0x7e)) ? '.' : (char)current;
                }
                PrintLine(disp);
                offset += n;
                buffer += n;
                length -= n;
            } while (length > 0);
        }

        // SECURITY: This is dev-debugging class and we need some permissions
        // to use it under trust-restricted environment as well.
        [PermissionSet(SecurityAction.Assert, Name="FullTrust")]
        internal override void DumpArrayToFile(bool shouldClose) {
            lock (this) {
                if (!shouldClose) {
                    if (_Stream==null) {
                        string mainLogFileRoot = GlobalLog.s_RootDirectory + "System.Net";
                        string mainLogFile = mainLogFileRoot;
                        for (int k=0; k<20; k++) {
                            if (k>0) {
                                mainLogFile = mainLogFileRoot + "." + k.ToString();
                            }
                            string fileName = mainLogFile + ".log";
                            if (!File.Exists(fileName)) {
                                try {
                                    _Stream = new StreamWriter(fileName);
                                    break;
                                }
                                catch (Exception exception) {
                                    if (exception is ThreadAbortException || exception is StackOverflowException || exception is OutOfMemoryException) {
                                        throw;
                                    }
                                    if (exception is SecurityException || exception is UnauthorizedAccessException) {
                                        // can't be CAS (we assert) this is an ACL issue
                                        break;
                                    }
                                }
                            }
                        }
                        if (_Stream==null) {
                            _Stream = StreamWriter.Null;
                        }
                        // write a header with information about the Process and the AppDomain
                        _Stream.Write("# MachineName: " + Environment.MachineName + "\r\n");
                        _Stream.Write("# ProcessName: " + Process.GetCurrentProcess().ProcessName + " (pid: " + Process.GetCurrentProcess().Id + ")\r\n");
                        _Stream.Write("# AppDomainId: " + AppDomain.CurrentDomain.Id + "\r\n");
                        _Stream.Write("# CurrentIdentity: " + WindowsIdentity.GetCurrent().Name + "\r\n");
                        _Stream.Write("# CommandLine: " + Environment.CommandLine + "\r\n");
                        _Stream.Write("# ClrVersion: " + Environment.Version + "\r\n");
                        _Stream.Write("# CreationDate: " + DateTime.Now.ToString("g") + "\r\n");
                    }
                }
                try {
                    if (_Logarray!=null) {
                        for (int i=0; i<_Logarray.Count; i++) {
                            _Stream.Write((string)_Logarray[i]);
                            _Stream.Write("\r\n");
                        }

                        if (_Logarray.Count > 0 && _Stream != null)
                            _Stream.Flush();
                    }
                }
                catch (Exception exception) {
                    if (exception is ThreadAbortException || exception is StackOverflowException || exception is OutOfMemoryException) {
                        throw;
                    }
                }
                if (shouldClose && _Stream!=null) {
                    try {
                        _Stream.Close();
                    }
                    catch (ObjectDisposedException) { }
                    _Stream = null;
                }
            }
        }

        internal override void Flush() {
            Flush(false);
        }

        internal override void Flush(bool close) {
            lock (this) {
                if (!GlobalLog.s_DumpToConsole) {
                    DumpArrayToFile(close);
                    _AddCount = 0;
                }
            }
        }

        private class ThreadInfoData {
            public ThreadInfoData(string indent) {
                Indent = indent;
                NestingStack = new Stack();
            }
            public string Indent;
            public Stack NestingStack;
        };

        string IndentString {
            get {
                string indent = " ";
                Object obj = _ThreadNesting[Thread.CurrentThread.GetHashCode()];
                if (!GlobalLog.s_DebugCallNesting) {
                    if (obj == null) {
                        _ThreadNesting[Thread.CurrentThread.GetHashCode()] = indent;
                    } else {
                        indent = (String) obj;
                    }
                } else {
                    ThreadInfoData threadInfo = obj as ThreadInfoData;
                    if (threadInfo == null) {
                        threadInfo = new ThreadInfoData(indent);
                        _ThreadNesting[Thread.CurrentThread.GetHashCode()] = threadInfo;
                    }
                    indent = threadInfo.Indent;
                }
                return indent;
            }
            set {
                Object obj = _ThreadNesting[Thread.CurrentThread.GetHashCode()];
                if (obj == null) {
                    return;
                }
                if (!GlobalLog.s_DebugCallNesting) {
                    _ThreadNesting[Thread.CurrentThread.GetHashCode()] = value;
                } else {
                    ThreadInfoData threadInfo = obj as ThreadInfoData;
                    if (threadInfo == null) {
                        threadInfo = new ThreadInfoData(value);
                        _ThreadNesting[Thread.CurrentThread.GetHashCode()] = threadInfo;
                    }
                    threadInfo.Indent = value;
                }
            }
        }

        [System.Diagnostics.Conditional("TRAVE")]
        private void IncNestingCount() {
            IndentString = IndentString + " ";
        }

        [System.Diagnostics.Conditional("TRAVE")]
        private void DecNestingCount() {
            string indent = IndentString;
            if (indent.Length>1) {
                try {
                    indent = indent.Substring(1);
                }
                catch {
                    indent = string.Empty;
                }
            }
            if (indent.Length==0) {
                indent = "< ";
            }
            IndentString = indent;
        }

        private string GetNestingString() {
            return IndentString;
        }

        [System.Diagnostics.Conditional("TRAVE")]
        private void ValidatePush(string name) {
            if (GlobalLog.s_DebugCallNesting) {
                Object obj = _ThreadNesting[Thread.CurrentThread.GetHashCode()];
                ThreadInfoData threadInfo = obj as ThreadInfoData;
                if (threadInfo == null) {
                    return;
                }
                threadInfo.NestingStack.Push(name);
            }
        }

        [System.Diagnostics.Conditional("TRAVE")]
        private void ValidatePop(string name) {
            if (GlobalLog.s_DebugCallNesting) {
                try {
                    Object obj = _ThreadNesting[Thread.CurrentThread.GetHashCode()];
                    ThreadInfoData threadInfo = obj as ThreadInfoData;
                    if (threadInfo == null) {
                        return;
                    }
                    if (threadInfo.NestingStack.Count == 0) {
                        PrintLine("++++====" + "Poped Empty Stack for :"+name);
                    }
                    string popedName = (string) threadInfo.NestingStack.Pop();
                    string [] parsedList = popedName.Split(new char [] {'(',')',' ','.',':',',','#'});
                    foreach (string element in parsedList) {
                        if (element != null && element.Length > 1 && name.IndexOf(element) != -1) {
                            return;
                        }
                    }
                    PrintLine("++++====" + "Expected:" + popedName + ": got :" + name + ": StackSize:"+threadInfo.NestingStack.Count);
                    // relevel the stack
                    while(threadInfo.NestingStack.Count>0) {
                        string popedName2 = (string) threadInfo.NestingStack.Pop();
                        string [] parsedList2 = popedName2.Split(new char [] {'(',')',' ','.',':',',','#'});
                        foreach (string element2 in parsedList2) {
                            if (element2 != null && element2.Length > 1 && name.IndexOf(element2) != -1) {
                                return;
                            }
                        }
                    }
                }
                catch {
                    PrintLine("++++====" + "ValidatePop failed for: "+name);
                }
            }
        }


        ~LoggingObject() {
            if(!_Finalized) {
                _Finalized = true;
                lock(this) {
                    DumpArray(true);
                }
            }
        }


    } // class LoggingObject

    internal static class TraveHelper {
        private static readonly string Hexizer = "0x{0:x}";
        internal static string ToHex(object value) {
            return String.Format(Hexizer, value);
        }
    }
#endif // TRAVE

#if TRAVE 
    internal class IntegerSwitch : BooleanSwitch {
        public IntegerSwitch(string switchName, string switchDescription) : base(switchName, switchDescription) {
        }
        public new int Value {
            get {
                return base.SwitchSetting;
            }
        }
    }

#endif

     // class GlobalLog
} // namespace System.Net
