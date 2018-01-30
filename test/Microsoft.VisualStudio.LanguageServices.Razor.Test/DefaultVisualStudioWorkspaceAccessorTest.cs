// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class DefaultVisualStudioWorkspaceAccessorTest
    {
        [Fact]
        public void TryGetWorkspace_CanGetWorkspaceFromProjectionBuffersOnly()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var workspaceAccessor = new TestWorkspaceAccessor(true, false);

            // Act
            var result = workspaceAccessor.TryGetWorkspace(textBuffer, out var workspace);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TryGetWorkspace_CanGetWorkspaceFromBuffersInHierarchyOnly()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var workspaceAccessor = new TestWorkspaceAccessor(false, true);

            // Act
            var result = workspaceAccessor.TryGetWorkspace(textBuffer, out var workspace);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TryGetWorkspace_CanGetWorkspaceFromBuffersInHierarchyOrProjectionBuffers()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var workspaceAccessor = new TestWorkspaceAccessor(true, true);

            // Act
            var result = workspaceAccessor.TryGetWorkspace(textBuffer, out var workspace);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TryGetWorkspaceFromProjectionBuffer_NoProjectionBuffer_ReturnsFalse()
        {
            // Arrange
            var bufferGraph = new Mock<IBufferGraph>();
            bufferGraph.Setup(graph => graph.GetTextBuffers(It.IsAny<Predicate<ITextBuffer>>()))
                .Returns<Predicate<ITextBuffer>>(predicate => new Collection<ITextBuffer>());
            var bufferGraphService = new Mock<IBufferGraphFactoryService>();
            bufferGraphService.Setup(service => service.CreateBufferGraph(It.IsAny<ITextBuffer>()))
                .Returns(bufferGraph.Object);
            var workspaceAccessor = new DefaultVisualStudioWorkspaceAccessor(bufferGraphService.Object, Mock.Of<TextBufferProjectService>(), TestWorkspace.Create());
            var textBuffer = Mock.Of<ITextBuffer>();

            // Act
            var result = workspaceAccessor.TryGetWorkspaceFromProjectionBuffer(textBuffer, out var workspace);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryGetWorkspaceFromHostProject_NoHostProject_ReturnsFalse()
        {
            // Arrange
            var workspaceAccessor = new DefaultVisualStudioWorkspaceAccessor(Mock.Of<IBufferGraphFactoryService>(), Mock.Of<TextBufferProjectService>(), TestWorkspace.Create());
            var textBuffer = Mock.Of<ITextBuffer>();

            // Act
            var result = workspaceAccessor.TryGetWorkspaceFromHostProject(textBuffer, out var workspace);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryGetWorkspaceFromHostProject_HasHostProject_ReturnsTrueWithDefaultWorkspace()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var projectService = Mock.Of<TextBufferProjectService>(service => service.GetHostProject(textBuffer) == new object());
            var defaultWorkspace = TestWorkspace.Create();
            var workspaceAccessor = new DefaultVisualStudioWorkspaceAccessor(Mock.Of<IBufferGraphFactoryService>(), projectService, defaultWorkspace);

            // Act
            var result = workspaceAccessor.TryGetWorkspaceFromHostProject(textBuffer, out var workspace);

            // Assert
            Assert.True(result);
            Assert.Same(defaultWorkspace, workspace);
        }

        private class TestWorkspaceAccessor : DefaultVisualStudioWorkspaceAccessor
        {
            private readonly bool _canGetWorkspaceFromProjectionBuffer;
            private readonly bool _canGetWorkspaceFromHostProject;

            internal TestWorkspaceAccessor(bool canGetWorkspaceFromProjectionBuffer, bool canGetWorkspaceFromHostProject) :
                base(
                    Mock.Of<IBufferGraphFactoryService>(),
                    Mock.Of<TextBufferProjectService>(),
                    TestWorkspace.Create())
            {
                _canGetWorkspaceFromProjectionBuffer = canGetWorkspaceFromProjectionBuffer;
                _canGetWorkspaceFromHostProject = canGetWorkspaceFromHostProject;
            }

            internal override bool TryGetWorkspaceFromProjectionBuffer(ITextBuffer textBuffer, out Workspace workspace)
            {
                if (_canGetWorkspaceFromProjectionBuffer)
                {
                    workspace = TestWorkspace.Create();
                    return true;
                }

                workspace = null;
                return false;
            }

            internal override bool TryGetWorkspaceFromHostProject(ITextBuffer textBuffer, out Workspace workspace)
            {
                if (_canGetWorkspaceFromHostProject)
                {
                    workspace = TestWorkspace.Create();
                    return true;
                }

                workspace = null;
                return false;
            }
        }
    }
}
