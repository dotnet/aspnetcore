// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class ExpressionRewriterTest
    {
        [Fact]
        public void ExpressionRewriter_CanRewriteExpression_IdentityExpression()
        {
            // Arrange
            var source = @"
using System;
using System.Linq.Expressions;
public class Program
{
    public static void CalledWithExpression(Expression<Func<object, object>> expression)
    {
    }

    public static void Main(string[] args)
    {
        CalledWithExpression(x => x);
    }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = Compile(tree);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var rewriter = new ExpressionRewriter(semanticModel);

            // Act
            var result = rewriter.Visit(tree.GetRoot());

            // Assert
            var fields = FindFields(result);

            var field = Assert.Single(fields);
            Assert.Collection(
                field.Modifiers,
                m => Assert.Equal("private", m.ToString()),
                m => Assert.Equal("static", m.ToString()),
                m => Assert.Equal("readonly", m.ToString()));

            var declaration = field.Declaration;
            Assert.Equal(
                "global::System.Linq.Expressions.Expression<global::System.Func<object, object>>",
                declaration.Type.ToString());

            var variable = Assert.Single(declaration.Variables);
            Assert.Equal("__h0", variable.Identifier.ToString());
            Assert.Equal("x => x", variable.Initializer.Value.ToString());

            var arguments = FindArguments(result);
            var argument = Assert.IsType<IdentifierNameSyntax>(Assert.Single(arguments.Arguments).Expression);
            Assert.Equal("__h0", argument.Identifier.ToString());
        }

        [Fact]
        public void ExpressionRewriter_CanRewriteExpression_MemberAccessExpression()
        {
            // Arrange
            var source = @"
using System;
using System.Linq.Expressions;
public class Program
{
    public static void CalledWithExpression(Expression<Func<Person, object>> expression)
    {
    }

    public static void Main(string[] args)
    {
        CalledWithExpression(x => x.Name);
    }
}

public class Person
{
    public string Name { get; set; }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = Compile(tree);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var rewriter = new ExpressionRewriter(semanticModel);

            // Act
            var result = rewriter.Visit(tree.GetRoot());

            // Assert
            var fields = FindFields(result);

            var field = Assert.Single(fields);
            Assert.Collection(
                field.Modifiers,
                m => Assert.Equal("private", m.ToString()),
                m => Assert.Equal("static", m.ToString()),
                m => Assert.Equal("readonly", m.ToString()));

            var declaration = field.Declaration;
            Assert.Equal(
                "global::System.Linq.Expressions.Expression<global::System.Func<global::Person, object>>",
                declaration.Type.ToString());

            var variable = Assert.Single(declaration.Variables);
            Assert.Equal("__h0", variable.Identifier.ToString());
            Assert.Equal("x => x.Name", variable.Initializer.Value.ToString());

            var arguments = FindArguments(result);
            var argument = Assert.IsType<IdentifierNameSyntax>(Assert.Single(arguments.Arguments).Expression);
            Assert.Equal("__h0", argument.Identifier.ToString());
        }

        [Fact]
        public void ExpressionRewriter_CanRewriteExpression_ChainedMemberAccessExpression()
        {
            // Arrange
            var source = @"
using System;
using System.Linq.Expressions;
public class Program
{
    public static void CalledWithExpression(Expression<Func<Person, int>> expression)
    {
    }

    public static void Main(string[] args)
    {
        CalledWithExpression(x => x.Name.Length);
    }
}

public class Person
{
    public string Name { get; set; }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = Compile(tree);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var rewriter = new ExpressionRewriter(semanticModel);

            // Act
            var result = rewriter.Visit(tree.GetRoot());

            // Assert
            var fields = FindFields(result);

            var field = Assert.Single(fields);
            Assert.Collection(
                field.Modifiers,
                m => Assert.Equal("private", m.ToString()),
                m => Assert.Equal("static", m.ToString()),
                m => Assert.Equal("readonly", m.ToString()));

            var declaration = field.Declaration;
            Assert.Equal(
                "global::System.Linq.Expressions.Expression<global::System.Func<global::Person, int>>",
                declaration.Type.ToString());

            var variable = Assert.Single(declaration.Variables);
            Assert.Equal("__h0", variable.Identifier.ToString());
            Assert.Equal("x => x.Name.Length", variable.Initializer.Value.ToString());

            var arguments = FindArguments(result);
            var argument = Assert.IsType<IdentifierNameSyntax>(Assert.Single(arguments.Arguments).Expression);
            Assert.Equal("__h0", argument.Identifier.ToString());
        }

        [Fact]
        public void ExpressionRewriter_CannotRewriteExpression_MethodCall()
        {
            // Arrange
            var source = @"
using System;
using System.Linq.Expressions;
public class Program
{
    public static void CalledWithExpression(Expression<Func<object, int>> expression)
    {
    }

    public static void Main(string[] args)
    {
        CalledWithExpression(x => x.GetHashCode());
    }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = Compile(tree);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var rewriter = new ExpressionRewriter(semanticModel);

            // Act
            var result = rewriter.Visit(tree.GetRoot());

            // Assert
            Assert.Empty(FindFields(result));
        }

        [Fact]
        public void ExpressionRewriter_CannotRewriteExpression_NonArgument()
        {
            // Arrange
            var source = @"
using System;
using System.Linq.Expressions;
public class Program
{
    public static void CalledWithExpression(Expression<Func<object, int>> expression)
    {
    }

    public static void Main(string[] args)
    {
        Expression<Func<object, int>> expr = x => x.GetHashCode();
        CalledWithExpression(expr);
    }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = Compile(tree);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var rewriter = new ExpressionRewriter(semanticModel);

            // Act
            var result = rewriter.Visit(tree.GetRoot());

            // Assert
            Assert.Empty(FindFields(result));
        }

        [Fact]
        public void ExpressionRewriter_CannotRewriteExpression_NestedClass()
        {
            // Arrange
            var source = @"
using System;
using System.Linq.Expressions;
public class Program
{
    private class Nested
    {
        public static void CalledWithExpression(Expression<Func<object, int>> expression)
        {
        }
        
        public static void Main(string[] args)
        {
            Expression<Func<object, int>> expr = x => x.GetHashCode();
            CalledWithExpression(expr);
        }
    }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = Compile(tree);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var rewriter = new ExpressionRewriter(semanticModel);

            // Act
            var result = rewriter.Visit(tree.GetRoot());

            // Assert
            Assert.Empty(FindFields(result));
        }

        [Fact]
        public void ExpressionRewriter_CanRewriteExpression_AdditionalArguments()
        {
            // Arrange
            var source = @"
using System;
using System.Linq.Expressions;
public class Program
{
    public static void CalledWithExpression(int x, Expression<Func<object, object>> expression, string name)
    {
    }

    public static void Main(string[] args)
    {
        CalledWithExpression(5, x => x, ""Billy"");
    }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = Compile(tree);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var rewriter = new ExpressionRewriter(semanticModel);

            // Act
            var result = rewriter.Visit(tree.GetRoot());

            // Assert
            var fields = FindFields(result);

            var field = Assert.Single(fields);
            Assert.Collection(
                field.Modifiers,
                m => Assert.Equal("private", m.ToString()),
                m => Assert.Equal("static", m.ToString()),
                m => Assert.Equal("readonly", m.ToString()));

            var declaration = field.Declaration;
            Assert.Equal(
                "global::System.Linq.Expressions.Expression<global::System.Func<object, object>>",
                declaration.Type.ToString());

            var variable = Assert.Single(declaration.Variables);
            Assert.Equal("__h0", variable.Identifier.ToString());
            Assert.Equal("x => x", variable.Initializer.Value.ToString());

            var arguments = FindArguments(result);
            Assert.Equal(3, arguments.Arguments.Count);
            var argument = Assert.IsType<IdentifierNameSyntax>(arguments.Arguments[1].Expression);
            Assert.Equal("__h0", argument.Identifier.ToString());
        }

        // When we rewrite the expression, we want to maintain the original span as much as possible.
        [Fact]
        public void ExpressionRewriter_CanRewriteExpression_SimpleFormatting()
        {
            // Arrange
            var source = @"
using System;
using System.Linq.Expressions;
public class Program
{
    public static void CalledWithExpression(Expression<Func<Person, int>> expression)
    {
    }

    public static void Main(string[] args)
    {
        CalledWithExpression(x => x.Name.Length);
    }
}

public class Person
{
    public string Name { get; set; }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);

            var originalArguments = FindArguments(tree.GetRoot());
            var originalSpan = originalArguments.GetLocation().GetMappedLineSpan();

            var compilation = Compile(tree);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var rewriter = new ExpressionRewriter(semanticModel);

            // Act
            var result = rewriter.Visit(tree.GetRoot());

            // Assert
            var arguments = FindArguments(result);
            Assert.Equal(originalSpan, arguments.GetLocation().GetMappedLineSpan());
        }

        // When we rewrite the expression, we want to maintain the original span as much as possible.
        [Fact]
        public void ExpressionRewriter_CanRewriteExpression_ComplexFormatting()
        {
            // Arrange
            var source = @"
using System;
using System.Linq.Expressions;
public class Program
{
    public static void CalledWithExpression(int z, Expression<Func<Person, int>> expression)
    {
    }

    public static void Main(string[] args)
    {
        CalledWithExpression(
            17,
            x =>
                    x.Name.
            Length
        );
    }
}

public class Person
{
    public string Name { get; set; }
}
";

            var tree = CSharpSyntaxTree.ParseText(source);

            var originalArguments = FindArguments(tree.GetRoot());
            var originalSpan = originalArguments.GetLocation().GetMappedLineSpan();

            var compilation = Compile(tree);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var rewriter = new ExpressionRewriter(semanticModel);

            // Act
            var result = rewriter.Visit(tree.GetRoot());

            // Assert
            var arguments = FindArguments(result);
            Assert.Equal(originalSpan, arguments.GetLocation().GetMappedLineSpan());
        }

        public ArgumentListSyntax FindArguments(SyntaxNode node)
        {
            return node
                .DescendantNodes(n => true)
                .Where(n => n.IsKind(SyntaxKind.ArgumentList))
                .Cast<ArgumentListSyntax>()
                .Single();
        }

        public IEnumerable<FieldDeclarationSyntax> FindFields(SyntaxNode node)
        {
            return node
                .DescendantNodes(n => true)
                .Where(n => n.IsKind(SyntaxKind.FieldDeclaration))
                .Cast<FieldDeclarationSyntax>();
        }

        private CSharpCompilation Compile(SyntaxTree tree)
        {
            var compilation = CSharpCompilation.Create(
                "Test.Assembly",
                new[] { tree },
                GetReferences());

            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Length > 0)
            {
                Assert.False(true, string.Join(Environment.NewLine, diagnostics));
            }

            return compilation;
        }

        private IEnumerable<MetadataReference> GetReferences()
        {
            var libraryExporter = CompilationServices.Default.LibraryExporter;
            var environment = PlatformServices.Default.Application;

            var references = new List<MetadataReference>();

            var libraryExports = libraryExporter.GetAllExports(environment.ApplicationName);
            foreach (var export in libraryExports.MetadataReferences)
            {
                references.Add(export.ConvertMetadataReference(MetadataReferenceExtensions.CreateAssemblyMetadata));
            }

            return references;
        }
    }
}
