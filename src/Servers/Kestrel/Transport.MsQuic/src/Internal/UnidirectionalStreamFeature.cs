// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections.Abstractions.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class UnidirectionalStreamFeature : IUnidirectionalStreamFeature
    {
        // TODO maybe make this IQuicStream feature which allows for getting information about the underlying connection.
    }
}
