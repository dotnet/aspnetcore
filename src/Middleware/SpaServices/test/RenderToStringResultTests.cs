// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.SpaServices.Tests
{
    public class RenderToStringResultTest
    {
        [Fact]
        public void HandlesNullGlobals()
        {
            // Arrange
#pragma warning disable CS0618 // Type or member is obsolete
            var renderToStringResult = new RenderToStringResult();
#pragma warning restore CS0618 // Type or member is obsolete
            renderToStringResult.Globals = null;

            // Act
            var actualScript = renderToStringResult.CreateGlobalsAssignmentScript();

            // Assert
            Assert.Equal(string.Empty, actualScript);
        }

        [Fact]
        public void HandlesGlobalsWithMultipleProperties()
        {
            // Arrange
#pragma warning disable CS0618 // Type or member is obsolete
            var renderToStringResult = new RenderToStringResult();
#pragma warning restore CS0618 // Type or member is obsolete
            renderToStringResult.Globals = ToJObject(new
            {
                FirstProperty = "first value",
                SecondProperty = new[] { "Array entry 0", "Array entry 1" }
            });

            // Act
            var actualScript = renderToStringResult.CreateGlobalsAssignmentScript();

            // Assert
            var expectedScript = @"window[""FirstProperty""] = JSON.parse(""\u0022first value\u0022"");" +
                @"window[""SecondProperty""] = JSON.parse(""[\u0022Array entry 0\u0022,\u0022Array entry 1\u0022]"");";
            Assert.Equal(expectedScript, actualScript);
        }

        [Fact]
        public void HandlesGlobalsWithCorrectStringEncoding()
        {
            // Arrange
#pragma warning disable CS0618 // Type or member is obsolete
            var renderToStringResult = new RenderToStringResult();
#pragma warning restore CS0618 // Type or member is obsolete
            renderToStringResult.Globals = ToJObject(new Dictionary<string, object>
            {
                { "Va<l'u\"e", "</tag>\"'}\u260E" }
            });

            // Act
            var actualScript = renderToStringResult.CreateGlobalsAssignmentScript();

            // Assert
            var expectedScript = @"window[""Va\u003Cl\u0027u\u0022e""] = JSON.parse(""\u0022\u003C/tag\u003E\\\u0022\u0027}\u260E\u0022"");";
            Assert.Equal(expectedScript, actualScript);
        }

        private static JObject ToJObject(object value)
        {
            return JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(value));
        }
    }
}
