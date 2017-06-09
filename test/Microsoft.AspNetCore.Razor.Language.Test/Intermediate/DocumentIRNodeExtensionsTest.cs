// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public class DocumentIRNodeExtensionsTest
    {
        [Fact]
        public void FindPrimaryClass_FindsClassWithAnnotation()
        {
            // Arrange
            var document = new DocumentIRNode();
            var @class = new ClassDeclarationIRNode();
            @class.Annotations[CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass;

            var builder = RazorIRBuilder.Create(document);
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
            var document = new DocumentIRNode();
            var method = new MethodDeclarationIRNode();
            method.Annotations[CommonAnnotations.PrimaryMethod] = CommonAnnotations.PrimaryMethod;

            var builder = RazorIRBuilder.Create(document);
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
            var document = new DocumentIRNode();
            var @namespace = new NamespaceDeclarationIRNode();
            @namespace.Annotations[CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace;

            var builder = RazorIRBuilder.Create(document);
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

            var document = new DocumentIRNode();
            var @namespace = new NamespaceDeclarationIRNode();

            var builder = RazorIRBuilder.Create(document);
            builder.Push(@namespace);

            var match1 = new DirectiveIRNode()
            {
                Descriptor = directive,
            };
            builder.Add(match1);

            var nonMatch = new DirectiveIRNode()
            {
                Descriptor = directive2,
            };
            builder.Add(nonMatch);

            var match2 = new DirectiveIRNode()
            {
                Descriptor = directive,
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
