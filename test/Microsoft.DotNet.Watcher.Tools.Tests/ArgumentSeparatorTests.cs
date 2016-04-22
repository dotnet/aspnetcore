// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.DotNet.Watcher.Tools.Tests
{
    public class ArgumentSeparatorTests
    {
        [Theory]
        [InlineData(new string[0],
            new string[0],
            new string[0])]
        [InlineData(new string[] { "--" },
            new string[0],
            new string[0])]
        [InlineData(new string[] { "--appArg1" },
            new string[0],
            new string[] { "--appArg1" })]
        [InlineData(new string[] { "--command", "test" },
            new string[0],
            new string[] { "--command", "test" })]
        [InlineData(new string[] { "--command", "test", "--" },
            new string[] { "--command", "test" },
            new string[0])]
        [InlineData(new string[] { "--command", "test", "--", "--appArg1", "arg1Value" },
            new string[] { "--command", "test" },
            new string[] { "--appArg1", "arg1Value" })]
        [InlineData(new string[] { "--", "--appArg1", "arg1Value" },
            new string[0],
            new string[] { "--appArg1", "arg1Value" })]
        [InlineData(new string[] { "--", "--" },
            new string[0],
            new string[] { "--" })]
        [InlineData(new string[] { "--", "--", "--" },
            new string[0],
            new string[] { "--", "--" })]
        [InlineData(new string[] { "--command", "run", "--", "--", "--appArg", "foo" },
            new string[] { "--command", "run" },
            new string[] { "--", "--appArg", "foo" })]
        [InlineData(new string[] { "--command", "run", "--", "-f", "net451", "--", "--appArg", "foo" },
            new string[] { "--command", "run" },
            new string[] { "-f", "net451", "--", "--appArg", "foo" })]
        public void SeparateWatchArguments(string[] args, string[] expectedWatchArgs, string[] expectedAppArgs)
        {
            SeparateWatchArgumentsTest(args, expectedWatchArgs, expectedAppArgs);
        }

        [Theory]
        // Help is special if it's the first argument
        [InlineData(new string[] { "--help" },
            new string[] { "--help" },
            new string[0])]
        [InlineData(new string[] { "-h" },
            new string[] { "-h" },
            new string[0])]
        [InlineData(new string[] { "-?" },
            new string[] { "-?" },
            new string[0])]
        [InlineData(new string[] { "--help", "--this-is-ignored" },
            new string[] { "--help" },
            new string[0])]
        [InlineData(new string[] { "--help", "--", "--this-is-ignored" },
            new string[] { "--help" },
            new string[0])]
        // But not otherwise
        [InlineData(new string[] { "--", "--help" },
            new string[0],
            new string[] { "--help" })]
        [InlineData(new string[] { "--foo", "--help" },
            new string[0],
            new string[] { "--foo", "--help" })]
        [InlineData(new string[] { "--foo", "--help" },
            new string[0],
            new string[] { "--foo", "--help" })]
        [InlineData(new string[] { "--foo", "--", "--help" },
            new string[] { "--foo" },
            new string[] { "--help" })]
        public void SeparateWatchArguments_Help(string[] args, string[] expectedWatchArgs, string[] expectedAppArgs)
        {
            SeparateWatchArgumentsTest(args, expectedWatchArgs, expectedAppArgs);
        }

        private static void SeparateWatchArgumentsTest(string[] args, string[] expectedWatchArgs, string[] expectedAppArgs)
        {
            string[] actualWatchArgs;
            string[] actualAppArgs;

            Program.SeparateWatchArguments(args, out actualWatchArgs, out actualAppArgs);

            Assert.Equal(expectedWatchArgs, actualWatchArgs);
            Assert.Equal(expectedAppArgs, actualAppArgs);
        }
    }
}
