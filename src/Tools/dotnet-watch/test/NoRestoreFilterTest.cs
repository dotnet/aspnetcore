// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class NoRestoreFilterTest
    {
        private readonly string[] _arguments = new[] { "run" };

        [Fact]
        public async Task ProcessAsync_LeavesArgumentsUnchangedOnFirstRun()
        {
            // Arrange
            var filter = new NoRestoreFilter();

            var context = new DotNetWatchContext
            {
                ProcessSpec = new ProcessSpec
                {
                    Arguments = _arguments,
                }
            };

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.Same(_arguments, context.ProcessSpec.Arguments);
        }

        [Fact]
        public async Task ProcessAsync_LeavesArgumentsUnchangedIfMsBuildRevaluationIsRequired()
        {
            // Arrange
            var filter = new NoRestoreFilter();

            var context = new DotNetWatchContext
            {
                Iteration = 0,
                ProcessSpec = new ProcessSpec
                {
                    Arguments = _arguments,
                }
            };
            await filter.ProcessAsync(context, default);

            context.ChangedFile = "Test.proj";
            context.RequiresMSBuildRevaluation = true;
            context.Iteration++;

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.Same(_arguments, context.ProcessSpec.Arguments);
        }

        [Fact]
        public async Task ProcessAsync_AddsNoRestoreSwitch()
        {
            // Arrange
            var filter = new NoRestoreFilter();

            var context = new DotNetWatchContext
            {
                Iteration = 0,
                ProcessSpec = new ProcessSpec
                {
                    Arguments = _arguments,
                }
            };
            await filter.ProcessAsync(context, default);

            context.ChangedFile = "Program.cs";
            context.Iteration++;

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.Equal(new[] { "run", "--no-restore" }, context.ProcessSpec.Arguments);
        }

        [Fact]
        public async Task ProcessAsync_AddsNoRestoreSwitch_WithAdditionalArguments()
        {
            // Arrange
            var filter = new NoRestoreFilter();

            var context = new DotNetWatchContext
            {
                Iteration = 0,
                ProcessSpec = new ProcessSpec
                {
                    Arguments = new[] { "run", "-f", "net5.0", "--", "foo=bar" },
                }
            };
            await filter.ProcessAsync(context, default);

            context.ChangedFile = "Program.cs";
            context.Iteration++;

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.Equal(new[] { "run", "--no-restore", "-f", "net5.0", "--", "foo=bar" }, context.ProcessSpec.Arguments);
        }

        [Fact]
        public async Task ProcessAsync_AddsNoRestoreSwitch_ForTestCommand()
        {
            // Arrange
            var filter = new NoRestoreFilter();

            var context = new DotNetWatchContext
            {
                Iteration = 0,
                ProcessSpec = new ProcessSpec
                {
                    Arguments = new[] { "test", "--filter SomeFilter" },
                }
            };
            await filter.ProcessAsync(context, default);

            context.ChangedFile = "Program.cs";
            context.Iteration++;

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.Equal(new[] { "test", "--no-restore", "--filter SomeFilter" }, context.ProcessSpec.Arguments);
        }

        [Fact]
        public async Task ProcessAsync_DoesNotModifyArgumentsForUnknownCommands()
        {
            // Arrange
            var filter = new NoRestoreFilter();
            var arguments = new[] { "ef", "database", "update" };

            var context = new DotNetWatchContext
            {
                Iteration = 0,
                ProcessSpec = new ProcessSpec
                {
                    Arguments = arguments,
                }
            };
            await filter.ProcessAsync(context, default);

            context.ChangedFile = "Program.cs";
            context.Iteration++;

            // Act
            await filter.ProcessAsync(context, default);

            // Assert
            Assert.Same(arguments, context.ProcessSpec.Arguments);
        }
    }
}
