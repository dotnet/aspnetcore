// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class ExpressionRewriterTest
    {
        [Fact]
        public void ExpressionRewriter_DoesNotThrowsOnUnknownTypes()
        {
            // Arrange
            var source = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;

public class ExamplePage : RazorPage
{
    public IViewComponentHelper Component { get; set; }

    public override async Task ExecuteAsync()
    {
        Write(
            await Component.InvokeAsync(
                ""SomeComponent"",
                item => new HelperResult((__razor_template_writer) => WriteLiteralTo(__razor_template_writer, ""Hello World""))));
        }
    }
";
            var tree = CSharpSyntaxTree.ParseText(source);

            // Allow errors here because of an anomaly where Roslyn (depending on code sample) will finish compilation
            // without diagnostic errors. This test case replicates that scenario by allowing a semantic model with
            // errors to be visited by the expression rewriter to validate unexpected exceptions aren't thrown.
            // Error created: "Cannot convert lambda expression to type 'object' because it is not a delegate type."
            var compilation = Compile(tree, allowErrors: true);
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            var rewriter = new ExpressionRewriter(semanticModel);
            var root = tree.GetRoot();

            // Act
            var result = rewriter.Visit(root);

            // Assert
            Assert.True(root.IsEquivalentTo(result));
        }

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

        [Fact]
        public void ExpressionRewriter_CanRewriteExpression_BadlyIndentedFormatting()
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
        CalledWithExpression(x =>
                    x.Name.
          Length);
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

        private CSharpCompilation Compile(SyntaxTree tree, bool allowErrors = false)
        {
            // Disable 1702 until roslyn turns this off by default
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
                {
                    { "CS1701", ReportDiagnostic.Suppress }, // Binding redirects
                    { "CS1702", ReportDiagnostic.Suppress },
                    { "CS1705", ReportDiagnostic.Suppress }
                });

            var compilation = CSharpCompilation.Create(
                "Test.Assembly",
                new[] { tree },
                GetReferences(),
                options: options);

            if (!allowErrors)
            {
                var diagnostics = compilation.GetDiagnostics();
                Assert.True(diagnostics.Length == 0, string.Join(Environment.NewLine, diagnostics));
            }

            return compilation;
        }

        private IEnumerable<MetadataReference> GetReferences()
        {
            var types = new[]
            {
                typeof(System.Linq.Expressions.Expression),
                typeof(string),
            };

            return types.Select(t => MetadataReference.CreateFromFile(t.GetTypeInfo().Assembly.Location));
        }
    }
}
