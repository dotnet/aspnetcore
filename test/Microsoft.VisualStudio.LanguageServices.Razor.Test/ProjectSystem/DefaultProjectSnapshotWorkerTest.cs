// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectSnapshotWorkerTest : ForegroundDispatcherTestBase
    {
        public DefaultProjectSnapshotWorkerTest()
        {
            Project = new AdhocWorkspace().AddProject("Test1", LanguageNames.CSharp);

            ConfigurationCompletionSource = new TaskCompletionSource<ProjectExtensibilityConfiguration>();
            TagHelpersCompletionSource = new TaskCompletionSource<TagHelperResolutionResult>();
            ConfigurationFactory = Mock.Of<ProjectExtensibilityConfigurationFactory>(f => f.GetConfigurationAsync(It.IsAny<Project>(), default(CancellationToken)) == ConfigurationCompletionSource.Task);
            TagHelperResolver = Mock.Of<TagHelperResolver>(f => f.GetTagHelpersAsync(It.IsAny<Project>(), default(CancellationToken)) == TagHelpersCompletionSource.Task);
        }

        private Project Project { get; }

        private ProjectExtensibilityConfigurationFactory ConfigurationFactory { get; }

        private TagHelperResolver TagHelperResolver { get; }

        private TaskCompletionSource<ProjectExtensibilityConfiguration> ConfigurationCompletionSource { get; }

        private TaskCompletionSource<TagHelperResolutionResult> TagHelpersCompletionSource { get; }

        [ForegroundFact]
        public async Task ProcessUpdateAsync_DoesntBlockForegroundThread()
        {
            // Arrange
            var worker = new DefaultProjectSnapshotWorker(Dispatcher, ConfigurationFactory, TagHelperResolver);

            var context = new ProjectSnapshotUpdateContext(Project);

            var configuration = Mock.Of<ProjectExtensibilityConfiguration>();
            var tagHelpers = Array.Empty<TagHelperDescriptor>();
            var tagHelperResolutionResult = new TagHelperResolutionResult(tagHelpers, Array.Empty<RazorDiagnostic>());

            // Act 1 -- We want to verify that this doesn't block the main thread
            var task = worker.ProcessUpdateAsync(context);

            // Assert 1
            //
            // We haven't let the background task proceed yet, so these are still null.
            Assert.Null(context.Configuration);
            Assert.Null(context.TagHelpers);

            // Act 2 - Ok let's go
            ConfigurationCompletionSource.SetResult(configuration);
            TagHelpersCompletionSource.SetResult(tagHelperResolutionResult);
            await task;

            // Assert 2
            Assert.Same(configuration, context.Configuration);
            Assert.Same(tagHelpers, context.TagHelpers);
        }
    }
}
