// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http
{
    public static class HttpMethods
    {
        public const string Connect = "CONNECT";
        public const string Delete = "DELETE";
        public const string Get = "GET";
        public const string Head = "HEAD";
        public const string Options = "OPTIONS";
        public const string Patch = "PATCH";
        public const string Post = "POST";
        public const string Put = "PUT";
        public const string Trace = "TRACE";

        public static bool IsConnect(string method)
        {
            return object.ReferenceEquals(Connect, method) || StringComparer.OrdinalIgnoreCase.Equals(Connect, method);
        }

        public static bool IsDelete(string method)
        {
            return object.ReferenceEquals(Delete, method) || StringComparer.OrdinalIgnoreCase.Equals(Delete, method);
        }

        public static bool IsGet(string method)
        {
            return object.ReferenceEquals(Get, method) || StringComparer.OrdinalIgnoreCase.Equals(Get, method);
        }

        public static bool IsHead(string method)
        {
            return object.ReferenceEquals(Head, method) || StringComparer.OrdinalIgnoreCase.Equals(Head, method);
        }

        public static bool IsOptions(string method)
        {
            return object.ReferenceEquals(Options, method) || StringComparer.OrdinalIgnoreCase.Equals(Options, method);
        }

        public static bool IsPatch(string method)
        {
            return object.ReferenceEquals(Patch, method) || StringComparer.OrdinalIgnoreCase.Equals(Patch, method);
        }

        public static bool IsPost(string method)
        {
            return object.ReferenceEquals(Post, method) || StringComparer.OrdinalIgnoreCase.Equals(Post, method);
        }

        public static bool IsPut(string method)
        {
            return object.ReferenceEquals(Put, method) || StringComparer.OrdinalIgnoreCase.Equals(Put, method);
        }

        public static bool IsTrace(string method)
        {
            return object.ReferenceEquals(Trace, method) || StringComparer.OrdinalIgnoreCase.Equals(Trace, method);
        }
    }
}
