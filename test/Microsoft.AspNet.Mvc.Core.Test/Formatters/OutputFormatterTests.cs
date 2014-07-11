// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class OutputFormatterTests
    {
        [Fact]
        public void SelectCharacterEncoding_FormatterWithNoEncoding_Throws()
        {
            // Arrange
            var testFormatter = new TestFormatter();
            var testContentType = MediaTypeHeaderValue.Parse("text/invalid");

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => testFormatter.SelectCharacterEncoding(testContentType));
            Assert.Equal("No encoding found for output formatter "+
                         "'Microsoft.AspNet.Mvc.Test.OutputFormatterTests+TestFormatter'." +
                         " There must be at least one supported encoding registered in order for the" +
                         " output formatter to write content.", ex.Message);
        }

        private class TestFormatter : OutputFormatter
        {
            public override bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
            {
                throw new NotImplementedException();
            }

            public override Task WriteAsync(OutputFormatterContext context, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
