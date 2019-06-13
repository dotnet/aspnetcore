// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class SystemTextJsonInputFormatterTest : JsonInputFormatterTestBase
    {
        [Fact]
        public override Task ReadAsync_AddsModelValidationErrorsToModelState()
        {
            return base.ReadAsync_AddsModelValidationErrorsToModelState();
        }

        [Fact]
        public override Task ReadAsync_InvalidArray_AddsOverflowErrorsToModelState()
        {
            return base.ReadAsync_InvalidArray_AddsOverflowErrorsToModelState();
        }

        [Fact]
        public override Task ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState()
        {
            return base.ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState();
        }

        [Fact]
        public override Task ReadAsync_UsesTryAddModelValidationErrorsToModelState()
        {
            return base.ReadAsync_UsesTryAddModelValidationErrorsToModelState();
        }

        [Fact(Skip = "https://github.com/dotnet/corefx/issues/38492")]
        public override Task ReadAsync_RequiredAttribute()
        {
            // System.Text.Json does not yet support an equivalent of Required.
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ReadAsync_SingleError()
        {
            // Arrange
            var failTotal = 0;
            var formatter = GetInputFormatter();

            var content = "[5, 'seven', 3, notnum ]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(List<int>), httpContext);

            // Act
            await formatter.ReadAsync(formatterContext);

            // Assert
            foreach(var modelState in formatterContext.ModelState)
            {
                foreach(var error in modelState.Value.Errors)
                {
                    failTotal++;
                    Assert.StartsWith("''' is an invalid start of a value", error.ErrorMessage);
                }
            }

            Assert.Equal(1, failTotal);
        }

        protected override TextInputFormatter GetInputFormatter()
        {
            return new SystemTextJsonInputFormatter(new JsonOptions(), LoggerFactory.CreateLogger<SystemTextJsonInputFormatter>());
        }
    }
}
