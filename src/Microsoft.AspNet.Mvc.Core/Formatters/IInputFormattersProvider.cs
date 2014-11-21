// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides an activated collection of <see cref="IInputFormatter"/> instances.
    /// </summary>
    public interface IInputFormattersProvider
    {
        /// <summary>
        /// Gets a collection of activated InputFormatter instances.
        /// </summary>
        IReadOnlyList<IInputFormatter> InputFormatters { get; }
    }
}
