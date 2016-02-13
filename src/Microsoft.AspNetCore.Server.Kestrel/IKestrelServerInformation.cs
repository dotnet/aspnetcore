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
        /// Gets or sets values that instruct <seealso cref="KestrelServer"/> whether it is safe to 
        /// pool the Request and Response <seealso cref="System.IO.Stream"/> objects, Headers etc
        /// for another request after the Response's OnCompleted callback has fired. 
        /// When these values are greater than zero it is not safe to retain references to feature components after this event has fired.
        /// They are zero by default.
        /// </summary>
        KestrelServerPoolingParameters PoolingParameters { get; }

        IConnectionFilter ConnectionFilter { get; set; }
    }
}
