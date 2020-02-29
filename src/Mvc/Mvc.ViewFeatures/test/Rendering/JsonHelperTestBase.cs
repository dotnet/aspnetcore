// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Html;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public abstract class JsonHelperTestBase
    {
        protected abstract IJsonHelper GetJsonHelper();

        [Fact]
        public virtual void Serialize_EscapesHtmlByDefault()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                HTML = "<b>John Doe</b>"
            };
            var expectedOutput = "{\"html\":\"\\u003cb\\u003eJohn Doe\\u003c/b\\u003e\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString(), ignoreCase: true);
        }

        [Fact]
        public void Serialize_WithNullValue()
        {
            // Arrange
            var helper = GetJsonHelper();
            var expectedOutput = "null";

            // Act
            var result = helper.Serialize(value: null);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }

        [Fact]
        public void Serialize_WithControlCharacters()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                HTML = $"Hello \n \b \a world"
            };
            var expectedOutput = "{\"html\":\"Hello \\n \\b \\u0007 world\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }

        [Fact]
        public virtual void Serialize_WithNonAsciiChars()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                HTML = $"Hello pingüino"
            };
            var expectedOutput = "{\"html\":\"Hello pingüino\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString(), ignoreCase: true);
        }

        [Fact]
        public void Serialize_WithHTMLEntities()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                HTML = $"Hello &nbsp; &lt;John&gt;"
            };
            var expectedOutput = "{\"html\":\"Hello \\u0026nbsp; \\u0026lt;John\\u0026gt;\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }


        [Fact]
        public virtual void Serialize_WithHTMLNonAsciiAndControlChars()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                HTML = "<b>Hello \n pingüino</b>"
            };
            var expectedOutput = "{\"html\":\"\\u003cb\\u003eHello \\n pingüino\\u003c/b\\u003e\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }
    }
}
