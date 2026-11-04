// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms;
public class HtmlFieldPrefixTest
{
    [Fact]
    public void CanCombineTwoExpressions()
    {
        // Arrange
        Person person = null!;
        Address value = null!;
        LambdaExpression parent = () => person.Children[0].BillingAddress;
        LambdaExpression valueExpression = () => value.City;

        // Act
        var prefix = new HtmlFieldPrefix(parent);

        // Assert
        Assert.Equal("person.Children[0].BillingAddress.City", prefix.GetFieldName(valueExpression));
    }

    [Fact]
    public void CanCombineMultipleExpressions()
    {
        // Arrange
        Person person = null!;
        Address value = null!;
        Person value2 = null!;
        LambdaExpression parent = () => person.Children[0];
        LambdaExpression child = () => value2.Children[1].BillingAddress;
        LambdaExpression valueExpression = () => value.City;

        // Act
        var prefix = new HtmlFieldPrefix(parent).Combine(child);

        // Assert
        Assert.Equal("person.Children[0].Children[1].BillingAddress.City", prefix.GetFieldName(valueExpression));
    }

    [Fact]
    public void CanCombineMultipleExpressionsPropertyAccess()
    {
        // Arrange
        Person person = null!;
        Address value = null!;
        LambdaExpression parent = () => person.BillingAddress;
        LambdaExpression valueExpression = () => value.City;

        // Act
        var prefix = new HtmlFieldPrefix(parent);

        // Assert
        Assert.Equal("person.BillingAddress.City", prefix.GetFieldName(valueExpression));
    }

    [Fact]
    public void CanCombineTwoExpressionsIndexers()
    {
        // Arrange
        Person person = null!;
        IReadOnlyList<string> value = null!;
        LambdaExpression parent = () => person.Nicknames;
        LambdaExpression valueExpression = () => value[0];

        // Act
        var prefix = new HtmlFieldPrefix(parent);

        // Assert
        Assert.Equal("person.Nicknames[0]", prefix.GetFieldName(valueExpression));
    }

    [Fact]
    public void CanCombineMultipleExpressionsIndexers()
    {
        // Arrange
        Person person = null!;
        Person[] children = null!;
        IReadOnlyList<string> value = null!;
        LambdaExpression parent = () => person.Children;
        LambdaExpression childrenExpression = () => children[0].Nicknames;
        LambdaExpression valueExpression = () => value[1];

        // Act
        var prefix = new HtmlFieldPrefix(parent).Combine(childrenExpression);

        // Assert
        Assert.Equal("person.Children[0].Nicknames[1]", prefix.GetFieldName(valueExpression));
    }

    private class Person
    {
        public string Name { get; init; }

        public int Age { get; init; }

        public Address BillingAddress { get; set; }

        public Person Parent { get; init; }

        public Person[] Children { get; init; } = Array.Empty<Person>();

        public IReadOnlyList<string> Nicknames { get; init; } = Array.Empty<string>();
    }

    private class Address
    {
        public string Street { get; init; }

        public string City { get; init; }

        public string State { get; init; }

        public string ZipCode { get; init; }
    }
}
