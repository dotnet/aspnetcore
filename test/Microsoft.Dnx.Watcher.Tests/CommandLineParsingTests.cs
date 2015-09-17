// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Dnx.Watcher.Tests
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class CommandLineParsingTests
    {
        [Fact]
        public void NoWatcherArgs()
        {
            var args = "--arg1 v1 --arg2 v2".Split(' ');

            string[] watcherArgs, dnxArgs;
            Program.SeparateWatchArguments(args, out watcherArgs, out dnxArgs);

            Assert.Empty(watcherArgs);
            Assert.Equal(args, dnxArgs);
        }

        [Fact]
        public void ArgsForBothDnxAndWatcher()
        {
            var args = "--arg1 v1 --arg2 v2 --dnx-args --arg3 --arg4 v4".Split(' ');

            string[] watcherArgs, dnxArgs;
            Program.SeparateWatchArguments(args, out watcherArgs, out dnxArgs);

            Assert.Equal(new string[] {"--arg1", "v1", "--arg2", "v2" }, watcherArgs);
            Assert.Equal(new string[] { "--arg3", "--arg4", "v4" }, dnxArgs);
        }

        [Fact]
        public void MultipleSeparators()
        {
            var args = "--arg1 v1 --arg2 v2 --dnx-args --arg3 --dnxArgs --arg4 v4".Split(' ');

            string[] watcherArgs, dnxArgs;
            Program.SeparateWatchArguments(args, out watcherArgs, out dnxArgs);

            Assert.Equal(new string[] { "--arg1", "v1", "--arg2", "v2" }, watcherArgs);
            Assert.Equal(new string[] { "--arg3", "--dnxArgs", "--arg4", "v4" }, dnxArgs);
        }
    }
}
