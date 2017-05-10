// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Identity.Service.Serialization
{
    public class TokenDataSerializer<TToken> : IDataSerializer<TToken>
        where TToken : Token
    {
        private readonly IdentityServiceOptions _options;
        private readonly JsonSerializer _serializer;
        private readonly IArrayPool<char> _pool;

        public TokenDataSerializer(
            IOptions<IdentityServiceOptions> options,
            ArrayPool<char> arrayPool)
        {
            _options = options.Value;
            _serializer = JsonSerializer.Create(_options.SerializationSettings);
            _pool = new JsonArrayPool(arrayPool);
        }

        private class JsonArrayPool : IArrayPool<char>
        {
            private ArrayPool<char> _pool;

            public JsonArrayPool(ArrayPool<char> pool)
            {
                _pool = pool;
            }
            public char[] Rent(int minimumLength)
            {
                return _pool.Rent(minimumLength);
            }

            public void Return(char[] array)
            {
                _pool.Return(array, clearArray: true);
            }
        }

        public TToken Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data, writable: false))
            {
                using (var streamWriter = new StreamReader(stream, Encoding.UTF8))
                {
                    using (var writer = new JsonTextReader(streamWriter) { ArrayPool = _pool })
                    {
                        return _serializer.Deserialize<TToken>(writer);
                    }
                }
            }
        }

        public byte[] Serialize(TToken model)
        {
            using (var stream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                {
                    using (var writer = new JsonTextWriter(streamWriter) { ArrayPool = _pool })
                    {
                        _serializer.Serialize(writer, model);
                    }
                }

                stream.Seek(0, SeekOrigin.Begin);
                return stream.ToArray();
            }
        }
    }
}
