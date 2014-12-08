// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class DefaultInputFormatterSelectorTests
    {
        [Fact]
        public void DefaultInputFormatterSelectorTests_ReturnsFirstFormatterWhichReturnsTrue()
        {
            // Arrange
            var actionContext = GetActionContext();
            var inputFormatters = new List<IInputFormatter>()
            {
                new TestInputFormatter(false, 0),
                new TestInputFormatter(false, 1),
                new TestInputFormatter(true, 2),
                new TestInputFormatter(true, 3)
            };

            var context = new InputFormatterContext(actionContext, typeof(int));
            var selector = new DefaultInputFormatterSelector();

            // Act
            var selectedFormatter = selector.SelectFormatter(inputFormatters, context);

            // Assert
            var testFormatter = Assert.IsType<TestInputFormatter>(selectedFormatter);
            Assert.Equal(2, testFormatter.Index);
        }

        private static ActionContext GetActionContext()
        {
            return new ActionContext(Mock.Of<HttpContext>(), new RouteData(), new ActionDescriptor());

        }

        private class TestInputFormatter : IInputFormatter
        {
            private bool _canRead = false;

            public TestInputFormatter(bool canRead, int index)
            {
                _canRead = canRead;
                Index = index;
            }
            public int Index { get; set; }

            public bool CanRead(InputFormatterContext context)
            {
                return _canRead;
            }

            public Task<object> ReadAsync(InputFormatterContext context)
            {
                return Task.FromResult<object>(Index);
            }
        }
    }
}
