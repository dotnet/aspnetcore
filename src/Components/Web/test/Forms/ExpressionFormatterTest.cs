// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms;

public class ExpressionFormatterTest
{
    [Fact]
    public void Works_MemberAccessOnly()
    {
        // Arrange
        var formatter = new ExpressionFormatter();
        var person = new Person();
        LambdaExpression lambda = () => person.Parent.Name;

        // Act
        var result = formatter.FormatLambda(lambda);

        // Assert
        Assert.Equal("Parent.Name", result);
    }

    [Fact]
    public void Works_MemberAccessWithConstIndex()
    {
        // Arrange
        var formatter = new ExpressionFormatter();
        var person = new Person();
        LambdaExpression lambda = () => person.Parent.Children[3].Name;

        // Act
        var result = formatter.FormatLambda(lambda);

        // Assert
        Assert.Equal("Parent.Children[3].Name", result);
    }

    [Fact]
    public void Works_MemberAccessWithConstIndex_SameLambdaMultipleTimes()
    {
        // TODO: Somehow validate the caching mechanism.

        // Arrange
        var formatter = new ExpressionFormatter();
        var person = new Person();
        var result = new string[3];
        LambdaExpression lambda = () => person.Parent.Children[3].Name;

        // Act
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = formatter.FormatLambda(lambda);
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
        var formatter = new ExpressionFormatter();
        var person = new Person();
        var i = 42;
        LambdaExpression lambda = () => person.Parent.Children[i].Name;

        // Act
        var result = formatter.FormatLambda(lambda);

        // Assert
        Assert.Equal("Parent.Children[42].Name", result);
    }

    [Fact]
    public void Works_ForLoopIteraterVariableIndex_Short()
    {
        // Arrange
        var formatter = new ExpressionFormatter();
        var person = new Person();
        var i = 0;
        LambdaExpression lambda = () => person.Parent.Children[i].Name;
        var result = new string[3];

        // Act
        for (; i < result.Length; i++)
        {
            result[i] = formatter.FormatLambda(lambda);
        }

        // Assert
        Assert.Equal("Parent.Children[0].Name", result[0]);
        Assert.Equal("Parent.Children[1].Name", result[1]);
        Assert.Equal("Parent.Children[2].Name", result[2]);
    }

    [Fact]
    public void Works_ForLoopIteraterVariableIndex_Long()
    {
        // Arrange
        var formatter = new ExpressionFormatter();
        var person = new Person();
        var i = 0;
        LambdaExpression lambda = () => person.Parent.Parent.Children[i].Parent.Children[i].Children[i].Name;
        var result = new string[3];

        // Act
        for (; i < result.Length; i++)
        {
            result[i] = formatter.FormatLambda(lambda);
        }

        // Assert
        Assert.Equal("Parent.Parent.Children[0].Parent.Children[0].Children[0].Name", result[0]);
        Assert.Equal("Parent.Parent.Children[1].Parent.Children[1].Children[1].Name", result[1]);
        Assert.Equal("Parent.Parent.Children[2].Parent.Children[2].Children[2].Name", result[2]);
    }

    [Fact]
    public void Works_ForLoopIteraterVariableIndex_SuperLong()
    {
        // Arrange
        var formatter = new ExpressionFormatter();
        var person = new Person();
        var i = 0;
        LambdaExpression lambda = () => person.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Children[i].Age;
        var result = new string[3];

        // Act
        for (; i < result.Length; i++)
        {
            result[i] = formatter.FormatLambda(lambda);
        }

        // Assert
        Assert.Equal("Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Children[0].Age", result[0]);
        Assert.Equal("Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Children[1].Age", result[1]);
        Assert.Equal("Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Children[2].Age", result[2]);
    }

    [Fact]
    public void Works_ForLoopIteraterVariableIndex_NonArrayType()
    {
        // Arrange
        var formatter = new ExpressionFormatter();
        var person = new Person();
        var i = 0;
        LambdaExpression lambda = () => person.Parent.Nicknames[i];
        var result = new string[3];

        // Act
        for (; i < result.Length; i++)
        {
            result[i] = formatter.FormatLambda(lambda);
        }

        // Assert
        Assert.Equal("Parent.Nicknames[0]", result[0]);
        Assert.Equal("Parent.Nicknames[1]", result[1]);
        Assert.Equal("Parent.Nicknames[2]", result[2]);
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
