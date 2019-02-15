// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Base JSON helpers.
    /// </summary>
    public interface IJsonHelper
    {
        /// <summary>
        /// Returns serialized JSON for the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to serialize as JSON.</param>
        /// <returns>A new <see cref="IHtmlContent"/> containing the serialized JSON.</returns>
        IHtmlContent Serialize(object value);
    }
}
