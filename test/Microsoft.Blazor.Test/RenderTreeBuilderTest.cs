// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.Rendering;
using Microsoft.Blazor.RenderTree;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.Blazor.Test
{
    public class RenderTreeBuilderTest
    {
        [Fact]
        public void StartsEmpty()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Assert
            var nodes = builder.GetNodes();
            Assert.NotNull(nodes.Array);
            Assert.Equal(0, nodes.Offset);
            Assert.Empty(nodes);
        }

        [Fact]
        public void CanAddText()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.AddText("First item");
            builder.AddText("Second item");

            // Assert
            var nodes = builder.GetNodes();
            Assert.Equal(0, nodes.Offset);
            Assert.Collection(nodes,
                node => AssertText(node, "First item"),
                node => AssertText(node, "Second item"));
        }

        [Fact]
        public void UnclosedElementsHaveNoEndDescendantIndex()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement("my element");

            // Assert
            var node = builder.GetNodes().Single();
            AssertElement(node, "my element", 0);
        }

        [Fact]
        public void ClosedEmptyElementsHaveSelfAsEndDescendantIndex()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.AddText("some node so that the element isn't at position zero");
            builder.OpenElement("my element");
            builder.CloseElement();

            // Assert
            var nodes = builder.GetNodes();
            Assert.Equal(2, nodes.Count);
            AssertElement(nodes[1], "my element", 1);
        }

        [Fact]
        public void ClosedElementsHaveCorrectEndDescendantIndex()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement("my element");
            builder.AddText("child 1");
            builder.AddText("child 2");
            builder.CloseElement();
            builder.AddText("unrelated item");

            // Assert
            var nodes = builder.GetNodes();
            Assert.Equal(4, nodes.Count);
            AssertElement(nodes[0], "my element", 2);
        }

        [Fact]
        public void CanNestElements()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.AddText("standalone text 1");   //  0: standalone text 1
            builder.OpenElement("root");            //  1: <root>
            builder.AddText("root text 1");         //  2:     root text 1
            builder.AddText("root text 2");         //  3:     root text 2
            builder.OpenElement("child");           //  4:     <child>
            builder.AddText("child text");          //  5:         child text
            builder.OpenElement("grandchild");      //  6:         <grandchild>
            builder.AddText("grandchild text 1");   //  7:             grandchild text 1
            builder.AddText("grandchild text 2");   //  8:             grandchild text 2
            builder.CloseElement();                 //             </grandchild>
            builder.CloseElement();                 //         </child>
            builder.AddText("root text 3");         //  9:     root text 3
            builder.OpenElement("child 2");         // 10:     <child 2>
            builder.CloseElement();                 //         </child 2>
            builder.CloseElement();                 //      </root>
            builder.AddText("standalone text 2");   // 11:  standalone text 2

            // Assert
            Assert.Collection(builder.GetNodes(),
                node => AssertText(node, "standalone text 1"),
                node => AssertElement(node, "root", 10),
                node => AssertText(node, "root text 1"),
                node => AssertText(node, "root text 2"),
                node => AssertElement(node, "child", 8),
                node => AssertText(node, "child text"),
                node => AssertElement(node, "grandchild", 8),
                node => AssertText(node, "grandchild text 1"),
                node => AssertText(node, "grandchild text 2"),
                node => AssertText(node, "root text 3"),
                node => AssertElement(node, "child 2", 10),
                node => AssertText(node, "standalone text 2"));
        }

        [Fact]
        public void CanAddAttributes()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());
            UIEventHandler eventHandler = eventInfo => { };

            // Act
            builder.OpenElement("myelement");                       //  0: <myelement
            builder.AddAttribute("attribute1", "value 1");          //  1:     attribute1="value 1"
            builder.AddAttribute("attribute2", "value 2");          //  2:     attribute2="value 2">
            builder.OpenElement("child");                           //  3:   <child
            builder.AddAttribute("childevent", eventHandler);       //  4:       childevent=eventHandler>
            builder.AddText("some text");                           //  5:     some text
            builder.CloseElement();                                 //       </child>
            builder.CloseElement();                                 //     </myelement>

            // Assert
            Assert.Collection(builder.GetNodes(),
                node => AssertElement(node, "myelement", 5),
                node => AssertAttribute(node, "attribute1", "value 1"),
                node => AssertAttribute(node, "attribute2", "value 2"),
                node => AssertElement(node, "child", 5),
                node => AssertAttribute(node, "childevent", eventHandler),
                node => AssertText(node, "some text"));
        }

        [Fact]
        public void CannotAddAttributeAtRoot()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.AddAttribute("name", "value");
            });
        }

        [Fact]
        public void CannotAddEventHandlerAttributeAtRoot()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.AddAttribute("name", eventInfo => { });
            });
        }

        [Fact]
        public void CannotAddAttributeToText()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenElement("some element");
                builder.AddText("hello");
                builder.AddAttribute("name", "value");
            });
        }

        [Fact]
        public void CannotAddEventHandlerAttributeToText()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenElement("some element");
                builder.AddText("hello");
                builder.AddAttribute("name", eventInfo => { });
            });
        }

        [Fact]
        public void CanAddChildComponents()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement("parent");                      //  0: <parent>
            builder.AddComponent<TestComponent>();              //  1:     <testcomponent
            builder.AddAttribute("child1attribute1", "A");      //  2:       child1attribute1="A"
            builder.AddAttribute("child1attribute2", "B");      //  3:       child1attribute2="B" />
            builder.AddComponent<TestComponent>();              //  4:     <testcomponent
            builder.AddAttribute("child2attribute", "C");       //  5:       child2attribute="C" />
            builder.CloseElement();                             //     </parent>

            // Assert
            Assert.Collection(builder.GetNodes(),
                node => AssertElement(node, "parent", 5),
                node => AssertComponent<TestComponent>(node),
                node => AssertAttribute(node, "child1attribute1", "A"),
                node => AssertAttribute(node, "child1attribute2", "B"),
                node => AssertComponent<TestComponent>(node),
                node => AssertAttribute(node, "child2attribute", "C"));
        }

        [Fact]
        public void CanClear()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.AddText("some text");
            builder.OpenElement("elem");
            builder.AddText("more text");
            builder.CloseElement();
            builder.Clear();

            // Assert
            Assert.Empty(builder.GetNodes());
        }

        void AssertText(RenderTreeNode node, string textContent)
        {
            Assert.Equal(RenderTreeNodeType.Text, node.NodeType);
            Assert.Equal(textContent, node.TextContent);
            Assert.Equal(0, node.ElementDescendantsEndIndex);
        }

        void AssertElement(RenderTreeNode node, string elementName, int descendantsEndIndex)
        {
            Assert.Equal(RenderTreeNodeType.Element, node.NodeType);
            Assert.Equal(elementName, node.ElementName);
            Assert.Equal(descendantsEndIndex, node.ElementDescendantsEndIndex);
        }

        void AssertAttribute(RenderTreeNode node, string attributeName)
        {
            Assert.Equal(RenderTreeNodeType.Attribute, node.NodeType);
            Assert.Equal(attributeName, node.AttributeName);
        }

        void AssertAttribute(RenderTreeNode node, string attributeName, string attributeValue)
        {
            AssertAttribute(node, attributeName);
            Assert.Equal(attributeValue, node.AttributeValue);
        }

        void AssertAttribute(RenderTreeNode node, string attributeName, UIEventHandler attributeEventHandlerValue)
        {
            AssertAttribute(node, attributeName);
            Assert.Equal(attributeEventHandlerValue, node.AttributeEventHandlerValue);
        }

        private void AssertComponent<T>(RenderTreeNode node) where T: IComponent
        {
            Assert.Equal(RenderTreeNodeType.Component, node.NodeType);

            // Currently, we instantiate child components during the tree building phase.
            // Later this will change so it happens during the tree diffing phase, so this
            // logic will need to change. It will need to verify that we're tracking the
            // information needed to instantiate the component.
            Assert.NotNull(node.Component);
            Assert.IsType<T>(node.Component);
        }

        private class TestComponent : IComponent
        {
            public void BuildRenderTree(RenderTreeBuilder builder)
                => throw new NotImplementedException();
        }

        private class TestRenderer : Renderer
        {
            protected override void UpdateDisplay(int componentId, ArraySegment<RenderTreeNode> renderTree)
                => throw new NotImplementedException();
        }
    }
}
