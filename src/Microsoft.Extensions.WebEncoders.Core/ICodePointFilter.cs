// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Represents a filter which allows only certain Unicode code points through.
    /// </summary>
    public interface ICodePointFilter
    {
        /// <summary>
        /// Gets an enumeration of all allowed code points.
        /// </summary>
        IEnumerable<int> GetAllowedCodePoints();
    }
}
