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
// <copyright file="WebSocketBuffer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.Net.WebSockets
{
    // This class helps to abstract the internal WebSocket buffer, which is used to interact with the native WebSocket
    // protocol component (WSPC). It helps to shield the details of the layout and the involved pointer arithmetic.
    // The internal WebSocket buffer also contains a segment, which is used by the WebSocketBase class to buffer 
    // payload (parsed by WSPC already) for the application, if the application requested fewer bytes than the
    // WSPC returned. The internal buffer is pinned for the whole lifetime if this class.
    // LAYOUT:
    // | Native buffer              | PayloadReceiveBuffer | PropertyBuffer |
    // | RBS + SBS + 144            | RBS                  | PBS            |
    // | Only WSPC may modify       | Only WebSocketBase may modify         | 
    //
    // *RBS = ReceiveBufferSize, *SBS = SendBufferSize
    // *PBS = PropertyBufferSize (32-bit: 16, 64 bit: 20 bytes)
    public class WebSocketBuffer : IDisposable
    {
        private const int NativeOverheadBufferSize = 144;
        public const int MinSendBufferSize = 16;
        internal const int MinReceiveBufferSize = 256;
        internal const int MaxBufferSize = 64 * 1024;
        private static readonly int SizeOfUInt = Marshal.SizeOf<uint>();
        private static readonly int SizeOfBool = Marshal.SizeOf<bool>();
        private static readonly int PropertyBufferSize = (2 * SizeOfUInt) + SizeOfBool + IntPtr.Size;

        private readonly int _ReceiveBufferSize;
        
        // Indicates the range of the pinned byte[] that can be used by the WSPC (nativeBuffer + pinnedSendBuffer)
        private readonly long _StartAddress;
        private readonly long _EndAddress;
        private readonly GCHandle _GCHandle;
        private readonly ArraySegment<byte> _InternalBuffer;
        private readonly ArraySegment<byte> _NativeBuffer;
        private readonly ArraySegment<byte> _PayloadBuffer;
        private readonly ArraySegment<byte> _PropertyBuffer;
        private readonly int _SendBufferSize;
        private volatile int _PayloadOffset;
        private WebSocketReceiveResult _BufferedPayloadReceiveResult;
        private long _PinnedSendBufferStartAddress;
        private long _PinnedSendBufferEndAddress;
        private ArraySegment<byte> _PinnedSendBuffer;
        private GCHandle _PinnedSendBufferHandle;
        private int _StateWhenDisposing = int.MinValue;
        private int _SendBufferState;
        
        private WebSocketBuffer(ArraySegment<byte> internalBuffer, int receiveBufferSize, int sendBufferSize)
        {
            Contract.Assert(internalBuffer.Array != null, "'internalBuffer' MUST NOT be NULL.");
            Contract.Assert(receiveBufferSize >= MinReceiveBufferSize,
                "'receiveBufferSize' MUST be at least " + MinReceiveBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Contract.Assert(sendBufferSize >= MinSendBufferSize,
                "'sendBufferSize' MUST be at least " + MinSendBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Contract.Assert(receiveBufferSize <= MaxBufferSize,
                "'receiveBufferSize' MUST NOT exceed " + MaxBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Contract.Assert(sendBufferSize <= MaxBufferSize,
                "'sendBufferSize' MUST NOT exceed  " + MaxBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            _ReceiveBufferSize = receiveBufferSize;
            _SendBufferSize = sendBufferSize;
            _InternalBuffer = internalBuffer;
            _GCHandle = GCHandle.Alloc(internalBuffer.Array, GCHandleType.Pinned);
            // Size of the internal buffer owned exclusively by the WSPC.
            int nativeBufferSize = _ReceiveBufferSize + _SendBufferSize + NativeOverheadBufferSize;
            _StartAddress = Marshal.UnsafeAddrOfPinnedArrayElement(internalBuffer.Array, internalBuffer.Offset).ToInt64();
            _EndAddress = _StartAddress + nativeBufferSize;
            _NativeBuffer = new ArraySegment<byte>(internalBuffer.Array, internalBuffer.Offset, nativeBufferSize);
            _PayloadBuffer = new ArraySegment<byte>(internalBuffer.Array,
                _NativeBuffer.Offset + _NativeBuffer.Count, 
                _ReceiveBufferSize);
            _PropertyBuffer = new ArraySegment<byte>(internalBuffer.Array,
                _PayloadBuffer.Offset + _PayloadBuffer.Count,
                PropertyBufferSize);
            _SendBufferState = SendBufferState.None;
        }

        public int ReceiveBufferSize
        {
            get { return _ReceiveBufferSize; }
        }

        public int SendBufferSize
        {
            get { return _SendBufferSize; }
        }

        internal static WebSocketBuffer CreateClientBuffer(ArraySegment<byte> internalBuffer, int receiveBufferSize, int sendBufferSize)
        {
            Contract.Assert(internalBuffer.Count >= GetInternalBufferSize(receiveBufferSize, sendBufferSize, false),
                "Array 'internalBuffer' is TOO SMALL. Call Validate before instantiating WebSocketBuffer.");

            return new WebSocketBuffer(internalBuffer, receiveBufferSize, GetNativeSendBufferSize(sendBufferSize, false));
        }

        internal static WebSocketBuffer CreateServerBuffer(ArraySegment<byte> internalBuffer, int receiveBufferSize)
        {
            int sendBufferSize = GetNativeSendBufferSize(MinSendBufferSize, true);
            Contract.Assert(internalBuffer.Count >= GetInternalBufferSize(receiveBufferSize, sendBufferSize, true),
                "Array 'internalBuffer' is TOO SMALL. Call Validate before instantiating WebSocketBuffer.");

            return new WebSocketBuffer(internalBuffer, receiveBufferSize, sendBufferSize);
        }

        public void Dispose(WebSocketState webSocketState)
        {
            if (Interlocked.CompareExchange(ref _StateWhenDisposing, (int)webSocketState, int.MinValue) != int.MinValue)
            {
                return;
            }
            
            this.CleanUp();
        }

        public void Dispose()
        {
            this.Dispose(WebSocketState.None);
        }

        internal UnsafeNativeMethods.WebSocketProtocolComponent.Property[] CreateProperties(bool useZeroMaskingKey)
        {
            ThrowIfDisposed();
            // serialize marshaled property values in the property segment of the internal buffer
            IntPtr internalBufferPtr = _GCHandle.AddrOfPinnedObject();
            int offset = _PropertyBuffer.Offset;
            Marshal.WriteInt32(internalBufferPtr, offset, _ReceiveBufferSize);
            offset += SizeOfUInt;
            Marshal.WriteInt32(internalBufferPtr, offset, _SendBufferSize);
            offset += SizeOfUInt;
            Marshal.WriteIntPtr(internalBufferPtr, offset, internalBufferPtr);
            offset += IntPtr.Size;
            Marshal.WriteInt32(internalBufferPtr, offset, useZeroMaskingKey ? (int)1 : (int)0);

            int propertyCount = useZeroMaskingKey ? 4 : 3;
            UnsafeNativeMethods.WebSocketProtocolComponent.Property[] properties =
                new UnsafeNativeMethods.WebSocketProtocolComponent.Property[propertyCount];

            // Calculate the pointers to the positions of the properties within the internal buffer
            offset = _PropertyBuffer.Offset;
            properties[0] = new UnsafeNativeMethods.WebSocketProtocolComponent.Property()
            {
                Type = UnsafeNativeMethods.WebSocketProtocolComponent.PropertyType.ReceiveBufferSize,
                PropertySize = (uint)SizeOfUInt,
                PropertyData = IntPtr.Add(internalBufferPtr, offset)
            };
            offset += SizeOfUInt;

            properties[1] = new UnsafeNativeMethods.WebSocketProtocolComponent.Property()
            {
                Type = UnsafeNativeMethods.WebSocketProtocolComponent.PropertyType.SendBufferSize,
                PropertySize = (uint)SizeOfUInt,
                PropertyData = IntPtr.Add(internalBufferPtr, offset)
            };
            offset += SizeOfUInt;

            properties[2] = new UnsafeNativeMethods.WebSocketProtocolComponent.Property()
            {
                Type = UnsafeNativeMethods.WebSocketProtocolComponent.PropertyType.AllocatedBuffer,
                PropertySize = (uint)_NativeBuffer.Count,
                PropertyData = IntPtr.Add(internalBufferPtr, offset)
            };
            offset += IntPtr.Size;

            if (useZeroMaskingKey)
            {
                properties[3] = new UnsafeNativeMethods.WebSocketProtocolComponent.Property()
                {
                    Type = UnsafeNativeMethods.WebSocketProtocolComponent.PropertyType.DisableMasking,
                    PropertySize = (uint)SizeOfBool,
                    PropertyData = IntPtr.Add(internalBufferPtr, offset)
                };
            }

            return properties;
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal void PinSendBuffer(ArraySegment<byte> payload, out bool bufferHasBeenPinned)
        {
            bufferHasBeenPinned = false;
            WebSocketHelpers.ValidateBuffer(payload.Array, payload.Offset, payload.Count);
            int previousState = Interlocked.Exchange(ref _SendBufferState, SendBufferState.SendPayloadSpecified);

            if (previousState != SendBufferState.None)
            {
                Contract.Assert(false, "'m_SendBufferState' MUST BE 'None' at this point.");
                // Indicates a violation in the API contract that could indicate 
                // memory corruption because the pinned sendbuffer is shared between managed and native code
                throw new AccessViolationException();
            }
            _PinnedSendBuffer = payload;
            _PinnedSendBufferHandle = GCHandle.Alloc(_PinnedSendBuffer.Array, GCHandleType.Pinned);
            bufferHasBeenPinned = true;
            _PinnedSendBufferStartAddress = 
                Marshal.UnsafeAddrOfPinnedArrayElement(_PinnedSendBuffer.Array, _PinnedSendBuffer.Offset).ToInt64();
            _PinnedSendBufferEndAddress = _PinnedSendBufferStartAddress + _PinnedSendBuffer.Count;
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal IntPtr ConvertPinnedSendPayloadToNative(ArraySegment<byte> payload)
        {
            return ConvertPinnedSendPayloadToNative(payload.Array, payload.Offset, payload.Count);
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal IntPtr ConvertPinnedSendPayloadToNative(byte[] buffer, int offset, int count)
        {
            if (!IsPinnedSendPayloadBuffer(buffer, offset, count))
            {
                // Indicates a violation in the API contract that could indicate 
                // memory corruption because the pinned sendbuffer is shared between managed and native code
                throw new AccessViolationException();
            }

            Contract.Assert(Marshal.UnsafeAddrOfPinnedArrayElement(_PinnedSendBuffer.Array,
                _PinnedSendBuffer.Offset).ToInt64() == _PinnedSendBufferStartAddress,
                "'m_PinnedSendBuffer.Array' MUST be pinned during the entire send operation.");

            return new IntPtr(_PinnedSendBufferStartAddress + offset - _PinnedSendBuffer.Offset);
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal ArraySegment<byte> ConvertPinnedSendPayloadFromNative(UnsafeNativeMethods.WebSocketProtocolComponent.Buffer buffer,
            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType)
        {
            if (!IsPinnedSendPayloadBuffer(buffer, bufferType))
            {
                // Indicates a violation in the API contract that could indicate 
                // memory corruption because the pinned sendbuffer is shared between managed and native code
                throw new AccessViolationException();
            }

            Contract.Assert(Marshal.UnsafeAddrOfPinnedArrayElement(_PinnedSendBuffer.Array,
                _PinnedSendBuffer.Offset).ToInt64() == _PinnedSendBufferStartAddress,
                "'m_PinnedSendBuffer.Array' MUST be pinned during the entire send operation.");

            IntPtr bufferData;
            uint bufferSize;

            UnwrapWebSocketBuffer(buffer, bufferType, out bufferData, out bufferSize);

            int internalOffset = (int)(bufferData.ToInt64() - _PinnedSendBufferStartAddress);

            return new ArraySegment<byte>(_PinnedSendBuffer.Array, _PinnedSendBuffer.Offset + internalOffset, (int)bufferSize);
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        private bool IsPinnedSendPayloadBuffer(byte[] buffer, int offset, int count)
        {
            if (_SendBufferState != SendBufferState.SendPayloadSpecified)
            {
                return false;
            }
            
            return object.ReferenceEquals(buffer, _PinnedSendBuffer.Array) &&
                offset >= _PinnedSendBuffer.Offset &&
                offset + count <= _PinnedSendBuffer.Offset + _PinnedSendBuffer.Count;
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        private bool IsPinnedSendPayloadBuffer(UnsafeNativeMethods.WebSocketProtocolComponent.Buffer buffer,
            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType)
        {
            if (_SendBufferState != SendBufferState.SendPayloadSpecified)
            {
                return false;
            }

            IntPtr bufferData;
            uint bufferSize;

            UnwrapWebSocketBuffer(buffer, bufferType, out bufferData, out bufferSize);

            long nativeBufferStartAddress = bufferData.ToInt64();
            long nativeBufferEndAddress = nativeBufferStartAddress + bufferSize;

            return nativeBufferStartAddress >= _PinnedSendBufferStartAddress &&
                nativeBufferEndAddress >= _PinnedSendBufferStartAddress &&
                nativeBufferStartAddress <= _PinnedSendBufferEndAddress &&
                nativeBufferEndAddress <= _PinnedSendBufferEndAddress;
        }

        // This method is only thread safe for races between Abort and at most 1 uncompleted send operation
        internal void ReleasePinnedSendBuffer()
        {
            int previousState = Interlocked.Exchange(ref _SendBufferState, SendBufferState.None);

            if (previousState != SendBufferState.SendPayloadSpecified)
            {
                return;
            }

            if (_PinnedSendBufferHandle.IsAllocated)
            {
                _PinnedSendBufferHandle.Free();
            }

            _PinnedSendBuffer = WebSocketHelpers.EmptyPayload;
        }

        internal void BufferPayload(ArraySegment<byte> payload, 
            int unconsumedDataOffset, 
            WebSocketMessageType messageType, 
            bool endOfMessage)
        {
            ThrowIfDisposed();
            int bytesBuffered = payload.Count - unconsumedDataOffset;

            Contract.Assert(_PayloadOffset == 0,
                "'m_PayloadOffset' MUST be '0' at this point.");
            Contract.Assert(_BufferedPayloadReceiveResult == null || _BufferedPayloadReceiveResult.Count == 0,
                "'m_BufferedPayloadReceiveResult.Count' MUST be '0' at this point.");

            Buffer.BlockCopy(payload.Array,
                payload.Offset + unconsumedDataOffset,
                _PayloadBuffer.Array,
                _PayloadBuffer.Offset,
                bytesBuffered);

            _BufferedPayloadReceiveResult =
                new WebSocketReceiveResult(bytesBuffered, messageType, endOfMessage);

            this.ValidateBufferedPayload();
        }

        internal bool ReceiveFromBufferedPayload(ArraySegment<byte> buffer, out WebSocketReceiveResult receiveResult)
        {
            ThrowIfDisposed();
            ValidateBufferedPayload();

            int bytesTransferred = Math.Min(buffer.Count, _BufferedPayloadReceiveResult.Count);
            receiveResult = WebSocketReceiveResultExtensions.DecrementAndClone(ref _BufferedPayloadReceiveResult, bytesTransferred);

            Buffer.BlockCopy(_PayloadBuffer.Array,
                _PayloadBuffer.Offset + _PayloadOffset,
                buffer.Array,
                buffer.Offset,
                bytesTransferred);

            bool morePayloadBuffered;
            if (_BufferedPayloadReceiveResult.Count == 0)
            {
                _PayloadOffset = 0;
                _BufferedPayloadReceiveResult = null;
                morePayloadBuffered = false;
            }
            else
            {
                _PayloadOffset += bytesTransferred;
                morePayloadBuffered = true;
                this.ValidateBufferedPayload();
            }

            return morePayloadBuffered;
        }

        internal ArraySegment<byte> ConvertNativeBuffer(UnsafeNativeMethods.WebSocketProtocolComponent.Action action,
            UnsafeNativeMethods.WebSocketProtocolComponent.Buffer buffer,
            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType)
        {
            ThrowIfDisposed();

            IntPtr bufferData;
            uint bufferLength;

            UnwrapWebSocketBuffer(buffer, bufferType, out bufferData, out bufferLength);

            if (bufferData == IntPtr.Zero)
            {
                return WebSocketHelpers.EmptyPayload;
            }

            if (this.IsNativeBuffer(bufferData, bufferLength))
            {
                return new ArraySegment<byte>(_InternalBuffer.Array,
                    this.GetOffset(bufferData),
                    (int)bufferLength);
            }

            Contract.Assert(false, "'buffer' MUST reference a memory segment within the pinned InternalBuffer.");
            // Indicates a violation in the contract with native Websocket.dll and could indicate 
            // memory corruption because the internal buffer is shared between managed and native code
            throw new AccessViolationException();
        }

        internal void ConvertCloseBuffer(UnsafeNativeMethods.WebSocketProtocolComponent.Action action,
            UnsafeNativeMethods.WebSocketProtocolComponent.Buffer buffer,
            out WebSocketCloseStatus closeStatus,
            out string reason)
        {
            ThrowIfDisposed();
            IntPtr bufferData;
            uint bufferLength;
            closeStatus = (WebSocketCloseStatus)buffer.CloseStatus.CloseStatus;

            UnwrapWebSocketBuffer(buffer, UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.Close, out bufferData, out bufferLength);

            if (bufferData == IntPtr.Zero)
            {
                reason = null;
            }
            else
            {
                ArraySegment<byte> reasonBlob;
                if (this.IsNativeBuffer(bufferData, bufferLength))
                {
                    reasonBlob = new ArraySegment<byte>(_InternalBuffer.Array,
                        this.GetOffset(bufferData),
                        (int)bufferLength);
                }
                else
                {
                    Contract.Assert(false, "'buffer' MUST reference a memory segment within the pinned InternalBuffer.");
                    // Indicates a violation in the contract with native Websocket.dll and could indicate 
                    // memory corruption because the internal buffer is shared between managed and native code
                    throw new AccessViolationException();
                }

                // No need to wrap DecoderFallbackException for invalid UTF8 chacters, because
                // Encoding.UTF8 will not throw but replace invalid characters instead.
                reason = Encoding.UTF8.GetString(reasonBlob.Array, reasonBlob.Offset, reasonBlob.Count);
            }
        }

        internal void ValidateNativeBuffers(UnsafeNativeMethods.WebSocketProtocolComponent.Action action,
            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType,
            UnsafeNativeMethods.WebSocketProtocolComponent.Buffer[] dataBuffers,
            uint dataBufferCount)
        {
            Contract.Assert(dataBufferCount <= (uint)int.MaxValue, 
                "'dataBufferCount' MUST NOT be bigger than Int32.MaxValue.");
            Contract.Assert(dataBuffers != null, "'dataBuffers' MUST NOT be NULL.");

            ThrowIfDisposed();
            if (dataBufferCount > dataBuffers.Length)
            {
                Contract.Assert(false, "'dataBufferCount' MUST NOT be bigger than 'dataBuffers.Length'.");
                // Indicates a violation in the contract with native Websocket.dll and could indicate 
                // memory corruption because the internal buffer is shared between managed and native code
                throw new AccessViolationException();
            }

            int count = dataBuffers.Length;
            bool isSendActivity = action == UnsafeNativeMethods.WebSocketProtocolComponent.Action.IndicateSendComplete ||
                action == UnsafeNativeMethods.WebSocketProtocolComponent.Action.SendToNetwork;

            if (isSendActivity)
            {
                count = (int)dataBufferCount;
            }

            bool nonZeroBufferFound = false;
            for (int i = 0; i < count; i++)
            {
                UnsafeNativeMethods.WebSocketProtocolComponent.Buffer dataBuffer = dataBuffers[i];

                IntPtr bufferData;
                uint bufferLength;
                UnwrapWebSocketBuffer(dataBuffer, bufferType, out bufferData, out bufferLength);

                if (bufferData == IntPtr.Zero)
                {
                    continue;
                }

                nonZeroBufferFound = true;

                bool isPinnedSendPayloadBuffer = IsPinnedSendPayloadBuffer(dataBuffer, bufferType);

                if (bufferLength > GetMaxBufferSize())
                {
                    if (!isSendActivity || !isPinnedSendPayloadBuffer)
                    {
                        Contract.Assert(false,
                        "'dataBuffer.BufferLength' MUST NOT be bigger than 'm_ReceiveBufferSize' and 'm_SendBufferSize'.");
                        // Indicates a violation in the contract with native Websocket.dll and could indicate 
                        // memory corruption because the internal buffer is shared between managed and native code
                        throw new AccessViolationException();
                    }
                }

                if (!isPinnedSendPayloadBuffer && !IsNativeBuffer(bufferData, bufferLength))
                {
                    Contract.Assert(false,
                        "WebSocketGetAction MUST return a pointer within the pinned internal buffer.");
                    // Indicates a violation in the contract with native Websocket.dll and could indicate 
                    // memory corruption because the internal buffer is shared between managed and native code
                    throw new AccessViolationException();
                }
            }

            if (!nonZeroBufferFound &&
                action != UnsafeNativeMethods.WebSocketProtocolComponent.Action.NoAction &&
                action != UnsafeNativeMethods.WebSocketProtocolComponent.Action.IndicateReceiveComplete &&
                action != UnsafeNativeMethods.WebSocketProtocolComponent.Action.IndicateSendComplete)
            {
                Contract.Assert(false, "At least one 'dataBuffer.Buffer' MUST NOT be NULL.");
            }
        }

        private static int GetNativeSendBufferSize(int sendBufferSize, bool isServerBuffer)
        {
            return isServerBuffer ? MinSendBufferSize : sendBufferSize;
        }

        internal static void UnwrapWebSocketBuffer(UnsafeNativeMethods.WebSocketProtocolComponent.Buffer buffer, 
            UnsafeNativeMethods.WebSocketProtocolComponent.BufferType bufferType, 
            out IntPtr bufferData, 
            out uint bufferLength)
        {
            bufferData = IntPtr.Zero;
            bufferLength = 0;

            switch (bufferType)
            {
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.Close:
                    bufferData = buffer.CloseStatus.ReasonData;
                    bufferLength = buffer.CloseStatus.ReasonLength;
                    break;
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.None:
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryFragment:
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.BinaryMessage:
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Fragment:
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UTF8Message:
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.PingPong:
                case UnsafeNativeMethods.WebSocketProtocolComponent.BufferType.UnsolicitedPong:
                    bufferData = buffer.Data.BufferData;
                    bufferLength = buffer.Data.BufferLength;
                    break;
                default:
                    Contract.Assert(false,
                        string.Format(CultureInfo.InvariantCulture,
                            "BufferType '{0}' is invalid/unknown.",
                            bufferType));
                    break;
            }
        }

        private void ThrowIfDisposed()
        {
            switch (_StateWhenDisposing)
            {
                case int.MinValue:
                    return;
                case (int)WebSocketState.Closed:
                case (int)WebSocketState.Aborted:
                    throw new WebSocketException(WebSocketError.InvalidState,
                        SR.GetString(SR.net_WebSockets_InvalidState_ClosedOrAborted, typeof(WebSocketBase), _StateWhenDisposing));
                default:
                    throw new ObjectDisposedException(GetType().FullName);
            }
        }

        [Conditional("DEBUG"), Conditional("CONTRACTS_FULL")]
        private void ValidateBufferedPayload()
        {
            Contract.Assert(_BufferedPayloadReceiveResult != null,
                "'m_BufferedPayloadReceiveResult' MUST NOT be NULL.");
            Contract.Assert(_BufferedPayloadReceiveResult.Count >= 0,
                "'m_BufferedPayloadReceiveResult.Count' MUST NOT be negative.");
            Contract.Assert(_PayloadOffset >= 0, "'m_PayloadOffset' MUST NOT be smaller than 0.");
            Contract.Assert(_PayloadOffset <= _PayloadBuffer.Count, 
                "'m_PayloadOffset' MUST NOT be bigger than 'm_PayloadBuffer.Count'.");
            Contract.Assert(_PayloadOffset + _BufferedPayloadReceiveResult.Count <= _PayloadBuffer.Count,
                "'m_PayloadOffset + m_PayloadBytesBuffered' MUST NOT be bigger than 'm_PayloadBuffer.Count'.");
        }

        private int GetOffset(IntPtr pBuffer)
        {
            Contract.Assert(pBuffer != IntPtr.Zero, "'pBuffer' MUST NOT be IntPtr.Zero.");
            int offset = (int)(pBuffer.ToInt64() - _StartAddress + _InternalBuffer.Offset);

            Contract.Assert(offset >= 0, "'offset' MUST NOT be negative.");
            return offset;
        }

        [Pure]
        private int GetMaxBufferSize()
        {
            return Math.Max(_ReceiveBufferSize, _SendBufferSize);
        }

        internal bool IsInternalBuffer(byte[] buffer, int offset, int count)
        {
            Contract.Assert(buffer != null, "'buffer' MUST NOT be NULL.");
            Contract.Assert(_InternalBuffer.Array != null, "'m_InternalBuffer.Array' MUST NOT be NULL.");
            Contract.Assert(offset >= 0, "'offset' MUST NOT be negative.");
            Contract.Assert(count >= 0, "'count' MUST NOT be negative.");
            Contract.Assert(offset + count <= buffer.Length, "'offset + count' MUST NOT exceed 'buffer.Length'.");

            return object.ReferenceEquals(buffer, _InternalBuffer.Array);
        }

        internal IntPtr ToIntPtr(int offset)
        {
            Contract.Assert(offset >= 0, "'offset' MUST NOT be negative.");
            Contract.Assert(_StartAddress + offset <= _EndAddress, "'offset' is TOO BIG.");
            return new IntPtr(_StartAddress + offset);
        }

        private bool IsNativeBuffer(IntPtr pBuffer, uint bufferSize)
        {
            Contract.Assert(pBuffer != IntPtr.Zero, "'pBuffer' MUST NOT be NULL.");
            Contract.Assert(bufferSize <= GetMaxBufferSize(),
                "'bufferSize' MUST NOT be bigger than 'm_ReceiveBufferSize' and 'm_SendBufferSize'.");

            long nativeBufferStartAddress = pBuffer.ToInt64();
            long nativeBufferEndAddress = bufferSize + nativeBufferStartAddress;

            Contract.Assert(Marshal.UnsafeAddrOfPinnedArrayElement(_InternalBuffer.Array, _InternalBuffer.Offset).ToInt64() == _StartAddress,
                "'m_InternalBuffer.Array' MUST be pinned for the whole lifetime of a WebSocket.");

            if (nativeBufferStartAddress >= _StartAddress &&
                nativeBufferStartAddress <= _EndAddress &&
                nativeBufferEndAddress >= _StartAddress &&
                nativeBufferEndAddress <= _EndAddress)
            {
                return true;
            }

            return false;
        }

        private void CleanUp()
        {
            if (_GCHandle.IsAllocated)
            {
                _GCHandle.Free();
            }

            ReleasePinnedSendBuffer();
        }

        public static ArraySegment<byte> CreateInternalBufferArraySegment(int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
        {
            Contract.Assert(receiveBufferSize >= MinReceiveBufferSize,
                "'receiveBufferSize' MUST be at least " + MinReceiveBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Contract.Assert(sendBufferSize >= MinSendBufferSize,
                "'sendBufferSize' MUST be at least " + MinSendBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            int internalBufferSize = GetInternalBufferSize(receiveBufferSize, sendBufferSize, isServerBuffer);
            return new ArraySegment<byte>(new byte[internalBufferSize]);
        }

        public static void Validate(int count, int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
        {
            Contract.Assert(receiveBufferSize >= MinReceiveBufferSize,
                "'receiveBufferSize' MUST be at least " + MinReceiveBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Contract.Assert(sendBufferSize >= MinSendBufferSize,
                "'sendBufferSize' MUST be at least " + MinSendBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            int minBufferSize = GetInternalBufferSize(receiveBufferSize, sendBufferSize, isServerBuffer);
            if (count < minBufferSize)
            {
                throw new ArgumentOutOfRangeException("internalBuffer",
                    SR.GetString(SR.net_WebSockets_ArgumentOutOfRange_InternalBuffer, minBufferSize));
            }
        }

        private static int GetInternalBufferSize(int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
        {
            Contract.Assert(receiveBufferSize >= MinReceiveBufferSize,
                "'receiveBufferSize' MUST be at least " + MinReceiveBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Contract.Assert(sendBufferSize >= MinSendBufferSize,
                "'sendBufferSize' MUST be at least " + MinSendBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            Contract.Assert(receiveBufferSize <= MaxBufferSize,
                "'receiveBufferSize' MUST be less than or equal to " + MaxBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Contract.Assert(sendBufferSize <= MaxBufferSize,
                "'sendBufferSize' MUST be at less than or equal to " + MaxBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            int nativeSendBufferSize = GetNativeSendBufferSize(sendBufferSize, isServerBuffer);
            return (2 * receiveBufferSize) + nativeSendBufferSize + NativeOverheadBufferSize + PropertyBufferSize;
        }

        private static class SendBufferState
        {
            public const int None = 0;
            public const int SendPayloadSpecified = 1;
        }
    }
}
