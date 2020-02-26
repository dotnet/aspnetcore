// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    // This a marker used to check if the underlying handler should be disposed. HttpClients
    // share a reference to an instance of this class, and when it goes out of scope the inner handler
    // is eligible to be disposed.
    internal class LifetimeTrackingHttpMessageHandler : DelegatingHandler
    {
        public LifetimeTrackingHttpMessageHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override void Dispose(bool disposing)
        {
            // The lifetime of this is tracked separately by ActiveHandlerTrackingEntry
        }
    }
}
