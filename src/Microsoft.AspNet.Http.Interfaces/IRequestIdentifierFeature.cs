// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Http.Interfaces
{
    /// <summary>
    /// Feature to identify a request.
    /// </summary>
    public interface IRequestIdentifierFeature
    {
        /// <summary>
        /// Identifier to trace a request.
        /// </summary>
        Guid TraceIdentifier { get; }
    }
}
