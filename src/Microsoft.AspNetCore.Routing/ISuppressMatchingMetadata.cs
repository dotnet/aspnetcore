// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Metadata used to prevent URL matching. The associated endpoint will not be
    /// considered URL matching for incoming requests.
    /// </summary>
    public interface ISuppressMatchingMetadata
    {
    }
}