// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net.Http.HPack;
using System.Net.Http.QPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    internal class EncoderStreamReader
    {
        private enum State
        {
            Ready,
            DynamicTableCapcity,
            NameIndex,
            NameLength,
            Name,
            ValueLength,
            ValueLengthContinue,
            Value,
            Duplicate
        }

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 | 1 |   Capacity(5+)   |
        //+---+---+---+-------------------+
        private const byte DynamicTableCapacityMask = 0xE0;
        private const byte DynamicTableCapacityRepresentation = 0x20;
        private const byte DynamicTableCapacityPrefixMask = 0x1F;
        private const int DynamicTableCapacityPrefix = 5;


        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 1 | S |    Name Index(6+)    |
        //+---+---+-----------------------+
        //| H |     Value Length(7+)     |
        //+---+---------------------------+
        //|  Value String(Length bytes)  |
        //+-------------------------------+
        private const byte InsertWithNameReferenceMask = 0x80;
        private const byte InsertWithNameReferenceRepresentation = 0x80;
        private const byte InsertWithNameReferencePrefixMask = 0x3F;
        private const byte InsertWithNameReferenceStaticMask = 0x40;
        private const int InsertWithNameReferencePrefix = 6;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 1 | H | Name Length(5+)  |
        //+---+---+---+-------------------+
        //|  Name String(Length bytes)   |
        //+---+---------------------------+
        //| H |     Value Length(7+)     |
        //+---+---------------------------+
        //|  Value String(Length bytes)  |
        //+-------------------------------+
        private const byte InsertWithoutNameReferenceMask = 0xC0;
        private const byte InsertWithoutNameReferenceRepresentation = 0x40;
        private const byte InsertWithoutNameReferencePrefixMask = 0x1F;
        private const byte InsertWithoutNameReferenceHuffmanMask = 0x20;
        private const int InsertWithoutNameReferencePrefix = 5;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 | 0 |    Index(5+)     |
        //+---+---+---+-------------------+
        private const byte DuplicateMask = 0xE0;
        private const byte DuplicateRepresentation = 0x00;
        private const byte DuplicatePrefixMask = 0x1F;
        private const int DuplicatePrefix = 5;

        private const int StringLengthPrefix = 7;
        private const byte HuffmanMask = 0x80;

        private bool _s;
        private byte[] _stringOctets;
        private byte[] _headerNameOctets;
        private byte[] _headerValueOctets;
        private byte[] _headerName;
        private int _headerNameLength;
        private int _headerValueLength;
        private int _stringLength;
        private int _stringIndex;
        private DynamicTable _dynamicTable = new DynamicTable(1000); // TODO figure out architecture.

        private readonly IntegerDecoder _integerDecoder = new IntegerDecoder();
        private State _state = State.Ready;
        private bool _huffman;

        public EncoderStreamReader(int maxRequestHeaderFieldSize)
        {
            // TODO how to propagate dynamic table around.

            _stringOctets = new byte[maxRequestHeaderFieldSize];
            _headerNameOctets = new byte[maxRequestHeaderFieldSize];
            _headerValueOctets = new byte[maxRequestHeaderFieldSize];
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
                    if ((b & DynamicTableCapacityMask) == DynamicTableCapacityRepresentation)
                    {
                        prefixInt = DynamicTableCapacityPrefixMask & b;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, DynamicTableCapacityPrefix, out intResult))
                        {
                            OnDynamicTableCapacity(intResult);
                        }
                        else
                        {
                            _state = State.DynamicTableCapcity;
                        }
                    }
                    else if ((b & InsertWithNameReferenceMask) == InsertWithNameReferenceRepresentation)
                    {
                        prefixInt = InsertWithNameReferencePrefixMask & b;
                        _s = (InsertWithNameReferenceStaticMask & b) == InsertWithNameReferenceStaticMask;

                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, InsertWithNameReferencePrefix, out intResult))
                        {
                            OnNameIndex(intResult);
                        }
                        else
                        {
                            _state = State.NameIndex;
                        }
                    }
                    else if ((b & InsertWithoutNameReferenceMask) == InsertWithoutNameReferenceRepresentation)
                    {
                        prefixInt = InsertWithoutNameReferencePrefixMask & b;
                        _huffman = (InsertWithoutNameReferenceHuffmanMask & b) == InsertWithoutNameReferenceHuffmanMask;

                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, InsertWithoutNameReferencePrefix, out intResult))
                        {
                            OnStringLength(intResult, State.Name);
                        }
                        else
                        {
                            _state = State.NameIndex;
                        }
                    }
                    else if ((b & DuplicateMask) == DuplicateRepresentation)
                    {
                        prefixInt = DuplicatePrefixMask & b;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, DuplicatePrefix, out intResult))
                        {
                            OnDuplicate(intResult);
                        }
                        else
                        {
                            _state = State.Duplicate;
                        }
                    }
                    break;
                case State.DynamicTableCapcity:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnDynamicTableCapacity(intResult);
                    }
                    break;
                case State.NameIndex:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnNameIndex(intResult);
                    }
                    break;
                case State.NameLength:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnStringLength(intResult, nextState: State.Name);
                    }
                    break;
                case State.Name:
                    _stringOctets[_stringIndex++] = b;

                    if (_stringIndex == _stringLength)
                    {
                        OnString(nextState: State.ValueLength);
                    }

                    break;
                case State.ValueLength:
                    _huffman = (b & HuffmanMask) != 0;

                    // TODO confirm this.
                    if (_integerDecoder.BeginTryDecode((byte)(b & ~HuffmanMask), StringLengthPrefix, out intResult))
                    {
                        OnStringLength(intResult, nextState: State.Value);
                        if (intResult == 0)
                        {
                            ProcessValue();
                        }
                    }
                    else
                    {
                        _state = State.ValueLengthContinue;
                    }
                    break;
                case State.ValueLengthContinue:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnStringLength(intResult, nextState: State.Value);
                        if (intResult == 0)
                        {
                            ProcessValue();
                        }
                    }
                    break;
                case State.Value:
                    _stringOctets[_stringIndex++] = b;
                    if (_stringIndex == _stringLength)
                    {
                        ProcessValue();
                    }
                    break;
                case State.Duplicate:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnDuplicate(intResult);
                    }
                    break;
            }
        }


        private void OnStringLength(int length, State nextState)
        {
            if (length > _stringOctets.Length)
            {
                throw new QPackDecodingException(/*CoreStrings.FormatQPackStringLengthTooLarge(length, _stringOctets.Length)*/);
            }

            _stringLength = length;
            _stringIndex = 0;
            _state = nextState;
        }

        private void ProcessValue()
        {
            OnString(nextState: State.Ready);
            var headerNameSpan = new Span<byte>(_headerName, 0, _headerNameLength);
            var headerValueSpan = new Span<byte>(_headerValueOctets, 0, _headerValueLength);
            _dynamicTable.Insert(headerNameSpan, headerValueSpan);
        }

        private void OnString(State nextState)
        {
            int Decode(byte[] dst)
            {
                if (_huffman)
                {
                    return Huffman.Decode(new ReadOnlySpan<byte>(_stringOctets, 0, _stringLength), ref dst);
                }
                else
                {
                    Buffer.BlockCopy(_stringOctets, 0, dst, 0, _stringLength);
                    return _stringLength;
                }
            }

            try
            {
                if (_state == State.Name)
                {
                    _headerName = _headerNameOctets;
                    _headerNameLength = Decode(_headerNameOctets);
                }
                else
                {
                    _headerValueLength = Decode(_headerValueOctets);
                }
            }
            catch (HuffmanDecodingException ex)
            {
                throw new QPackDecodingException(""/*CoreStrings.QPackHuffmanError*/, ex);
            }

            _state = nextState;
        }

        private void OnNameIndex(int index)
        {
            var header = GetHeader(index);
            _headerName = header.Name;
            _headerNameLength = header.Name.Length;
            _state = State.ValueLength;
        }

        private void OnDynamicTableCapacity(int dynamicTableSize)
        {
            // Call Decoder to update the table size.
            _dynamicTable.Resize(dynamicTableSize);
            _state = State.Ready;
        }

        private void OnDuplicate(int index)
        {
            _dynamicTable.Duplicate(index);
            _state = State.Ready;
        }

        private System.Net.Http.QPack.HeaderField GetHeader(int index)
        {
            try
            {
                return _s ? H3StaticTable.Instance[index] : _dynamicTable[index];
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new QPackDecodingException( "" /*CoreStrings.FormatQPackErrorIndexOutOfRange(index)*/, ex);
            }
        }
    }
}
