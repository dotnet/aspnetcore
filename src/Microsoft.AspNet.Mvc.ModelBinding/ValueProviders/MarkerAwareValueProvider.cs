// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="IMarkerAwareValueProvider"/> value provider which can filter 
    /// based on <see cref="IValueBinderMarker"/>.
    /// </summary>
    /// <typeparam name="TBinderMarker">Represents a type implementing <see cref="IValueBinderMarker"/></typeparam>
    public abstract class MarkerAwareValueProvider<TBinderMarker> : IMarkerAwareValueProvider
        where TBinderMarker : IValueBinderMarker
    {
        public abstract Task<bool> ContainsPrefixAsync(string prefix);

        public abstract Task<ValueProviderResult> GetValueAsync(string key);

        public virtual IValueProvider Filter(IValueBinderMarker valueBinderMarker)
        {
            if (valueBinderMarker is TBinderMarker)
            {
                return this;
            }

            return null;
        }
    }
}
