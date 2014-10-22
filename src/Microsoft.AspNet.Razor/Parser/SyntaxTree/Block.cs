// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class Block : SyntaxTreeNode
    {
        public Block(BlockBuilder source)
            : this(source.Type, source.Children, source.CodeGenerator)
        {
            source.Reset();
        }

        protected Block(BlockType? type, IEnumerable<SyntaxTreeNode> contents, IBlockCodeGenerator generator)
        {
            if (type == null)
            {
                throw new InvalidOperationException(RazorResources.Block_Type_Not_Specified);
            }

            Type = type.Value;
            Children = contents;
            CodeGenerator = generator;

            foreach (SyntaxTreeNode node in Children)
            {
                node.Parent = this;
            }
        }

        // A Test constructor
        internal Block(BlockType type, IEnumerable<SyntaxTreeNode> contents, IBlockCodeGenerator generator)
        {
            Type = type;
            CodeGenerator = generator;
            Children = contents;
        }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Type is the most appropriate name for this property and there is little chance of confusion with GetType")]
        public BlockType Type { get; private set; }

        public IEnumerable<SyntaxTreeNode> Children { get; private set; }

        public IBlockCodeGenerator CodeGenerator { get; private set; }

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
            return String.Format(CultureInfo.CurrentCulture, "{0} Block at {1}::{2} (Gen:{3})", Type, Start, Length, CodeGenerator);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Block;
            return other != null &&
                   Type == other.Type &&
                   Equals(CodeGenerator, other.CodeGenerator) &&
                   ChildrenEqual(Children, other.Children);
        }

        public override int GetHashCode()
        {
            return (int)Type;
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
    }
}
