// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public static class RazorIRAssert
    {
        public static TNode SingleChild<TNode>(RazorIRNode node)
        {
            if (node.Children.Count == 0)
            {
                throw new IRAssertException(node, "The node has no children.");
            }
            else if (node.Children.Count > 1)
            {
                throw new IRAssertException(node, node.Children, "The node has multiple children");
            }

            var child = node.Children[0];
            return Assert.IsType<TNode>(child);
        }

        public static void NoChildren(RazorIRNode node)
        {
            if (node.Children.Count > 0)
            {
                throw new IRAssertException(node, node.Children, "The node has children.");
            }
        }

        public static void Children(RazorIRNode node, params Action<RazorIRNode>[] validators)
        {
            var i = 0;
            for (; i < validators.Length; i++)
            {
                if (node.Children.Count == i)
                {
                    throw new IRAssertException(node, node.Children, $"The node only has {node.Children.Count} children.");
                }

                try
                {
                    validators[i].Invoke(node.Children[i]);
                }
                catch (XunitException e)
                {
                    throw new IRAssertException(node, node.Children, $"Failed while validating node {node.Children[i]} at {i}.", e);
                }
            }

            if (i < node.Children.Count)
            {
                throw new IRAssertException(node, node.Children, $"The node has extra child {node.Children[i]} at {i}.");
            }
        }

        public static void Html(string expected, RazorIRNode node)
        {
            try
            {
                var html = Assert.IsType<HtmlContentIRNode>(node);
                Assert.Equal(expected, html.Content);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }
        }

        public static void Using(string expected, RazorIRNode node)
        {
            try
            {
                var @using = Assert.IsType<UsingStatementIRNode>(node);
                Assert.Equal(expected, @using.Content);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }
        }

        public static void ConditionalAttribute(
            string prefix,
            string name,
            string suffix,
            RazorIRNode node,
            params Action<RazorIRNode>[] valueValidators)
        {
            var attribute = Assert.IsType<HtmlAttributeIRNode>(node);

            try
            {
                Assert.Equal(prefix, attribute.Prefix);
                Assert.Equal(name, attribute.Name);
                Assert.Equal(suffix, attribute.Suffix);

                Children(attribute, valueValidators);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(attribute, attribute.Children, e.Message, e);
            }
        }

        public static void CSharpAttributeValue(string prefix, string expected, RazorIRNode node)
        {
            var attributeValue = Assert.IsType<CSharpAttributeValueIRNode>(node);

            try
            {
                Assert.Equal(prefix, attributeValue.Prefix);

                Children(attributeValue, n => CSharpExpression(expected, n));
            }
            catch (XunitException e)
            {
                throw new IRAssertException(attributeValue, attributeValue.Children, e.Message, e);
            }
        }

        public static void LiteralAttributeValue(string prefix, string expected, RazorIRNode node)
        {
            var attributeValue = Assert.IsType<HtmlAttributeValueIRNode>(node);

            try
            {
                Assert.Equal(prefix, attributeValue.Prefix);
                Assert.Equal(expected, attributeValue.Content);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(attributeValue, e.Message);
            }
        }

        public static void CSharpExpression(string expected, RazorIRNode node)
        {
            try
            {
                var cSharp = Assert.IsType<CSharpExpressionIRNode>(node);

                var content = new StringBuilder();
                for (var i = 0; i < cSharp.Children.Count; i++)
                {
                    content.Append(((CSharpTokenIRNode)cSharp.Children[i]).Content);
                }

                Assert.Equal(expected, content.ToString());
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }
        }

        private class IRAssertException : XunitException
        {
            public IRAssertException(RazorIRNode node, string userMessage)
                : base(Format(node, null, userMessage))
            {
                Node = node;
            }

            public IRAssertException(RazorIRNode node, IEnumerable<RazorIRNode> nodes, string userMessage)
                : base(Format(node, nodes, userMessage))
            {
                Node = node;
                Nodes = nodes;
            }

            public IRAssertException(
                RazorIRNode node,
                IEnumerable<RazorIRNode> nodes,
                string userMessage,
                Exception innerException)
                : base(Format(node, nodes, userMessage), innerException)
            {
            }

            public RazorIRNode Node { get; }

            public IEnumerable<RazorIRNode> Nodes { get; }

            private static string Format(RazorIRNode node, IEnumerable<RazorIRNode> nodes, string userMessage)
            {
                var builder = new StringBuilder();
                builder.AppendLine(userMessage);
                builder.AppendLine();

                if (nodes != null)
                {
                    builder.AppendLine("Nodes:");

                    foreach (var n in nodes)
                    {
                        builder.AppendLine(n.ToString());
                    }

                    builder.AppendLine();
                }


                builder.AppendLine("Path:");

                var current = node;
                do
                {
                    builder.AppendLine(current.ToString());
                }
                while ((current = current.Parent) != null);

                return builder.ToString();
            }
        }
    }
}
