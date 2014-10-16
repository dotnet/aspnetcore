// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extensions for <see cref="MvcOptions.ExcludeFromValidationDelegates"/>.
    /// </summary>
    public static class ExcludeFromValidationDelegateExtensions
    {
        /// <summary>
        /// Adds a delegate to the specified <paramref name="list" />
        /// that exludes the properties of the specified and it's derived types from validaton.
        /// </summary>
        /// <param name="list"><see cref="IList{T}"/> of <see cref="ExcludeFromValidationDelegate"/>.</param>
        /// <param name="type"><see cref="Type"/> which should be excluded from validation.</param>
        public static void Add(this IList<ExcludeFromValidationDelegate> list, Type type)
        {
            list.Add(t => t.IsAssignableFrom(type));
        }
    }
}