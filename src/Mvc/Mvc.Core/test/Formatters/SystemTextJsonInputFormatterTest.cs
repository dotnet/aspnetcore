// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class SystemTextJsonInputFormatterTest : JsonInputFormatterTestBase
    {
        [Fact(Skip = "https://github.com/aspnet/AspNetCore/issues/8474")]
        public override Task ReadAsync_AddsModelValidationErrorsToModelState()
        {
            return base.ReadAsync_AddsModelValidationErrorsToModelState();
        }

        [Fact(Skip = "https://github.com/aspnet/AspNetCore/issues/8474")]
        public override Task ReadAsync_InvalidArray_AddsOverflowErrorsToModelState()
        {
            return base.ReadAsync_InvalidArray_AddsOverflowErrorsToModelState();
        }

        [Fact(Skip = "https://github.com/aspnet/AspNetCore/issues/8474")]
        public override Task ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState()
        {
            return base.ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState();
        }

        [Fact(Skip = "https://github.com/aspnet/AspNetCore/issues/8474")]
        public override Task ReadAsync_UsesTryAddModelValidationErrorsToModelState()
        {
            return base.ReadAsync_UsesTryAddModelValidationErrorsToModelState();
        }

        protected override TextInputFormatter GetInputFormatter()
        {
            return new SystemTextJsonInputFormatter(new MvcOptions());
        }
    }
}
