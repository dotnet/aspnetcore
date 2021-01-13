// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http
{
    public static class HttpMethods
    {
        // We are intentionally using 'static readonly' here instead of 'const'.
        // 'const' values would be embedded in to each assembly that used them
        // and each consuming assembly would have a different 'string' instance.
        // Using .'static readonly' means that all consumers get thee exact same
        // 'string' instance, which means the 'ReferenceEquals' checks below work
        // and allow us to optimize comparisons when these constants are used.
        
        // Please do NOT change these to 'const'
        public static readonly string Connect = "CONNECT";
        public static readonly string Delete = "DELETE";
        public static readonly string Get = "GET";
        public static readonly string Head = "HEAD";
        public static readonly string Options = "OPTIONS";
        public static readonly string Patch = "PATCH";
        public static readonly string Post = "POST";
        public static readonly string Put = "PUT";
        public static readonly string Trace = "TRACE";

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
