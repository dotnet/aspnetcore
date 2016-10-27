// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public abstract class FrameHeaders : IHeaderDictionary
    {
        protected bool _isReadOnly;
        protected Dictionary<string, StringValues> MaybeUnknown;

        protected Dictionary<string, StringValues> Unknown => MaybeUnknown ?? (MaybeUnknown = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase));

        StringValues IHeaderDictionary.this[string key]
        {
            get
            {
                StringValues value;
                TryGetValueFast(key, out value);
                return value;
            }
            set
            {
                if (_isReadOnly)
                {
                    ThrowHeadersReadOnlyException();
                }
                SetValueFast(key, value);
            }
        }

        StringValues IDictionary<string, StringValues>.this[string key]
        {
            get
            {
                // Unlike the IHeaderDictionary version, this getter will throw a KeyNotFoundException.
                return GetValueFast(key);
            }
            set
            {
                ((IHeaderDictionary)this)[key] = value;
            }
        }

        protected void ThrowHeadersReadOnlyException()
        {
            throw new InvalidOperationException("Headers are read-only, response has already started.");
        }

        protected void ThrowArgumentException()
        {
            throw new ArgumentException();
        }

        protected void ThrowKeyNotFoundException()
        {
            throw new KeyNotFoundException();
        }

        protected void ThrowDuplicateKeyException()
        {
            throw new ArgumentException("An item with the same key has already been added.");
        }

        int ICollection<KeyValuePair<string, StringValues>>.Count => GetCountFast();

        bool ICollection<KeyValuePair<string, StringValues>>.IsReadOnly => _isReadOnly;

        ICollection<string> IDictionary<string, StringValues>.Keys => ((IDictionary<string, StringValues>)this).Select(pair => pair.Key).ToList();

        ICollection<StringValues> IDictionary<string, StringValues>.Values => ((IDictionary<string, StringValues>)this).Select(pair => pair.Value).ToList();

        public void SetReadOnly()
        {
            _isReadOnly = true;
        }

        public void Reset()
        {
            _isReadOnly = false;
            ClearFast();
        }

        protected static StringValues AppendValue(StringValues existing, string append)
        {
            return StringValues.Concat(existing, append);
        }

        protected static int BitCount(long value)
        {
            // see https://github.com/dotnet/corefx/blob/5965fd3756bc9dd9c89a27621eb10c6931126de2/src/System.Reflection.Metadata/src/System/Reflection/Internal/Utilities/BitArithmetic.cs

            const ulong Mask01010101 = 0x5555555555555555UL;
            const ulong Mask00110011 = 0x3333333333333333UL;
            const ulong Mask00001111 = 0x0F0F0F0F0F0F0F0FUL;
            const ulong Mask00000001 = 0x0101010101010101UL;

            var v = (ulong)value;

            v = v - ((v >> 1) & Mask01010101);
            v = (v & Mask00110011) + ((v >> 2) & Mask00110011);
            return (int)(unchecked(((v + (v >> 4)) & Mask00001111) * Mask00000001) >> 56);
        }

        protected virtual int GetCountFast()
        { throw new NotImplementedException(); }

        protected virtual StringValues GetValueFast(string key)
        { throw new NotImplementedException(); }

        protected virtual bool TryGetValueFast(string key, out StringValues value)
        { throw new NotImplementedException(); }

        protected virtual void SetValueFast(string key, StringValues value)
        { throw new NotImplementedException(); }

        protected virtual void AddValueFast(string key, StringValues value)
        { throw new NotImplementedException(); }

        protected virtual bool RemoveFast(string key)
        { throw new NotImplementedException(); }

        protected virtual void ClearFast()
        { throw new NotImplementedException(); }

        protected virtual void CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        { throw new NotImplementedException(); }

        protected virtual IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
        { throw new NotImplementedException(); }

        void ICollection<KeyValuePair<string, StringValues>>.Add(KeyValuePair<string, StringValues> item)
        {
            ((IDictionary<string, StringValues>)this).Add(item.Key, item.Value);
        }

        void IDictionary<string, StringValues>.Add(string key, StringValues value)
        {
            if (_isReadOnly)
            {
                ThrowHeadersReadOnlyException();
            }
            AddValueFast(key, value);
        }

        void ICollection<KeyValuePair<string, StringValues>>.Clear()
        {
            if (_isReadOnly)
            {
                ThrowHeadersReadOnlyException();
            }
            ClearFast();
        }

        bool ICollection<KeyValuePair<string, StringValues>>.Contains(KeyValuePair<string, StringValues> item)
        {
            StringValues value;
            return
                TryGetValueFast(item.Key, out value) &&
                value.Equals(item.Value);
        }

        bool IDictionary<string, StringValues>.ContainsKey(string key)
        {
            StringValues value;
            return TryGetValueFast(key, out value);
        }

        void ICollection<KeyValuePair<string, StringValues>>.CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            CopyToFast(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorFast();
        }

        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
        {
            return GetEnumeratorFast();
        }

        bool ICollection<KeyValuePair<string, StringValues>>.Remove(KeyValuePair<string, StringValues> item)
        {
            StringValues value;
            return
                TryGetValueFast(item.Key, out value) &&
                value.Equals(item.Value) &&
                RemoveFast(item.Key);
        }

        bool IDictionary<string, StringValues>.Remove(string key)
        {
            if (_isReadOnly)
            {
                ThrowHeadersReadOnlyException();
            }
            return RemoveFast(key);
        }

        bool IDictionary<string, StringValues>.TryGetValue(string key, out StringValues value)
        {
            return TryGetValueFast(key, out value);
        }

        public static void ValidateHeaderCharacters(StringValues headerValues)
        {
            foreach (var value in headerValues)
            {
                ValidateHeaderCharacters(value);
            }
        }

        public static void ValidateHeaderCharacters(string headerCharacters)
        {
            if (headerCharacters != null)
            {
                foreach (var ch in headerCharacters)
                {
                    if (ch < 0x20 || ch > 0x7E)
                    {
                        ThrowInvalidHeaderCharacter(ch);
                    }
                }
            }
        }

        public static unsafe long ParseContentLength(StringValues value)
        {
            var input = value.ToString();
            var parsed = 0L;

            fixed (char* ptr = input)
            {
                var ch = (ushort*)ptr;
                var end = ch + input.Length;

                if (ch == end)
                {
                    ThrowInvalidContentLengthException(value);
                }

                ushort digit = 0;
                while (ch < end && (digit = (ushort)(*ch - 0x30)) <= 9)
                {
                    parsed *= 10;
                    parsed += digit;
                    ch++;
                }

                if (ch != end)
                {
                    ThrowInvalidContentLengthException(value);
                }
            }

            return parsed;
        }

        public static unsafe ConnectionOptions ParseConnection(StringValues connection)
        {
            var connectionOptions = ConnectionOptions.None;

            foreach (var value in connection)
            {
                fixed (char* ptr = value)
                {
                    var ch = ptr;
                    var tokenEnd = ch;
                    var end = ch + value.Length;

                    while (ch < end)
                    {
                        while (tokenEnd < end && *tokenEnd != ',')
                        {
                            tokenEnd++;
                        }

                        while (ch < tokenEnd && *ch == ' ')
                        {
                            ch++;
                        }

                        var tokenLength = tokenEnd - ch;

                        if (tokenLength >= 9 && (*ch | 0x20) == 'k')
                        {
                            if ((*++ch | 0x20) == 'e' &&
                                (*++ch | 0x20) == 'e' &&
                                (*++ch | 0x20) == 'p' &&
                                *++ch == '-' &&
                                (*++ch | 0x20) == 'a' &&
                                (*++ch | 0x20) == 'l' &&
                                (*++ch | 0x20) == 'i' &&
                                (*++ch | 0x20) == 'v' &&
                                (*++ch | 0x20) == 'e')
                            {
                                ch++;
                                while (ch < tokenEnd && *ch == ' ')
                                {
                                    ch++;
                                }

                                if (ch == tokenEnd)
                                {
                                    connectionOptions |= ConnectionOptions.KeepAlive;
                                }
                            }
                        }
                        else if (tokenLength >= 7 && (*ch | 0x20) == 'u')
                        {
                            if ((*++ch | 0x20) == 'p' &&
                                (*++ch | 0x20) == 'g' &&
                                (*++ch | 0x20) == 'r' &&
                                (*++ch | 0x20) == 'a' &&
                                (*++ch | 0x20) == 'd' &&
                                (*++ch | 0x20) == 'e')
                            {
                                ch++;
                                while (ch < tokenEnd && *ch == ' ')
                                {
                                    ch++;
                                }

                                if (ch == tokenEnd)
                                {
                                    connectionOptions |= ConnectionOptions.Upgrade;
                                }
                            }
                        }
                        else if (tokenLength >= 5 && (*ch | 0x20) == 'c')
                        {
                            if ((*++ch | 0x20) == 'l' &&
                                (*++ch | 0x20) == 'o' &&
                                (*++ch | 0x20) == 's' &&
                                (*++ch | 0x20) == 'e')
                            {
                                ch++;
                                while (ch < tokenEnd && *ch == ' ')
                                {
                                    ch++;
                                }

                                if (ch == tokenEnd)
                                {
                                    connectionOptions |= ConnectionOptions.Close;
                                }
                            }
                        }

                        tokenEnd++;
                        ch = tokenEnd;
                    }
                }
            }

            return connectionOptions;
        }

        public static unsafe TransferCoding GetFinalTransferCoding(StringValues transferEncoding)
        {
            var transferEncodingOptions = TransferCoding.None;

            foreach (var value in transferEncoding)
            {
                fixed (char* ptr = value)
                {
                    var ch = ptr;
                    var tokenEnd = ch;
                    var end = ch + value.Length;

                    while (ch < end)
                    {
                        while (tokenEnd < end && *tokenEnd != ',')
                        {
                            tokenEnd++;
                        }

                        while (ch < tokenEnd && *ch == ' ')
                        {
                            ch++;
                        }

                        var tokenLength = tokenEnd - ch;

                        if (tokenLength >= 7 && (*ch | 0x20) == 'c')
                        {
                            if ((*++ch | 0x20) == 'h' &&
                                (*++ch | 0x20) == 'u' &&
                                (*++ch | 0x20) == 'n' &&
                                (*++ch | 0x20) == 'k' &&
                                (*++ch | 0x20) == 'e' &&
                                (*++ch | 0x20) == 'd')
                            {
                                ch++;
                                while (ch < tokenEnd && *ch == ' ')
                                {
                                    ch++;
                                }

                                if (ch == tokenEnd)
                                {
                                    transferEncodingOptions = TransferCoding.Chunked;
                                }
                            }
                        }

                        if (tokenLength > 0 && ch != tokenEnd)
                        {
                            transferEncodingOptions = TransferCoding.Other;
                        }

                        tokenEnd++;
                        ch = tokenEnd;
                    }
                }
            }

            return transferEncodingOptions;
        }

        private static void ThrowInvalidContentLengthException(string value)
        {
            throw new InvalidOperationException($"Invalid Content-Length: \"{value}\". Value must be a positive integral number.");
        }

        private static void ThrowInvalidHeaderCharacter(char ch)
        {
            throw new InvalidOperationException(string.Format("Invalid non-ASCII or control character in header: 0x{0:X4}", (ushort)ch));
        }
    }
}
