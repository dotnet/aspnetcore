// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class DocumentIntermediateNodeExtensionsTest
    {
        [Fact]
        public void FindPrimaryClass_FindsClassWithAnnotation()
        {
            // Arrange
            var document = new DocumentIntermediateNode();
            var @class = new ClassDeclarationIntermediateNode();
            @class.Annotations[CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass;

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Add(@class);

            // Act
            var result = document.FindPrimaryClass();

            // Assert
            Assert.Same(@class, result);
        }

        [Fact]
        public void FindPrimaryMethod_FindsMethodWithAnnotation()
        {
            // Arrange
            var document = new DocumentIntermediateNode();
            var method = new MethodDeclarationIntermediateNode();
            method.Annotations[CommonAnnotations.PrimaryMethod] = CommonAnnotations.PrimaryMethod;

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Add(method);

            // Act
            var result = document.FindPrimaryMethod();

            // Assert
            Assert.Same(method, result);
        }

        [Fact]
        public void FindPrimaryNamespace_FindsNamespaceWithAnnotation()
        {
            // Arrange
            var document = new DocumentIntermediateNode();
            var @namespace = new NamespaceDeclarationIntermediateNode();
            @namespace.Annotations[CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace;

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Add(@namespace);

            // Act
            var result = document.FindPrimaryNamespace();

            // Assert
            Assert.Same(@namespace, result);
        }

        [Fact]
        public void FindDirectiveReferences_FindsMatchingDirectives()
        {
            // Arrange
            var directive = DirectiveDescriptor.CreateSingleLineDirective("test");
            var directive2 = DirectiveDescriptor.CreateSingleLineDirective("test");

            var document = new DocumentIntermediateNode();
            var @namespace = new NamespaceDeclarationIntermediateNode();

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Push(@namespace);

            var match1 = new DirectiveIntermediateNode()
            {
                Directive = directive,
            };
            builder.Add(match1);

            var nonMatch = new DirectiveIntermediateNode()
            {
                Directive = directive2,
            };
            builder.Add(nonMatch);

            var match2 = new DirectiveIntermediateNode()
            {
                Directive = directive,
            };
            builder.Add(match2);

            // Act
            var results = document.FindDirectiveReferences(directive);

            // Assert
            Assert.Collection(
                results,
                r =>
                {
                    Assert.Same(@namespace, r.Parent);
                    Assert.Same(match1, r.Node);
                },
                r =>
                {
                    Assert.Same(@namespace, r.Parent);
                    Assert.Same(match2, r.Node);
                });
        }
    }
}
