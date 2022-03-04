// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    public class ModelValidationResultComparer : IEqualityComparer<ModelValidationResult>
    {
        public static readonly ModelValidationResultComparer Instance = new ModelValidationResultComparer();

        private ModelValidationResultComparer()
        {
        }

        public bool Equals(ModelValidationResult x, ModelValidationResult y)
        {
            if (x == null || y == null)
            {
                return x == null && y == null;
            }

            return string.Equals(x.MemberName, y.MemberName, StringComparison.Ordinal) &&
                string.Equals(x.Message, y.Message, StringComparison.Ordinal);
        }

        public int GetHashCode(ModelValidationResult obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return obj.MemberName.GetHashCode();
        }
    }
}
