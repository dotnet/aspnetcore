// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class IntermediateNodeReferenceTest
    {
        [Fact]
        public void InsertAfter_SingleNode_AddsNodeAfterNode()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");

            parent.Children.Add(node1);
            parent.Children.Add(node3);

            var reference = new IntermediateNodeReference(parent, node1);

            // Act
            reference.InsertAfter(node2);

            // Assert
            Assert.Equal(new[] { node1, node2, node3, }, parent.Children);
        }

        [Fact]
        public void InsertAfter_SingleNode_AddsNodeAfterNode_AtEnd()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");

            parent.Children.Add(node1);
            parent.Children.Add(node2);

            var reference = new IntermediateNodeReference(parent, node2);

            // Act
            reference.InsertAfter(node3);

            // Assert
            Assert.Equal(new[] { node1, node2, node3, }, parent.Children);
        }

        [Fact]
        public void InsertAfter_MultipleNodes_AddsNodesAfterNode()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");
            var node4 = new BasicIntermediateNode("Node4");

            parent.Children.Add(node1);
            parent.Children.Add(node4);

            var reference = new IntermediateNodeReference(parent, node1);

            // Act
            reference.InsertAfter(new[] { node2, node3 });

            // Assert
            Assert.Equal(new[] { node1, node2, node3, node4, }, parent.Children);
        }

        [Fact]
        public void InsertAfter_MultipleNodes_AddsNodesAfterNode_AtEnd()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");
            var node4 = new BasicIntermediateNode("Node4");

            parent.Children.Add(node1);
            parent.Children.Add(node2);

            var reference = new IntermediateNodeReference(parent, node2);

            // Act
            reference.InsertAfter(new[] { node3, node4 });

            // Assert
            Assert.Equal(new[] { node1, node2, node3, node4, }, parent.Children);
        }

        [Fact]
        public void InsertBefore_SingleNode_AddsNodeBeforeNode()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");

            parent.Children.Add(node1);
            parent.Children.Add(node3);

            var reference = new IntermediateNodeReference(parent, node3);

            // Act
            reference.InsertBefore(node2);

            // Assert
            Assert.Equal(new[] { node1, node2, node3, }, parent.Children);
        }

        [Fact]
        public void InsertBefore_SingleNode_AddsNodeBeforeNode_AtBeginning()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");

            parent.Children.Add(node2);
            parent.Children.Add(node3);

            var reference = new IntermediateNodeReference(parent, node2);

            // Act
            reference.InsertBefore(node1);

            // Assert
            Assert.Equal(new[] { node1, node2, node3, }, parent.Children);
        }

        [Fact]
        public void InsertBefore_MultipleNodes_AddsNodesBeforeNode()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");
            var node4 = new BasicIntermediateNode("Node4");

            parent.Children.Add(node1);
            parent.Children.Add(node4);

            var reference = new IntermediateNodeReference(parent, node4);

            // Act
            reference.InsertBefore(new[] { node2, node3 });

            // Assert
            Assert.Equal(new[] { node1, node2, node3, node4, }, parent.Children);
        }

        [Fact]
        public void InsertAfter_MultipleNodes_AddsNodesBeforeNode_AtBeginning()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");
            var node4 = new BasicIntermediateNode("Node4");

            parent.Children.Add(node3);
            parent.Children.Add(node4);

            var reference = new IntermediateNodeReference(parent, node3);

            // Act
            reference.InsertBefore(new[] { node1, node2 });

            // Assert
            Assert.Equal(new[] { node1, node2, node3, node4, }, parent.Children);
        }

        [Fact]
        public void Remove_RemovesNode()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");

            parent.Children.Add(node1);
            parent.Children.Add(node3);
            parent.Children.Add(node2);

            var reference = new IntermediateNodeReference(parent, node3);

            // Act
            reference.Remove();

            // Assert
            Assert.Equal(new[] { node1, node2,}, parent.Children);
        }

        [Fact]
        public void Replace_ReplacesNode()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");
            var node2 = new BasicIntermediateNode("Node2");
            var node3 = new BasicIntermediateNode("Node3");
            var node4 = new BasicIntermediateNode("Node4");

            parent.Children.Add(node1);
            parent.Children.Add(node4);
            parent.Children.Add(node3);

            var reference = new IntermediateNodeReference(parent, node4);

            // Act
            reference.Replace(node2);

            // Assert
            Assert.Equal(new[] { node1, node2, node3, }, parent.Children);
        }

        [Fact]
        public void InsertAfter_SingleNode_ThrowsForReferenceNotInitialized()
        {
            // Arrange
            var reference = new IntermediateNodeReference();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertAfter(new BasicIntermediateNode("_")));
            Assert.Equal("The reference is invalid. References initialized with the default constructor cannot modify nodes.", exception.Message);
        }

        [Fact]
        public void InsertAfter_MulipleNodes_ThrowsForReferenceNotInitialized()
        {
            // Arrange
            var reference = new IntermediateNodeReference();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertAfter(new[] { new BasicIntermediateNode("_") }));
            Assert.Equal("The reference is invalid. References initialized with the default constructor cannot modify nodes.", exception.Message);
        }

        [Fact]
        public void InsertBefore_SingleNode_ThrowsForReferenceNotInitialized()
        {
            // Arrange
            var reference = new IntermediateNodeReference();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertBefore(new BasicIntermediateNode("_")));
            Assert.Equal("The reference is invalid. References initialized with the default constructor cannot modify nodes.", exception.Message);
        }

        [Fact]
        public void InsertBefore_MulipleNodes_ThrowsForReferenceNotInitialized()
        {
            // Arrange
            var reference = new IntermediateNodeReference();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertBefore(new[] { new BasicIntermediateNode("_") }));
            Assert.Equal("The reference is invalid. References initialized with the default constructor cannot modify nodes.", exception.Message);
        }

        [Fact]
        public void Remove_ThrowsForReferenceNotInitialized()
        {
            // Arrange
            var reference = new IntermediateNodeReference();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.Remove());
            Assert.Equal("The reference is invalid. References initialized with the default constructor cannot modify nodes.", exception.Message);
        }

        [Fact]
        public void Replace_ThrowsForReferenceNotInitialized()
        {
            // Arrange
            var reference = new IntermediateNodeReference();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.Replace(new BasicIntermediateNode("_")));
            Assert.Equal("The reference is invalid. References initialized with the default constructor cannot modify nodes.", exception.Message);
        }

        [Fact]
        public void InsertAfter_SingleNode_ThrowsForReadOnlyCollection()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent", IntermediateNodeCollection.ReadOnly);

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertAfter(new BasicIntermediateNode("_")));
            Assert.Equal("The node 'Parent' has a read-only child collection and cannot be modified.", exception.Message);
        }

        [Fact]
        public void InsertAfter_MulipleNodes_ThrowsForReadOnlyCollection()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent", IntermediateNodeCollection.ReadOnly);

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertAfter(new[] { new BasicIntermediateNode("_") }));
            Assert.Equal("The node 'Parent' has a read-only child collection and cannot be modified.", exception.Message);
        }

        [Fact]
        public void InsertBefore_SingleNode_ThrowsForReadOnlyCollection()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent", IntermediateNodeCollection.ReadOnly);

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertBefore(new BasicIntermediateNode("_")));
            Assert.Equal("The node 'Parent' has a read-only child collection and cannot be modified.", exception.Message);
        }

        [Fact]
        public void InsertBefore_MulipleNodes_ThrowsForReadOnlyCollection()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent", IntermediateNodeCollection.ReadOnly);

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertBefore(new[] { new BasicIntermediateNode("_") }));
            Assert.Equal("The node 'Parent' has a read-only child collection and cannot be modified.", exception.Message);
        }

        [Fact]
        public void Remove_ThrowsForReadOnlyCollection()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent", IntermediateNodeCollection.ReadOnly);

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.Remove());
            Assert.Equal("The node 'Parent' has a read-only child collection and cannot be modified.", exception.Message);
        }

        [Fact]
        public void Replace_ThrowsForReadOnlyCollection()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent", IntermediateNodeCollection.ReadOnly);

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.Replace(new BasicIntermediateNode("_")));
            Assert.Equal("The node 'Parent' has a read-only child collection and cannot be modified.", exception.Message);
        }

        [Fact]
        public void InsertAfter_SingleNode_ThrowsForNodeNotFound()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertAfter(new BasicIntermediateNode("_")));
            Assert.Equal("The reference is invalid. The node 'Node1' could not be found as a child of 'Parent'.", exception.Message);
        }

        [Fact]
        public void InsertAfter_MulipleNodes_ThrowsForNodeNotFound()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertAfter(new[] { new BasicIntermediateNode("_") }));
            Assert.Equal("The reference is invalid. The node 'Node1' could not be found as a child of 'Parent'.", exception.Message);
        }

        [Fact]
        public void InsertBefore_SingleNode_ThrowsForNodeNotFound()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertBefore(new BasicIntermediateNode("_")));
            Assert.Equal("The reference is invalid. The node 'Node1' could not be found as a child of 'Parent'.", exception.Message);
        }

        [Fact]
        public void InsertBefore_MulipleNodes_ThrowsForNodeNotFound()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.InsertBefore(new[] { new BasicIntermediateNode("_") }));
            Assert.Equal("The reference is invalid. The node 'Node1' could not be found as a child of 'Parent'.", exception.Message);
        }

        [Fact]
        public void Remove_ThrowsForNodeNotFound()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.Remove());
            Assert.Equal("The reference is invalid. The node 'Node1' could not be found as a child of 'Parent'.", exception.Message);
        }

        [Fact]
        public void Replace_ThrowsForNodeNotFound()
        {
            // Arrange
            var parent = new BasicIntermediateNode("Parent");

            var node1 = new BasicIntermediateNode("Node1");

            var reference = new IntermediateNodeReference(parent, node1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reference.Replace(new BasicIntermediateNode("_")));
            Assert.Equal("The reference is invalid. The node 'Node1' could not be found as a child of 'Parent'.", exception.Message);
        }

        private class BasicIntermediateNode : IntermediateNode
        {
            public BasicIntermediateNode(string name)
                : this(name, new IntermediateNodeCollection())
            {
                Name = name;
            }

            public BasicIntermediateNode(string name, IntermediateNodeCollection children)
            {
                Name = name;
                Children = children;
            }

            public string Name { get; }

            public override IntermediateNodeCollection Children { get; }

            public override void Accept(IntermediateNodeVisitor visitor)
            {
                throw new System.NotImplementedException();
            }

            public override string ToString() => Name;
        }
    }
}
