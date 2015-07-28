// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class Block : SyntaxTreeNode
    {
        public Block(BlockBuilder source)
            : this(source.Type, source.Children, source.ChunkGenerator)
        {
            source.Reset();
        }

        protected Block(BlockType? type, IEnumerable<SyntaxTreeNode> contents, IParentChunkGenerator generator)
        {
            if (type == null)
            {
                throw new InvalidOperationException(RazorResources.Block_Type_Not_Specified);
            }

            Type = type.Value;
            Children = contents;
            ChunkGenerator = generator;

            foreach (SyntaxTreeNode node in Children)
            {
                node.Parent = this;
            }
        }

        // A Test constructor
        internal Block(BlockType type, IEnumerable<SyntaxTreeNode> contents, IParentChunkGenerator generator)
        {
            Type = type;
            ChunkGenerator = generator;
            Children = contents;
        }

        [SuppressMessage(
            "Microsoft.Naming",
            "CA1721:PropertyNamesShouldNotMatchGetMethods",
            Justification = "Type is the most appropriate name for this property and there is little chance of " +
            "confusion with GetType")]
        public BlockType Type { get; }

        public IEnumerable<SyntaxTreeNode> Children { get; }

        public IParentChunkGenerator ChunkGenerator { get; }

        public override bool IsBlock
        {
            get { return true; }
        }

        public override SourceLocation Start
        {
            get
            {
                SyntaxTreeNode child = Children.FirstOrDefault();
                if (child == null)
                {
                    return SourceLocation.Zero;
                }
                else
                {
                    return child.Start;
                }
            }
        }

        public override int Length
        {
            get { return Children.Sum(child => child.Length); }
        }

        public Span FindFirstDescendentSpan()
        {
            SyntaxTreeNode current = this;
            while (current != null && current.IsBlock)
            {
                current = ((Block)current).Children.FirstOrDefault();
            }
            return current as Span;
        }

        public Span FindLastDescendentSpan()
        {
            SyntaxTreeNode current = this;
            while (current != null && current.IsBlock)
            {
                current = ((Block)current).Children.LastOrDefault();
            }
            return current as Span;
        }

        public override void Accept(ParserVisitor visitor)
        {
            visitor.VisitBlock(this);
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0} Block at {1}::{2} (Gen:{3})",
                Type,
                Start,
                Length,
                ChunkGenerator);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Block;
            return other != null &&
                Type == other.Type &&
                Equals(ChunkGenerator, other.ChunkGenerator) &&
                ChildrenEqual(Children, other.Children);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Type)
                .Add(ChunkGenerator)
                .Add(Children)
                .CombinedHash;
        }

        public IEnumerable<Span> Flatten()
        {
            // Create an enumerable that flattens the tree for use by syntax highlighters, etc.
            foreach (SyntaxTreeNode element in Children)
            {
                var span = element as Span;
                if (span != null)
                {
                    yield return span;
                }
                else
                {
                    var block = element as Block;
                    foreach (Span childSpan in block.Flatten())
                    {
                        yield return childSpan;
                    }
                }
            }
        }

        public Span LocateOwner(TextChange change)
        {
            // Ask each child recursively
            Span owner = null;
            foreach (SyntaxTreeNode element in Children)
            {
                var span = element as Span;
                if (span == null)
                {
                    owner = ((Block)element).LocateOwner(change);
                }
                else
                {
                    if (change.OldPosition < span.Start.AbsoluteIndex)
                    {
                        // Early escape for cases where changes overlap multiple spans
                        // In those cases, the span will return false, and we don't want to search the whole tree
                        // So if the current span starts after the change, we know we've searched as far as we need to
                        break;
                    }
                    owner = span.EditHandler.OwnsChange(span, change) ? span : owner;
                }

                if (owner != null)
                {
                    break;
                }
            }
            return owner;
        }

        private static bool ChildrenEqual(IEnumerable<SyntaxTreeNode> left, IEnumerable<SyntaxTreeNode> right)
        {
            IEnumerator<SyntaxTreeNode> leftEnum = left.GetEnumerator();
            IEnumerator<SyntaxTreeNode> rightEnum = right.GetEnumerator();
            while (leftEnum.MoveNext())
            {
                if (!rightEnum.MoveNext() || // More items in left than in right
                    !Equals(leftEnum.Current, rightEnum.Current))
                {
                    // Nodes are not equal
                    return false;
                }
            }
            if (rightEnum.MoveNext())
            {
                // More items in right than left
                return false;
            }
            return true;
        }

        public override bool EquivalentTo(SyntaxTreeNode node)
        {
            var other = node as Block;
            if (other == null || other.Type != Type)
            {
                return false;
            }

            return Enumerable.SequenceEqual(Children, other.Children, new EquivalenceComparer());
        }

        public override int GetEquivalenceHash()
        {
            var combiner = HashCodeCombiner.Start().Add(Type);
            foreach (var child in Children)
            {
                combiner.Add(child.GetEquivalenceHash());
            }

            return combiner.CombinedHash;
        }
    }
}
