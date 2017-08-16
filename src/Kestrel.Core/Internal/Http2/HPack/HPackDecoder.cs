// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    public class HPackDecoder
    {
        private enum State
        {
            Ready,
            HeaderFieldIndex,
            HeaderNameIndex,
            HeaderNameLength,
            HeaderNameLengthContinue,
            HeaderName,
            HeaderValueLength,
            HeaderValueLengthContinue,
            HeaderValue,
            DynamicTableSize
        }

        private const byte IndexedHeaderFieldMask = 0x80;
        private const byte LiteralHeaderFieldWithIncrementalIndexingMask = 0x40;
        private const byte LiteralHeaderFieldWithoutIndexingMask = 0x00;
        private const byte LiteralHeaderFieldNeverIndexedMask = 0x10;
        private const byte DynamicTableSizeUpdateMask = 0x20;
        private const byte HuffmanMask = 0x80;

        private const int IndexedHeaderFieldPrefix = 7;
        private const int LiteralHeaderFieldWithIncrementalIndexingPrefix = 6;
        private const int LiteralHeaderFieldWithoutIndexingPrefix = 4;
        private const int LiteralHeaderFieldNeverIndexedPrefix = 4;
        private const int DynamicTableSizeUpdatePrefix = 5;
        private const int StringLengthPrefix = 7;

        private readonly DynamicTable _dynamicTable = new DynamicTable(4096);
        private readonly IntegerDecoder _integerDecoder = new IntegerDecoder();

        private State _state = State.Ready;
        // TODO: add new HTTP/2 header size limit and allocate accordingly
        private byte[] _stringOctets = new byte[Http2Frame.MinAllowedMaxFrameSize];
        private string _headerName = string.Empty;
        private string _headerValue = string.Empty;
        private int _stringLength;
        private int _stringIndex;
        private bool _index;
        private bool _huffman;

        public void Decode(Span<byte> data, IHeaderDictionary headers)
        {
            for (var i = 0; i < data.Length; i++)
            {
                OnByte(data[i], headers);
            }
        }

        public void OnByte(byte b, IHeaderDictionary headers)
        {
            switch (_state)
            {
                case State.Ready:
                    if ((b & IndexedHeaderFieldMask) == IndexedHeaderFieldMask)
                    {
                        if (_integerDecoder.BeginDecode((byte)(b & ~IndexedHeaderFieldMask), IndexedHeaderFieldPrefix))
                        {
                            OnIndexedHeaderField(_integerDecoder.Value, headers);
                        }
                        else
                        {
                            _state = State.HeaderFieldIndex;
                        }
                    }
                    else if ((b & LiteralHeaderFieldWithIncrementalIndexingMask) == LiteralHeaderFieldWithIncrementalIndexingMask)
                    {
                        _index = true;
                        var val = b & ~LiteralHeaderFieldWithIncrementalIndexingMask;

                        if (val == 0)
                        {
                            _state = State.HeaderNameLength;
                        }
                        else if (_integerDecoder.BeginDecode((byte)val, LiteralHeaderFieldWithIncrementalIndexingPrefix))
                        {
                            OnIndexedHeaderName(_integerDecoder.Value);
                        }
                        else
                        {
                            _state = State.HeaderNameIndex;
                        }
                    }
                    else if ((b & LiteralHeaderFieldWithoutIndexingMask) == LiteralHeaderFieldWithoutIndexingMask)
                    {
                        _index = false;
                        var val = b & ~LiteralHeaderFieldWithoutIndexingMask;

                        if (val == 0)
                        {
                            _state = State.HeaderNameLength;
                        }
                        else if (_integerDecoder.BeginDecode((byte)val, LiteralHeaderFieldWithoutIndexingPrefix))
                        {
                            OnIndexedHeaderName(_integerDecoder.Value);
                        }
                        else
                        {
                            _state = State.HeaderNameIndex;
                        }
                    }
                    else if ((b & LiteralHeaderFieldNeverIndexedMask) == LiteralHeaderFieldNeverIndexedMask)
                    {
                        _index = false;
                        var val = b & ~LiteralHeaderFieldNeverIndexedMask;

                        if (val == 0)
                        {
                            _state = State.HeaderNameLength;
                        }
                        else if (_integerDecoder.BeginDecode((byte)val, LiteralHeaderFieldNeverIndexedPrefix))
                        {
                            OnIndexedHeaderName(_integerDecoder.Value);
                        }
                        else
                        {
                            _state = State.HeaderNameIndex;
                        }
                    }
                    else if ((b & DynamicTableSizeUpdateMask) == DynamicTableSizeUpdateMask)
                    {
                        if (_integerDecoder.BeginDecode((byte)(b & ~DynamicTableSizeUpdateMask), DynamicTableSizeUpdatePrefix))
                        {
                            // TODO: validate that it's less than what's defined via SETTINGS
                            _dynamicTable.Resize(_integerDecoder.Value);
                        }
                        else
                        {
                            _state = State.DynamicTableSize;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    break;
                case State.HeaderFieldIndex:
                    if (_integerDecoder.Decode(b))
                    {
                        OnIndexedHeaderField(_integerDecoder.Value, headers);
                    }

                    break;
                case State.HeaderNameIndex:
                    if (_integerDecoder.Decode(b))
                    {
                        OnIndexedHeaderName(_integerDecoder.Value);
                    }

                    break;
                case State.HeaderNameLength:
                    _huffman = (b & HuffmanMask) == HuffmanMask;

                    if (_integerDecoder.BeginDecode((byte)(b & ~HuffmanMask), StringLengthPrefix))
                    {
                        OnStringLength(_integerDecoder.Value, nextState: State.HeaderName);
                    }
                    else
                    {
                        _state = State.HeaderNameLengthContinue;
                    }

                    break;
                case State.HeaderNameLengthContinue:
                    if (_integerDecoder.Decode(b))
                    {
                        OnStringLength(_integerDecoder.Value, nextState: State.HeaderName);
                    }

                    break;
                case State.HeaderName:
                    _stringOctets[_stringIndex++] = b;

                    if (_stringIndex == _stringLength)
                    {
                        _headerName = OnString(nextState: State.HeaderValueLength);
                    }

                    break;
                case State.HeaderValueLength:
                    _huffman = (b & HuffmanMask) == HuffmanMask;

                    if (_integerDecoder.BeginDecode((byte)(b & ~HuffmanMask), StringLengthPrefix))
                    {
                        OnStringLength(_integerDecoder.Value, nextState: State.HeaderValue);
                    }
                    else
                    {
                        _state = State.HeaderValueLengthContinue;
                    }

                    break;
                case State.HeaderValueLengthContinue:
                    if (_integerDecoder.Decode(b))
                    {
                        OnStringLength(_integerDecoder.Value, nextState: State.HeaderValue);
                    }

                    break;
                case State.HeaderValue:
                    _stringOctets[_stringIndex++] = b;

                    if (_stringIndex == _stringLength)
                    {
                        _headerValue = OnString(nextState: State.Ready);
                        headers.Append(_headerName, _headerValue);

                        if (_index)
                        {
                            _dynamicTable.Insert(_headerName, _headerValue);
                        }
                    }

                    break;
                case State.DynamicTableSize:
                    if (_integerDecoder.Decode(b))
                    {
                        // TODO: validate that it's less than what's defined via SETTINGS
                        _dynamicTable.Resize(_integerDecoder.Value);
                        _state = State.Ready;
                    }

                    break;
                default:
                    // Can't happen
                    throw new InvalidOperationException();
            }
        }

        private void OnIndexedHeaderField(int index, IHeaderDictionary headers)
        {
            var header = GetHeader(index);
            headers.Append(header.Name, header.Value);
            _state = State.Ready;
        }

        private void OnIndexedHeaderName(int index)
        {
            var header = GetHeader(index);
            _headerName = header.Name;
            _state = State.HeaderValueLength;
        }

        private void OnStringLength(int length, State nextState)
        {
            _stringLength = length;
            _stringIndex = 0;
            _state = nextState;
        }

        private string OnString(State nextState)
        {
            _state = nextState;
            return _huffman
                ? Huffman.Decode(_stringOctets, 0, _stringLength)
                : Encoding.ASCII.GetString(_stringOctets, 0, _stringLength);
        }

        private HeaderField GetHeader(int index) => index <= StaticTable.Instance.Length
            ? StaticTable.Instance[index - 1]
            : _dynamicTable[index - StaticTable.Instance.Length - 1];
    }
}
