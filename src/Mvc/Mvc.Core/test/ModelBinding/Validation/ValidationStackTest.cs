// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class ValidationStackTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(ValidationStack.CutOff + 1)]
    public void Push_ReturnsFalseIfValueAlreadyExists(int preloadCount)
    {
        // Arrange
        var validationStack = new TestValidationStack();
        var model = "This is a value";

        PreLoad(preloadCount, validationStack);

        // Act & Assert
        Assert.True(validationStack.Push(model));
        Assert.False(validationStack.Push(model));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(ValidationStack.CutOff + 1)]
    public void Pop_RemovesValueFromTheStack(int preloadCount)
    {
        // Arrange
        var validationStack = new TestValidationStack();
        var model = "This is a value";

        PreLoad(preloadCount, validationStack);

        // Act
        validationStack.Push(model);
        validationStack.Pop(model);

        // Assert
        Assert.False(validationStack.Contains(model));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(ValidationStack.CutOff + 1)]
    public void Pop_DoesNotThrowIfValueIsNull(int preloadCount)
    {
        // Arrange
        var validationStack = new TestValidationStack();

        PreLoad(preloadCount, validationStack);

        // Act & Assert
        // Popping null when it's not there must not throw
        validationStack.Pop(null);
    }

    [Fact]
    public void PushAndPop_TransitionsFromSingleModelToList()
    {
        // Arrange
        var validationStack = new TestValidationStack();
        var first = new Model { Position = 0 };
        var second = new Model { Position = 1 };
        var third = new Model { Position = 2 };

        // Act & Assert
        Assert.Equal(0, validationStack.Count);

        Assert.True(validationStack.Push(first));
        Assert.Equal(1, validationStack.Count);
        Assert.True(validationStack.Contains(first));
        Assert.False(validationStack.Push(first));

        Assert.True(validationStack.Push(second));
        Assert.Equal(2, validationStack.Count);
        Assert.False(validationStack.Push(first));
        Assert.False(validationStack.Push(second));

        validationStack.Pop(second);
        Assert.Equal(1, validationStack.Count);
        Assert.False(validationStack.Contains(second));
        Assert.True(validationStack.Contains(first));

        validationStack.Pop(first);
        Assert.Equal(0, validationStack.Count);
        Assert.False(validationStack.Contains(first));

        Assert.True(validationStack.Push(third));
        Assert.Equal(1, validationStack.Count);
        Assert.True(validationStack.Contains(third));
        validationStack.Pop(third);
        Assert.Equal(0, validationStack.Count);
    }

    [Fact]
    public void Pop_DoesNotThrowIfValueIsNull_WithSingleModel()
    {
        // Arrange
        var validationStack = new TestValidationStack();
        var model = new Model();
        validationStack.Push(model);

        // Act
        validationStack.Pop(null);

        // Assert
        Assert.Equal(1, validationStack.Count);
        Assert.True(validationStack.Contains(model));
    }

    [Fact]
    public void PushingMoreThanCutOffElements_SwitchesToHashSet()
    {
        // Arrange
        var size = ValidationStack.CutOff + 1;

        var validationStack = new TestValidationStack();
        var models = new List<Model>();
        for (var i = 0; i < size; i++)
        {
            models.Add(new Model { Position = i });
        }

        // Act & Assert
        foreach (var model in models)
        {
            validationStack.Push(model);
        }

        Assert.Equal(size, validationStack.Count);

        models.Reverse();
        foreach (var model in models)
        {
            validationStack.Pop(model);
        }

        Assert.Equal(0, validationStack.Count);
        Assert.True(validationStack.UsingHashSet());
    }

    private void PreLoad(int preloadCount, ValidationStack stack)
    {
        for (var i = 0; i < preloadCount; i++)
        {
            stack.Push(i);
        }
    }

    private class Model
    {
        public int Position { get; set; }
    }

    private class TestValidationStack : ValidationStack
    {
        public bool Contains(object model)
        {
            if (HashSet != null)
            {
                return HashSet.Contains(model);
            }
            else if (List != null)
            {
                return List.Contains(model);
            }
            else
            {
                return ReferenceEquals(First, model);
            }
        }

        public bool UsingHashSet()
        {
            return HashSet != null;
        }
    }
}
