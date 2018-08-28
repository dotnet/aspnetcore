using Microsoft.Repl.Parsing;
using Xunit;

namespace Microsoft.Repl.Tests
{
    public class ParserTests
    {
        [Fact]
        public void ParserTests_Basic()
        {
            string testString = "\"this is a test\" of \"Escape\\\\Sequences\\\"\"";

            CoreParser parser = new CoreParser();
            ICoreParseResult result = parser.Parse(testString, 29);

            Assert.Equal(3, result.Sections.Count);
            Assert.Equal(2, result.SelectedSection);
            Assert.Equal(0, result.SectionStartLookup[0]);
            Assert.Equal(17, result.SectionStartLookup[1]);
            Assert.Equal(20, result.SectionStartLookup[2]);
            Assert.Equal(7, result.CaretPositionWithinSelectedSection);
            Assert.Equal(29, result.CaretPositionWithinCommandText);
            Assert.Equal(testString, result.CommandText);
        }
    }
}
