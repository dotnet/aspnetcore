// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public interface IClientModelValidator
    {
        IEnumerable<ModelClientValidationRule> GetClientValidationRules([NotNull] ClientModelValidationContext context);
    }
}
