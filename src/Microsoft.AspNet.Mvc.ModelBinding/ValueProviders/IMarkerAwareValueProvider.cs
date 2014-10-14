// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A value provider which is aware of <see cref="IValueBinderMarker"/>.
    /// </summary>
    public interface IMarkerAwareValueProvider : IValueProvider
    {
        /// <summary>
        /// Filters the value provider based on <paramref name="valueBinderMarker"/>.
        /// </summary>
        /// <param name="valueBinderMarker">The <see cref="IValueBinderMarker"/> associated with a model.</param>
        /// <returns>The filtered value provider.</returns>
        IValueProvider Filter([NotNull] IValueBinderMarker valueBinderMarker);
    }
}
