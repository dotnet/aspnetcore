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

//------------------------------------------------------------------------------
// <copyright file="WebSocketBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.WebSockets
{
    internal abstract class WebSocketBase : WebSocket, IDisposable
    {
        // private static volatile bool s_LoggingEnabled;

        private readonly OutstandingOperationHelper _closeOutstandingOperationHelper;
        private readonly OutstandingOperationHelper _closeOutputOutstandingOperationHelper;
        private readonly OutstandingOperationHelper _receiveOutstandingOperationHelper;
        private readonly OutstandingOperationHelper _sendOutstandingOperationHelper;
        private readonly Stream _innerStream;
        private readonly IWebSocketStream _innerStreamAsWebSocketStream;
        private readonly string _subProtocol;

        // We are not calling Dispose method on this object in Cleanup method to avoid a race condition while one thread is calling disposing on 
        // this object and another one is still using WaitAsync. According to Dev11 358715, this should be fine as long as we are not accessing the
        // AvailableWaitHandle on this SemaphoreSlim object.
        private readonly SemaphoreSlim _sendFrameThrottle;
        // locking m_ThisLock protects access to
        // - State
        // - m_CloseStack
        // - m_CloseAsyncStartedReceive
        // - m_CloseReceivedTaskCompletionSource
        // - m_CloseNetworkConnectionTask
        private readonly object _thisLock;
        private readonly WebSocketBuffer _internalBuffer;
        private readonly KeepAliveTracker _keepAliveTracker;

#if DEBUG
        private volatile string _closeStack;
#endif

        private volatile bool _cleanedUp;
        private volatile TaskCompletionSource<object> _closeReceivedTaskCompletionSource;
        private volatile Task _closeOutputTask;
        private volatile bool _isDisposed;
        private volatile Task _closeNetworkConnectionTask;
        private volatile bool _closeAsyncStartedReceive;
        private volatile WebSocketState _state;
        private volatile Task _keepAliveTask;
        private volatile WebSocketOperation.ReceiveOperation _receiveOperation;
        private volatile WebSocketOperation.SendOperation _sendOperation;
        private volatile WebSocketOperation.SendOperation _keepAliveOperation;
        private volatile WebSocketOperation.CloseOutputOperation _closeOutputOperation;
        private WebSocketCloseStatus? _closeStatus;
        private string _closeStatusDescription;
        private int _receiveState;
        private Exception _pendingException;

        protected WebSocketBase(Stream innerStream,
            string subProtocol,
            TimeSpan keepAliveInterval,
            WebSocketBuffer internalBuffer)
        {
            Contract.Assert(internalBuffer != null, "'internalBuffer' MUST NOT be NULL.");
            WebSocketHelpers.ValidateInnerStream(innerStream);
            WebSocketHelpers.ValidateOptions(subProtocol, internalBuffer.ReceiveBufferSize,
                internalBuffer.SendBufferSize, keepAliveInterval);

            // s_LoggingEnabled = Logging.On && Logging.WebSockets.Switch.ShouldTrace(TraceEventType.Critical);
            string parameters = string.Empty;
            /*
            if (s_LoggingEnabled)
            {
                parameters = string.Format(CultureInfo.InvariantCulture,
                    "ReceiveBufferSize: {0}, SendBufferSize: {1},  Protocols: {2}, KeepAliveInterval: {3}, innerStream: {4}, internalBuffer: {5}",
                    internalBuffer.ReceiveBufferSize,
                    internalBuffer.SendBufferSize,
                    subProtocol,
                    keepAliveInterval,
                    Logging.GetObjectLogHash(innerStream),
                    Logging.GetObjectLogHash(internalBuffer));

                Logging.Enter(Logging.WebSockets, this, Methods.Initialize, parameters);
            }
            */
            _thisLock = new object();

            try
            {
                _innerStream = innerStream;
                _internalBuffer = internalBuffer;
                /*if (s_LoggingEnabled)
                {
                    Logging.Associate(Logging.WebSockets, this, m_InnerStream);
                    Logging.Associate(Logging.WebSockets, this, m_InternalBuffer);
                }*/

                _closeOutstandingOperationHelper = new OutstandingOperationHelper();
                _closeOutputOutstandingOperationHelper = new OutstandingOperationHelper();
                _receiveOutstandingOperationHelper = new OutstandingOperationHelper();
                _sendOutstandingOperationHelper = new OutstandingOperationHelper();
                _state = WebSocketState.Open;
                _subProtocol = subProtocol;
                _sendFrameThrottle = new SemaphoreSlim(1, 1);
                _closeStatus = null;
                _closeStatusDescription = null;
                _innerStreamAsWebSocketStream = innerStream as IWebSocketStream;
                if (_innerStreamAsWebSocketStream != null)
                {
                    _innerStreamAsWebSocketStream.SwitchToOpaqueMode(this);
                }
                _keepAliveTracker = KeepAliveTracker.Create(keepAliveInterval);
            }
            finally
            {
                /*if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.Initialize, parameters);
                }*/
            }
        }
        /*
        internal static bool LoggingEnabled
        {
            get
            {
                return s_LoggingEnabled;
            }
        }
        */
        public override WebSocketState State
        {
            get
            {
                Contract.Assert(_state != WebSocketState.None, "'m_State' MUST NOT be 'WebSocketState.None'.");
                return _state;
            }
        }

        public override string SubProtocol
        {
            get
            {
                return _subProtocol;
            }
        }

        public override WebSocketCloseStatus? CloseStatus
        {
            get
            {
                return _closeStatus;
            }
        }

        public override string CloseStatusDescription
        {
            get
            {
                return _closeStatusDescription;
            }
        }

        internal WebSocketBuffer InternalBuffer
        {
            get
            {
                Contract.Assert(_internalBuffer != null, "'m_InternalBuffer' MUST NOT be NULL.");
                return _internalBuffer;
            }
        }

        protected void StartKeepAliveTimer()
        {
            _keepAliveTracker.StartTimer(this);
        }

        // locking SessionHandle protects access to
        // - WSPC (WebSocketProtocolComponent)
        // - m_KeepAliveTask
        // - m_CloseOutputTask
        // - m_LastSendActivity
        internal abstract SafeHandle SessionHandle { get; }

        // MultiThreading: ThreadSafe; At most one outstanding call to ReceiveAsync is allowed
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            WebSocketHelpers.ValidateArraySegment<byte>(buffer, "buffer");
            return ReceiveAsyncCore(buffer, cancellationToken);
        }

        private async Task<WebSocketReceiveResult> ReceiveAsyncCore(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            Contract.Assert(buffer.Array != null);
            /*
            if (s_LoggingEnabled)
            {
                Logging.Enter(Logging.WebSockets, this, Methods.ReceiveAsync, string.Empty);
            }
            */
            WebSocketReceiveResult receiveResult;
            try
            {
                ThrowIfPendingException();
                ThrowIfDisposed();
                WebSocketHelpers.ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseSent);

                bool ownsCancellationTokenSource = false;
                CancellationToken linkedCancellationToken = CancellationToken.None;
                try
                {
                    ownsCancellationTokenSource = _receiveOutstandingOperationHelper.TryStartOperation(cancellationToken,
                        out linkedCancellationToken);
                    if (!ownsCancellationTokenSource)
                    {
                        lock (_thisLock)
                        {
                            if (_closeAsyncStartedReceive)
                            {
                                throw new InvalidOperationException(
                                    SR.GetString(SR.net_WebSockets_ReceiveAsyncDisallowedAfterCloseAsync, Methods.CloseAsync, Methods.CloseOutputAsync));
                            }

                            throw new InvalidOperationException(
                                SR.GetString(SR.net_Websockets_AlreadyOneOutstandingOperation, Methods.ReceiveAsync));
                        }
                    }

                    EnsureReceiveOperation();
                    receiveResult = await _receiveOperation.Process(buffer, linkedCancellationToken).SuppressContextFlow();
                    /*
                    if (s_LoggingEnabled && receiveResult.Count > 0)
                    {
                        Logging.Dump(Logging.WebSockets,
                            this,
                            Methods.ReceiveAsync,
                            buffer.Array,
                            buffer.Offset,
                            receiveResult.Count);
                    }*/
                }
                catch (Exception exception)
                {
                    bool aborted = linkedCancellationToken.IsCancellationRequested;
                    Abort();
                    ThrowIfConvertibleException(Methods.ReceiveAsync, exception, cancellationToken, aborted);
                    throw;
                }
                finally
                {
                    _receiveOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                }
            }
            finally
            {/*
                if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.ReceiveAsync, string.Empty);
                }*/
            }

            return receiveResult;
        }

        // MultiThreading: ThreadSafe; At most one outstanding call to SendAsync is allowed
        public override Task SendAsync(ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            if (messageType != WebSocketMessageType.Binary &&
                    messageType != WebSocketMessageType.Text)
            {
                throw new ArgumentException(SR.GetString(SR.net_WebSockets_Argument_InvalidMessageType,
                    messageType,
                    Methods.SendAsync,
                    WebSocketMessageType.Binary,
                    WebSocketMessageType.Text,
                    Methods.CloseOutputAsync),
                    "messageType");
            }

            WebSocketHelpers.ValidateArraySegment<byte>(buffer, "buffer");

            return SendAsyncCore(buffer, messageType, endOfMessage, cancellationToken);
        }

        private async Task SendAsyncCore(ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            Contract.Assert(messageType == WebSocketMessageType.Binary || messageType == WebSocketMessageType.Text,
                "'messageType' MUST be either 'WebSocketMessageType.Binary' or 'WebSocketMessageType.Text'.");
            Contract.Assert(buffer.Array != null);

            string inputParameter = string.Empty;
            /*if (s_LoggingEnabled)
            {
                inputParameter = string.Format(CultureInfo.InvariantCulture,
                    "messageType: {0}, endOfMessage: {1}",
                    messageType,
                    endOfMessage);
                Logging.Enter(Logging.WebSockets, this, Methods.SendAsync, inputParameter);
            }*/

            try
            {
                ThrowIfPendingException();
                ThrowIfDisposed();
                WebSocketHelpers.ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseReceived);
                bool ownsCancellationTokenSource = false;
                CancellationToken linkedCancellationToken = CancellationToken.None;

                try
                {
                    while (!(ownsCancellationTokenSource = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken)))
                    {
                        Task keepAliveTask;

                        lock (SessionHandle)
                        {
                            keepAliveTask = _keepAliveTask;

                            if (keepAliveTask == null)
                            {
                                // Check whether there is still another outstanding send operation
                                // Potentially the keepAlive operation has completed before this thread
                                // was able to enter the SessionHandle-lock. 
                                _sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                                if (ownsCancellationTokenSource = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken))
                                {
                                    break;
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        SR.GetString(SR.net_Websockets_AlreadyOneOutstandingOperation, Methods.SendAsync));
                                }
                            }
                        }

                        await keepAliveTask.SuppressContextFlow();
                        ThrowIfPendingException();

                        _sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                    }
                    /*
                    if (s_LoggingEnabled && buffer.Count > 0)
                    {
                        Logging.Dump(Logging.WebSockets,
                            this,
                            Methods.SendAsync,
                            buffer.Array,
                            buffer.Offset,
                            buffer.Count);
                    }*/

                    int position = buffer.Offset;

                    EnsureSendOperation();
                    _sendOperation.BufferType = GetBufferType(messageType, endOfMessage);
                    await _sendOperation.Process(buffer, linkedCancellationToken).SuppressContextFlow();
                }
                catch (Exception exception)
                {
                    bool aborted = linkedCancellationToken.IsCancellationRequested;
                    Abort();
                    ThrowIfConvertibleException(Methods.SendAsync, exception, cancellationToken, aborted);
                    throw;
                }
                finally
                {
                    _sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                }
            }
            finally
            {
                /*if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.SendAsync, inputParameter);
                }*/
            }
        }

        private async Task SendFrameAsync(IList<ArraySegment<byte>> sendBuffers, CancellationToken cancellationToken)
        {
            bool sendFrameLockTaken = false;
            try
            {
                await _sendFrameThrottle.WaitAsync(cancellationToken).SuppressContextFlow();
                sendFrameLockTaken = true;

                if (sendBuffers.Count > 1 &&
                    _innerStreamAsWebSocketStream != null &&
                    _innerStreamAsWebSocketStream.SupportsMultipleWrite)
                {
                    await _innerStreamAsWebSocketStream.MultipleWriteAsync(sendBuffers,
                        cancellationToken).SuppressContextFlow();
                }
                else
                {
                    foreach (ArraySegment<byte> buffer in sendBuffers)
                    {
                        await _innerStream.WriteAsync(buffer.Array,
                            buffer.Offset,
                            buffer.Count,
                            cancellationToken).SuppressContextFlow();
                    }
                }
            }
            catch (ObjectDisposedException objectDisposedException)
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, objectDisposedException);
            }
            catch (NotSupportedException notSupportedException)
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, notSupportedException);
            }
            finally
            {
                if (sendFrameLockTaken)
                {
                    _sendFrameThrottle.Release();
                }
            }
        }

        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override void Abort()
        {
            /*if (s_LoggingEnabled)
            {
                Logging.Enter(Logging.WebSockets, this, Methods.Abort, string.Empty);
            }*/

            bool thisLockTaken = false;
            bool sessionHandleLockTaken = false;
            try
            {
                if (WebSocketHelpers.IsStateTerminal(State))
                {
                    return;
                }

                TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                if (WebSocketHelpers.IsStateTerminal(State))
                {
                    return;
                }

                _state = WebSocketState.Aborted;

#if DEBUG && NET451
                string stackTrace = new StackTrace().ToString();
                if (_closeStack == null)
                {
                    _closeStack = stackTrace;
                }
                /*
                if (s_LoggingEnabled)
                {
                    string message = string.Format(CultureInfo.InvariantCulture, "Stack: {0}", stackTrace);
                    Logging.PrintWarning(Logging.WebSockets, this, Methods.Abort, message);
                }*/
#endif

                // Abort any outstanding IO operations.
                if (SessionHandle != null && !SessionHandle.IsClosed && !SessionHandle.IsInvalid)
                {
                    UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketAbortHandle(SessionHandle);
                }

                _receiveOutstandingOperationHelper.CancelIO();
                _sendOutstandingOperationHelper.CancelIO();
                _closeOutputOutstandingOperationHelper.CancelIO();
                _closeOutstandingOperationHelper.CancelIO();
                if (_innerStreamAsWebSocketStream != null)
                {
                    _innerStreamAsWebSocketStream.Abort();
                }
                CleanUp();
            }
            finally
            {
                ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                /*if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.Abort, string.Empty);
                }*/
            }
        }

        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            WebSocketHelpers.ValidateCloseStatus(closeStatus, statusDescription);

            return CloseOutputAsyncCore(closeStatus, statusDescription, cancellationToken);
        }

        private async Task CloseOutputAsyncCore(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            string inputParameter = string.Empty;
            /*if (s_LoggingEnabled)
            {
                inputParameter = string.Format(CultureInfo.InvariantCulture,
                    "closeStatus: {0}, statusDescription: {1}",
                    closeStatus,
                    statusDescription);
                Logging.Enter(Logging.WebSockets, this, Methods.CloseOutputAsync, inputParameter);
            }*/

            try
            {
                ThrowIfPendingException();
                if (WebSocketHelpers.IsStateTerminal(State))
                {
                    return;
                }
                ThrowIfDisposed();

                bool thisLockTaken = false;
                bool sessionHandleLockTaken = false;
                bool needToCompleteSendOperation = false;
                bool ownsCloseOutputCancellationTokenSource = false;
                bool ownsSendCancellationTokenSource = false;
                CancellationToken linkedCancellationToken = CancellationToken.None;
                try
                {
                    TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                    ThrowIfPendingException();
                    ThrowIfDisposed();

                    if (WebSocketHelpers.IsStateTerminal(State))
                    {
                        return;
                    }

                    WebSocketHelpers.ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseReceived);
                    ownsCloseOutputCancellationTokenSource = _closeOutputOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                    if (!ownsCloseOutputCancellationTokenSource)
                    {
                        Task closeOutputTask = _closeOutputTask;

                        if (closeOutputTask != null)
                        {
                            ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                            await closeOutputTask.SuppressContextFlow();
                            TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                        }
                    }
                    else
                    {
                        needToCompleteSendOperation = true;
                        while (!(ownsSendCancellationTokenSource =
                            _sendOutstandingOperationHelper.TryStartOperation(cancellationToken,
                                out linkedCancellationToken)))
                        {
                            if (_keepAliveTask != null)
                            {
                                Task keepAliveTask = _keepAliveTask;

                                ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                                await keepAliveTask.SuppressContextFlow();
                                TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);

                                ThrowIfPendingException();
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    SR.GetString(SR.net_Websockets_AlreadyOneOutstandingOperation, Methods.SendAsync));
                            }

                            _sendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationTokenSource);
                        }

                        EnsureCloseOutputOperation();
                        _closeOutputOperation.CloseStatus = closeStatus;
                        _closeOutputOperation.CloseReason = statusDescription;
                        _closeOutputTask = _closeOutputOperation.Process(null, linkedCancellationToken);

                        ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                        await _closeOutputTask.SuppressContextFlow();
                        TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);

                        if (OnCloseOutputCompleted())
                        {
                            bool callCompleteOnCloseCompleted = false;

                            try
                            {
                                callCompleteOnCloseCompleted = await StartOnCloseCompleted(
                                    thisLockTaken, sessionHandleLockTaken, linkedCancellationToken).SuppressContextFlow();
                            }
                            catch (Exception)
                            {
                                // If an exception is thrown we know that the locks have been released,
                                // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                                ResetFlagsAndTakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                                throw;
                            }

                            if (callCompleteOnCloseCompleted)
                            {
                                ResetFlagsAndTakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                                FinishOnCloseCompleted();
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    bool aborted = linkedCancellationToken.IsCancellationRequested;
                    Abort();
                    ThrowIfConvertibleException(Methods.CloseOutputAsync, exception, cancellationToken, aborted);
                    throw;
                }
                finally
                {
                    _closeOutputOutstandingOperationHelper.CompleteOperation(ownsCloseOutputCancellationTokenSource);

                    if (needToCompleteSendOperation)
                    {
                        _sendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationTokenSource);
                    }

                    _closeOutputTask = null;
                    ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                }
            }
            finally
            {
                /*if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.CloseOutputAsync, inputParameter);
                }*/
            }
        }

        // returns TRUE if the caller should also call StartOnCloseCompleted
        private bool OnCloseOutputCompleted()
        {
            if (WebSocketHelpers.IsStateTerminal(State))
            {
                return false;
            }

            switch (State)
            {
                case WebSocketState.Open:
                    _state = WebSocketState.CloseSent;
                    return false;
                case WebSocketState.CloseReceived:
                    return true;
                default:
                    return false;
            }
        }

        // MultiThreading: This method has to be called under a m_ThisLock-lock
        // ReturnValue: This method returns true only if CompleteOnCloseCompleted needs to be called
        // If this method returns true all locks were released before starting the IO operation 
        // and they have to be retaken by the caller before calling CompleteOnCloseCompleted
        // Exception handling: If an exception is thrown from await StartOnCloseCompleted
        // it always means the locks have been released already - so the caller has to retake the
        // locks in the catch-block. 
        // This is ensured by enforcing a Task.Yield for IWebSocketStream.CloseNetowrkConnectionAsync
        private async Task<bool> StartOnCloseCompleted(bool thisLockTakenSnapshot,
            bool sessionHandleLockTakenSnapshot,
            CancellationToken cancellationToken)
        {
            Contract.Assert(thisLockTakenSnapshot, "'thisLockTakenSnapshot' MUST be 'true' at this point.");

            if (WebSocketHelpers.IsStateTerminal(_state))
            {
                return false;
            }

            _state = WebSocketState.Closed;

#if DEBUG && NET451
            if (_closeStack == null)
            {
                _closeStack = new StackTrace().ToString();
            }
#endif

            if (_innerStreamAsWebSocketStream != null)
            {
                bool thisLockTaken = thisLockTakenSnapshot;
                bool sessionHandleLockTaken = sessionHandleLockTakenSnapshot;

                try
                {
                    if (_closeNetworkConnectionTask == null)
                    {
                        _closeNetworkConnectionTask =
                            _innerStreamAsWebSocketStream.CloseNetworkConnectionAsync(cancellationToken);
                    }

                    if (thisLockTaken && sessionHandleLockTaken)
                    {
                        ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                    }
                    else if (thisLockTaken)
                    {
                        ReleaseLock(_thisLock, ref thisLockTaken);
                    }

                    await _closeNetworkConnectionTask.SuppressContextFlow();
                }
                catch (Exception closeNetworkConnectionTaskException)
                {
                    if (!CanHandleExceptionDuringClose(closeNetworkConnectionTaskException))
                    {
                        ThrowIfConvertibleException(Methods.StartOnCloseCompleted,
                            closeNetworkConnectionTaskException,
                            cancellationToken,
                            cancellationToken.IsCancellationRequested);
                        throw;
                    }
                }
            }

            return true;
        }

        // MultiThreading: This method has to be called under a thisLock-lock
        private void FinishOnCloseCompleted()
        {
            CleanUp();
        }

        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override Task CloseAsync(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            WebSocketHelpers.ValidateCloseStatus(closeStatus, statusDescription);
            return CloseAsyncCore(closeStatus, statusDescription, cancellationToken);
        }

        private async Task CloseAsyncCore(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            string inputParameter = string.Empty;
            /*if (s_LoggingEnabled)
            {
                inputParameter = string.Format(CultureInfo.InvariantCulture,
                    "closeStatus: {0}, statusDescription: {1}",
                    closeStatus,
                    statusDescription);
                Logging.Enter(Logging.WebSockets, this, Methods.CloseAsync, inputParameter);
            }*/

            try
            {
                ThrowIfPendingException();
                if (WebSocketHelpers.IsStateTerminal(State))
                {
                    return;
                }
                ThrowIfDisposed();

                bool lockTaken = false;
                Monitor.Enter(_thisLock, ref lockTaken);
                bool ownsCloseCancellationTokenSource = false;
                CancellationToken linkedCancellationToken = CancellationToken.None;
                try
                {
                    ThrowIfPendingException();
                    if (WebSocketHelpers.IsStateTerminal(State))
                    {
                        return;
                    }
                    ThrowIfDisposed();
                    WebSocketHelpers.ThrowOnInvalidState(State,
                        WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent);

                    Task closeOutputTask;
                    ownsCloseCancellationTokenSource = _closeOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                    if (ownsCloseCancellationTokenSource)
                    {
                        closeOutputTask = _closeOutputTask;
                        if (closeOutputTask == null && State != WebSocketState.CloseSent)
                        {
                            if (_closeReceivedTaskCompletionSource == null)
                            {
                                _closeReceivedTaskCompletionSource = new TaskCompletionSource<object>();
                            }

                            closeOutputTask = CloseOutputAsync(closeStatus,
                                statusDescription,
                                linkedCancellationToken);
                        }
                    }
                    else
                    {
                        Contract.Assert(_closeReceivedTaskCompletionSource != null,
                            "'m_CloseReceivedTaskCompletionSource' MUST NOT be NULL.");
                        closeOutputTask = _closeReceivedTaskCompletionSource.Task;
                    }

                    if (closeOutputTask != null)
                    {
                        ReleaseLock(_thisLock, ref lockTaken);
                        try
                        {
                            await closeOutputTask.SuppressContextFlow();
                        }
                        catch (Exception closeOutputError)
                        {
                            Monitor.Enter(_thisLock, ref lockTaken);

                            if (!CanHandleExceptionDuringClose(closeOutputError))
                            {
                                ThrowIfConvertibleException(Methods.CloseOutputAsync,
                                    closeOutputError,
                                    cancellationToken,
                                    linkedCancellationToken.IsCancellationRequested);
                                throw;
                            }
                        }

                        // When closeOutputTask != null  and an exception thrown from await closeOutputTask is handled, 
                        // the lock will be taken in the catch-block. So the logic here avoids taking the lock twice. 
                        if (!lockTaken)
                        {
                            Monitor.Enter(_thisLock, ref lockTaken);
                        }
                    }

                    if (OnCloseOutputCompleted())
                    {
                        bool callCompleteOnCloseCompleted = false;
                        
                        try
                        {
                            // linkedCancellationToken can be CancellationToken.None if ownsCloseCancellationTokenSource==false
                            // This is still ok because OnCloseOutputCompleted won't start any IO operation in this case
                            callCompleteOnCloseCompleted = await StartOnCloseCompleted(
                                lockTaken, false, linkedCancellationToken).SuppressContextFlow();
                        }
                        catch (Exception)
                        {
                            // If an exception is thrown we know that the locks have been released,
                            // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                            ResetFlagAndTakeLock(_thisLock, ref lockTaken);
                            throw;
                        }

                        if (callCompleteOnCloseCompleted)
                        {
                            ResetFlagAndTakeLock(_thisLock, ref lockTaken);
                            FinishOnCloseCompleted();
                        }
                    }

                    if (WebSocketHelpers.IsStateTerminal(State))
                    {
                        return;
                    }

                    linkedCancellationToken = CancellationToken.None;

                    bool ownsReceiveCancellationTokenSource = _receiveOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                    if (ownsReceiveCancellationTokenSource)
                    {
                        _closeAsyncStartedReceive = true;
                        ArraySegment<byte> closeMessageBuffer =
                            new ArraySegment<byte>(new byte[WebSocketBuffer.MinReceiveBufferSize]);
                        EnsureReceiveOperation();
                        Task<WebSocketReceiveResult> receiveAsyncTask = _receiveOperation.Process(closeMessageBuffer,
                            linkedCancellationToken);
                        ReleaseLock(_thisLock, ref lockTaken);

                        WebSocketReceiveResult receiveResult = null;
                        try
                        {
                            receiveResult = await receiveAsyncTask.SuppressContextFlow();
                        }
                        catch (Exception receiveException)
                        {
                            Monitor.Enter(_thisLock, ref lockTaken);

                            if (!CanHandleExceptionDuringClose(receiveException))
                            {
                                ThrowIfConvertibleException(Methods.CloseAsync,
                                    receiveException,
                                    cancellationToken,
                                    linkedCancellationToken.IsCancellationRequested);
                                throw;
                            }
                        }

                        // receiveResult is NEVER NULL if WebSocketBase.ReceiveOperation.Process completes successfully 
                        // - but in the close code path we handle some exception if another thread was able to tranistion 
                        // the state into Closed successfully. In this case receiveResult can be NULL and it is safe to 
                        // skip the statements in the if-block.
                        if (receiveResult != null)
                        {
                            /*if (s_LoggingEnabled && receiveResult.Count > 0)
                            {
                                Logging.Dump(Logging.WebSockets,
                                    this,
                                    Methods.ReceiveAsync,
                                    closeMessageBuffer.Array,
                                    closeMessageBuffer.Offset,
                                    receiveResult.Count);
                            }*/

                            if (receiveResult.MessageType != WebSocketMessageType.Close)
                            {
                                throw new WebSocketException(WebSocketError.InvalidMessageType,
                                    SR.GetString(SR.net_WebSockets_InvalidMessageType,
                                        typeof(WebSocket).Name + "." + Methods.CloseAsync,
                                        typeof(WebSocket).Name + "." + Methods.CloseOutputAsync,
                                        receiveResult.MessageType));
                            }
                        }
                    }
                    else
                    {
                        _receiveOutstandingOperationHelper.CompleteOperation(ownsReceiveCancellationTokenSource);
                        ReleaseLock(_thisLock, ref lockTaken);
                        await _closeReceivedTaskCompletionSource.Task.SuppressContextFlow();
                    }

                    // When ownsReceiveCancellationTokenSource is true and an exception is thrown, the lock will be taken.
                    // So this logic here is to avoid taking the lock twice. 
                    if (!lockTaken)
                    {
                        Monitor.Enter(_thisLock, ref lockTaken);
                    }

                    if (!WebSocketHelpers.IsStateTerminal(State))
                    {
                        bool ownsSendCancellationSource = false;
                        try
                        {
                            // We know that the CloseFrame has been sent at this point. So no Send-operation is allowed anymore and we
                            // can hijack the m_SendOutstandingOperationHelper to create a linkedCancellationToken
                            ownsSendCancellationSource = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                            Contract.Assert(ownsSendCancellationSource, "'ownsSendCancellationSource' MUST be 'true' at this point.");

                            bool callCompleteOnCloseCompleted = false;

                            try
                            {
                                // linkedCancellationToken can be CancellationToken.None if ownsCloseCancellationTokenSource==false
                                // This is still ok because OnCloseOutputCompleted won't start any IO operation in this case
                                callCompleteOnCloseCompleted = await StartOnCloseCompleted(
                                    lockTaken, false, linkedCancellationToken).SuppressContextFlow();
                            }
                            catch (Exception)
                            {
                                // If an exception is thrown we know that the locks have been released,
                                // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                                ResetFlagAndTakeLock(_thisLock, ref lockTaken);
                                throw;
                            }

                            if (callCompleteOnCloseCompleted)
                            {
                                ResetFlagAndTakeLock(_thisLock, ref lockTaken);
                                FinishOnCloseCompleted();
                            }
                        }
                        finally
                        {
                            _sendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationSource);
                        }
                    }
                }
                catch (Exception exception)
                {
                    bool aborted = linkedCancellationToken.IsCancellationRequested;
                    Abort();
                    ThrowIfConvertibleException(Methods.CloseAsync, exception, cancellationToken, aborted);
                    throw;
                }
                finally
                {
                    _closeOutstandingOperationHelper.CompleteOperation(ownsCloseCancellationTokenSource);
                    ReleaseLock(_thisLock, ref lockTaken);
                }
            }
            finally
            {
                /*if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.CloseAsync, inputParameter);
                }*/
            }
        }

        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_sendFrameThrottle",
            Justification = "SemaphoreSlim.Dispose is not threadsafe and can cause NullRef exceptions on other threads." +
            "Also according to the CLR Dev11#358715) there is no need to dispose SemaphoreSlim if the ManualResetEvent " +
            "is not used.")]
        public override void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            bool thisLockTaken = false;
            bool sessionHandleLockTaken = false;

            try
            {
                TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);

                if (_isDisposed)
                {
                    return;
                }

                if (!WebSocketHelpers.IsStateTerminal(State))
                {
                    Abort();
                }
                else
                {
                    CleanUp();
                }

                _isDisposed = true;
            }
            finally
            {
                ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
            }
        }

        private void ResetFlagAndTakeLock(object lockObject, ref bool thisLockTaken)
        {
            Contract.Assert(lockObject != null, "'lockObject' MUST NOT be NULL.");
            thisLockTaken = false;
            Monitor.Enter(lockObject, ref thisLockTaken);
        }

        private void ResetFlagsAndTakeLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
        {
            thisLockTaken = false;
            sessionHandleLockTaken = false;
            TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
        }

        private void TakeLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
        {
            Contract.Assert(_thisLock != null, "'m_ThisLock' MUST NOT be NULL.");
            Contract.Assert(SessionHandle != null, "'SessionHandle' MUST NOT be NULL.");

            Monitor.Enter(SessionHandle, ref sessionHandleLockTaken);
            Monitor.Enter(_thisLock, ref thisLockTaken);
        }

        private void ReleaseLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
        {
            Contract.Assert(_thisLock != null, "'m_ThisLock' MUST NOT be NULL.");
            Contract.Assert(SessionHandle != null, "'SessionHandle' MUST NOT be NULL.");

            if (thisLockTaken || sessionHandleLockTaken)
            {
#if !DOTNET5_4
                RuntimeHelpers.PrepareConstrainedRegions();
#endif
                try
                {
                }
                finally
                {
                    if (thisLockTaken)
                    {
                        Monitor.Exit(_thisLock);
                        thisLockTaken = false;
                    }

                    if (sessionHandleLockTaken)
                    {
                        Monitor.Exit(SessionHandle);
                        sessionHandleLockTaken = false;
                    }
                }
            }
        }

        private void EnsureReceiveOperation()
        {
            if (_receiveOperation == null)
            {
                lock (_thisLock)
                {
                    if (_receiveOperation == null)
                    {
                        _receiveOperation = new WebSocketOperation.ReceiveOperation(this);
                    }
                }
            }
        }

        private void EnsureSendOperation()
        {
            if (_sendOperation == null)
            {
                lock (_thisLock)
                {
                    if (_sendOperation == null)
                    {
                        _sendOperation = new WebSocketOperation.SendOperation(this);
                    }
                }
            }
        }

        private void EnsureKeepAliveOperation()
        {
            if (_keepAliveOperation == null)
            {
                lock (_thisLock)
                {
                    if (_keepAliveOperation == null)
                    {
                        WebSocketOperation.SendOperation keepAliveOperation = new WebSocketOperation.SendOperation(this);
                        keepAliveOperation.BufferType = UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UnsolicitedPong;
                        _keepAliveOperation = keepAliveOperation;
                    }
                }
            }
        }

        private void EnsureCloseOutputOperation()
        {
            if (_closeOutputOperation == null)
            {
                lock (_thisLock)
                {
                    if (_closeOutputOperation == null)
                    {
                        _closeOutputOperation = new WebSocketOperation.CloseOutputOperation(this);
                    }
                }
            }
        }

        private static void ReleaseLock(object lockObject, ref bool lockTaken)
        {
            Contract.Assert(lockObject != null, "'lockObject' MUST NOT be NULL.");
            if (lockTaken)
            {
#if !DOTNET5_4
                RuntimeHelpers.PrepareConstrainedRegions();
#endif
                try
                {
                }
                finally
                {
                    Monitor.Exit(lockObject);
                    lockTaken = false;
                }
            }
        }

        private static UnsafeNativeMethods.WebSocketProtocolComponent.BufferType GetBufferType(WebSocketMessageType messageType,
            bool endOfMessage)
        {
            Contract.Assert(messageType == WebSocketMessageType.Binary || messageType == WebSocketMessageType.Text,
                string.Format(CultureInfo.InvariantCulture,
                    "The value of 'messageType' ({0}) is invalid. Valid message types: '{1}, {2}'",
                    messageType,
                    WebSocketMessageType.Binary,
                    WebSocketMessageType.Text));

            if (messageType == WebSocketMessageType.Text)
            {
                if (endOfMessage)
                {
                    return UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Message;
                }

                return UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Fragment;
            }
            else
            {
                if (endOfMessage)
                {
                    return UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryMessage;
                }

                return UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryFragment;
            }
        }

        private static WebSocketMessageType GetMessageType(UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType)
        {
            switch (bufferType)
            {
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.Close:
                    return WebSocketMessageType.Close;
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryFragment:
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryMessage:
                    return WebSocketMessageType.Binary;
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Fragment:
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Message:
                    return WebSocketMessageType.Text;
                default:
                    // This indicates a contract violation of the websocket protocol component,
                    // because we currently don't support any WebSocket extensions and would
                    // not accept a Websocket handshake requesting extensions
                    Contract.Assert(false,
                    string.Format(CultureInfo.InvariantCulture,
                        "The value of 'bufferType' ({0}) is invalid. Valid buffer types: {1}, {2}, {3}, {4}, {5}.",
                        bufferType,
                        UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.Close,
                        UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryFragment,
                        UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryMessage,
                        UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Fragment,
                        UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Message));

                    throw new WebSocketException(WebSocketError.NativeError,
                        SR.GetString(SR.net_WebSockets_InvalidBufferType,
                            bufferType,
                            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.Close,
                            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryFragment,
                            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryMessage,
                            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Fragment,
                            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Message));
            }
        }

        internal void ValidateNativeBuffers(UnsafeNativeMethods.WebSocketProtocolComponent.Action action,
            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType,
            UnsafeNativeMethods.WebSocketProtocolComponent.Buffer[] dataBuffers,
            uint dataBufferCount)
        {
            _internalBuffer.ValidateNativeBuffers(action, bufferType, dataBuffers, dataBufferCount);
        }

        internal void ThrowIfClosedOrAborted()
        {
            if (State == WebSocketState.Closed || State == WebSocketState.Aborted)
            {
                throw new WebSocketException(WebSocketError.InvalidState,
                    SR.GetString(SR.net_WebSockets_InvalidState_ClosedOrAborted, GetType().FullName, State));
            }
        }

        private void ThrowIfAborted(bool aborted, Exception innerException)
        {
            if (aborted)
            {
                throw new WebSocketException(WebSocketError.InvalidState,
                    SR.GetString(SR.net_WebSockets_InvalidState_ClosedOrAborted, GetType().FullName, WebSocketState.Aborted),
                    innerException);
            }
        }

        private bool CanHandleExceptionDuringClose(Exception error)
        {
            Contract.Assert(error != null, "'error' MUST NOT be NULL.");

            if (State != WebSocketState.Closed)
            {
                return false;
            }

            return error is OperationCanceledException ||
                error is WebSocketException ||
                // error is SocketException ||
                // error is HttpListenerException ||
                error is IOException;
        }

        // We only want to throw an OperationCanceledException if the CancellationToken passed
        // down from the caller is canceled - not when Abort is called on another thread and
        // the linkedCancellationToken is canceled.
        private void ThrowIfConvertibleException(string methodName,
            Exception exception,
            CancellationToken cancellationToken,
            bool aborted)
        {
            Contract.Assert(exception != null, "'exception' MUST NOT be NULL.");
            /*
            if (s_LoggingEnabled && !string.IsNullOrEmpty(methodName))
            {
                Logging.Exception(Logging.WebSockets, this, methodName, exception);
            }*/

            OperationCanceledException operationCanceledException = exception as OperationCanceledException;
            if (operationCanceledException != null)
            {
                if (cancellationToken.IsCancellationRequested ||
                    !aborted)
                {
                    return;
                }
                ThrowIfAborted(aborted, exception);
            }

            WebSocketException convertedException = exception as WebSocketException;
            if (convertedException != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfAborted(aborted, convertedException);
                return;
            }
            /*
            SocketException socketException = exception as SocketException;
            if (socketException != null)
            {
                convertedException = new WebSocketException(socketException.NativeErrorCode, socketException);
            }
            HttpListenerException httpListenerException = exception as HttpListenerException;
            if (httpListenerException != null)
            {
                convertedException = new WebSocketException(httpListenerException.ErrorCode, httpListenerException);
            }

            IOException ioException = exception as IOException;
            if (ioException != null)
            {
                socketException = exception.InnerException as SocketException;
                if (socketException != null)
                {
                    convertedException = new WebSocketException(socketException.NativeErrorCode, ioException);
                }
            }
*/
            if (convertedException != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfAborted(aborted, convertedException);
                throw convertedException;
            }

            AggregateException aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                // Collapse possibly nested graph into a flat list.
                // Empty inner exception list is unlikely but possible via public api.
                ReadOnlyCollection<Exception> unwrappedExceptions = aggregateException.Flatten().InnerExceptions;
                if (unwrappedExceptions.Count == 0)
                {
                    return;
                }

                foreach (Exception unwrappedException in unwrappedExceptions)
                {
                    ThrowIfConvertibleException(null, unwrappedException, cancellationToken, aborted);
                }
            }
        }

        private void CleanUp()
        {
            // Multithreading: This method is always called under the m_ThisLock lock
            if (_cleanedUp)
            {
                return;
            }

            _cleanedUp = true;

            if (SessionHandle != null)
            {
                SessionHandle.Dispose();
            }

            if (_internalBuffer != null)
            {
                _internalBuffer.Dispose(this.State);
            }

            if (_receiveOutstandingOperationHelper != null)
            {
                _receiveOutstandingOperationHelper.Dispose();
            }

            if (_sendOutstandingOperationHelper != null)
            {
                _sendOutstandingOperationHelper.Dispose();
            }

            if (_closeOutputOutstandingOperationHelper != null)
            {
                _closeOutputOutstandingOperationHelper.Dispose();
            }

            if (_closeOutstandingOperationHelper != null)
            {
                _closeOutstandingOperationHelper.Dispose();
            }

            if (_innerStream != null)
            {
                try
                {
                    _innerStream.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (IOException)
                {
                }
                /*catch (SocketException)
                {
                }*/
                catch (Exception)
                {
                }
            }

            _keepAliveTracker.Dispose();
        }

        private void OnBackgroundTaskException(Exception exception)
        {
            if (Interlocked.CompareExchange<Exception>(ref _pendingException, exception, null) == null)
            {
                /*if (s_LoggingEnabled)
                {
                    Logging.Exception(Logging.WebSockets, this, Methods.Fault, exception);
                }*/
                Abort();
            }
        }

        private void ThrowIfPendingException()
        {
            Exception pendingException = Interlocked.Exchange<Exception>(ref _pendingException, null);
            if (pendingException != null)
            {
                throw new WebSocketException(WebSocketError.Faulted, pendingException);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void UpdateReceiveState(int newReceiveState, int expectedReceiveState)
        {
            int receiveState;
            if ((receiveState = Interlocked.Exchange(ref _receiveState, newReceiveState)) != expectedReceiveState)
            {
                Contract.Assert(false,
                    string.Format(CultureInfo.InvariantCulture,
                        "'m_ReceiveState' had an invalid value '{0}'. The expected value was '{1}'.",
                        receiveState,
                        expectedReceiveState));
            }
        }

        private bool StartOnCloseReceived(ref bool thisLockTaken)
        {
            ThrowIfDisposed();

            if (WebSocketHelpers.IsStateTerminal(State) || State == WebSocketState.CloseReceived)
            {
                return false;
            }

            Monitor.Enter(_thisLock, ref thisLockTaken);
            if (WebSocketHelpers.IsStateTerminal(State) || State == WebSocketState.CloseReceived)
            {
                return false;
            }

            if (State == WebSocketState.Open)
            {
                _state = WebSocketState.CloseReceived;

                if (_closeReceivedTaskCompletionSource == null)
                {
                    _closeReceivedTaskCompletionSource = new TaskCompletionSource<object>();
                }

                return false;
            }

            return true;
        }

        private void FinishOnCloseReceived(WebSocketCloseStatus closeStatus,
            string closeStatusDescription)
        {
            if (_closeReceivedTaskCompletionSource != null)
            {
                _closeReceivedTaskCompletionSource.TrySetResult(null);
            }

            _closeStatus = closeStatus;
            _closeStatusDescription = closeStatusDescription;
            /*
            if (s_LoggingEnabled)
            {
                string parameters = string.Format(CultureInfo.InvariantCulture,
                    "closeStatus: {0}, closeStatusDescription: {1}, m_State: {2}",
                    closeStatus, closeStatusDescription, m_State);

                Logging.PrintInfo(Logging.WebSockets, this, Methods.FinishOnCloseReceived, parameters);
            }*/
        }

        private async static void OnKeepAlive(object sender)
        {
            Contract.Assert(sender != null, "'sender' MUST NOT be NULL.");
            Contract.Assert((sender as WebSocketBase) != null, "'sender as WebSocketBase' MUST NOT be NULL.");

            WebSocketBase thisPtr = sender as WebSocketBase;
            bool lockTaken = false;
            /*
            if (s_LoggingEnabled)
            {
                Logging.Enter(Logging.WebSockets, thisPtr, Methods.OnKeepAlive, string.Empty);
            }*/

            CancellationToken linkedCancellationToken = CancellationToken.None;
            try
            {
                Monitor.Enter(thisPtr.SessionHandle, ref lockTaken);

                if (thisPtr._isDisposed ||
                    thisPtr._state != WebSocketState.Open ||
                    thisPtr._closeOutputTask != null)
                {
                    return;
                }

                if (thisPtr._keepAliveTracker.ShouldSendKeepAlive())
                {
                    bool ownsCancellationTokenSource = false;
                    try
                    {
                        ownsCancellationTokenSource = thisPtr._sendOutstandingOperationHelper.TryStartOperation(CancellationToken.None, out linkedCancellationToken);
                        if (ownsCancellationTokenSource)
                        {
                            thisPtr.EnsureKeepAliveOperation();
                            thisPtr._keepAliveTask = thisPtr._keepAliveOperation.Process(null, linkedCancellationToken);
                            ReleaseLock(thisPtr.SessionHandle, ref lockTaken);
                            await thisPtr._keepAliveTask.SuppressContextFlow();
                        }
                    }
                    finally
                    {
                        if (!lockTaken)
                        {
                            Monitor.Enter(thisPtr.SessionHandle, ref lockTaken);
                        }
                        thisPtr._sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                        thisPtr._keepAliveTask = null;
                    }

                    thisPtr._keepAliveTracker.ResetTimer();
                }
            }
            catch (Exception exception)
            {
                try
                {
                    thisPtr.ThrowIfConvertibleException(Methods.OnKeepAlive,
                        exception,
                        CancellationToken.None,
                        linkedCancellationToken.IsCancellationRequested);
                    throw;
                }
                catch (Exception backgroundException)
                {
                    thisPtr.OnBackgroundTaskException(backgroundException);
                }
            }
            finally
            {
                ReleaseLock(thisPtr.SessionHandle, ref lockTaken);
                /*
                if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, thisPtr, Methods.OnKeepAlive, string.Empty);
                }*/
            }
        }

        private abstract class WebSocketOperation
        {
            private readonly WebSocketBase _webSocket;

            internal WebSocketOperation(WebSocketBase webSocket)
            {
                Contract.Assert(webSocket != null, "'webSocket' MUST NOT be NULL.");
                _webSocket = webSocket;
            }

            public WebSocketReceiveResult ReceiveResult { get; protected set; }
            protected abstract int BufferCount { get; }
            protected abstract UnsafeNativeMethods.WebSocketProtocolComponent.ActionQueue ActionQueue { get; }
            protected abstract void Initialize(ArraySegment<byte>? buffer, CancellationToken cancellationToken);
            protected abstract bool ShouldContinue(CancellationToken cancellationToken);

            // Multi-Threading: This method has to be called under a SessionHandle-lock. It returns true if a 
            // close frame was received. Handling the received close frame might involve IO - to make the locking
            // strategy easier and reduce one level in the await-hierarchy the IO is kicked off by the caller.
            protected abstract bool ProcessAction_NoAction();
            
            protected virtual void ProcessAction_IndicateReceiveComplete(
                ArraySegment<byte>? buffer,
                UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType,
                UnsafeNativeMethods.WebSocketProtocolComponent.Action action,
                UnsafeNativeMethods.WebSocketProtocolComponent.Buffer[] dataBuffers,
                uint dataBufferCount,
                IntPtr actionContext)
            {
                throw new NotImplementedException();
            }

            protected abstract void Cleanup();

            internal async Task<WebSocketReceiveResult> Process(ArraySegment<byte>? buffer,
                CancellationToken cancellationToken)
            {
                Contract.Assert(BufferCount >= 1 && BufferCount <= 2, "'bufferCount' MUST ONLY BE '1' or '2'.");

                bool sessionHandleLockTaken = false;
                ReceiveResult = null;
                try
                {
                    Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                    _webSocket.ThrowIfPendingException();
                    Initialize(buffer, cancellationToken);

                    while (ShouldContinue(cancellationToken))
                    {
                        UnsafeNativeMethods.WebSocketProtocolComponent.Action action;
                        UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType;

                        bool completed = false;
                        while (!completed)
                        {
                            UnsafeNativeMethods.WebSocketProtocolComponent.Buffer[] dataBuffers =
                                new UnsafeNativeMethods.WebSocketProtocolComponent.Buffer[BufferCount];
                            uint dataBufferCount = (uint)BufferCount;
                            IntPtr actionContext;

                            _webSocket.ThrowIfDisposed();
                            UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketGetAction(_webSocket,
                                ActionQueue,
                                dataBuffers,
                                ref dataBufferCount,
                                out action,
                                out bufferType,
                                out actionContext);

                            switch (action)
                            {
                                case UnsafeNativeMethods.WebSocketProtocolComponent.Action.NoAction:
                                    if (ProcessAction_NoAction())
                                    {
                                        // A close frame was received

                                        Contract.Assert(ReceiveResult.Count == 0, "'receiveResult.Count' MUST be 0.");
                                        Contract.Assert(ReceiveResult.CloseStatus != null, "'receiveResult.CloseStatus' MUST NOT be NULL for message type 'Close'.");
                                        bool thisLockTaken = false;
                                        try
                                        {
                                            if (_webSocket.StartOnCloseReceived(ref thisLockTaken))
                                            {
                                                // If StartOnCloseReceived returns true the WebSocket close handshake has been completed
                                                // so there is no need to retake the SessionHandle-lock.
                                                // m_ThisLock lock is guaranteed to be taken by StartOnCloseReceived when returning true
                                                ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                                bool callCompleteOnCloseCompleted = false;

                                                try
                                                {
                                                    callCompleteOnCloseCompleted = await _webSocket.StartOnCloseCompleted(
                                                        thisLockTaken, sessionHandleLockTaken, cancellationToken).SuppressContextFlow();
                                                }
                                                catch (Exception)
                                                {
                                                    // If an exception is thrown we know that the locks have been released,
                                                    // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                                                    _webSocket.ResetFlagAndTakeLock(_webSocket._thisLock, ref thisLockTaken);
                                                    throw;
                                                }

                                                if (callCompleteOnCloseCompleted)
                                                {
                                                    _webSocket.ResetFlagAndTakeLock(_webSocket._thisLock, ref thisLockTaken);
                                                    _webSocket.FinishOnCloseCompleted();
                                                }
                                            }
                                            _webSocket.FinishOnCloseReceived(ReceiveResult.CloseStatus.Value, ReceiveResult.CloseStatusDescription);
                                        }
                                        finally
                                        {
                                            if (thisLockTaken)
                                            {
                                                ReleaseLock(_webSocket._thisLock, ref thisLockTaken);
                                            }
                                        }
                                    }
                                    completed = true;
                                    break;
                                case UnsafeNativeMethods.WebSocketProtocolComponent.Action.IndicateReceiveComplete:
                                    ProcessAction_IndicateReceiveComplete(buffer,
                                        bufferType,
                                        action,
                                        dataBuffers,
                                        dataBufferCount,
                                        actionContext);
                                    break;
                                case UnsafeNativeMethods.WebSocketProtocolComponent.Action.ReceiveFromNetwork:
                                    int count = 0;
                                    try
                                    {
                                        ArraySegment<byte> payload = _webSocket._internalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);

                                        ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                        WebSocketHelpers.ThrowIfConnectionAborted(_webSocket._innerStream, true);
                                        try
                                        {
                                            Task<int> readTask = _webSocket._innerStream.ReadAsync(payload.Array,
                                                payload.Offset,
                                                payload.Count,
                                                cancellationToken);
                                            count = await readTask.SuppressContextFlow();
                                            _webSocket._keepAliveTracker.OnDataReceived();
                                        }
                                        catch (ObjectDisposedException objectDisposedException)
                                        {
                                            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, objectDisposedException);
                                        }
                                        catch (NotSupportedException notSupportedException)
                                        {
                                            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, notSupportedException);
                                        }
                                        Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                        _webSocket.ThrowIfPendingException();
                                        // If the client unexpectedly closed the socket we throw an exception as we didn't get any close message
                                        if (count == 0)
                                        {
                                            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
                                        }
                                    }
                                    finally
                                    {
                                        UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket,
                                            actionContext,
                                            count);
                                    }
                                    break;
                                case UnsafeNativeMethods.WebSocketProtocolComponent.Action.IndicateSendComplete:
                                    UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket, actionContext, 0);
                                    ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                    await _webSocket._innerStream.FlushAsync().SuppressContextFlow();
                                    Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                    break;
                                case UnsafeNativeMethods.WebSocketProtocolComponent.Action.SendToNetwork:
                                    int bytesSent = 0;
                                    try
                                    {
                                        if (_webSocket.State != WebSocketState.CloseSent ||
                                            (bufferType != UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.PingPong &&
                                            bufferType != UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UnsolicitedPong))
                                        {
                                            if (dataBufferCount == 0)
                                            {
                                                break;
                                            }

                                            List<ArraySegment<byte>> sendBuffers = new List<ArraySegment<byte>>((int)dataBufferCount);
                                            int sendBufferSize = 0;
                                            ArraySegment<byte> framingBuffer = _webSocket._internalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);
                                            sendBuffers.Add(framingBuffer);
                                            sendBufferSize += framingBuffer.Count;

                                            // There can be at most 2 dataBuffers
                                            // - one for the framing header and one for the payload
                                            if (dataBufferCount == 2)
                                            {
                                                ArraySegment<byte> payload = _webSocket._internalBuffer.ConvertPinnedSendPayloadFromNative(dataBuffers[1], bufferType);
                                                sendBuffers.Add(payload);
                                                sendBufferSize += payload.Count;
                                            }

                                            ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                            WebSocketHelpers.ThrowIfConnectionAborted(_webSocket._innerStream, false);
                                            await _webSocket.SendFrameAsync(sendBuffers, cancellationToken).SuppressContextFlow();
                                            Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                                            _webSocket.ThrowIfPendingException();
                                            bytesSent += sendBufferSize;
                                            _webSocket._keepAliveTracker.OnDataSent();
                                        }
                                    }
                                    finally
                                    {
                                        UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket,
                                            actionContext,
                                            bytesSent);
                                    }

                                    break;
                                default:
                                    string assertMessage = string.Format(CultureInfo.InvariantCulture,
                                        "Invalid action '{0}' returned from WebSocketGetAction.",
                                        action);
                                    Contract.Assert(false, assertMessage);
                                    throw new InvalidOperationException();
                            }
                        }
                    }
                }
                finally
                {
                    Cleanup();
                    ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
                }

                return ReceiveResult;
            }

            public class ReceiveOperation : WebSocketOperation
            {
                private int _receiveState;
                private bool _pongReceived;
                private bool _receiveCompleted;

                public ReceiveOperation(WebSocketBase webSocket)
                    : base(webSocket)
                {
                }

                protected override UnsafeNativeMethods.WebSocketProtocolComponent.ActionQueue ActionQueue
                {
                    get { return UnsafeNativeMethods.WebSocketProtocolComponent.ActionQueue.Receive; }
                }

                protected override int BufferCount
                {
                    get { return 1; }
                }

                protected override void Initialize(ArraySegment<byte>? buffer, CancellationToken cancellationToken)
                {
                    Contract.Assert(buffer != null, "'buffer' MUST NOT be NULL.");
                    _pongReceived = false;
                    _receiveCompleted = false;
                    _webSocket.ThrowIfDisposed();

                    int originalReceiveState = Interlocked.CompareExchange(ref _webSocket._receiveState,
                        ReceiveState.Application, ReceiveState.Idle);

                    switch (originalReceiveState)
                    {
                        case ReceiveState.Idle:
                            _receiveState = ReceiveState.Application;
                            break;
                        case ReceiveState.Application:
                            Contract.Assert(false, "'originalReceiveState' MUST NEVER be ReceiveState.Application at this point.");
                            break;
                        case ReceiveState.PayloadAvailable:
                            WebSocketReceiveResult receiveResult;
                            if (!_webSocket._internalBuffer.ReceiveFromBufferedPayload(buffer.Value, out receiveResult))
                            {
                                _webSocket.UpdateReceiveState(ReceiveState.Idle, ReceiveState.PayloadAvailable);
                            }
                            ReceiveResult = receiveResult;
                            _receiveCompleted = true;
                            break;
                        default:
                            Contract.Assert(false,
                                string.Format(CultureInfo.InvariantCulture, "Invalid ReceiveState '{0}'.", originalReceiveState));
                            break;
                    }
                }

                protected override void Cleanup()
                {
                }

                protected override bool ShouldContinue(CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (_receiveCompleted)
                    {
                        return false;
                    }

                    _webSocket.ThrowIfDisposed();
                    _webSocket.ThrowIfPendingException();
                    UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketReceive(_webSocket);

                    return true;
                }

                protected override bool ProcessAction_NoAction()
                {
                    if (_pongReceived)
                    {
                        _receiveCompleted = false;
                        _pongReceived = false;
                        return false;
                    }

                    Contract.Assert(ReceiveResult != null,
                        "'ReceiveResult' MUST NOT be NULL.");
                    _receiveCompleted = true;

                    if (ReceiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        return true;
                    }

                    return false;
                }

                protected override void ProcessAction_IndicateReceiveComplete(
                    ArraySegment<byte>? buffer,
                    UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType,
                    UnsafeNativeMethods.WebSocketProtocolComponent.Action action,
                    UnsafeNativeMethods.WebSocketProtocolComponent.Buffer[] dataBuffers,
                    uint dataBufferCount,
                    IntPtr actionContext)
                {
                    Contract.Assert(buffer != null, "'buffer MUST NOT be NULL.");

                    int bytesTransferred = 0;
                    _pongReceived = false;

                    if (bufferType == UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.PingPong)
                    {
                        // ignoring received pong frame 
                        _pongReceived = true;
                        UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket,
                            actionContext,
                            bytesTransferred);
                        return;
                    }

                    WebSocketReceiveResult receiveResult;
                    try
                    {
                        ArraySegment<byte> payload;
                        WebSocketMessageType messageType = GetMessageType(bufferType);
                        int newReceiveState = ReceiveState.Idle;

                        if (bufferType == UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.Close)
                        {
                            payload = WebSocketHelpers.EmptyPayload;
                            string reason;
                            WebSocketCloseStatus closeStatus;
                            _webSocket._internalBuffer.ConvertCloseBuffer(action, dataBuffers[0], out closeStatus, out reason);

                            receiveResult = new WebSocketReceiveResult(bytesTransferred,
                                messageType, true, closeStatus, reason);
                        }
                        else
                        {
                            payload = _webSocket._internalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);

                            bool endOfMessage = bufferType ==
                                UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryMessage ||
                                bufferType == UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Message ||
                                bufferType == UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.Close;

                            if (payload.Count > buffer.Value.Count)
                            {
                                _webSocket._internalBuffer.BufferPayload(payload, buffer.Value.Count, messageType, endOfMessage);
                                newReceiveState = ReceiveState.PayloadAvailable;
                                endOfMessage = false;
                            }

                            bytesTransferred = Math.Min(payload.Count, (int)buffer.Value.Count);
                            if (bytesTransferred > 0)
                            {
                                Buffer.BlockCopy(payload.Array,
                                    payload.Offset,
                                    buffer.Value.Array,
                                    buffer.Value.Offset,
                                    bytesTransferred);
                            }

                            receiveResult = new WebSocketReceiveResult(bytesTransferred, messageType, endOfMessage);
                        }

                        _webSocket.UpdateReceiveState(newReceiveState, _receiveState);
                    }
                    finally
                    {
                        UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket,
                            actionContext,
                            bytesTransferred);
                    }

                    ReceiveResult = receiveResult;
                }
            }

            public class SendOperation : WebSocketOperation
            {
                private bool _completed;
                protected bool _bufferHasBeenPinned;

                public SendOperation(WebSocketBase webSocket)
                    : base(webSocket)
                {
                }

                protected override UnsafeNativeMethods.WebSocketProtocolComponent.ActionQueue ActionQueue
                {
                    get { return UnsafeNativeMethods.WebSocketProtocolComponent.ActionQueue.Send; }
                }

                protected override int BufferCount
                {
                    get { return 2; }
                }

                protected virtual UnsafeNativeMethods.WebSocketProtocolComponent.Buffer? CreateBuffer(ArraySegment<byte>? buffer)
                {
                    if (buffer == null)
                    {
                        return null;
                    }

                    UnsafeNativeMethods.WebSocketProtocolComponent.Buffer payloadBuffer;
                    payloadBuffer = new UnsafeNativeMethods.WebSocketProtocolComponent.Buffer();
                    _webSocket._internalBuffer.PinSendBuffer(buffer.Value, out _bufferHasBeenPinned);
                    payloadBuffer.Data.BufferData = _webSocket._internalBuffer.ConvertPinnedSendPayloadToNative(buffer.Value);
                    payloadBuffer.Data.BufferLength = (uint)buffer.Value.Count;
                    return payloadBuffer;
                }

                protected override bool ProcessAction_NoAction()
                {
                    _completed = true;
                    return false;
                }

                protected override void Cleanup()
                {
                    if (_bufferHasBeenPinned)
                    {
                        _bufferHasBeenPinned = false;
                        _webSocket._internalBuffer.ReleasePinnedSendBuffer();
                    }
                }

                internal UnsafeNativeMethods.WebSocketProtocolComponent.BufferType BufferType { get; set; }

                protected override void Initialize(ArraySegment<byte>? buffer,
                    CancellationToken cancellationToken)
                {
                    Contract.Assert(!_bufferHasBeenPinned, "'m_BufferHasBeenPinned' MUST NOT be pinned at this point.");
                    _webSocket.ThrowIfDisposed();
                    _webSocket.ThrowIfPendingException();
                    _completed = false;

                    UnsafeNativeMethods.WebSocketProtocolComponent.Buffer? payloadBuffer = CreateBuffer(buffer);
                    if (payloadBuffer != null)
                    {
                        UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketSend(_webSocket, BufferType, payloadBuffer.Value);
                    }
                    else
                    {
                        UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketSendWithoutBody(_webSocket, BufferType);
                    }
                }

                protected override bool ShouldContinue(CancellationToken cancellationToken)
                {
                    Contract.Assert(ReceiveResult == null, "'ReceiveResult' MUST be NULL.");
                    if (_completed)
                    {
                        return false;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    return true;
                }
            }

            public class CloseOutputOperation : SendOperation
            {
                public CloseOutputOperation(WebSocketBase webSocket)
                    : base(webSocket)
                {
                    BufferType = UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.Close;
                }

                internal WebSocketCloseStatus CloseStatus { get; set; }
                internal string CloseReason { get; set; }

                protected override UnsafeNativeMethods.WebSocketProtocolComponent.Buffer? CreateBuffer(ArraySegment<byte>? buffer)
                {
                    Contract.Assert(buffer == null, "'buffer' MUST BE NULL.");
                    _webSocket.ThrowIfDisposed();
                    _webSocket.ThrowIfPendingException();

                    if (CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        return null;
                    }

                    UnsafeNativeMethods.WebSocketProtocolComponent.Buffer payloadBuffer = new UnsafeNativeMethods.WebSocketProtocolComponent.Buffer();
                    if (CloseReason != null)
                    {
                        byte[] blob = UTF8Encoding.UTF8.GetBytes(CloseReason);
                        Contract.Assert(blob.Length <= WebSocketHelpers.MaxControlFramePayloadLength,
                            "The close reason is too long.");
                        ArraySegment<byte> closeBuffer = new ArraySegment<byte>(blob, 0, Math.Min(WebSocketHelpers.MaxControlFramePayloadLength, blob.Length));
                        _webSocket._internalBuffer.PinSendBuffer(closeBuffer, out _bufferHasBeenPinned);
                        payloadBuffer.CloseStatus.ReasonData = _webSocket._internalBuffer.ConvertPinnedSendPayloadToNative(closeBuffer);
                        payloadBuffer.CloseStatus.ReasonLength = (uint)closeBuffer.Count;
                    }

                    payloadBuffer.CloseStatus.CloseStatus = (ushort)CloseStatus;
                    return payloadBuffer;
                }
            }
        }

        private abstract class KeepAliveTracker : IDisposable
        {
            // Multi-Threading: only one thread at a time is allowed to call OnDataReceived or OnDataSent 
            // - but both methods can be called from different threads at the same time.
            public abstract void OnDataReceived();
            public abstract void OnDataSent();
            public abstract void Dispose();
            public abstract void StartTimer(WebSocketBase webSocket);
            public abstract void ResetTimer();
            public abstract bool ShouldSendKeepAlive();
            
            public static KeepAliveTracker Create(TimeSpan keepAliveInterval)
            {
                if ((int)keepAliveInterval.TotalMilliseconds > 0)
                {
                    return new DefaultKeepAliveTracker(keepAliveInterval);
                }

                return new DisabledKeepAliveTracker();
            }

            private class DisabledKeepAliveTracker : KeepAliveTracker
            {
                public override void OnDataReceived() 
                {
                }

                public override void OnDataSent()
                {
                }

                public override void ResetTimer()
                {
                }

                public override void StartTimer(WebSocketBase webSocket)
                {
                }

                public override bool ShouldSendKeepAlive()
                {
                    return false;
                }

                public override void Dispose()
                {
                }
            }

            private class DefaultKeepAliveTracker : KeepAliveTracker
            {
                private static readonly TimerCallback _keepAliveTimerElapsedCallback = new TimerCallback(OnKeepAlive);
                private readonly TimeSpan _keepAliveInterval;
                private readonly Stopwatch _lastSendActivity;
                private readonly Stopwatch _lastReceiveActivity;
                private Timer _keepAliveTimer;

                public DefaultKeepAliveTracker(TimeSpan keepAliveInterval)
                {
                    _keepAliveInterval = keepAliveInterval;
                    _lastSendActivity = new Stopwatch();
                    _lastReceiveActivity = new Stopwatch();
                }

                public override void OnDataReceived()
                {
                    _lastReceiveActivity.Restart();
                }

                public override void OnDataSent()
                {
                    _lastSendActivity.Restart();
                }

                public override void ResetTimer()
                {
                    ResetTimer((int)_keepAliveInterval.TotalMilliseconds);
                }

                public override void StartTimer(WebSocketBase webSocket)
                {
                    Contract.Assert(webSocket != null, "'webSocket' MUST NOT be NULL.");
                    Contract.Assert(webSocket._keepAliveTracker != null, 
                        "'webSocket.m_KeepAliveTracker' MUST NOT be NULL at this point.");
                    int keepAliveIntervalMilliseconds = (int)_keepAliveInterval.TotalMilliseconds;
                    Contract.Assert(keepAliveIntervalMilliseconds > 0, "'keepAliveIntervalMilliseconds' MUST be POSITIVE.");
#if DOTNET5_4
                    _keepAliveTimer = new Timer(_keepAliveTimerElapsedCallback, webSocket, keepAliveIntervalMilliseconds, Timeout.Infinite);
#else
                    if (ExecutionContext.IsFlowSuppressed())
                    {
                        _keepAliveTimer = new Timer(_keepAliveTimerElapsedCallback, webSocket, keepAliveIntervalMilliseconds, Timeout.Infinite);
                    }
                    else
                    {
                        using (ExecutionContext.SuppressFlow())
                        {
                            _keepAliveTimer = new Timer(_keepAliveTimerElapsedCallback, webSocket, keepAliveIntervalMilliseconds, Timeout.Infinite);
                        }
                    }
#endif
                }

                public override bool ShouldSendKeepAlive()
                {
                    TimeSpan idleTime = GetIdleTime();
                    if (idleTime >= _keepAliveInterval)
                    {
                        return true;
                    }

                    ResetTimer((int)(_keepAliveInterval - idleTime).TotalMilliseconds);
                    return false;
                }

                public override void Dispose()
                {
                    _keepAliveTimer.Dispose();
                }

                private void ResetTimer(int dueInMilliseconds)
                {
                    _keepAliveTimer.Change(dueInMilliseconds, Timeout.Infinite);
                }

                private TimeSpan GetIdleTime()
                {
                    TimeSpan sinceLastSendActivity = GetTimeElapsed(_lastSendActivity);
                    TimeSpan sinceLastReceiveActivity = GetTimeElapsed(_lastReceiveActivity);

                    if (sinceLastReceiveActivity < sinceLastSendActivity)
                    {
                        return sinceLastReceiveActivity;
                    }

                    return sinceLastSendActivity;
                }

                private TimeSpan GetTimeElapsed(Stopwatch watch)
                {
                    if (watch.IsRunning)
                    {
                        return watch.Elapsed;
                    }

                    return _keepAliveInterval;
                }
            }
        }

        private class OutstandingOperationHelper : IDisposable
        {
            private volatile int _operationsOutstanding;
            private volatile CancellationTokenSource _cancellationTokenSource;
            private volatile bool _isDisposed;
            private readonly object _thisLock = new object();

            public bool TryStartOperation(CancellationToken userCancellationToken, out CancellationToken linkedCancellationToken)
            {
                linkedCancellationToken = CancellationToken.None;
                ThrowIfDisposed();

                lock (_thisLock)
                {
                    int operationsOutstanding = ++_operationsOutstanding;

                    if (operationsOutstanding == 1)
                    {
                        linkedCancellationToken = CreateLinkedCancellationToken(userCancellationToken);
                        return true;
                    }

                    Contract.Assert(operationsOutstanding >= 1, "'operationsOutstanding' must never be smaller than 1.");
                    return false;
                }
            }

            public void CompleteOperation(bool ownsCancellationTokenSource)
            {
                if (_isDisposed)
                {
                    // no-op if the WebSocket is already aborted
                    return;
                }

                CancellationTokenSource snapshot = null;

                lock (_thisLock)
                {
                    --_operationsOutstanding;
                    Contract.Assert(_operationsOutstanding >= 0, "'m_OperationsOutstanding' must never be smaller than 0.");

                    if (ownsCancellationTokenSource)
                    {
                        snapshot = _cancellationTokenSource;
                        _cancellationTokenSource = null;
                    }
                }

                if (snapshot != null)
                {
                    snapshot.Dispose();
                }
            }

            // Has to be called under m_ThisLock lock
            private CancellationToken CreateLinkedCancellationToken(CancellationToken cancellationToken)
            {
                CancellationTokenSource linkedCancellationTokenSource;

                if (cancellationToken == CancellationToken.None)
                {
                    linkedCancellationTokenSource = new CancellationTokenSource();
                }
                else
                {
                    linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                        new CancellationTokenSource().Token);
                }

                Contract.Assert(_cancellationTokenSource == null, "'m_CancellationTokenSource' MUST be NULL.");
                _cancellationTokenSource = linkedCancellationTokenSource;

                return linkedCancellationTokenSource.Token;
            }

            public void CancelIO()
            {
                CancellationTokenSource cancellationTokenSourceSnapshot = null;

                lock (_thisLock)
                {
                    if (_operationsOutstanding == 0)
                    {
                        return;
                    }

                    cancellationTokenSourceSnapshot = _cancellationTokenSource;
                }

                if (cancellationTokenSourceSnapshot != null)
                {
                    try
                    {
                        cancellationTokenSourceSnapshot.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Simply ignore this exception - There is apparently a rare race condition
                        // where the cancellationTokensource is disposed before the Cancel method call completed.
                    }
                }
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                CancellationTokenSource snapshot = null;
                lock (_thisLock)
                {
                    if (_isDisposed)
                    {
                        return;
                    }

                    _isDisposed = true;
                    snapshot = _cancellationTokenSource;
                    _cancellationTokenSource = null;
                }

                if (snapshot != null)
                {
                    snapshot.Dispose();
                }
            }

            private void ThrowIfDisposed()
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }
        }

        internal interface IWebSocketStream
        {
            // Switching to opaque mode will change the behavior to use the knowledge that the WebSocketBase class
            // is pinning all payloads already and that we will have at most one outstanding send and receive at any
            // given time. This allows us to avoid creation of OverlappedData and pinning for each operation.

            void SwitchToOpaqueMode(WebSocketBase webSocket);
            void Abort();
            bool SupportsMultipleWrite { get; }
            Task MultipleWriteAsync(IList<ArraySegment<byte>> buffers, CancellationToken cancellationToken);

            // Any implementation has to guarantee that no exception is thrown synchronously
            // for example by enforcing a Task.Yield at the beginning of the method
            // This is necessary to enforce an API contract (for WebSocketBase.StartOnCloseCompleted) that ensures 
            // that all locks have been released whenever an exception is thrown from it.
            Task CloseNetworkConnectionAsync(CancellationToken cancellationToken);
        }

        private static class ReceiveState
        {
            internal const int SendOperation = -1;
            internal const int Idle = 0;
            internal const int Application = 1;
            internal const int PayloadAvailable = 2;
        }

        internal static class Methods
        {
            internal const string ReceiveAsync = "ReceiveAsync";
            internal const string SendAsync = "SendAsync";
            internal const string CloseAsync = "CloseAsync";
            internal const string CloseOutputAsync = "CloseOutputAsync";
            internal const string Abort = "Abort";
            internal const string Initialize = "Initialize";
            internal const string Fault = "Fault";
            internal const string StartOnCloseCompleted = "StartOnCloseCompleted";
            internal const string FinishOnCloseReceived = "FinishOnCloseReceived";
            internal const string OnKeepAlive = "OnKeepAlive";
        }
    }
}
