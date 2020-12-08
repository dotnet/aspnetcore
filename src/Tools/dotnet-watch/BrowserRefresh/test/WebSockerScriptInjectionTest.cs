// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(stream, input);

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
{WebSocketScriptInjection.InjectedScript}</body>
</html>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes(
@"<footer>
    This is the footer
</footer>
</body>
</html>");

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(stream, input);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_WithOffsetBodyTagAppearsInMiddle()
        {
            // Arrange
            var expected = $"</table>{WebSocketScriptInjection.InjectedScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("unused</table></body>");

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(stream, input.AsMemory(6));

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_WithOffsetBodyTagAppearsAtStartOfOffset()
        {
            // Arrange
            var expected = $"{WebSocketScriptInjection.InjectedScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("unused</body>");

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(stream, input.AsMemory(6));

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_InjectsMarkupIfBodyTagAppearsAtTheStartOfOutput()
        {
            // Arrange
            var expected = $"{WebSocketScriptInjection.InjectedScript}</body></html>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("</body></html>");

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(stream, input);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_InjectsMarkupIfBodyTagAppearsByItself()
        {
            // Arrange
            var expected = $"{WebSocketScriptInjection.InjectedScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("</body>");

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(stream, input);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public async Task TryInjectLiveReloadScriptAsync_MultipleBodyTags()
        {
            // Arrange
            var expected = $"<p></body>some text</p>{WebSocketScriptInjection.InjectedScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("abc<p></body>some text</p></body>").AsMemory(3);

            // Act
            var result = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(stream, input);

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
            var input = Encoding.UTF8.GetBytes(expected).AsSpan();

            // Act
            var result = WebSocketScriptInjection.TryInjectLiveReloadScript(stream, input);

            // Assert
            Assert.False(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public void TryInjectLiveReloadScript_NoOffset()
        {
            // Arrange
            var expected = $"</table>{WebSocketScriptInjection.InjectedScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("</table></body>").AsSpan();

            // Act
            var result = WebSocketScriptInjection.TryInjectLiveReloadScript(stream, input);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }

        [Fact]
        public void TryInjectLiveReloadScript_WithOffset()
        {
            // Arrange
            var expected = $"</table>{WebSocketScriptInjection.InjectedScript}</body>";
            var stream = new MemoryStream();
            var input = Encoding.UTF8.GetBytes("unused</table></body>").AsSpan(6);

            // Act
            var result = WebSocketScriptInjection.TryInjectLiveReloadScript(stream, input);

            // Assert
            Assert.True(result);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, output);
        }
    }
}
