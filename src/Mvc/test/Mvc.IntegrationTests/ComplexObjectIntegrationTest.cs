// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class ComplexObjectIntegrationTest : ComplexTypeIntegrationTestBase
    {
        protected override Type ExpectedModelBinderType => typeof(ComplexObjectModelBinder);
    }
}
