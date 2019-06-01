// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features
{
    public class ResponseBodyPipeFeatureTests
    {
        [Fact]
        public void ResponseBodyReturnsStreamPipeReader()
        {
            var context = new DefaultHttpContext();
            var expectedStream = new MemoryStream();
            context.Response.Body = expectedStream;

            var feature = new ResponseBodyPipeFeature(context);

            var pipeBody = feature.Writer;

            Assert.NotNull(pipeBody);
        }
    }
}
