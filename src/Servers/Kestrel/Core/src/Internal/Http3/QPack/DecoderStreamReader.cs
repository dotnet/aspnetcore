// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Net.Http.HPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    internal class DecoderStreamReader
    {
        private enum State
        {
            Ready,
            HeaderAckowledgement,
            StreamCancellation,
            InsertCountIncrement
        }

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 1 |      Stream ID(7+)       |
        //+---+---------------------------+
        private const byte HeaderAcknowledgementMask = 0x80;
        private const byte HeaderAcknowledgementRepresentation = 0x80;
        private const byte HeaderAcknowledgementPrefixMask = 0x7F;
        private const int HeaderAcknowledgementPrefix = 7;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 1 |     Stream ID(6+)    |
        //+---+---+-----------------------+
        private const byte StreamCancellationMask = 0xC0;
        private const byte StreamCancellationRepresentation = 0x40;
        private const byte StreamCancellationPrefixMask = 0x3F;
        private const int StreamCancellationPrefix = 6;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 |     Increment(6+)    |
        //+---+---+-----------------------+
        private const byte InsertCountIncrementMask = 0xC0;
        private const byte InsertCountIncrementRepresentation = 0x00;
        private const byte InsertCountIncrementPrefixMask = 0x3F;
        private const int InsertCountIncrementPrefix = 6;

        private IntegerDecoder _integerDecoder = new IntegerDecoder();
        private State _state;

        public DecoderStreamReader()
        {
        }

        public void Read(ReadOnlySequence<byte> data)
        {
            foreach (var segment in data)
            {
                var span = segment.Span;
                for (var i = 0; i < span.Length; i++)
                {
                    OnByte(span[i]);
                }
            }
        }

        private void OnByte(byte b)
        {
            int intResult;
            int prefixInt;
            switch (_state)
            {
                case State.Ready:
                    if ((b & HeaderAcknowledgementMask) == HeaderAcknowledgementRepresentation)
                    {
                        prefixInt = HeaderAcknowledgementPrefixMask & b;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, HeaderAcknowledgementPrefix, out intResult))
                        {
                            OnHeaderAcknowledgement(intResult);
                        }
                        else
                        {
                            _state = State.HeaderAckowledgement;
                        }
                    }
                    else if ((b & StreamCancellationMask) == StreamCancellationRepresentation)
                    {
                        prefixInt = StreamCancellationPrefixMask & b;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, StreamCancellationPrefix, out intResult))
                        {
                            OnStreamCancellation(intResult);
                        }
                        else
                        {
                            _state = State.StreamCancellation;
                        }
                    }
                    else if ((b & InsertCountIncrementMask) == InsertCountIncrementRepresentation)
                    {
                        prefixInt = InsertCountIncrementPrefixMask & b;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, InsertCountIncrementPrefix, out intResult))
                        {
                            OnInsertCountIncrement(intResult);
                        }
                        else
                        {
                            _state = State.InsertCountIncrement;
                        }
                    }
                    break;
            }
        }

        private void OnInsertCountIncrement(int intResult)
        {
            // increment some count.
            _state = State.Ready;
        }

        private void OnStreamCancellation(int streamId)
        {
            // Remove stream?
            _state = State.Ready;
        }

        private void OnHeaderAcknowledgement(int intResult)
        {
            // Acknowledge header somehow
            _state = State.Ready;
        }
    }
}
