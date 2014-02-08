//------------------------------------------------------------------------------
// <copyright file="Logging.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Microsoft.AspNet.Security.Windows
{
    internal class Logging
    {
        private const string AttributeNameMaxSize = "maxdatasize";
        private const string AttributeNameTraceMode = "tracemode";
        private const string AttributeValueProtocolOnly = "protocolonly";
        // private const string AttributeValueIncludeHex = "includehex";

        private const int DefaultMaxDumpSize = 1024;
        private const bool DefaultUseProtocolTextOnly = false;

        private const string TraceSourceWebName = "System.Net";
        private const string TraceSourceHttpListenerName = "System.Net.HttpListener";

        private static readonly string[] SupportedAttributes = new string[] { AttributeNameMaxSize, AttributeNameTraceMode };

        private static volatile bool s_LoggingEnabled = true;
        private static volatile bool s_LoggingInitialized;
        private static volatile bool s_AppDomainShutdown;

        private static TraceSource s_WebTraceSource;
        private static TraceSource s_HttpListenerTraceSource;
        
        private static object s_InternalSyncObject;

        private Logging()
        {
        }

        private static object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        internal static bool On
        {
            get
            {
                if (!s_LoggingInitialized)
                {
                    InitializeLogging();
                }
                return s_LoggingEnabled;
            }
        }

        internal static bool IsVerbose(TraceSource traceSource)
        {
            return ValidateSettings(traceSource, TraceEventType.Verbose);
        }

        internal static TraceSource Web
        {
            get
            {
                if (!s_LoggingInitialized)
                {
                    InitializeLogging();
                }
                if (!s_LoggingEnabled)
                {
                    return null;
                }
                return s_WebTraceSource;
            }
        }

        internal static TraceSource HttpListener
        {
            get
            {
                if (!s_LoggingInitialized)
                {
                    InitializeLogging();
                }
                if (!s_LoggingEnabled)
                {
                    return null;
                }
                return s_HttpListenerTraceSource;
            }
        }

        private static bool GetUseProtocolTextSetting(TraceSource traceSource)
        {
            bool useProtocolTextOnly = DefaultUseProtocolTextOnly;
            if (traceSource.Attributes[AttributeNameTraceMode] == AttributeValueProtocolOnly)
            {
                useProtocolTextOnly = true;
            }
            return useProtocolTextOnly;
        }

        private static int GetMaxDumpSizeSetting(TraceSource traceSource)
        {
            int maxDumpSize = DefaultMaxDumpSize;
            if (traceSource.Attributes.ContainsKey(AttributeNameMaxSize))
            {
                try
                {
                    maxDumpSize = Int32.Parse(traceSource.Attributes[AttributeNameMaxSize], NumberFormatInfo.InvariantInfo);
                }
                catch (Exception exception)
                {
                    if (exception is ThreadAbortException || exception is StackOverflowException || exception is OutOfMemoryException)
                    {
                        throw;
                    }
                    traceSource.Attributes[AttributeNameMaxSize] = maxDumpSize.ToString(NumberFormatInfo.InvariantInfo);
                }
            }
            return maxDumpSize;
        }

        /// <devdoc>
        ///    <para>Sets up internal config settings for logging. (MUST be called under critsec) </para>
        /// </devdoc>
        private static void InitializeLogging()
        {
            lock (InternalSyncObject)
            {
                if (!s_LoggingInitialized)
                {
                    bool loggingEnabled = false;
                    s_WebTraceSource = new NclTraceSource(TraceSourceWebName);
                    s_HttpListenerTraceSource = new NclTraceSource(TraceSourceHttpListenerName);

                    GlobalLog.Print("Initalizating tracing");

                    try
                    {
                        loggingEnabled = (s_WebTraceSource.Switch.ShouldTrace(TraceEventType.Critical) ||
                                          s_HttpListenerTraceSource.Switch.ShouldTrace(TraceEventType.Critical));
                    }
                    catch (SecurityException)
                    {
                        // These may throw if the caller does not have permission to hook up trace listeners.
                        // We treat this case as though logging were disabled.
                        Close();
                        loggingEnabled = false;
                    }
                    if (loggingEnabled)
                    {
                        AppDomain currentDomain = AppDomain.CurrentDomain;
                        currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
                        currentDomain.DomainUnload += new EventHandler(AppDomainUnloadEvent);
                        currentDomain.ProcessExit += new EventHandler(ProcessExitEvent);
                    }
                    s_LoggingEnabled = loggingEnabled;
                    s_LoggingInitialized = true;
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Logging functions must work in partial trust mode")]
        private static void Close()
        {
            if (s_WebTraceSource != null)
            {
                s_WebTraceSource.Close();
            }
            if (s_HttpListenerTraceSource != null)
            {
                s_HttpListenerTraceSource.Close();
            }
        }

        /// <devdoc>
        ///    <para>Logs any unhandled exception through this event handler</para>
        /// </devdoc>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Exception(Web, sender, "UnhandledExceptionHandler", e);
        }

        private static void ProcessExitEvent(object sender, EventArgs e)
        {
            Close();
            s_AppDomainShutdown = true;
        }

        /// <devdoc>
        ///    <para>Called when the system is shutting down, used to prevent additional logging post-shutdown</para>
        /// </devdoc>
        private static void AppDomainUnloadEvent(object sender, EventArgs e)
        {
            Close();
            s_AppDomainShutdown = true;
        }

        /// <devdoc>
        ///    <para>Confirms logging is enabled, given current logging settings</para>
        /// </devdoc>
        private static bool ValidateSettings(TraceSource traceSource, TraceEventType traceLevel)
        {
            if (!s_LoggingEnabled)
            {
                return false;
            }
            if (!s_LoggingInitialized)
            {
                InitializeLogging();
            }
            if (traceSource == null || !traceSource.Switch.ShouldTrace(traceLevel))
            {
                return false;
            }
            if (s_AppDomainShutdown)
            {
                return false;
            }
            return true;
        }

        /// <devdoc>
        ///    <para>Converts an object to a normalized string that can be printed
        ///         takes System.Net.ObjectNamedFoo and coverts to ObjectNamedFoo, 
        ///         except IPAddress, IPEndPoint, and Uri, which return ToString()
        ///         </para>
        /// </devdoc>
        private static string GetObjectName(object obj)
        {
            if (obj is Uri || obj is System.Net.IPAddress || obj is System.Net.IPEndPoint)
            {
                return obj.ToString();
            }
            else
            {
                return obj.GetType().Name;
            }
        }

        internal static uint GetThreadId()
        {
            uint threadId = UnsafeNclNativeMethods.GetCurrentThreadId();
            if (threadId == 0)
            {
                threadId = (uint)Thread.CurrentThread.GetHashCode();
            }
            return threadId;
        }

        internal static void PrintLine(TraceSource traceSource, TraceEventType eventType, int id, string msg)
        {
            string logHeader = "[" + GetThreadId().ToString("d4", CultureInfo.InvariantCulture) + "] ";
            traceSource.TraceEvent(eventType, id, logHeader + msg);
        }

        /// <devdoc>
        ///    <para>Indicates that two objects are getting used with one another</para>
        /// </devdoc>
        internal static void Associate(TraceSource traceSource, object objA, object objB)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }

            string lineA = GetObjectName(objA) + "#" + ValidationHelper.HashString(objA);
            string lineB = GetObjectName(objB) + "#" + ValidationHelper.HashString(objB);

            PrintLine(traceSource, TraceEventType.Information, 0, "Associating " + lineA + " with " + lineB);
        }

        /// <devdoc>
        ///    <para>Logs entrance to a function</para>
        /// </devdoc>
        internal static void Enter(TraceSource traceSource, object obj, string method, string param)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            Enter(traceSource, GetObjectName(obj) + "#" + ValidationHelper.HashString(obj), method, param);
        }

        /// <devdoc>
        ///    <para>Logs entrance to a function</para>
        /// </devdoc>
        internal static void Enter(TraceSource traceSource, object obj, string method, object paramObject)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            Enter(traceSource, GetObjectName(obj) + "#" + ValidationHelper.HashString(obj), method, paramObject);
        }

        /// <devdoc>
        ///    <para>Logs entrance to a function</para>
        /// </devdoc>
        internal static void Enter(TraceSource traceSource, string obj, string method, string param)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            Enter(traceSource, obj + "::" + method + "(" + param + ")");
        }

        /// <devdoc>
        ///    <para>Logs entrance to a function</para>
        /// </devdoc>
        internal static void Enter(TraceSource traceSource, string obj, string method, object paramObject)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            string paramObjectValue = string.Empty;
            if (paramObject != null)
            {
                paramObjectValue = GetObjectName(paramObject) + "#" + ValidationHelper.HashString(paramObject);
            }
            Enter(traceSource, obj + "::" + method + "(" + paramObjectValue + ")");
        }

        /// <devdoc>
        ///    <para>Logs entrance to a function, indents and points that out</para>
        /// </devdoc>
        internal static void Enter(TraceSource traceSource, string method, string parameters)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            Enter(traceSource, method + "(" + parameters + ")");
        }

        /// <devdoc>
        ///    <para>Logs entrance to a function, indents and points that out</para>
        /// </devdoc>
        internal static void Enter(TraceSource traceSource, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            // Trace.CorrelationManager.StartLogicalOperation();
            PrintLine(traceSource, TraceEventType.Verbose, 0, msg);
        }

        /// <devdoc>
        ///    <para>Logs exit from a function</para>
        /// </devdoc>
        internal static void Exit(TraceSource traceSource, object obj, string method, object retObject)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            string retValue = string.Empty;
            if (retObject != null)
            {
                retValue = GetObjectName(retObject) + "#" + ValidationHelper.HashString(retObject);
            }
            Exit(traceSource, obj, method, retValue);
        }

        /// <devdoc>
        ///    <para>Logs exit from a function</para>
        /// </devdoc>
        internal static void Exit(TraceSource traceSource, string obj, string method, object retObject)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            string retValue = string.Empty;
            if (retObject != null)
            {
                retValue = GetObjectName(retObject) + "#" + ValidationHelper.HashString(retObject);
            }
            Exit(traceSource, obj, method, retValue);
        }

        /// <devdoc>
        ///    <para>Logs exit from a function</para>
        /// </devdoc>
        internal static void Exit(TraceSource traceSource, object obj, string method, string retValue)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            Exit(traceSource, GetObjectName(obj) + "#" + ValidationHelper.HashString(obj), method, retValue);
        }

        /// <devdoc>
        ///    <para>Logs exit from a function</para>
        /// </devdoc>
        internal static void Exit(TraceSource traceSource, string obj, string method, string retValue)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            if (!string.IsNullOrEmpty(retValue))
            {
                retValue = "\t-> " + retValue;
            }
            Exit(traceSource, obj + "::" + method + "() " + retValue);
        }

        /// <devdoc>
        ///    <para>Logs exit from a function</para>
        /// </devdoc>
        internal static void Exit(TraceSource traceSource, string method, string parameters)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            Exit(traceSource, method + "() " + parameters);
        }

        /// <devdoc>
        ///    <para>Logs exit from a function</para>
        /// </devdoc>
        internal static void Exit(TraceSource traceSource, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Verbose, 0, "Exiting " + msg);
            // Trace.CorrelationManager.StopLogicalOperation();
        }

        /// <devdoc>
        ///    <para>Logs Exception, restores indenting</para>
        /// </devdoc>
        internal static void Exception(TraceSource traceSource, object obj, string method, Exception e)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Error))
            {
                return;
            }

            string infoLine = SR.GetString(SR.net_log_exception, GetObjectLogHash(obj), method, e.Message);
            if (!string.IsNullOrEmpty(e.StackTrace))
            {
                infoLine += "\r\n" + e.StackTrace;
            }
            PrintLine(traceSource, TraceEventType.Error, 0, infoLine);
        }

        /// <devdoc>
        ///    <para>Logs an Info line</para>
        /// </devdoc>
        internal static void PrintInfo(TraceSource traceSource, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Information, 0, msg);
        }

        /// <devdoc>
        ///    <para>Logs an Info line</para>
        /// </devdoc>
        internal static void PrintInfo(TraceSource traceSource, object obj, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Information, 0,
                                   GetObjectName(obj) + "#" + ValidationHelper.HashString(obj)
                                   + " - " + msg);
        }

        /// <devdoc>
        ///    <para>Logs an Info line</para>
        /// </devdoc>
        internal static void PrintInfo(TraceSource traceSource, object obj, string method, string param)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Information))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Information, 0,
                                   GetObjectName(obj) + "#" + ValidationHelper.HashString(obj)
                                   + "::" + method + "(" + param + ")");
        }

        /// <devdoc>
        ///    <para>Logs a Warning line</para>
        /// </devdoc>
        internal static void PrintWarning(TraceSource traceSource, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Warning))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Warning, 0, msg);
        }

        /// <devdoc>
        ///    <para>Logs a Warning line</para>
        /// </devdoc>
        internal static void PrintWarning(TraceSource traceSource, object obj, string method, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Warning))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Warning, 0,
                                   GetObjectName(obj) + "#" + ValidationHelper.HashString(obj)
                                   + "::" + method + "() - " + msg);
        }

        /// <devdoc>
        ///    <para>Logs an Error line</para>
        /// </devdoc>
        internal static void PrintError(TraceSource traceSource, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Error))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Error, 0, msg);
        }

        /// <devdoc>
        ///    <para>Logs an Error line</para>
        /// </devdoc>
        internal static void PrintError(TraceSource traceSource, object obj, string method, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Error))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Error, 0,
                                   GetObjectName(obj) + "#" + ValidationHelper.HashString(obj)
                                   + "::" + method + "() - " + msg);
        }

        internal static string GetObjectLogHash(object obj)
        {
            return GetObjectName(obj) + "#" + ValidationHelper.HashString(obj);
        }

        /// <devdoc>
        ///    <para>Marhsalls a buffer ptr to an array and then dumps the byte array to the log</para>
        /// </devdoc>
        internal static void Dump(TraceSource traceSource, object obj, string method, IntPtr bufferPtr, int length)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Verbose) || bufferPtr == IntPtr.Zero || length < 0)
            {
                return;
            }
            byte[] buffer = new byte[length];
            Marshal.Copy(bufferPtr, buffer, 0, length);
            Dump(traceSource, obj, method, buffer, 0, length);
        }

        /// <devdoc>
        ///    <para>Dumps a byte array to the log</para>
        /// </devdoc>
        internal static void Dump(TraceSource traceSource, object obj, string method, byte[] buffer, int offset, int length)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Verbose))
            {
                return;
            }
            if (buffer == null)
            {
                PrintLine(traceSource, TraceEventType.Verbose, 0, "(null)");
                return;
            }
            if (offset > buffer.Length)
            {
                PrintLine(traceSource, TraceEventType.Verbose, 0, "(offset out of range)");
                return;
            }
            PrintLine(traceSource, TraceEventType.Verbose, 0, "Data from " + GetObjectName(obj) + "#" + ValidationHelper.HashString(obj) + "::" + method);
            int maxDumpSize = GetMaxDumpSizeSetting(traceSource);
            if (length > maxDumpSize)
            {
                PrintLine(traceSource, TraceEventType.Verbose, 0, "(printing " + maxDumpSize.ToString(NumberFormatInfo.InvariantInfo) + " out of " + length.ToString(NumberFormatInfo.InvariantInfo) + ")");
                length = maxDumpSize;
            }
            if ((length < 0) || (length > buffer.Length - offset))
            {
                length = buffer.Length - offset;
            }
            if (GetUseProtocolTextSetting(traceSource))
            {
                string output = "<<" + HeaderEncoding.GetString(buffer, offset, length) + ">>";
                PrintLine(traceSource, TraceEventType.Verbose, 0, output);
                return;
            }
            do
            {
                int n = Math.Min(length, 16);
                string disp = String.Format(CultureInfo.CurrentCulture, "{0:X8} : ", offset);
                for (int i = 0; i < n; ++i)
                {
                    disp += String.Format(CultureInfo.CurrentCulture, "{0:X2}", buffer[offset + i]) + ((i == 7) ? '-' : ' ');
                }
                for (int i = n; i < 16; ++i)
                {
                    disp += "   ";
                }
                disp += ": ";
                for (int i = 0; i < n; ++i)
                {
                    disp += ((buffer[offset + i] < 0x20) || (buffer[offset + i] > 0x7e))
                                ? '.'
                                : (char)(buffer[offset + i]);
                }
                PrintLine(traceSource, TraceEventType.Verbose, 0, disp);
                offset += n;
                length -= n;
            }
            while (length > 0);
        }

        private class NclTraceSource : TraceSource
        {
            internal NclTraceSource(string name) : base(name)
            {
            }
            /*
            protected internal override string[] GetSupportedAttributes()
            {
                return Logging.SupportedAttributes;
            }*/
        }
    }
}
