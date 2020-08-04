// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    public class WebSockerScriptInjectionTest
    {
        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_DoesNotInjectMarkup_IfInputDoesNotContainBodyTag()
        {
            // Arrange
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("<div>this is not a real body tag.</div>");

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(input, 0, input.Length, stream);

            // Assert
            Assert.False(result);
            Assert.Equal(input, stream.ToArray());
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_InjectsMarkupIfBodyTagAppearsInTheMiddle()
        {
            // Arrange
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes(
@"<footer>
    This is the footer
</footer>
</body>
</html>");

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(input, 0, input.Length, stream);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Contains("</script></body>", output);
            Assert.Contains("// dotnet-watch browser reload script", output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_InjectsMarkupIfBodyTagAppearsAtTheStartOfOutput()
        {
            // Arrange
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("</body></html>");

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(input, 0, input.Length, stream);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Contains("</script></body>", output);
            Assert.Contains("// dotnet-watch browser reload script", output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_InjectsMarkupIfBodyTagAppearsByItself()
        {
            // Arrange
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("</body>");

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(input, 0, input.Length, stream);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Contains("</script></body>", output);
            Assert.Contains("// dotnet-watch browser reload script", output);
        }
    }
}
