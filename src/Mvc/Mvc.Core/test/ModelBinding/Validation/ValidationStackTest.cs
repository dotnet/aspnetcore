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
            else
            {
                return List.Contains(model);
            }
        }

        public bool UsingHashSet()
        {
            return HashSet != null;
        }
    }
}
