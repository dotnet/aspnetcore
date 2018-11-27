// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A builder abstraction for configuring SignalR object instances.
    /// </summary>
    public interface ISignalRBuilder
    {
        /// <summary>
        /// Gets the builder service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
