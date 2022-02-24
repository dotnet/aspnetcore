// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class CachedExpressionCompilerTest
{
    [Fact]
    public void Process_IdentityExpression()
    {
        // Arrange
        var model = new TestModel();
        var expression = GetTestModelExpression(m => m);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Same(model, result);
    }

    [Fact]
    public void Process_CachesIdentityExpression()
    {
        // Arrange
        var expression1 = GetTestModelExpression(m => m);
        var expression2 = GetTestModelExpression(m => m);

        // Act
        var func1 = CachedExpressionCompiler.Process(expression1);
        var func2 = CachedExpressionCompiler.Process(expression2);

        // Assert
        Assert.NotNull(func1);
        Assert.Same(func1, func2);
    }

    [Fact]
    public void Process_ConstLookup()
    {
        // Arrange
        var model = new TestModel();
        var differentModel = new DifferentModel();
        var expression = GetTestModelExpression(m => differentModel);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Same(differentModel, result);
    }

    [Fact]
    public void Process_ConstLookup_ReturningNull()
    {
        // Arrange
        var model = new TestModel();
        var expression = GetTestModelExpression(m => (DifferentModel)null);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_ConstLookup_WithNullModel()
    {
        // Arrange
        var differentModel = new DifferentModel();
        var expression = GetTestModelExpression(m => differentModel);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(null);
        Assert.Same(differentModel, result);
    }

    [Fact]
    public void Process_ConstLookup_UsingCachedValue()
    {
        // Arrange
        var model = new TestModel();
        var differentModel = new DifferentModel();
        var expression1 = GetTestModelExpression(m => differentModel);
        var expression2 = GetTestModelExpression(m => differentModel);

        // Act - 1
        var func1 = CachedExpressionCompiler.Process(expression1);

        // Assert - 1
        var result1 = func1(null);
        Assert.Same(differentModel, result1);

        // Act - 2
        var func2 = CachedExpressionCompiler.Process(expression2);

        // Assert - 2
        var result2 = func1(null);
        Assert.Same(differentModel, result2);
    }

    [Fact]
    public void Process_ConstLookup_WhenCapturedLocalChanges()
    {
        // Arrange
        var model = new TestModel();
        var differentModel = new DifferentModel();
        var expression = GetTestModelExpression(m => differentModel);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert - 1
        var result1 = func(null);
        Assert.Same(differentModel, result1);

        // Act - 2
        differentModel = new DifferentModel();

        // Assert - 2
        var result2 = func(null);
        Assert.NotSame(differentModel, result1);
        Assert.Same(differentModel, result2);
    }

    [Fact]
    public void Process_ConstLookup_WithPrimitiveConstant()
    {
        // Arrange
        var model = new TestModel();
        var expression = GetTestModelExpression(m => 10);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(10, result);
    }

    [Fact]
    public void Process_StaticFieldAccess()
    {
        // Arrange
        var model = new TestModel();
        var expression = GetTestModelExpression(m => TestModel.StaticField);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal("StaticValue", result);
    }

    [Fact]
    public void Process_CachesStaticFieldAccess()
    {
        // Arrange
        var expression1 = GetTestModelExpression(m => TestModel.StaticField);
        var expression2 = GetTestModelExpression(m => TestModel.StaticField);

        // Act
        var func1 = CachedExpressionCompiler.Process(expression1);
        var func2 = CachedExpressionCompiler.Process(expression2);

        // Assert
        Assert.NotNull(func1);
        Assert.Same(func1, func2);
    }

    [Fact]
    public void Process_StaticPropertyAccess()
    {
        // Arrange
        var expected = "TestValue";
        TestModel.StaticProperty = expected;
        var model = new TestModel();
        var expression = GetTestModelExpression(m => TestModel.StaticProperty);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Process_CachesStaticPropertyAccess()
    {
        // Arrange
        var expression1 = GetTestModelExpression(m => TestModel.StaticProperty);
        var expression2 = GetTestModelExpression(m => TestModel.StaticProperty);

        // Act
        var func1 = CachedExpressionCompiler.Process(expression1);
        var func2 = CachedExpressionCompiler.Process(expression2);

        // Assert
        Assert.NotNull(func1);
        Assert.Same(func1, func2);
    }

    [Fact]
    public void Process_StaticPropertyAccess_WithNullModel()
    {
        // Arrange
        var expected = "TestValue";
        TestModel.StaticProperty = expected;
        var expression = GetTestModelExpression(m => TestModel.StaticProperty);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(null);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Process_ConstFieldLookup()
    {
        // Arrange
        var model = new TestModel();
        var expression = GetTestModelExpression(m => DifferentModel.Constant);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(10, result);
    }

    [Fact]
    public void Process_ConstFieldLookup_WthNullModel()
    {
        // Arrange
        var expression = GetTestModelExpression(m => DifferentModel.Constant);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(null);
        Assert.Equal(10, result);
    }

    [Fact]
    public void Process_SimpleMemberAccess()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var expression = GetTestModelExpression(m => m.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal("Test", result);
    }

    [Fact]
    public void Process_CachesSimpleMemberAccess()
    {
        // Arrange
        var expression1 = GetTestModelExpression(m => m.Name);
        var expression2 = GetTestModelExpression(m => m.Name);

        // Act
        var func1 = CachedExpressionCompiler.Process(expression1);
        var func2 = CachedExpressionCompiler.Process(expression2);

        // Assert
        Assert.NotNull(func1);
        Assert.Same(func1, func2);
    }

    [Fact]
    public void Process_SimpleMemberAccess_ToPrimitive()
    {
        // Arrange
        var model = new TestModel { Age = 12 };
        var expression = GetTestModelExpression(m => m.Age);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(12, result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_WithNullModel()
    {
        // Arrange
        var model = (TestModel)null;
        var expression = GetTestModelExpression(m => m.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_ToPrimitive_WithNullModel()
    {
        // Arrange
        var model = (TestModel)null;
        var expression = GetTestModelExpression(m => m.Age);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_OnTypeWithBadEqualityComparer()
    {
        // Arrange
        var model = new BadEqualityModel { Id = 7 };
        var expression = GetExpression<BadEqualityModel, int>(m => m.Id);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(7, result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_OnTypeWithBadEqualityComparer_WithNullModel()
    {
        // Arrange
        var model = (BadEqualityModel)null;
        var expression = GetExpression<BadEqualityModel, int>(m => m.Id);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_OnValueTypeWithBadEqualityComparer()
    {
        // Arrange
        var model = new BadEqualityValueTypeModel { Id = 7 };
        var expression = GetExpression<BadEqualityValueTypeModel, int>(m => m.Id);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(7, result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_OnTypeWithBadEqualityComparer_WithDefaultValue()
    {
        // Arrange
        var model = (BadEqualityValueTypeModel)default;
        var expression = GetExpression<BadEqualityValueTypeModel, int>(m => m.Id);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(model.Id, result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_OnValueType()
    {
        // Arrange
        var model = new DateTime(2000, 1, 1);
        var expression = GetExpression<DateTime, int>(m => m.Year);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(2000, result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_OnValueType_WithDefaultValue()
    {
        // Arrange
        var model = default(DateTime);
        var expression = GetExpression<DateTime, int>(m => m.Year);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_OnNullableValueType()
    {
        // Arrange
        var model = new DateTime(2000, 1, 1);
        var nullableModel = (DateTime?)model;
        var expression = GetExpression<DateTime?, DateTime>(m => m.Value);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(nullableModel);
        Assert.Equal(model, result);
    }

    [Fact]
    public void Process_SimpleMemberAccess_OnNullableValueType_WithNullValue()
    {
        // Arrange
        var nullableModel = (DateTime?)null;
        var expression = GetExpression<DateTime?, DateTime>(m => m.Value);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(nullableModel);
        Assert.Null(result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_ToValueType()
    {
        // Arrange
        var dateTime = new DateTime(2000, 1, 1);
        var model = new TestModel { Date = dateTime };
        var expression = GetTestModelExpression(m => m.Date.Year);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(2000, result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_ToValueType_WithNullModel()
    {
        // Arrange
        var model = (TestModel)null;
        var expression = GetTestModelExpression(m => m.Date.Year);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_ToReferenceType()
    {
        // Arrange
        var expected = "Test1";
        var model = new TestModel { DifferentModel = new DifferentModel { Name = expected } };
        var expression = GetTestModelExpression(m => m.DifferentModel.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Process_CachesChainedMemberAccess()
    {
        // Arrange
        var expression1 = GetTestModelExpression(m => m.DifferentModel.Name);
        var expression2 = GetTestModelExpression(m => m.DifferentModel.Name);

        // Act
        var func1 = CachedExpressionCompiler.Process(expression1);
        var func2 = CachedExpressionCompiler.Process(expression2);

        // Assert
        Assert.NotNull(func1);
        Assert.Same(func1, func2);
    }

    [Fact]
    public void Process_CachesChainedMemberAccess_ToValueType()
    {
        // Arrange
        var expression1 = GetTestModelExpression(m => m.Date.Year);
        var expression2 = GetTestModelExpression(m => m.Date.Year);

        // Act
        var func1 = CachedExpressionCompiler.Process(expression1);
        var func2 = CachedExpressionCompiler.Process(expression2);

        // Assert
        Assert.NotNull(func1);
        Assert.Same(func1, func2);
    }

    [Fact]
    public void Process_ChainedMemberAccess_LongChain_WithReferenceType()
    {
        // Arrange
        var expected = "TestVal";
        var model = new Chain0Model
        {
            Chain1 = new Chain1Model
            {
                ValueTypeModel = new ValueType1
                {
                    TestModel = new TestModel { DifferentModel = new DifferentModel { Name = expected } }
                }
            }
        };

        var expression = GetExpression<Chain0Model, string>(m => m.Chain1.ValueTypeModel.TestModel.DifferentModel.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_LongChain_WithNullIntermediary()
    {
        // Arrange
        var model = new Chain0Model
        {
            Chain1 = new Chain1Model
            {
                ValueTypeModel = new ValueType1 { TestModel = null },
            }
        };

        var expression = GetExpression<Chain0Model, string>(m => m.Chain1.ValueTypeModel.TestModel.DifferentModel.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_LongChain_WithNullValueTypeAccessor()
    {
        // Arrange
        // Chain2 is a value type
        var model = new Chain0Model
        {
            Chain1 = null
        };

        var expression = GetExpression<Chain0Model, string>(m => m.Chain1.ValueTypeModel.TestModel.DifferentModel.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_LongChain_WithNullableValueType()
    {
        // Arrange
        var expected = "TestVal";
        var model = new Chain0Model
        {
            Chain1 = new Chain1Model
            {
                NullableValueTypeModel = new ValueType1
                {
                    TestModel = new TestModel { DifferentModel = new DifferentModel { Name = expected } }
                }
            }
        };

        var expression = GetExpression<Chain0Model, string>(m => m.Chain1.NullableValueTypeModel.Value.TestModel.DifferentModel.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_LongChain_WithNullValuedNullableValueType()
    {
        // Arrange
        var model = new Chain0Model
        {
            Chain1 = new Chain1Model
            {
                NullableValueTypeModel = null
            }
        };

        var expression = GetExpression<Chain0Model, string>(m => m.Chain1.NullableValueTypeModel.Value.TestModel.DifferentModel.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_ToReferenceType_WithNullIntermediary()
    {
        // Arrange
        var model = new TestModel { DifferentModel = null };
        var expression = GetTestModelExpression(m => m.DifferentModel.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_ToReferenceType_WithNullModel()
    {
        // Arrange
        var model = (TestModel)null;
        var expression = GetTestModelExpression(m => m.DifferentModel.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_OfValueTypes_ReturningReferenceTypeMember()
    {
        // Arrange
        var expected = "TestName";
        var model = new ValueType1
        {
            ValueType2 = new ValueType2 { Name = expected },
        };
        var expression = GetExpression<ValueType1, string>(m => m.ValueType2.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_OfValueTypes_ReturningValueType()
    {
        // Arrange
        var expected = new DateTime(2001, 1, 1);
        var model = new ValueType1
        {
            ValueType2 = new ValueType2 { Date = expected },
        };
        var expression = GetExpression<ValueType1, DateTime>(m => m.ValueType2.Date);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_OfValueTypes_IncludingNullableType()
    {
        // Arrange
        var expected = "TestName";
        var model = new ValueType1
        {
            NullableValueType2 = new ValueType2 { Name = expected },
        };
        var expression = GetExpression<ValueType1, string>(m => m.NullableValueType2.Value.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_OfValueTypes_WithNullValuedNullable()
    {
        // Arrange
        var model = new ValueType1 { NullableValueType2 = null };
        var expression = GetExpression<ValueType1, string>(m => m.NullableValueType2.Value.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_ChainedMemberAccess_OfValueTypes_WithNullValuedNullable_ReturningValueType()
    {
        // Arrange
        var model = new ValueType1 { NullableValueType2 = null };
        var expression = GetExpression<ValueType1, DateTime>(m => m.NullableValueType2.Value.Date);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Null(result);
    }

    [Fact]
    public void Process_MemberAccessOnCapturedVariable_ReturnsNull()
    {
        // Arrange
        var differentModel = new DifferentModel { Name = "Test" };
        var expression = GetTestModelExpression(m => differentModel.Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.Null(func);
    }

    [Fact]
    public void Process_CapturedVariable()
    {
        // Arrange
        var differentModel = new DifferentModel();
        var model = new TestModel();
        var expression = GetTestModelExpression(m => differentModel);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Same(differentModel, result);
    }

    [Fact]
    public void Process_CapturedVariable_WithNullModel()
    {
        // Arrange
        var differentModel = new DifferentModel();
        var model = (TestModel)null;
        var expression = GetTestModelExpression(m => differentModel);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.NotNull(func);
        var result = func(model);
        Assert.Same(differentModel, result);
    }

    [Fact]
    public void Process_MemberAccess_OnCapturedVariable_ReturnsNull()
    {
        // Arrange
        var differentModel = "Hello world";
        var expression = GetTestModelExpression(m => differentModel.Length);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.Null(func);
    }

    [Fact]
    public void Process_ComplexChainedMemberAccess_ReturnsNull()
    {
        // Arrange
        var expected = "SomeName";
        var model = new TestModel { DifferentModels = new[] { new DifferentModel { Name = expected } } };
        var expression = GetTestModelExpression(m => m.DifferentModels[0].Name);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.Null(func);
    }

    [Fact]
    public void Process_ArrayMemberAccess_ReturnsNull()
    {
        // Arrange
        var expression = GetTestModelExpression(m => m.Sizes[1]);

        // Act
        var func = CachedExpressionCompiler.Process(expression);

        // Assert
        Assert.Null(func);
    }

    private static Expression<Func<TModel, TResult>> GetExpression<TModel, TResult>(Expression<Func<TModel, TResult>> expression)
        => expression;

    private static Expression<Func<TestModel, TResult>> GetTestModelExpression<TResult>(Expression<Func<TestModel, TResult>> expression)
        => GetExpression(expression);

    public class TestModel
    {
        public static readonly string StaticField = "StaticValue";

        public static string StaticProperty { get; set; }

        public int Age { get; set; }

        public string Name { get; set; }

        public DateTime Date { get; set; }

        public DifferentModel DifferentModel { get; set; }

        public int[] Sizes { get; set; }

        public DifferentModel[] DifferentModels { get; set; }
    }

    public class DifferentModel
    {
        public const int Constant = 10;

        public string Name { get; set; }
    }

    public class Chain0Model
    {
        public Chain1Model Chain1 { get; set; }
    }

    public class Chain1Model
    {
        public ValueType1 ValueTypeModel { get; set; }

        public ValueType1? NullableValueTypeModel { get; set; }
    }

    public struct ValueType1
    {
        public TestModel TestModel { get; set; }

        public ValueType2 ValueType2 { get; set; }

        public ValueType2? NullableValueType2 { get; set; }
    }

    public struct ValueType2
    {
        public string Name { get; set; }

        public DateTime Date { get; set; }
    }

    public class BadEqualityModel
    {
        public int Id { get; set; }

        public override bool Equals(object obj)
        {
            return this == obj;
        }

        public static bool operator ==(BadEqualityModel a, object b)
        {
            if (a is null || b is null)
            {
                throw new TimeZoneNotFoundException();
            }

            return true;
        }

        public static bool operator !=(BadEqualityModel a, object b)
        {
            return !(a == b);
        }

        public override int GetHashCode() => 0;
    }

    public struct BadEqualityValueTypeModel
    {
        public int Id { get; set; }

        public override bool Equals(object obj)
        {
            return this == obj;
        }

        public static bool operator ==(BadEqualityValueTypeModel a, object b)
        {
            if (b is null)
            {
                throw new TimeZoneNotFoundException();
            }

            return true;
        }

        public static bool operator !=(BadEqualityValueTypeModel a, object b)
        {
            return !(a == b);
        }

        public override int GetHashCode() => 0;
    }
}
