// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNet.Http
{
    public static class SessionCollectionExtensions
    {
        public static void SetInt(this ISessionCollection session, string key, int value)
        {
            var bytes = new byte[]
            {
                (byte)(value >> 24),
                (byte)(0xFF & (value >> 16)),
                (byte)(0xFF & (value >> 8)),
                (byte)(0xFF & value)
            };
            session.Set(key, bytes);
        }

        public static int? GetInt(this ISessionCollection session, string key)
        {
            var data = session.Get(key);
            if (data == null || data.Length < 4)
            {
                return null;
            }
            return data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
        }

        public static void SetString(this ISessionCollection session, string key, string value)
        {
            session.Set(key, Encoding.UTF8.GetBytes(value));
        }

        public static string GetString(this ISessionCollection session, string key)
        {
            var data = session.Get(key);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data);
        }

        public static byte[] Get(this ISessionCollection session, string key)
        {
            byte[] value = null;
            session.TryGetValue(key, out value);
            return value;
        }

        public static void Set(this ISessionCollection session, string key, byte[] value)
        {
            session.Set(key, new ArraySegment<byte>(value));
        }
    }
}