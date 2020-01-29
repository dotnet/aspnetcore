using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static class LoggerEventIds
    {
        public static EventId HttpSysListenerCtorException = new EventId(1, "HttpSysListenerCtorException");
        public static EventId BindingToDefault = new EventId(2, "BindingToDefault");
        public static EventId ClearedPrefixes = new EventId(3, "ClearedPrefixes");
        public static EventId AcceptErrorStopping = new EventId(4, "AcceptErrorStopping");
        public static EventId AcceptError = new EventId(5, "AcceptError");
        public static EventId RequestProcessFailed = new EventId(6, "RequestProcessFailed");
        public static EventId RequestsDrained = new EventId(7, "RequestsDrained");
        public static EventId StopCancelled = new EventId(8, "StopCancelled");
        public static EventId WaitingForRequestsToDrain = new EventId(9, "WaitingForRequestsToDrain");
        public static EventId DisconnectRegistrationError = new EventId(10, "DisconnectRegistrationError");
        public static EventId RegisterDisconnectListener = new EventId(11, "RegisterDisconnectListener");
        public static EventId UnknownDisconnectError = new EventId(12, "UnknownDisconnectError");
        public static EventId DisconnectHandlerError = new EventId(13, "DisconnectHandlerError");
        public static EventId ListenerStarting = new EventId(14, "ListenerStarting");
        public static EventId ErrorInDispose = new EventId(15, "ErrorInDispose");
        public static EventId RequestListenerProcessFailed = new EventId(16, "RequestListenerProcessFailed");
        public static EventId AttachedToQueue = new EventId(17, "AttachedToQueue");
        public static EventId SetUrlPropertyFailed = new EventId(18, "SetUrlPropertyFailed");
        public static EventId RegisteringPrefix = new EventId(19, "RegisteringPrefix");
        public static EventId UnregisteringPrefix = new EventId(20, "UnregisteringPrefix");
        public static EventId CloseUrlGroupError = new EventId(21, "CloseUrlGroupError");
        public static EventId ChannelBindingUnSupported = new EventId(22, "ChannelBindingUnSupported");
        public static EventId ChannelBindingMissing = new EventId(23, "ChannelBindingMissing");
        public static EventId ListenerCtorException = new EventId(24, "ListenerCtorException");
        public static EventId ErrorInReadingCertificate = new EventId(25, "ErrorInReadingCertificate");
        public static EventId ChannelBindingNeedsHttps = new EventId(26, "ChannelBindingNeedsHttps");
        public static EventId ChannelBindingRetrived = new EventId(27, "ChannelBindingRetrived");
        public static EventId AbortException = new EventId(28, "AbortException");
        public static EventId ErrorWhileRead = new EventId(29, "ErrorWhileRead");
        public static EventId ErrorWhenReadBegun = new EventId(30, "ErrorWhenReadBegun");
        public static EventId ErrorWhenReadAsync = new EventId(31, "ErrorWhenReadAsync");
        public static EventId ErrorWhenFlushAsync = new EventId(32, "ErrorWhenFlushAsync");
        public static EventId LessBytesThenExpected = new EventId(33, "LessBytesThenExpected");
        public static EventId WriteError = new EventId(34, "WriteError");
        public static EventId WriteErrorIgnored = new EventId(35, "WriteFlushedIgnored");
        public static EventId WriteFlushCancelled = new EventId(36, "WriteFlushCancelled");
        public static EventId ClearedAddresses = new EventId(37, "ClearedAddresses");
        public static EventId FileSendAsyncFailed = new EventId(38, "FileSendAsyncFailed");
        public static EventId FileSendAsyncCancelled = new EventId(39, "FileSendAsyncCancelled");
        public static EventId FileSendAsyncErrorIgnored = new EventId(40, "FileSendAsyncAbortButDonotClose");
        public static EventId WriteCancelled = new EventId(41, "WriteCancelled");
        public static EventId ListenerStopping = new EventId(42, "ListenerStopping");
        public static EventId RequestFailed = new EventId(43, "RequestProcessFailed");
        public static EventId DisconnectTriggered = new EventId(44, "DisconnectTriggered");
        public static EventId ListenerStopError = new EventId(45, "ListenerStopError");
        public static EventId ListenerDisposing = new EventId(46, "ListenerDisposing");
        public static EventId ListenerStartException = new EventId(47, "ListenerStartException");
    
   

      
    }
}
