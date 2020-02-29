// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    /// <summary>
    /// Abstract class for grouping attributes of type <see cref="ValidationAttribute"/> into
    /// one <see cref="Attribute"/>
    /// </summary>
    public abstract class ValidationProviderAttribute : Attribute
    {
        /// <summary>
        /// Gets <see cref="ValidationAttribute" /> instances associated with this attribute.
        /// </summary>
        /// <returns>Sequence of <see cref="ValidationAttribute" /> associated with this attribute.</returns>
        public abstract IEnumerable<ValidationAttribute> GetValidationAttributes();
    }
}
