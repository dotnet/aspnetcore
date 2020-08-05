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
        private const string ClientScript = "<script><!--My cool script--></script>";
        private readonly WebSocketScriptInjection ScriptInjection = new WebSocketScriptInjection(ClientScript);

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_DoesNotInjectMarkup_IfInputDoesNotContainBodyTag()
        {
            // Arrange
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("<div>this is not a real body tag.</div>");

            // Act
            var result = await ScriptInjection.TryInjectLiveReloadScriptAsync(stream, input, 0, input.Length);

            // Assert
            Assert.False(result);
            Assert.Equal(input, stream.ToArray());
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_InjectsMarkupIfBodyTagAppearsInTheMiddle()
        {
            // Arrange
            var expected =
$@"<footer>
    This is the footer
</footer>
{ClientScript}</body>
</html>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes(
@"<footer>
    This is the footer
</footer>
</body>
</html>");

            // Act
            var result = await ScriptInjection.TryInjectLiveReloadScriptAsync(stream, input, 0, input.Length);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_WithOffsetBodyTagAppearsInMiddle()
        {
            // Arrange
            var expected = $"</table>{ClientScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("unused</table></body>");

            // Act
            var result = await ScriptInjection.TryInjectLiveReloadScriptAsync(stream, input, 6, input.Length - 6);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_WithOffsetBodyTagAppearsAtStartOfOffset()
        {
            // Arrange
            var expected = $"{ClientScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("unused</body>");

            // Act
            var result = await ScriptInjection.TryInjectLiveReloadScriptAsync(stream, input, 6, input.Length - 6);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_InjectsMarkupIfBodyTagAppearsAtTheStartOfOutput()
        {
            // Arrange
            var expected = $"{ClientScript}</body></html>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("</body></html>");

            // Act
            var result = await ScriptInjection.TryInjectLiveReloadScriptAsync(stream, input, 0, input.Length);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_InjectsMarkupIfBodyTagAppearsByItself()
        {
            // Arrange
            var expected = $"{ClientScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("</body>");

            // Act
            var result = await ScriptInjection.TryInjectLiveReloadScriptAsync(stream, input, 0, input.Length);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_MultipleBodyTags()
        {
            // Arrange
            var expected = $"<p></body>some text</p>{ClientScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("abc<p></body>some text</p></body>");

            // Act
            var result = await ScriptInjection.TryInjectLiveReloadScriptAsync(stream, input, 3, input.Length - 3);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public void TryInjectLiveReloadScript_NoBodyTag()
        {
            // Arrange
            var expected = "<p>Hello world</p>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes(expected);

            // Act
            var result = ScriptInjection.TryInjectLiveReloadScript(stream, input, 0, input.Length);

            // Assert
            Assert.False(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public void TryInjectLiveReloadScript_NoOffset()
        {
            // Arrange
            var expected = $"</table>{ClientScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("</table></body>");

            // Act
            var result = ScriptInjection.TryInjectLiveReloadScript(stream, input, 0, input.Length);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public void TryInjectLiveReloadScript_WithOffset()
        {
            // Arrange
            var expected = $"</table>{ClientScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("unused</table></body>");

            // Act
            var result = ScriptInjection.TryInjectLiveReloadScript(stream, input, 6, input.Length - 6);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }
    }
}
