// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
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

        public static void AnnotationEquals(RazorIRNode node, object value)
        {
            AnnotationEquals(node, value, value);
        }

        public static void AnnotationEquals(RazorIRNode node, object key, object value)
        {
            try
            {
                Assert.NotNull(node);
                Assert.Equal(value, node.Annotations[key]);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }
        }

        public static void HasAnnotation(RazorIRNode node, object key)
        {
            try
            {
                Assert.NotNull(node);
                Assert.NotNull(node.Annotations[key]);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }
        }

        public static void Html(string expected, RazorIRNode node)
        {
            try
            {
                var html = Assert.IsType<HtmlContentIRNode>(node);
                var content = new StringBuilder();
                for (var i = 0; i < html.Children.Count; i++)
                {
                    var token = Assert.IsType<RazorIRToken>(html.Children[i]);
                    Assert.Equal(RazorIRToken.TokenKind.Html, token.Kind);
                    content.Append(token.Content);
                }

                Assert.Equal(expected, content.ToString());
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }
        }

        public static void CSharpCode(string expected, RazorIRNode node)
        {
            try
            {
                var statement = Assert.IsType<CSharpCodeIRNode>(node);
                var content = new StringBuilder();
                for (var i = 0; i < statement.Children.Count; i++)
                {
                    var token = Assert.IsType<RazorIRToken>(statement.Children[i]);
                    Assert.Equal(RazorIRToken.TokenKind.CSharp, token.Kind);
                    content.Append(token.Content);
                }

                Assert.Equal(expected, content.ToString());
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

        public static void DirectiveToken(DirectiveTokenKind expectedKind, string expectedContent, RazorIRNode node)
        {
            try
            {
                var token = Assert.IsType<DirectiveTokenIRNode>(node);
                Assert.Equal(expectedKind, token.Descriptor.Kind);
                Assert.Equal(expectedContent, token.Content);
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

        public static void CSharpExpressionAttributeValue(string prefix, string expected, RazorIRNode node)
        {
            var attributeValue = Assert.IsType<CSharpExpressionAttributeValueIRNode>(node);

            try
            {
                var content = new StringBuilder();
                for (var i = 0; i < attributeValue.Children.Count; i++)
                {
                    var token = Assert.IsType<RazorIRToken>(attributeValue.Children[i]);
                    Assert.True(token.IsCSharp);
                    content.Append(token.Content);
                }

                Assert.Equal(prefix, attributeValue.Prefix);
                Assert.Equal(expected, content.ToString());
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
                var content = new StringBuilder();
                for (var i = 0; i < attributeValue.Children.Count; i++)
                {
                    var token = Assert.IsType<RazorIRToken>(attributeValue.Children[i]);
                    Assert.True(token.IsHtml);
                    content.Append(token.Content);
                }

                Assert.Equal(prefix, attributeValue.Prefix);
                Assert.Equal(expected, content.ToString());
            }
            catch (XunitException e)
            {
                throw new IRAssertException(attributeValue, e.Message);
            }
        }

        public static void Checksum(RazorIRNode node)
        {
            try
            {
                Assert.IsType<ChecksumIRNode>(node);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
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
                    var token = Assert.IsType<RazorIRToken>(cSharp.Children[i]);
                    Assert.Equal(RazorIRToken.TokenKind.CSharp, token.Kind);
                    content.Append(token.Content);
                }

                Assert.Equal(expected, content.ToString());
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }
        }

        public static void BeginInstrumentation(string expected, RazorIRNode node)
        {
            try
            {
                var beginNode = Assert.IsType<CSharpCodeIRNode>(node);
                var content = new StringBuilder();
                for (var i = 0; i < beginNode.Children.Count; i++)
                {
                    var token = Assert.IsType<RazorIRToken>(beginNode.Children[i]);
                    Assert.True(token.IsCSharp);
                    content.Append(token.Content);
                }

                Assert.Equal($"BeginContext({expected});", content.ToString());
            }
            catch (XunitException e)
            {
                throw new IRAssertException(node, node.Children, e.Message, e);
            }
        }

        public static void EndInstrumentation(RazorIRNode node)
        {
            try
            {
                var endNode = Assert.IsType<CSharpCodeIRNode>(node);
                var content = new StringBuilder();
                for (var i = 0; i < endNode.Children.Count; i++)
                {
                    var token = Assert.IsType<RazorIRToken>(endNode.Children[i]);
                    Assert.Equal(RazorIRToken.TokenKind.CSharp, token.Kind);
                    content.Append(token.Content);
                }
                Assert.Equal("EndContext();", content.ToString());
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

        internal static void TagHelper(string tagName, TagMode tagMode, RazorIRNode node, params Action<RazorIRNode>[] childValidators)
        {
            var tagHelperNode = Assert.IsType<TagHelperIRNode>(node);

            try
            {
                Assert.Equal(tagName, tagHelperNode.TagName);
                Assert.Equal(tagMode, tagHelperNode.TagMode);
            }
            catch (XunitException e)
            {
                throw new IRAssertException(tagHelperNode, e.Message);
            }

            Children(node, childValidators);
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
                : base(Format(node, null, null, userMessage))
            {
                Node = node;
            }

            public IRAssertException(RazorIRNode node, IEnumerable<RazorIRNode> nodes, string userMessage)
                : base(Format(node, null, nodes, userMessage))
            {
                Node = node;
                Nodes = nodes;
            }

            public IRAssertException(
                RazorIRNode node,
                IEnumerable<RazorIRNode> nodes,
                string userMessage,
                Exception innerException)
                : base(Format(node, null, nodes, userMessage), innerException)
            {
            }

            public IRAssertException(
                RazorIRNode node,
                RazorIRNode[] ancestors,
                IEnumerable<RazorIRNode> nodes,
                string userMessage,
                Exception innerException)
                : base(Format(node, ancestors, nodes, userMessage), innerException)
            {
            }

            public RazorIRNode Node { get; }

            public IEnumerable<RazorIRNode> Nodes { get; }

            private static string Format(RazorIRNode node, RazorIRNode[] ancestors, IEnumerable<RazorIRNode> nodes, string userMessage)
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

                if (ancestors != null)
                {
                    builder.AppendLine();
                    builder.AppendLine("Path:");

                    foreach (var ancestor in ancestors)
                    {
                        builder.AppendLine(ancestor.ToString());
                    }
                }

                return builder.ToString();
            }
        }
    }
}
