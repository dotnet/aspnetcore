// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public sealed class ExpressionFormatterTest : IDisposable
{
    [Fact]
    public void Works_MemberAccessOnly()
    {
        // Arrange
        var person = new Person();

        // Act
        var result = ExpressionFormatter.FormatLambda(() => person.Parent.Name);

        // Assert
        Assert.Equal("Parent.Name", result);
    }

    [Fact]
    public void Works_MemberAccessWithConstIndex()
    {
        // Arrange
        var person = new Person();

        // Act
        var result = ExpressionFormatter.FormatLambda(() => person.Parent.Children[3].Name);

        // Assert
        Assert.Equal("Parent.Children[3].Name", result);
    }

    [Fact]
    public void Works_MemberAccessWithConstIndex_SameLambdaMultipleTimes()
    {
        // Arrange
        var person = new Person();
        var result = new string[3];

        // Act
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = ExpressionFormatter.FormatLambda(() => person.Parent.Children[3].Name);
        }

        // Assert
        Assert.Equal("Parent.Children[3].Name", result[0]);
        Assert.Equal("Parent.Children[3].Name", result[1]);
        Assert.Equal("Parent.Children[3].Name", result[2]);
    }

    [Fact]
    public void Works_MemberAccessWithVariableIndex()
    {
        // Arrange
        var person = new Person();
        var i = 42;

        // Act
        var result = ExpressionFormatter.FormatLambda(() => person.Parent.Children[i].Name);

        // Assert
        Assert.Equal("Parent.Children[42].Name", result);
    }

    [Fact]
    public void Works_ForLoopIteratorVariableIndex_Short()
    {
        // Arrange
        var person = new Person();
        var i = 0;
        var result = new string[3];

        // Act
        for (; i < result.Length; i++)
        {
            result[i] = ExpressionFormatter.FormatLambda(() => person.Parent.Children[i].Name);
        }

        // Assert
        Assert.Equal("Parent.Children[0].Name", result[0]);
        Assert.Equal("Parent.Children[1].Name", result[1]);
        Assert.Equal("Parent.Children[2].Name", result[2]);
    }

    [Fact]
    public void Works_ForLoopIteratorVariableIndex_MultipleClosures()
    {
        // Arrange
        var person = new Person();

        // Act
        var result1 = ComputeResult();
        var result2 = ComputeResult();

        // Assert
        Assert.Equal("Parent.Children[0].Name", result1[0]);
        Assert.Equal("Parent.Children[1].Name", result1[1]);
        Assert.Equal("Parent.Children[2].Name", result1[2]);

        Assert.Equal("Parent.Children[0].Name", result2[0]);
        Assert.Equal("Parent.Children[1].Name", result2[1]);
        Assert.Equal("Parent.Children[2].Name", result2[2]);

        string[] ComputeResult()
        {
            var result = new string[3];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = ExpressionFormatter.FormatLambda(() => person.Parent.Children[i].Name);
            }

            return result;
        }
    }

    [Fact]
    public void Works_ForLoopIteratorVariableIndex_Long()
    {
        // Arrange
        var person = new Person();
        var i = 0;
        var result = new string[3];

        // Act
        for (; i < result.Length; i++)
        {
            result[i] = ExpressionFormatter.FormatLambda(() => person.Parent.Parent.Children[i].Parent.Children[i].Children[i].Name);
        }

        // Assert
        Assert.Equal("Parent.Parent.Children[0].Parent.Children[0].Children[0].Name", result[0]);
        Assert.Equal("Parent.Parent.Children[1].Parent.Children[1].Children[1].Name", result[1]);
        Assert.Equal("Parent.Parent.Children[2].Parent.Children[2].Children[2].Name", result[2]);
    }

    [Fact]
    public void Works_ForLoopIteratorVariableIndex_NonArrayType()
    {
        // Arrange
        var person = new Person();
        var i = 0;
        var result = new string[3];

        // Act
        for (; i < result.Length; i++)
        {
            result[i] = ExpressionFormatter.FormatLambda(() => person.Parent.Nicknames[i]);
        }

        // Assert
        Assert.Equal("Parent.Nicknames[0]", result[0]);
        Assert.Equal("Parent.Nicknames[1]", result[1]);
        Assert.Equal("Parent.Nicknames[2]", result[2]);
    }

    public void Dispose()
    {
        ExpressionFormatter.ClearCache();
    }

    private class Person
    {
        public string Name { get; init; }

        public int Age { get; init; }

        public Person Parent { get; init; }

        public Person[] Children { get; init; } = Array.Empty<Person>();

        public IReadOnlyList<string> Nicknames { get; init; } = Array.Empty<string>();
    }
}
