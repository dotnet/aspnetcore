// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components;

public partial class ParameterViewTest
{
    [Fact]
    public void CanInitializeUsingComponentWithNoDescendants()
    {
        // Arrange
        var frames = new[]
        {
            RenderTreeFrame.ChildComponent(0, typeof(FakeComponent)).WithComponentSubtreeLength(1)
        };
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, frames, 0);

        // Assert
        Assert.Empty(ToEnumerable(parameters));
    }

    [Fact]
    public void CanInitializeUsingElementWithNoDescendants()
    {
        // Arrange
        var frames = new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(1)
        };
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, frames, 0);

        // Assert
        Assert.Empty(ToEnumerable(parameters));
    }

    [Fact]
    public void EnumerationStopsAtEndOfOwnerDescendants()
    {
        // Arrange
        var attribute1Value = new object();
        var attribute2Value = new object();
        var frames = new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(3),
            RenderTreeFrame.Attribute(1, "attribute 1", attribute1Value),
            RenderTreeFrame.Attribute(2, "attribute 2", attribute2Value),
            // Although RenderTreeBuilder doesn't let you add orphaned attributes like this,
            // still want to verify that parameters doesn't attempt to read past the
            // end of the owner's descendants
            RenderTreeFrame.Attribute(3, "orphaned attribute", "value")
        };
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, frames, 0);

        // Assert
        Assert.Collection(ToEnumerable(parameters),
            AssertParameter("attribute 1", attribute1Value, false),
            AssertParameter("attribute 2", attribute2Value, false));
    }

    [Fact]
    public void EnumerationStopsAtEndOfOwnerAttributes()
    {
        // Arrange
        var attribute1Value = new object();
        var attribute2Value = new object();
        var frames = new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(3),
            RenderTreeFrame.Attribute(1, "attribute 1", attribute1Value),
            RenderTreeFrame.Attribute(2, "attribute 2", attribute2Value),
            RenderTreeFrame.Element(3, "child element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(4, "child attribute", "some value")
        };
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, frames, 0);

        // Assert
        Assert.Collection(ToEnumerable(parameters),
            AssertParameter("attribute 1", attribute1Value, false),
            AssertParameter("attribute 2", attribute2Value, false));
    }

    [Fact]
    public void EnumerationIncludesCascadingParameters()
    {
        // Arrange
        var attribute1Value = new object();
        var attribute2Value = new object();
        var attribute3Value = new object();
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "attribute 1", attribute1Value)
        }, 0).WithCascadingParameters(new List<CascadingParameterState>
        {
            new CascadingParameterState(new(null, "attribute 2", attribute2Value.GetType()), new TestCascadingValue(attribute2Value)),
            new CascadingParameterState(new(null, "attribute 3", attribute3Value.GetType()), new TestCascadingValue(attribute3Value)),
        });

        // Assert
        Assert.Collection(ToEnumerable(parameters),
            AssertParameter("attribute 1", attribute1Value, false),
            AssertParameter("attribute 2", attribute2Value, true),
            AssertParameter("attribute 3", attribute3Value, true));
    }

    [Fact]
    public void CanTryGetNonExistingValue()
    {
        // Arrange
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "some other entry", new object())
        }, 0);

        // Act
        var didFind = parameters.TryGetValue<string>("nonexisting entry", out var value);

        // Assert
        Assert.False(didFind);
        Assert.Null(value);
    }

    [Fact]
    public void CanTryGetExistingValueWithCorrectType()
    {
        // Arrange
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "my entry", "hello")
        }, 0);

        // Act
        var didFind = parameters.TryGetValue<string>("my entry", out var value);

        // Assert
        Assert.True(didFind);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void CanGetValueOrDefault_WithExistingValue()
    {
        // Arrange
        var myEntryValue = new object();
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "my entry", myEntryValue),
            RenderTreeFrame.Attribute(1, "my other entry", new object())
        }, 0);

        // Act
        var result = parameters.GetValueOrDefault<object>("my entry");

        // Assert
        Assert.Same(myEntryValue, result);
    }

    [Fact]
    public void CanGetValueOrDefault_WithMultipleMatchingValues()
    {
        // Arrange
        var myEntryValue = new object();
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(3),
            RenderTreeFrame.Attribute(1, "my entry", myEntryValue),
            RenderTreeFrame.Attribute(1, "my entry", new object()),
        }, 0);

        // Act
        var result = parameters.GetValueOrDefault<object>("my entry");

        // Assert: Picks first match
        Assert.Same(myEntryValue, result);
    }

    [Fact]
    public void CanGetValueOrDefault_WithNonExistingValue()
    {
        // Arrange
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "some other entry", new object())
        }, 0).WithCascadingParameters(new List<CascadingParameterState>
        {
            new CascadingParameterState(new(null, "another entry", typeof(object)), new TestCascadingValue(null))
        });

        // Act
        var result = parameters.GetValueOrDefault<DateTime>("nonexisting entry");

        // Assert
        Assert.Equal(default, result);
    }

    [Fact]
    public void CanGetValueOrDefault_WithNonExistingValueAndExplicitDefault()
    {
        // Arrange
        var explicitDefaultValue = new DateTime(2018, 3, 20);
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "some other entry", new object())
        }, 0);

        // Act
        var result = parameters.GetValueOrDefault("nonexisting entry", explicitDefaultValue);

        // Assert
        Assert.Equal(explicitDefaultValue, result);
    }

    [Fact]
    public void ThrowsIfTryGetExistingValueWithIncorrectType()
    {
        // Arrange
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "my entry", "hello")
        }, 0);

        // Act/Assert
        Assert.Throws<InvalidCastException>(() =>
        {
            parameters.TryGetValue<bool>("my entry", out var value);
        });
    }

    [Fact]
    public void FromDictionary_CanBeInitializedWithEmptyDictionary()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>();

        // Act
        var collection = ParameterView.FromDictionary(dictionary);

        // Assert
        Assert.Empty(collection.ToDictionary());
    }

    [Fact]
    public void FromDictionary_RoundTrips()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            ["IntValue"] = 1,
            ["StringValue"] = "String"
        };

        // Act
        var collection = ParameterView.FromDictionary(dictionary);

        // Assert
        Assert.Equal(dictionary, collection.ToDictionary());
    }

    [Fact]
    public void CanConvertToReadOnlyDictionary()
    {
        // Arrange
        var entry2Value = new object();
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(3),
            RenderTreeFrame.Attribute(0, "entry 1", "value 1"),
            RenderTreeFrame.Attribute(0, "entry 2", entry2Value),
        }, 0);

        // Act
        IReadOnlyDictionary<string, object> dict = parameters.ToDictionary();

        // Assert
        Assert.Collection(dict,
            entry =>
            {
                Assert.Equal("entry 1", entry.Key);
                Assert.Equal("value 1", entry.Value);
            },
            entry =>
            {
                Assert.Equal("entry 2", entry.Key);
                Assert.Same(entry2Value, entry.Value);
            });
    }

    [Fact]
    public void CanGetValueOrDefault_WithMatchingCascadingParameter()
    {
        // Arrange
        var myEntryValue = new object();
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "unrelated value", new object())
        }, 0).WithCascadingParameters(new List<CascadingParameterState>
        {
            new CascadingParameterState(new(null, "unrelated value 2", typeof(object)), new TestCascadingValue(null)),
            new CascadingParameterState(new(null, "my entry", myEntryValue.GetType()), new TestCascadingValue(myEntryValue)),
            new CascadingParameterState(new(null, "unrelated value 3", typeof(object)), new TestCascadingValue(null)),
        });

        // Act
        var result = parameters.GetValueOrDefault<object>("my entry");

        // Assert
        Assert.Same(myEntryValue, result);
    }

    [Fact]
    public void CannotReadAfterLifetimeExpiry()
    {
        // Arrange
        var builder = new RenderBatchBuilder();
        var lifetime = new ParameterViewLifetime(builder);
        var frames = new[]
        {
            RenderTreeFrame.ChildComponent(0, typeof(FakeComponent)).WithComponentSubtreeLength(1)
        };
        var parameterView = new ParameterView(lifetime, frames, 0);

        // Act
        builder.InvalidateParameterViews();

        // Assert
        Assert.Throws<InvalidOperationException>(() => parameterView.GetEnumerator());
        Assert.Throws<InvalidOperationException>(() => parameterView.GetValueOrDefault<object>("anything"));
        Assert.Throws<InvalidOperationException>(() => parameterView.SetParameterProperties(new object()));
        Assert.Throws<InvalidOperationException>(() => parameterView.ToDictionary());
        var ex = Assert.Throws<InvalidOperationException>(() => parameterView.TryGetValue<object>("anything", out _));

        // It's enough to assert about one of the messages
        Assert.Equal($"The {nameof(ParameterView)} instance can no longer be read because it has expired. {nameof(ParameterView)} can only be read synchronously and must not be stored for later use.", ex.Message);
    }

    [Fact]
    public void Clone_EmptyParameterView()
    {
        // Arrange
        var initial = ParameterView.Empty;

        // Act
        var cloned = initial.Clone();

        // Assert
        Assert.Empty(ToEnumerable(cloned));
    }

    [Fact]
    public void Clone_ParameterViewSingleParameter()
    {
        // Arrange
        var attribute1Value = new object();
        var initial = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "attribute 1", attribute1Value),
        }, 0);

        // Act
        var cloned = initial.Clone();

        // Assert
        Assert.Collection(
            ToEnumerable(cloned),
            p => AssertParameter("attribute 1", attribute1Value, expectedIsCascading: false));
    }

    [Fact]
    public void Clone_ParameterPreservesOrder()
    {
        // Arrange
        var attribute1Value = new object();
        var attribute2Value = new object();
        var attribute3Value = new object();
        var initial = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(4),
            RenderTreeFrame.Attribute(1, "attribute 1", attribute1Value),
            RenderTreeFrame.Attribute(1, "attribute 2", attribute2Value),
            RenderTreeFrame.Attribute(1, "attribute 3", attribute3Value),
        }, 0);

        // Act
        var cloned = initial.Clone();

        // Assert
        Assert.Collection(
            ToEnumerable(cloned),
            p => AssertParameter("attribute 1", attribute1Value, expectedIsCascading: false),
            p => AssertParameter("attribute 2", attribute2Value, expectedIsCascading: false),
            p => AssertParameter("attribute 3", attribute3Value, expectedIsCascading: false));
    }

    [Fact]
    public void HasRemovedDirectParameters_BothEmpty()
    {
        // Arrange
        var oldParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(1),
        }, 0);
        var newParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(1),
        }, 0);

        // Act
        var hasRemovedDirectParameters = newParameters.HasRemovedDirectParameters(oldParameters);

        // Assert
        Assert.False(hasRemovedDirectParameters);
    }

    [Fact]
    public void HasRemovedDirectParameters_OldEmpty_NewNonEmpty()
    {
        // Arrange
        var oldParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(1),
        }, 0);
        var newParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "attribute 1", "value 1"),
        }, 0);

        // Act
        var hasRemovedDirectParameters = newParameters.HasRemovedDirectParameters(oldParameters);

        // Assert
        Assert.False(hasRemovedDirectParameters);
    }

    [Fact]
    public void HasRemovedDirectParameters_OldNonEmpty_NewEmpty()
    {
        // Arrange
        var oldParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
            RenderTreeFrame.Attribute(1, "attribute 1", "value 1"),
        }, 0);
        var newParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(1),
        }, 0);

        // Act
        var hasRemovedDirectParameters = newParameters.HasRemovedDirectParameters(oldParameters);

        // Assert
        Assert.True(hasRemovedDirectParameters);
    }

    [Fact]
    public void HasRemovedDirectParameters_ParameterRemoved()
    {
        // Arrange
        var oldParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(4),
            RenderTreeFrame.Attribute(1, "attribute 1", "value 1"),
            RenderTreeFrame.Attribute(2, "attribute 2", "value 2"),
            RenderTreeFrame.Attribute(3, "attribute 3", "value 3"),
        }, 0);
        var newParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(3),
            RenderTreeFrame.Attribute(1, "attribute 1", "value 1"),
            RenderTreeFrame.Attribute(2, "attribute 3", "value 3"),
        }, 0);

        // Act
        var hasRemovedDirectParameters = newParameters.HasRemovedDirectParameters(oldParameters);

        // Assert
        Assert.True(hasRemovedDirectParameters);
    }

    [Fact]
    public void HasRemovedDirectParameters_ParameterReplaced()
    {
        // Arrange
        var oldParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(4),
            RenderTreeFrame.Attribute(1, "attribute 1", "value 1"),
            RenderTreeFrame.Attribute(2, "attribute 2", "value 2"),
            RenderTreeFrame.Attribute(3, "attribute 3", "value 3"),
        }, 0);
        var newParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(4),
            RenderTreeFrame.Attribute(1, "attribute 2", "value 1"),
            RenderTreeFrame.Attribute(2, "attribute replaced", "value 2"),
            RenderTreeFrame.Attribute(3, "attribute 3", "value 3"),
        }, 0);

        // Act
        var hasRemovedDirectParameters = newParameters.HasRemovedDirectParameters(oldParameters);

        // Assert
        Assert.True(hasRemovedDirectParameters);
    }

    [Fact]
    public void HasRemovedDirectParameters_ParameterReplacedAndAdded()
    {
        // Arrange
        var oldParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(4),
            RenderTreeFrame.Attribute(1, "attribute 1", "value 1"),
            RenderTreeFrame.Attribute(2, "attribute 2", "value 2"),
            RenderTreeFrame.Attribute(3, "attribute 3", "value 3"),
        }, 0);
        var newParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(5),
            RenderTreeFrame.Attribute(1, "attribute 2", "value 1"),
            RenderTreeFrame.Attribute(2, "attribute replaced", "value 2"),
            RenderTreeFrame.Attribute(3, "attribute 3", "value 3"),
            RenderTreeFrame.Attribute(4, "attribute 4", "value 3"),
        }, 0);

        // Act
        var hasRemovedDirectParameters = newParameters.HasRemovedDirectParameters(oldParameters);

        // Assert
        Assert.True(hasRemovedDirectParameters);
    }

    [Fact]
    public void HasRemovedDirectParameters_ParametersSwapped()
    {
        // Arrange
        var oldParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(4),
            RenderTreeFrame.Attribute(1, "attribute 1", "value 1"),
            RenderTreeFrame.Attribute(2, "attribute 2", "value 2"),
            RenderTreeFrame.Attribute(3, "attribute 3", "value 3"),
        }, 0);
        var newParameters = new ParameterView(ParameterViewLifetime.Unbound, new[]
        {
            RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(4),
            RenderTreeFrame.Attribute(1, "attribute 1", "value 1"),
            RenderTreeFrame.Attribute(2, "attribute 3", "value 3"),
            RenderTreeFrame.Attribute(3, "attribute 2", "value 2"),
        }, 0);

        // Act
        var hasRemovedDirectParameters = newParameters.HasRemovedDirectParameters(oldParameters);

        // Assert
        Assert.False(hasRemovedDirectParameters);
    }

    private Action<ParameterValue> AssertParameter(string expectedName, object expectedValue, bool expectedIsCascading)
    {
        return parameter =>
        {
            Assert.Equal(expectedName, parameter.Name);
            Assert.Same(expectedValue, parameter.Value);
            Assert.Equal(expectedIsCascading, parameter.Cascading);
        };
    }

    public IEnumerable<ParameterValue> ToEnumerable(ParameterView parameters)
    {
        foreach (var item in parameters)
        {
            yield return item;
        }
    }

    private class FakeComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
            => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters)
            => throw new NotImplementedException();
    }

    private class TestCascadingValue : ICascadingValueSupplier
    {
        private readonly object _value;

        public TestCascadingValue(object value)
        {
            _value = value;
        }

        public bool IsFixed => false;

        public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
            => throw new NotImplementedException();

        public object GetCurrentValue(in CascadingParameterInfo parameterInfo)
            => _value;

        public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
            => throw new NotImplementedException();

        public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
            => throw new NotImplementedException();
    }
}
