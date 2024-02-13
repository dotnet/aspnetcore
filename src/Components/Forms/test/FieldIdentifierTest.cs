// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms;

public class FieldIdentifierTest
{
    [Fact]
    public void CannotUseNullModel()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FieldIdentifier(null, "somefield"));
        Assert.Equal("model", ex.ParamName);
    }

    [Fact]
    public void CannotUseValueTypeModel()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FieldIdentifier(DateTime.Now, "somefield"));
        Assert.Equal("model", ex.ParamName);
        Assert.StartsWith("The model must be a reference-typed object.", ex.Message);
    }

    [Fact]
    public void CannotUseNullFieldName()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FieldIdentifier(new object(), null));
        Assert.Equal("fieldName", ex.ParamName);
    }

    [Fact]
    public void CanUseEmptyFieldName()
    {
        var fieldIdentifier = new FieldIdentifier(new object(), string.Empty);
        Assert.Equal(string.Empty, fieldIdentifier.FieldName);
    }

    [Fact]
    public void CanGetModelAndFieldName()
    {
        // Arrange/Act
        var model = new object();
        var fieldIdentifier = new FieldIdentifier(model, "someField");

        // Assert
        Assert.Same(model, fieldIdentifier.Model);
        Assert.Equal("someField", fieldIdentifier.FieldName);
    }

    [Fact]
    public void DistinctModelsProduceDistinctHashCodesAndNonEquality()
    {
        // Arrange
        var fieldIdentifier1 = new FieldIdentifier(new object(), "field");
        var fieldIdentifier2 = new FieldIdentifier(new object(), "field");

        // Act/Assert
        Assert.NotEqual(fieldIdentifier1.GetHashCode(), fieldIdentifier2.GetHashCode());
        Assert.False(fieldIdentifier1.Equals(fieldIdentifier2));
    }

    [Fact]
    public void DistinctFieldNamesProduceDistinctHashCodesAndNonEquality()
    {
        // Arrange
        var model = new object();
        var fieldIdentifier1 = new FieldIdentifier(model, "field1");
        var fieldIdentifier2 = new FieldIdentifier(model, "field2");

        // Act/Assert
        Assert.NotEqual(fieldIdentifier1.GetHashCode(), fieldIdentifier2.GetHashCode());
        Assert.False(fieldIdentifier1.Equals(fieldIdentifier2));
    }

    [Fact]
    public void FieldIdentifier_ForModelWithoutField_ProduceSameHashCodesAndEquality()
    {
        // Arrange
        var model = new object();
        var fieldIdentifier1 = new FieldIdentifier(model, fieldName: string.Empty);
        var fieldIdentifier2 = new FieldIdentifier(model, fieldName: string.Empty);

        // Act/Assert
        Assert.Equal(fieldIdentifier1.GetHashCode(), fieldIdentifier2.GetHashCode());
        Assert.True(fieldIdentifier1.Equals(fieldIdentifier2));
    }

    [Fact]
    public void SameContentsProduceSameHashCodesAndEquality()
    {
        // Arrange
        var model = new object();
        var fieldIdentifier1 = new FieldIdentifier(model, "field");
        var fieldIdentifier2 = new FieldIdentifier(model, "field");

        // Act/Assert
        Assert.Equal(fieldIdentifier1.GetHashCode(), fieldIdentifier2.GetHashCode());
        Assert.True(fieldIdentifier1.Equals(fieldIdentifier2));
    }

    [Fact]
    public void SameContents_WithOverridenEqualsAndGetHashCode_ProduceSameHashCodesAndEquality()
    {
        // Arrange
        var model = new EquatableModel();
        var fieldIdentifier1 = new FieldIdentifier(model, nameof(EquatableModel.Property));
        model.Property = "changed value"; // To show it makes no difference if the overridden `GetHashCode` result changes
        var fieldIdentifier2 = new FieldIdentifier(model, nameof(EquatableModel.Property));

        // Act/Assert
        Assert.Equal(fieldIdentifier1.GetHashCode(), fieldIdentifier2.GetHashCode());
        Assert.True(fieldIdentifier1.Equals(fieldIdentifier2));
    }

    [Fact]
    public void FieldNamesAreCaseSensitive()
    {
        // Arrange
        var model = new object();
        var fieldIdentifierLower = new FieldIdentifier(model, "field");
        var fieldIdentifierPascal = new FieldIdentifier(model, "Field");

        // Act/Assert
        Assert.Equal("field", fieldIdentifierLower.FieldName);
        Assert.Equal("Field", fieldIdentifierPascal.FieldName);
        Assert.NotEqual(fieldIdentifierLower.GetHashCode(), fieldIdentifierPascal.GetHashCode());
        Assert.False(fieldIdentifierLower.Equals(fieldIdentifierPascal));
    }

    [Fact]
    public void CanCreateFromExpression_Property()
    {
        var model = new TestModel();
        var fieldIdentifier = FieldIdentifier.Create(() => model.StringProperty);
        Assert.Same(model, fieldIdentifier.Model);
        Assert.Equal(nameof(model.StringProperty), fieldIdentifier.FieldName);
    }

    [Fact]
    public void CanCreateFromExpression_PropertyUsesCache()
    {
        var models = new TestModel[] { new TestModel(), new TestModel() };
        var cache = new ConcurrentDictionary<(Type ModelType, MemberInfo FieldName), Func<object, object>>();
        var result = new TestModel[2];
        for (var i = 0; i < models.Length; i++)
        {
            var model = models[i];
            LambdaExpression expression = () => model.StringProperty;
            var body = expression.Body as MemberExpression;
            var value = FieldIdentifier.GetModelFromMemberAccess((MemberExpression)body.Expression, cache);
            result[i] = Assert.IsType<TestModel>(value);
        }

        Assert.Single(cache);
        Assert.Equal(models, result);
    }

    [Fact]
    public void CannotCreateFromExpression_NonMember()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FieldIdentifier.Create(() => new TestModel()));
        Assert.Equal($"The provided expression contains a NewExpression which is not supported. {nameof(FieldIdentifier)} only supports simple member accessors (fields, properties) of an object.", ex.Message);
    }

    [Fact]
    public void CanCreateFromExpression_Field()
    {
        var model = new TestModel();
        var fieldIdentifier = FieldIdentifier.Create(() => model.StringField);
        Assert.Same(model, fieldIdentifier.Model);
        Assert.Equal(nameof(model.StringField), fieldIdentifier.FieldName);
    }

    [Fact]
    public void CanCreateFromExpression_WithCastToObject()
    {
        // This case is needed because, if a component is declared as receiving
        // an Expression<Func<object>>, then any value types will be implicitly cast
        var model = new TestModel();
        Expression<Func<object>> accessor = () => model.IntProperty;
        var fieldIdentifier = FieldIdentifier.Create(accessor);
        Assert.Same(model, fieldIdentifier.Model);
        Assert.Equal(nameof(model.IntProperty), fieldIdentifier.FieldName);
    }

    [Fact]
    public void CanCreateFromExpression_MemberOfConstantExpression()
    {
        var fieldIdentifier = FieldIdentifier.Create(() => StringPropertyOnThisClass);
        Assert.Same(this, fieldIdentifier.Model);
        Assert.Equal(nameof(StringPropertyOnThisClass), fieldIdentifier.FieldName);
    }

    [Fact]
    public void CanCreateFromExpression_MemberOfChildObject()
    {
        var parentModel = new ParentModel { Child = new TestModel() };
        var fieldIdentifier = FieldIdentifier.Create(() => parentModel.Child.StringField);
        Assert.Same(parentModel.Child, fieldIdentifier.Model);
        Assert.Equal(nameof(TestModel.StringField), fieldIdentifier.FieldName);
    }

    [Fact]
    public void CanCreateFromExpression_MemberOfIndexedCollectionEntry()
    {
        var models = new List<TestModel>() { null, new TestModel() };
        var fieldIdentifier = FieldIdentifier.Create(() => models[1].StringField);
        Assert.Same(models[1], fieldIdentifier.Model);
        Assert.Equal(nameof(TestModel.StringField), fieldIdentifier.FieldName);
    }

    [Fact]
    public void CanCreateFromExpression_MemberOfObjectWithCast()
    {
        var model = new TestModel();
        var fieldIdentifier = FieldIdentifier.Create(() => ((TestModel)(object)model).StringField);
        Assert.Same(model, fieldIdentifier.Model);
        Assert.Equal(nameof(TestModel.StringField), fieldIdentifier.FieldName);
    }

    [Fact]
    public void CanCreateFromExpression_DifferentCaseField()
    {
        var fieldIdentifier = FieldIdentifier.Create(() => model.Field);
        Assert.Same(model, fieldIdentifier.Model);
        Assert.Equal(nameof(model.Field), fieldIdentifier.FieldName);
    }

    private DifferentCaseFieldModel model = new() { Field = 1 };
#pragma warning disable CA1823 // This is used in the test above
    private DifferentCaseFieldModel Model = new() { field = 2 };
#pragma warning restore CA1823 // Avoid unused private fields

    [Fact]
    public void CanCreateFromExpression_DifferentCaseProperty()
    {
        var fieldIdentifier = FieldIdentifier.Create(() => Model2.Property);
        Assert.Same(Model2, fieldIdentifier.Model);
        Assert.Equal(nameof(Model2.Property), fieldIdentifier.FieldName);
    }

    protected DifferentCasePropertyModel Model2 { get; } = new() { property = 1 };

    protected DifferentCasePropertyModel model2 { get; } = new() { Property = 2 };

    [Fact]
    public void CanCreateFromExpression_DifferentCasePropertyAndField()
    {
        var fieldIdentifier = FieldIdentifier.Create(() => model3.Value);
        Assert.Same(model3, fieldIdentifier.Model);
        Assert.Equal(nameof(Model3.Value), fieldIdentifier.FieldName);
    }

    [Fact]
    public void CanCreateFromExpression_NonAsciiCharacters()
    {
        var fieldIdentifier = FieldIdentifier.Create(() => @ÖvrigAnställning.Ort);
        Assert.Same(@ÖvrigAnställning, fieldIdentifier.Model);
        Assert.Equal(nameof(@ÖvrigAnställning.Ort), fieldIdentifier.FieldName);
    }

    public DifferentCasePropertyFieldModel Model3 { get; } = new() { value = 1 };

    public DifferentCasePropertyFieldModel model3 = new() { Value = 2 };

    public ÖvrigAnställningModel @ÖvrigAnställning { get; set; } = new();

    string StringPropertyOnThisClass { get; set; }

    class TestModel
    {
        public string StringProperty { get; set; }

        public int IntProperty { get; set; }

#pragma warning disable 649
        public string StringField;
#pragma warning restore 649
    }

    class ParentModel
    {
        public TestModel Child { get; set; }
    }

    class EquatableModel : IEquatable<EquatableModel>
    {
        public string Property { get; set; } = "";

        public bool Equals(EquatableModel other)
        {
            return string.Equals(Property, other?.Property, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Property);
        }
    }

    public class ÖvrigAnställningModel
    {
        public int Ort { get; set; }
    }

    private class DifferentCaseFieldModel
    {
        public int Field;
        public int field;
    }

    protected class DifferentCasePropertyModel
    {
        public int Property { get; set; }
        public int property { get; set; }
    }

    public class DifferentCasePropertyFieldModel
    {
        public int Value { get; set; }
        public int value;
    }
}
