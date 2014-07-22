// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides a return type and a set of possible content types returned by a successful execution of the action.
    /// </summary>
    public interface IProducesMetadataProvider
    {
        /// <summary>
        /// Optimistic return type of the action.
        /// </summary>
        Type Type { get; set; }

        /// <summary>
        /// A collection of allowed content types which can be produced by the action.
        /// </summary>
        IList<MediaTypeHeaderValue> ContentTypes { get; set; }
    }
}