// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    // These tests cover the methods on ExtensionIntermediateNode that are used to implement visitors
    // that special case an extension node.
    public class ExtensionIntermediateNodeTest
    {
        [Fact]
        public void Accept_CallsStandardVisitExtension_ForStandardVisitor()
        {
            // Arrange
            var node = new TestExtensionIntermediateNode();
            var visitor = new StandardVisitor();

            // Act
            node.Accept(visitor);

            // Assert
            Assert.True(visitor.WasStandardMethodCalled);
            Assert.False(visitor.WasSpecificMethodCalled);
        }

        [Fact]
        public void Accept_CallsSpecialVisitExtension_ForSpecialVisitor()
        {
            // Arrange
            var node = new TestExtensionIntermediateNode();
            var visitor = new SpecialVisitor();

            // Act
            node.Accept(visitor);

            // Assert
            Assert.False(visitor.WasStandardMethodCalled);
            Assert.True(visitor.WasSpecificMethodCalled);
        }

        private class TestExtensionIntermediateNode : ExtensionIntermediateNode
        {
            public override IntermediateNodeCollection Children => ReadOnlyIntermediateNodeCollection.Instance;

            public override void Accept(IntermediateNodeVisitor visitor)
            {
                // This is the standard visitor boilerplate for an extension node.
                AcceptExtensionNode<TestExtensionIntermediateNode>(this, visitor);
            }

            public override void WriteNode(CodeTarget target, CodeRenderingContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class StandardVisitor : IntermediateNodeVisitor
        {
            public bool WasStandardMethodCalled { get; private set; }
            public bool WasSpecificMethodCalled { get; private set; }

            public override void VisitExtension(ExtensionIntermediateNode node)
            {
                WasStandardMethodCalled = true;
            }

            public void VisitExtension(TestExtensionIntermediateNode node)
            {
                WasSpecificMethodCalled = true;
            }
        }

        private class SpecialVisitor : IntermediateNodeVisitor, IExtensionIntermediateNodeVisitor<TestExtensionIntermediateNode>
        {
            public bool WasStandardMethodCalled { get; private set; }
            public bool WasSpecificMethodCalled { get; private set; }

            public override void VisitExtension(ExtensionIntermediateNode node)
            {
                WasStandardMethodCalled = true;
            }

            public void VisitExtension(TestExtensionIntermediateNode node)
            {
                WasSpecificMethodCalled = true;
            }
        }       
    }
}
