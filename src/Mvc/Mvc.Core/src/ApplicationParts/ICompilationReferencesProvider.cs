// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Exposes one or more reference paths from an <see cref="ApplicationPart"/>.
    /// </summary>
    public interface ICompilationReferencesProvider
    {
        /// <summary>
        /// Gets reference paths used to perform runtime compilation.
        /// </summary>
        IEnumerable<string> GetReferencePaths();
    }
}
