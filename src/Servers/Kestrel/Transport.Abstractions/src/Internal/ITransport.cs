// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public interface ITransport
    {
        // Can only be called once per ITransport
        Task BindAsync();
        Task UnbindAsync();
        Task StopAsync();
    }
}
