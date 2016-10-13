// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class CommandLineOptionsTests
    {
        [Theory]
        [InlineData(new object[] { new[] { "-h" } })]
        [InlineData(new object[] { new[] { "-?" } })]
        [InlineData(new object[] { new[] { "--help" } })]
        [InlineData(new object[] { new[] { "--help", "--bogus" } })]
        [InlineData(new object[] { new[] { "--" } })]
        [InlineData(new object[] { new string[0] })]
        public void HelpArgs(string[] args)
        {
            var stdout = new StringBuilder();

            var options = CommandLineOptions.Parse(args, new StringWriter(stdout), new StringWriter());

            Assert.True(options.IsHelp);
            Assert.Contains("Usage: dotnet watch ", stdout.ToString());
        }

        [Theory]
        [InlineData(new[] { "run" }, new[] { "run" })]
        [InlineData(new[] { "run", "--", "subarg" }, new[] { "run", "--", "subarg" })]
        [InlineData(new[] { "--", "run", "--", "subarg" }, new[] { "run", "--", "subarg" })]
        [InlineData(new[] { "--unrecognized-arg" }, new[] { "--unrecognized-arg" })]
        public void ParsesRemainingArgs(string[] args, string[] expected)
        {
            var stdout = new StringBuilder();

            var options = CommandLineOptions.Parse(args, new StringWriter(stdout), new StringWriter());

            Assert.Equal(expected, options.RemainingArguments.ToArray());
            Assert.False(options.IsHelp);
            Assert.Empty(stdout.ToString());
        }

        [Fact]
        public void CannotHaveQuietAndVerbose()
        {
            var sb = new StringBuilder();
            var stderr = new StringWriter(sb);
            Assert.Null(CommandLineOptions.Parse(new[] { "--quiet", "--verbose" }, new StringWriter(), stderr));
            Assert.Contains(Resources.Error_QuietAndVerboseSpecified, sb.ToString());
        }
    }
}
