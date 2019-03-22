// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class SystemTextJsonInputFormatterTest : JsonInputFormatterTestBase
    {
        [Fact(Skip = "https://github.com/aspnet/AspNetCore/issues/8489")]
        public override Task JsonFormatterReadsDateTimeValue()
        {
            return base.JsonFormatterReadsDateTimeValue();
        }

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

        [Fact(Skip = "https://github.com/dotnet/corefx/issues/36026")]
        public override Task ReadAsync_ReadsValidArray_AsCollectionOfT()
        {
            return base.ReadAsync_ReadsValidArray_AsCollectionOfT();
        }

        [Fact(Skip = "https://github.com/dotnet/corefx/issues/36026")]
        public override Task ReadAsync_ReadsValidArray_AsEnumerableOfT()
        {
            return base.ReadAsync_ReadsValidArray_AsEnumerableOfT();
        }

        [Fact(Skip = "https://github.com/dotnet/corefx/issues/36026")]
        public override Task ReadAsync_ReadsValidArray_AsIListOfT()
        {
            return base.ReadAsync_ReadsValidArray_AsIListOfT();
        }

        protected override TextInputFormatter GetInputFormatter()
        {
            return new SystemTextJsonInputFormatter(new MvcOptions());
        }
    }
}
