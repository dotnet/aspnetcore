// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FiltersWebSite
{
    /// <summary>
    /// A middleware that reports errors via header values. Useful for tests that want to verify
    /// an exception that goes unhandled by the MVC part of the stack.
    /// </summary>
    public class ErrorReporterMiddleware
    {
        public static readonly string ExceptionMessageHeader = "ExceptionMessage";
        public static readonly string ExceptionTypeHeader = "ExceptionType";

        private readonly RequestDelegate _next;

        public ErrorReporterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                if (context.Response.HasStarted)
                {
                    throw;
                }
                else
                {
                    context.Response.StatusCode = 500;

                    var escapedMessage = exception.Message.Replace('\r', '_').Replace('\n', '_');

                    context.Response.Headers.Add(ExceptionTypeHeader, new string[] { exception.GetType().FullName });
                    context.Response.Headers.Add(ExceptionMessageHeader, new string[] { escapedMessage });
                }
            }
        }
    }
}
