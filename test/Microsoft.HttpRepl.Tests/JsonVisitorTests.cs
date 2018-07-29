using Microsoft.HttpRepl.Formatting;
using Microsoft.HttpRepl.Preferences;
using Microsoft.Repl.ConsoleHandling;
using Xunit;

namespace Microsoft.HttpRepl.Tests
{
    public class JsonVisitorTests
    {
        [Fact]
        public void JsonVisitor_ObjectWithComments()
        {
            string testData = @"[
    {
        //Object 1
        ""property"": ""value"",
        ""and"": ""again""
    },
    {
        //Object 2
    },
    [
        //An array
    ],
    null,
    1,
    3.2,
    ""test"",
    false
]";

            string formatted = JsonVisitor.FormatAndColorize(new MockJsonConfig(), testData);
        }

        private class MockJsonConfig : IJsonConfig
        {
            public int IndentSize => 2;

            public AllowedColors DefaultColor => AllowedColors.None;

            public AllowedColors ArrayBraceColor => AllowedColors.None;

            public AllowedColors ObjectBraceColor => AllowedColors.None;

            public AllowedColors CommaColor => AllowedColors.None;

            public AllowedColors NameColor => AllowedColors.None;

            public AllowedColors NameSeparatorColor => AllowedColors.None;

            public AllowedColors BoolColor => AllowedColors.None;

            public AllowedColors NumericColor => AllowedColors.None;

            public AllowedColors StringColor => AllowedColors.None;

            public AllowedColors NullColor => AllowedColors.None;
        }
    }
}
