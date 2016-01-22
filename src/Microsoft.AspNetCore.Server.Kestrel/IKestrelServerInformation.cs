// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Filter;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public interface IKestrelServerInformation
    {
        int ThreadCount { get; set; }

        bool NoDelay { get; set; }

        /// <summary>
        /// Gets or sets a flag that instructs <seealso cref="KestrelServer"/> whether it is safe to 
        /// reuse the Request and Response <seealso cref="System.IO.Stream"/> objects
        /// for another request after the Response's OnCompleted callback has fired. 
        /// When this is set to true it is not safe to retain references to these streams after this event has fired.
        /// It is false by default.
        /// </summary>
        /// <remarks>
        /// When this is set to true it is not safe to retain references to these streams after this event has fired.
        /// It is false by default.
        /// </remarks>
        bool ReuseStreams { get; set; }

        IConnectionFilter ConnectionFilter { get; set; }
    }
}
