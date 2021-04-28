using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static class LoggerEventIds
    {
        public static readonly EventId HttpSysListenerCtorError = new EventId(1, "HttpSysListenerCtorError");
        public static readonly EventId BindingToDefault = new EventId(2, "BindingToDefault");
        public static readonly EventId ClearedPrefixes = new EventId(3, "ClearedPrefixes");
        public static readonly EventId AcceptErrorStopping = new EventId(4, "AcceptErrorStopping");
        public static readonly EventId AcceptError = new EventId(5, "AcceptError");
        public static readonly EventId RequestProcessError = new EventId(6, "RequestProcessError");
        public static readonly EventId RequestsDrained = new EventId(7, "RequestsDrained");
        public static readonly EventId StopCancelled = new EventId(8, "StopCancelled");
        public static readonly EventId WaitingForRequestsToDrain = new EventId(9, "WaitingForRequestsToDrain");
        public static readonly EventId DisconnectRegistrationError = new EventId(10, "DisconnectRegistrationError");
        public static readonly EventId RegisterDisconnectListener = new EventId(11, "RegisterDisconnectListener");
        public static readonly EventId UnknownDisconnectError = new EventId(12, "UnknownDisconnectError");
        public static readonly EventId DisconnectHandlerError = new EventId(13, "DisconnectHandlerError");
        public static readonly EventId ListenerStarting = new EventId(14, "ListenerStarting");
        public static readonly EventId ListenerDisposeError = new EventId(15, "ListenerDisposeError");
        public static readonly EventId RequestListenerProcessError = new EventId(16, "RequestListenerProcessError");
        public static readonly EventId AttachedToQueue = new EventId(17, "AttachedToQueue");
        public static readonly EventId SetUrlPropertyError = new EventId(18, "SetUrlPropertyError");
        public static readonly EventId RegisteringPrefix = new EventId(19, "RegisteringPrefix");
        public static readonly EventId UnregisteringPrefix = new EventId(20, "UnregisteringPrefix");
        public static readonly EventId CloseUrlGroupError = new EventId(21, "CloseUrlGroupError");
        public static readonly EventId ChannelBindingUnsupported = new EventId(22, "ChannelBindingUnSupported");
        public static readonly EventId ChannelBindingMissing = new EventId(23, "ChannelBindingMissing");
        public static readonly EventId RequestError = new EventId(24, "RequestError");
        public static readonly EventId ErrorInReadingCertificate = new EventId(25, "ErrorInReadingCertificate");
        public static readonly EventId ChannelBindingNeedsHttps = new EventId(26, "ChannelBindingNeedsHttps");
        public static readonly EventId ChannelBindingRetrieved = new EventId(27, "ChannelBindingRetrived");
        public static readonly EventId AbortError = new EventId(28, "AbortError");
        public static readonly EventId ErrorWhileRead = new EventId(29, "ErrorWhileRead");
        public static readonly EventId ErrorWhenReadBegun = new EventId(30, "ErrorWhenReadBegun");
        public static readonly EventId ErrorWhenReadAsync = new EventId(31, "ErrorWhenReadAsync");
        public static readonly EventId ErrorWhenFlushAsync = new EventId(32, "ErrorWhenFlushAsync");
        public static readonly EventId FewerBytesThanExpected = new EventId(33, "FewerBytesThanExpected");
        public static readonly EventId WriteError = new EventId(34, "WriteError");
        public static readonly EventId WriteErrorIgnored = new EventId(35, "WriteFlushedIgnored");
        public static readonly EventId WriteFlushCancelled = new EventId(36, "WriteFlushCancelled");
        public static readonly EventId ClearedAddresses = new EventId(37, "ClearedAddresses");
        public static readonly EventId FileSendAsyncError = new EventId(38, "FileSendAsyncError");
        public static readonly EventId FileSendAsyncCancelled = new EventId(39, "FileSendAsyncCancelled");
        public static readonly EventId FileSendAsyncErrorIgnored = new EventId(40, "FileSendAsyncErrorIgnored");
        public static readonly EventId WriteCancelled = new EventId(41, "WriteCancelled");
        public static readonly EventId ListenerStopping = new EventId(42, "ListenerStopping");
        public static readonly EventId ListenerStartError = new EventId(43, "ListenerStartError");
        public static readonly EventId DisconnectTriggered = new EventId(44, "DisconnectTriggered");
        public static readonly EventId ListenerStopError = new EventId(45, "ListenerStopError");
        public static readonly EventId ListenerDisposing = new EventId(46, "ListenerDisposing");
        public static readonly EventId RequestValidationFailed = new EventId(47, "RequestValidationFailed");
        public static readonly EventId CreateDisconnectTokenError = new EventId(48, "CreateDisconnectTokenError");
    }
}
