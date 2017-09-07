// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ErrorDescriptionContext
    {
        public ErrorDescriptionContext(ActionDescriptor actionDescriptor)
        {
            ActionDescriptor = actionDescriptor;
        }

        public ActionDescriptor ActionDescriptor { get; }

        public object Result { get; set; }
    }
}
