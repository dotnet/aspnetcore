// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http
{
    public class HttpContextAccessor : IHttpContextAccessor
    {
        private static readonly AsyncLocal<HttpContextHolder> _httpContextCurrent = new AsyncLocal<HttpContextHolder>();

        public HttpContext HttpContext
        {
            get => _httpContextCurrent.Value?.Context;
            set
            {
                var holder = _httpContextCurrent.Value;
                if (holder != null)
                {
                    // Dispose any HttpContext in case anyone holds a local reference (copied out of HttpContextAccessor),
                    // so it can throw ObjectDisposedExceptions when request ends
                    holder.Context?.Dispose();
                    // Clear current HttpContext trapped in the AsyncLocals, as its done and should return null
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the HttpContext in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    _httpContextCurrent.Value = new HttpContextHolder { Context = new DisposableHttpContext(value) };
                }
            }
        }

        private class HttpContextHolder
        {
            public DisposableHttpContext Context;
        }

        private class DisposableHttpContext : HttpContext, IDisposable
        {
            private HttpContext _context;

            // For testing
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                return _context.Equals(obj);
            }

            public override int GetHashCode() =>  _context.GetHashCode();

            public DisposableHttpContext(HttpContext context)
            {
                _context = context;
            }

            public override IFeatureCollection Features
            {
                get
                {
                    CheckIfDisposed();
                    return _context.Features;
                }
            }

            public override HttpRequest Request
            {
                get
                {
                    CheckIfDisposed();
                    return _context.Request;
                }
            }

            public override HttpResponse Response
            {
                get
                {
                    CheckIfDisposed();
                    return _context.Response;
                }
            }

            public override ConnectionInfo Connection
            {
                get
                {
                    CheckIfDisposed();
                    return _context.Connection;
                }
            }

            public override WebSocketManager WebSockets
            {
                get
                {
                    CheckIfDisposed();
                    return _context.WebSockets;
                }
            }

            [Obsolete("This is obsolete and will be removed in a future version. The recommended alternative is to use Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions. See https://go.microsoft.com/fwlink/?linkid=845470.")]
            public override AuthenticationManager Authentication
            {
                get
                {
                    CheckIfDisposed();
                    return _context.Authentication;
                }
            }

            public override ClaimsPrincipal User
            {
                get
                {
                    CheckIfDisposed();
                    return _context.User;
                }
                set
                {
                    CheckIfDisposed();
                    _context.User = value;
                }
            }

            public override IDictionary<object, object> Items
            {
                get
                {
                    CheckIfDisposed();
                    return _context.Items;
                }
                set
                {
                    CheckIfDisposed();
                    _context.Items = value;
                }
            }

            public override IServiceProvider RequestServices
            {
                get
                {
                    CheckIfDisposed();
                    return _context.RequestServices;
                }
                set
                {
                    CheckIfDisposed();
                    _context.RequestServices = value;
                }
            }

            public override CancellationToken RequestAborted
            {
                get
                {
                    CheckIfDisposed();
                    return _context.RequestAborted;
                }
                set
                {
                    CheckIfDisposed();
                    _context.RequestAborted = value;
                }
            }

            public override string TraceIdentifier
            {
                get
                {
                    CheckIfDisposed();
                    return _context.TraceIdentifier;
                }
                set
                {
                    CheckIfDisposed();
                    _context.TraceIdentifier = value;
                }
            }

            public override ISession Session
            {
                get
                {
                    CheckIfDisposed();
                    return _context.Session;
                }
                set
                {
                    CheckIfDisposed();
                    _context.Session = value;
                }
            }

            public override void Abort()
            {
                CheckIfDisposed();
                _context.Abort();
            }

            public void Dispose()
            {
                _context = null;
            }

            private void CheckIfDisposed()
            {
                if (_context is null)
                {
                    ThrowDisposed();
                }
            }

            private void ThrowDisposed() => throw new ObjectDisposedException(nameof(HttpContext));
        }
    }
}
