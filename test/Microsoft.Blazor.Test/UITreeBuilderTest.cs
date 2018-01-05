// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.UITree;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.Blazor.Test
{
    public class UITreeBuilderTest
    {
        [Fact]
        public void StartsEmpty()
        {
            // Arrange
            var builder = new UITreeBuilder();

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
            var builder = new UITreeBuilder();

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
            var builder = new UITreeBuilder();

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
            var builder = new UITreeBuilder();

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
            var builder = new UITreeBuilder();

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
            var builder = new UITreeBuilder();

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
            var builder = new UITreeBuilder();
            UIEventHandler eventHandler = () => { };

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
            var builder = new UITreeBuilder();

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
            var builder = new UITreeBuilder();

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.AddAttribute("name", () => { });
            });
        }

        [Fact]
        public void CannotAddAttributeToText()
        {
            // Arrange
            var builder = new UITreeBuilder();

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
            var builder = new UITreeBuilder();

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenElement("some element");
                builder.AddText("hello");
                builder.AddAttribute("name", () => { });
            });
        }

        [Fact]
        public void CanClear()
        {
            // Arrange
            var builder = new UITreeBuilder();

            // Act
            builder.AddText("some text");
            builder.OpenElement("elem");
            builder.AddText("more text");
            builder.CloseElement();
            builder.Clear();

            // Assert
            Assert.Empty(builder.GetNodes());
        }

        void AssertText(UITreeNode node, string textContent)
        {
            Assert.Equal(UITreeNodeType.Text, node.NodeType);
            Assert.Equal(textContent, node.TextContent);
            Assert.Equal(0, node.ElementDescendantsEndIndex);
        }

        void AssertElement(UITreeNode node, string elementName, int descendantsEndIndex)
        {
            Assert.Equal(UITreeNodeType.Element, node.NodeType);
            Assert.Equal(elementName, node.ElementName);
            Assert.Equal(descendantsEndIndex, node.ElementDescendantsEndIndex);
        }

        void AssertAttribute(UITreeNode node, string attributeName)
        {
            Assert.Equal(UITreeNodeType.Attribute, node.NodeType);
            Assert.Equal(attributeName, node.AttributeName);
        }

        void AssertAttribute(UITreeNode node, string attributeName, string attributeValue)
        {
            AssertAttribute(node, attributeName);
            Assert.Equal(attributeValue, node.AttributeValue);
        }

        void AssertAttribute(UITreeNode node, string attributeName, UIEventHandler attributeEventHandlerValue)
        {
            AssertAttribute(node, attributeName);
            Assert.Equal(attributeEventHandlerValue, node.AttributeEventHandlerValue);
        }
    }
}
