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
            var method = new RazorMethodDeclarationIRNode();
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
    }
}
