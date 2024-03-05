// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static class LoggerEventIds
{
    public const int HttpSysListenerCtorError = 1;
    public const int BindingToDefault = 2;
    public const int ClearedPrefixes = 3;
    public const int AcceptErrorStopping = 4;
    public const int AcceptError = 5;
    public const int RequestProcessError = 6;
    public const int RequestsDrained = 7;
    public const int StopCancelled = 8;
    public const int WaitingForRequestsToDrain = 9;
    public const int DisconnectRegistrationError = 10;
    public const int RegisterDisconnectListener = 11;
    public const int UnknownDisconnectError = 12;
    public const int DisconnectHandlerError = 13;
    public const int ListenerStarting = 14;
    public const int ListenerDisposeError = 15;
    public const int RequestListenerProcessError = 16;
    public const int AttachedToQueue = 17;
    public const int SetUrlPropertyError = 18;
    public const int RegisteringPrefix = 19;
    public const int UnregisteringPrefix = 20;
    public const int CloseUrlGroupError = 21;
    public const int ChannelBindingUnsupported = 22;
    public const int ChannelBindingMissing = 23;
    public const int RequestError = 24;
    public const int ErrorInReadingCertificate = 25;
    public const int ChannelBindingNeedsHttps = 26;
    public const int ChannelBindingRetrieved = 27;
    public const int AbortError = 28;
    public const int ErrorWhileRead = 29;
    public const int ErrorWhenReadBegun = 30;
    public const int ErrorWhenReadAsync = 31;
    public const int ErrorWhenFlushAsync = 32;
    public const int FewerBytesThanExpected = 33;
    public const int WriteError = 34;
    public const int WriteErrorIgnored = 35;
    public const int WriteFlushCancelled = 36;
    public const int ClearedAddresses = 37;
    public const int FileSendAsyncError = 38;
    public const int FileSendAsyncCancelled = 39;
    public const int FileSendAsyncErrorIgnored = 40;
    public const int WriteCancelled = 41;
    public const int ListenerStopping = 42;
    public const int ListenerStartError = 43;
    public const int DisconnectTriggered = 44;
    public const int ListenerStopError = 45;
    public const int ListenerDisposing = 46;
    public const int RequestValidationFailed = 47;
    public const int CreateDisconnectTokenError = 48;
    public const int RequestAborted = 49;
    public const int AcceptSetResultFailed = 50;
    public const int AcceptSetExpectationMismatch = 51;
    public const int AcceptCancelExpectationMismatch = 52;
    public const int AcceptObserveExpectationMismatch = 53;
}
