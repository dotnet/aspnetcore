// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
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

        public static void CSharpStatement(string expected, RazorIRNode node)
        {
            try
            {
                var statement = Assert.IsType<CSharpStatementIRNode>(node);
                Assert.Equal(expected, statement.Content);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }
        }

        public static void Directive(string expectedName, RazorIRNode node, params Action<RazorIRNode>[] childValidators)
        {
            try
            {
                var directive = Assert.IsType<DirectiveIRNode>(node);
                Assert.Equal(expectedName, directive.Name);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }

            Children(node, childValidators);
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

        internal static void TagHelperFieldDeclaration(RazorIRNode node, params string[] tagHelperTypes)
        {
            var declareTagHelperFields = Assert.IsType<DeclareTagHelperFieldsIRNode>(node);

            try
            {
                Assert.Equal(tagHelperTypes, declareTagHelperFields.UsedTagHelperTypeNames);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(declareTagHelperFields, e.Message);
            }
        }

        internal static void DeclarePreallocatedTagHelperAttribute(
            RazorIRNode node,
            string attributeName,
            string value,
            HtmlAttributeValueStyle valueStyle)
        {
            var declarePreallocatedTagHelperAttribute = Assert.IsType<DeclarePreallocatedTagHelperAttributeIRNode>(node);

            try
            {
                Assert.Equal(attributeName, declarePreallocatedTagHelperAttribute.Name);
                Assert.Equal(value, declarePreallocatedTagHelperAttribute.Value);
                Assert.Equal(valueStyle, declarePreallocatedTagHelperAttribute.ValueStyle);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(declarePreallocatedTagHelperAttribute, e.Message);
            }
        }

        internal static void TagHelperStructure(string tagName, TagMode tagMode, RazorIRNode node)
        {
            var tagHelperStructureNode = Assert.IsType<InitializeTagHelperStructureIRNode>(node);

            try
            {
                Assert.Equal(tagName, tagHelperStructureNode.TagName);
                Assert.Equal(tagMode, tagHelperStructureNode.TagMode);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(tagHelperStructureNode, e.Message);
            }
        }

        internal static void TagHelperHtmlAttribute(
            string name,
            HtmlAttributeValueStyle valueStyle,
            RazorIRNode node,
            params Action<RazorIRNode>[] valueValidators)
        {
            var tagHelperHtmlAttribute = Assert.IsType<AddTagHelperHtmlAttributeIRNode>(node);

            try
            {
                Assert.Equal(name, tagHelperHtmlAttribute.Name);
                Assert.Equal(valueStyle, tagHelperHtmlAttribute.ValueStyle);
                Children(tagHelperHtmlAttribute, valueValidators);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(tagHelperHtmlAttribute, tagHelperHtmlAttribute.Children, e.Message, e);
            }
        }

        internal static void SetPreallocatedTagHelperProperty(RazorIRNode node, string attributeName, string propertyName)
        {
            var setPreallocatedTagHelperProperty = Assert.IsType<SetPreallocatedTagHelperPropertyIRNode>(node);

            try
            {
                Assert.Equal(attributeName, setPreallocatedTagHelperProperty.AttributeName);
                Assert.Equal(propertyName, setPreallocatedTagHelperProperty.PropertyName);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(setPreallocatedTagHelperProperty, e.Message);
            }
        }

        internal static void SetTagHelperProperty(
            string name,
            string propertyName,
            HtmlAttributeValueStyle valueStyle,
            RazorIRNode node,
            params Action<RazorIRNode>[] valueValidators)
        {
            var tagHelperBoundAttribute = Assert.IsType<SetTagHelperPropertyIRNode>(node);

            try
            {
                Assert.Equal(name, tagHelperBoundAttribute.AttributeName);
                Assert.Equal(propertyName, tagHelperBoundAttribute.PropertyName);
                Assert.Equal(valueStyle, tagHelperBoundAttribute.ValueStyle);
                Children(tagHelperBoundAttribute, valueValidators);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(tagHelperBoundAttribute, tagHelperBoundAttribute.Children, e.Message, e);
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
