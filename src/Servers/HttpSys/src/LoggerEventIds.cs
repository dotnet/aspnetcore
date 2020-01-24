using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static class LoggerEventIds
    {
        public static EventId CtorException = new EventId(1, "CtorException");
        public static EventId BindingDefaulted = new EventId(2, "BindingDefaulted");
        public static EventId Cleared = new EventId(3, "Cleared");
        public static EventId Stopping = new EventId(4, "Stopping");
        public static EventId ErrorInListen = new EventId(5, "ErrorInListen");
        public static EventId RequestProcessFailed = new EventId(6, "RequestProcessFailed");
        public static EventId RequestEmptied = new EventId(7, "RequestEmptied");
        public static EventId RequestCancelled = new EventId(8, "RequestCancelled");
        public static EventId RequestStopped = new EventId(9, "RequestStopped");
        public static EventId TokenError = new EventId(10, "TokenError");
        public static EventId TokenDisconnect = new EventId(11, "TokenDisconnect");
        public static EventId WaitTokenClosed = new EventId(12, "WaitTokenClosed");
        public static EventId TokenDisconnectError= new EventId(13, "TokenDisconnectError");
        public static EventId TokenDisconnectTriggered = new EventId(13, "TokenDisconnectTriggered");
        public static EventId Started = new EventId(14, "Started");
        public static EventId ErrorInStopping = new EventId(1, "ErrorInStopping");
        public static EventId Disposed = new EventId(14, "Disposed");
        public static EventId ErrorInDispose = new EventId(15, "ErrorInDispose");
        public static EventId ContextException = new EventId(16, "ContextException");
        public static EventId AttachedToQueue = new EventId(17, "AttachedToQueue");
        public static EventId UrlGroupFailed = new EventId(18, "UrlGroupFailed");
        public static EventId PrefixListened = new EventId(19, "PrefixListened");
        public static EventId PrefixListenStopped = new EventId(20, "PrefixListenStopped");
        public static EventId V2ConfigCleanupFailed = new EventId(21, "V2ConfigCleanupFailed");
        public static EventId ChannelBindingUnSupported = new EventId(22, "ChannelBindingUnSupported");
        public static EventId ChannelBindingMissing = new EventId(23, "ChannelBindingMissing");
        public static EventId CryptoException = new EventId(24, "CryptoException");
        public static EventId ErrorInReadingCertificate = new EventId(25, "ErrorInReadingCertificate");
        public static EventId ChannelBindingNeedsHttps = new EventId(26, "ChannelBindingNeedsHttps");
        public static EventId ChannelBindingRetrived = new EventId(27, "ChannelBindingRetrived");
        public static EventId AbortException = new EventId(28, "AbortException");
        public static EventId ErrorWhileRead = new EventId(29, "ErrorWhileRead");
        public static EventId ErrorWhenReadBegun = new EventId(30, "ErrorWhenReadBegun");
        public static EventId ErrorWhenReadAsync = new EventId(31, "ErrorWhenReadAsync");
        public static EventId ErrorWhenFlushAsync = new EventId(32, "ErrorWhenFlushAsync");
        public static EventId LessBytesThenExpected = new EventId(33, "LessBytesThenExpected");
        public static EventId WriteFlushed = new EventId(34, "WriteFlushed");
        public static EventId WriteFlushedIgnored = new EventId(35, "WriteFlushedIgnored");
        public static EventId WriteFlushCancelled = new EventId(36, "WriteFlushCancelled");
        public static EventId AbortButDonotClose = new EventId(37, "AbortButDonotClose");
        public static EventId FileSendAsyncFailed = new EventId(38, "FileSendAsyncFailed");
        public static EventId FileSendAsyncCancelled = new EventId(39, "FileSendAsyncCancelled");
        public static EventId FileSendAsyncAbortButDonotClose = new EventId(40, "FileSendAsyncAbortButDonotClose");
        public static EventId IOCompletedCancelled = new EventId(41, "IOCompletedCancelled");
        public static EventId IOCompletedFailed= new EventId(42, "IOCompletedFailed");
        public static EventId IOCompletedFailQuiet = new EventId(43, "IOCompletedFailQuiet");


    }
}
