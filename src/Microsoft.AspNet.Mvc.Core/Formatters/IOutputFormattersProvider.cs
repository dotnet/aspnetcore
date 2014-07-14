// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides an activated collection of <see cref="IOutputFormatter"/> instances.
    /// </summary>
    public interface IOutputFormattersProvider
    {
        /// <summary>
        /// Gets a collection of activated OutputFormatter instances.
        /// </summary>
        IReadOnlyList<IOutputFormatter> OutputFormatters { get; }
    }
}
