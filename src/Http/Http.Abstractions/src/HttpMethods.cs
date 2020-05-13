// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Contains methods to verify the request method of an HTTP request. 
    /// </summary>
    public static class HttpMethods
    {
        // We are intentionally using 'static readonly' here instead of 'const'.
        // 'const' values would be embedded into each assembly that used them
        // and each consuming assembly would have a different 'string' instance.
        // Using .'static readonly' means that all consumers get these exact same
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

        /// <summary>
        /// Returns a value that indicates if the HTTP request method is CONNECT. 
        /// </summary>
        /// <param name="method">The HTTP request method.</param>
        /// <returns>
        /// <see langword="true" /> if the method is CONNECT; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsConnect(string method)
        {
            return object.ReferenceEquals(Connect, method) || StringComparer.OrdinalIgnoreCase.Equals(Connect, method);
        }

        /// <summary>
        /// Returns a value that indicates if the HTTP request method is DELETE. 
        /// </summary>
        /// <param name="method">The HTTP request method.</param>
        /// <returns>
        /// <see langword="true" /> if the method is DELETE; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsDelete(string method)
        {
            return object.ReferenceEquals(Delete, method) || StringComparer.OrdinalIgnoreCase.Equals(Delete, method);
        }

        /// <summary>
        /// Returns a value that indicates if the HTTP request method is GET. 
        /// </summary>
        /// <param name="method">The  HTTP request method.</param>
        /// <returns>
         /// <see langword="true" /> if the method is GET; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsGet(string method)
        {
            return object.ReferenceEquals(Get, method) || StringComparer.OrdinalIgnoreCase.Equals(Get, method);
        }

        /// <summary>
        /// Returns a value that indicates if the HTTP request method is HEAD. 
        /// </summary>
        /// <param name="method">The HTTP request method.</param>
        /// <returns>
         /// <see langword="true" /> if the method is HEAD; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsHead(string method)
        {
            return object.ReferenceEquals(Head, method) || StringComparer.OrdinalIgnoreCase.Equals(Head, method);
        }

        /// <summary>
        /// Returns a value that indicates if the HTTP request method is OPTIONS. 
        /// </summary>
        /// <param name="method">The HTTP request method.</param>
        /// <returns>
         /// <see langword="true" /> if the method is OPTIONS; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsOptions(string method)
        {
            return object.ReferenceEquals(Options, method) || StringComparer.OrdinalIgnoreCase.Equals(Options, method);
        }

        /// <summary>
        /// Returns a value that indicates if the HTTP request method is PATCH. 
        /// </summary>
        /// <param name="method">The HTTP request method.</param>
        /// <returns>
        /// <see langword="true" /> if the method is PATCH; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsPatch(string method)
        {
            return object.ReferenceEquals(Patch, method) || StringComparer.OrdinalIgnoreCase.Equals(Patch, method);
        }

        /// <summary>
        /// Returns a value that indicates if the HTTP request method is POST. 
        /// </summary>
        /// <param name="method">The HTTP request method.</param>
        /// <returns>
        /// <see langword="true" /> if the method is POST; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsPost(string method)
        {
            return object.ReferenceEquals(Post, method) || StringComparer.OrdinalIgnoreCase.Equals(Post, method);
        }

        /// <summary>
        /// Returns a value that indicates if the HTTP request method is PUT. 
        /// </summary>
        /// <param name="method">The HTTP request method.</param>
        /// <returns>
        /// <see langword="true" /> if the method is PUT; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsPut(string method)
        {
            return object.ReferenceEquals(Put, method) || StringComparer.OrdinalIgnoreCase.Equals(Put, method);
        }

        /// <summary>
        /// Returns a value that indicates if the HTTP request method is TRACE. 
        /// </summary>
        /// <param name="method">The HTTP request method.</param>
        /// <returns>
        /// <see langword="true" /> if the method is TRACE; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsTrace(string method)
        {
            return object.ReferenceEquals(Trace, method) || StringComparer.OrdinalIgnoreCase.Equals(Trace, method);
        }
    }
}
