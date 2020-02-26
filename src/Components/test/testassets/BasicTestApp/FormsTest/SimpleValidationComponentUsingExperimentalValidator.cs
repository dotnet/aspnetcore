// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace BasicTestApp.FormsTest
{
    public class TypicalValidationComponentUsingExperimentalValidator : TypicalValidationComponent
    {
        protected override bool UseExperimentalValidator => true;
    }
}
