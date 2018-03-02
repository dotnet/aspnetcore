// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Editor.Razor;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    public class DefaultDotNetProjectHostTest : ForegroundDispatcherTestBase
    {
        [Fact]
        public void UpdateRazorHostProject_UnsupportedProjectNoops()
        {
            // Arrange
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.IsSupportedProject(It.IsAny<object>()))
                .Returns(false);
            var dotNetProjectHost = new DefaultDotNetProjectHost(
                Dispatcher,
                Mock.Of<VisualStudioMacWorkspaceAccessor>(),
                projectService.Object);

            // Act & Assert
            dotNetProjectHost.UpdateRazorHostProject();
        }

        // -------------------------------------------------------------------------------------------
        // Purposefully do not have any more tests here because that would involve mocking MonoDevelop 
        // types. The default constructors for the Solution / DotNetProject MonoDevelop types change
        // static classes (they assume they're being created in an IDE).
        // -------------------------------------------------------------------------------------------
    }
}
