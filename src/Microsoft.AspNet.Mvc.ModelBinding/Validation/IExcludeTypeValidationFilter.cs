// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Provides an interface which is used to determine if <see cref="Type"/>s are excluded from model validation.
    /// </summary>
    public interface IExcludeTypeValidationFilter
    {
        /// <summary>
        /// Determines if the given type is excluded from validation.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which the check is to be performed.</param>
        /// <returns>True if the type is to be excluded. False otherwise.</returns>
        bool IsTypeExcluded([NotNull] Type type);
    }
}
