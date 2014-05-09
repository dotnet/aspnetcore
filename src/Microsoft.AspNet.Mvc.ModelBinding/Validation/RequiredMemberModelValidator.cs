// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RequiredMemberModelValidator : IModelValidator
    {
        public bool IsRequired
        {
            get { return true; }
        }

        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
        {
            return Enumerable.Empty<ModelValidationResult>();
        }
    }
}
