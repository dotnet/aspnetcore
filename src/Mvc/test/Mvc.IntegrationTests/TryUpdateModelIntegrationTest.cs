// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class TryUpdateModelIntegrationTest
{
    private class Address
    {
        public string Street { get; set; }

        public string City { get; set; }
    }

    [Fact]
    public async Task TryUpdateModel_ExistingModel_EmptyPrefix_OverwritesBoundValues()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Address
        {
            Street = "DefaultStreet",
            City = "Toronto",
        };
        var oldModel = model;

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Same(oldModel, model);
        Assert.Equal("SomeStreet", model.Street);
        Assert.Equal("Toronto", model.City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_ExistingModel_EmptyPrefix_GetsBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Address();

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Equal("SomeStreet", model.Street);
        Assert.Null(model.City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    private class Person1
    {
        public string Name { get; set; }

        public Address Address { get; set; }
    }

    [Fact]
    public async Task TryUpdateModel_TopLevelCollection_EmptyPrefix_BindsAfterClearing()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create(new Dictionary<string, string>
            {
                    { "[0].Name", "One Name" },
                    { "[1].Address.Street", "Two Street" },
            });
        });

        var modelState = testContext.ModelState;
        var model = new List<Person1>
            {
                new Person1
                {
                    Name = "One",
                    Address = new Address
                    {
                        Street = "DefaultStreet",
                        City = "Toronto",
                    },
                },
                new Person1 { Name = "Two" },
                new Person1 { Name = "Three" },
            };

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Collection(
            model,
            element =>
            {
                Assert.Equal("One Name", element.Name);
                Assert.Null(element.Address);
            },
            element =>
            {
                Assert.Null(element.Name);
                Assert.NotNull(element.Address);
                Assert.Equal("Two Street", element.Address.Street);
                Assert.Null(element.Address.City);
            });

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Equal(2, modelState.Count);
        Assert.NotNull(modelState["[0].Name"]);
        Assert.NotNull(modelState["[1].Address.Street"]);
    }

    [Fact]
    public async Task TryUpdateModel_NestedPoco_EmptyPrefix_DoesNotTrounceUnboundValues()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address.Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person1
        {
            Name = "Joe",
            Address = new Address
            {
                Street = "DefaultStreet",
                City = "Toronto",
            },
        };
        var oldModel = model;

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Same(oldModel, model);
        Assert.Equal("Joe", model.Name);
        Assert.Equal("SomeStreet", model.Address.Street);
        Assert.Equal("Toronto", model.Address.City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Address.Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    private class Person2
    {
        public List<Address> Address { get; set; }
    }

    [Fact]
    public async Task TryUpdateModel_SettableCollectionModel_EmptyPrefix_CreatesCollection()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person2();

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_SettableCollectionModel_EmptyPrefix_MaintainsCollectionIfNonNull()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person2
        {
            Address = new List<Address>(),
        };
        var collection = model.Address;

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);
        Assert.Same(collection, model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    private class Person3
    {
        public Person3()
        {
            Address = new List<Address>();
        }

        public List<Address> Address { get; }
    }

    [Fact]
    public async Task TryUpdateModel_NonSettableCollectionModel_EmptyPrefix_GetsBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person3
        {
            Address =
                {
                    new Address
                    {
                        Street = "Old street",
                        City = "Redmond",
                    },
                    new Address
                    {
                        Street = "Older street",
                        City = "Toronto",
                    },
                },
        };

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model (collection is cleared and new members created from scratch).
        Assert.NotNull(model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    private class Person6
    {
        public CustomReadOnlyCollection<Address> Address { get; set; }
    }

    [Fact]
    public async Task TryUpdateModel_ReadOnlyCollectionModel_EmptyPrefix_DoesNotGetBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person6();

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // ModelState
        Assert.True(modelState.IsValid);
        var entry = Assert.Single(modelState);
        Assert.Equal("Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.NotNull(state);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Equal("SomeStreet", state.AttemptedValue);
    }

    [Fact]
    public async Task TryUpdateModel_ReadOnlyCollectionModel_WithPrefix_DoesNotGetBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person6();

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // ModelState
        Assert.True(modelState.IsValid);
        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.NotNull(state);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Equal("SomeStreet", state.AttemptedValue);
    }

    private class Person4
    {
        public Address[] Address { get; set; }
    }

    [Fact]
    public async Task TryUpdateModel_SettableArrayModel_EmptyPrefix_CreatesArray()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person4();

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_SettableArrayModel_EmptyPrefix_OverwritesArray()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person4
        {
            Address = new Address[]
            {
                    new Address
                    {
                        Street = "Old street",
                        City = "Toronto",
                    },
            },
        };
        var collection = model.Address;

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);
        Assert.NotSame(collection, model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    private class Person5
    {
        public Address[] Address { get; } = new Address[] { };
    }

    [Fact]
    public async Task TryUpdateModel_NonSettableArrayModel_EmptyPrefix_IsNotBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person5();

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);

        // Arrays should not be updated.
        Assert.Empty(model.Address);

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState);
    }

    private class Person7
    {
        public IEnumerable<Address> Address { get; } = new Address[]
        {
                new Address()
                {
                     City = "Redmond",
                     Street = "One Microsoft Way"
                }
        };
    }

    [Fact]
    public async Task TryUpdateModel_NonSettableIEnumerableModel_EmptyPrefix_IsNotBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person7();

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);

        // Arrays should not be updated.
        Assert.Single(model.Address);
        Assert.Collection(
            model.Address,
            (a) =>
            {
                Assert.Equal("Redmond", a.City);
                Assert.Equal("One Microsoft Way", a.Street);
            });

        // ModelState
        Assert.True(modelState.IsValid);
    }

    private class Person8
    {
        public ICollection<Address> Address { get; } = new Address[]
        {
                new Address()
                {
                     City = "Redmond",
                     Street = "One Microsoft Way"
                }
        };
    }

    [Fact]
    public async Task TryUpdateModel_NonSettableICollectionModel_EmptyPrefix_IsNotBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person8();

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);

        // Arrays should not be updated.
        Assert.Single(model.Address);
        Assert.Collection(
            model.Address,
            (a) =>
            {
                Assert.Equal("Redmond", a.City);
                Assert.Equal("One Microsoft Way", a.Street);
            });

        // ModelState
        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task TryUpdateModel_ExistingModel_WithPrefix_ValuesGetOverwritten()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Address
        {
            Street = "DefaultStreet",
            City = "Toronto",
        };
        var oldModel = model;

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Same(oldModel, model);
        Assert.Equal("SomeStreet", model.Street);
        Assert.Equal("Toronto", model.City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_ExistingModel_WithPrefix_GetsBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Address();

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Equal("SomeStreet", model.Street);
        Assert.Null(model.City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_TopLevelCollection_WithPrefix_BindsAfterClearing()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create(new Dictionary<string, string>
            {
                    { "prefix[0].Name", "One Name" },
                    { "prefix[1].Address.Street", "Two Street" },
            });
        });

        var modelState = testContext.ModelState;
        var model = new List<Person1>
            {
                new Person1
                {
                    Name = "One",
                    Address = new Address
                    {
                        Street = "DefaultStreet",
                        City = "Toronto",
                    },
                },
                new Person1 { Name = "Two" },
                new Person1 { Name = "Three" },
            };

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Collection(
            model,
            element =>
            {
                Assert.Equal("One Name", element.Name);
                Assert.Null(element.Address);
            },
            element =>
            {
                Assert.Null(element.Name);
                Assert.NotNull(element.Address);
                Assert.Equal("Two Street", element.Address.Street);
                Assert.Null(element.Address.City);
            });

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Equal(2, modelState.Count);
        Assert.NotNull(modelState["prefix[0].Name"]);
        Assert.NotNull(modelState["prefix[1].Address.Street"]);
    }

    [Fact]
    public async Task TryUpdateModel_NestedPoco_WithPrefix_DoesNotTrounceUnboundValues()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Address.Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person1
        {
            Name = "Joe",
            Address = new Address
            {
                Street = "DefaultStreet",
                City = "Toronto",
            },
        };
        var oldModel = model;

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Same(oldModel, model);
        Assert.Equal("Joe", model.Name);
        Assert.Equal("SomeStreet", model.Address.Street);
        Assert.Equal("Toronto", model.Address.City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Address.Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_SettableCollectionModel_WithPrefix_CreatesCollection()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person2();

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_SettableCollectionModel_WithPrefix_MaintainsCollectionIfNonNull()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person2
        {
            Address = new List<Address>(),
        };
        var collection = model.Address;

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);
        Assert.Same(collection, model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_NonSettableCollectionModel_WithPrefix_GetsBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person3
        {
            Address =
                {
                    new Address
                    {
                        Street = "Old street",
                        City = "Redmond",
                    },
                    new Address
                    {
                        Street = "Older street",
                        City = "Toronto",
                    },
                },
        };

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model (collection is cleared and new members created from scratch).
        Assert.NotNull(model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_SettableArrayModel_WithPrefix_CreatesArray()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person4();

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_SettableArrayModel_WithPrefix_OverwritesArray()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person4
        {
            Address = new Address[]
            {
                    new Address
                    {
                        Street = "Old street",
                        City = "Toronto",
                    },
            },
        };
        var collection = model.Address;

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);
        Assert.NotSame(collection, model.Address);
        Assert.Single(model.Address);
        Assert.Equal("SomeStreet", model.Address[0].Street);
        Assert.Null(model.Address[0].City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("prefix.Address[0].Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_NonSettableArrayModel_WithPrefix_GetsBound()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new Person5();

        // Act
        var result = await TryUpdateModelAsync(model, "prefix", testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.NotNull(model.Address);

        // Arrays should not be updated.
        Assert.Empty(model.Address);

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState);
    }

    [Fact]
    public async Task TryUpdateModelAsync_TopLevelFormFileCollection_IsBound()
    {
        // Arrange
        var data = "some data";
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => UpdateRequest(request, data, "files"));
        var modelState = testContext.ModelState;
        var model = new List<IFormFile>
            {
                new FormFile(new MemoryStream(), baseStreamOffset: 0, length: 0, name: "file", fileName: "file1"),
                new FormFile(new MemoryStream(), baseStreamOffset: 0, length: 0, name: "file", fileName: "file2"),
                new FormFile(new MemoryStream(), baseStreamOffset: 0, length: 0, name: "file", fileName: "file3"),
            };

        // Act
        var result = await TryUpdateModelAsync(model, prefix: "files", testContext: testContext);

        // Assert
        Assert.True(result);

        // Model
        var file = Assert.Single(model);
        Assert.Equal("form-data; name=files; filename=text.txt", file.ContentDisposition);
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            Assert.Equal(data, reader.ReadToEnd());
        }

        // ModelState
        Assert.True(modelState.IsValid);
        var kvp = Assert.Single(modelState);
        Assert.Equal("files", kvp.Key);
        var modelStateEntry = kvp.Value;
        Assert.NotNull(modelStateEntry);
        Assert.Empty(modelStateEntry.Errors);
        Assert.Equal(ModelValidationState.Valid, modelStateEntry.ValidationState);
        Assert.Null(modelStateEntry.AttemptedValue);
        Assert.Null(modelStateEntry.RawValue);
    }

    private class AddressWithNoParameterlessConstructor
    {
        private readonly int _id;
        public AddressWithNoParameterlessConstructor(int id)
        {
            _id = id;
        }
        public string Street { get; set; }
        public string City { get; set; }
    }

    [Fact]
    public async Task TryUpdateModel_ExistingModelWithNoParameterlessConstructor_OverwritesBoundValues()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new AddressWithNoParameterlessConstructor(10)
        {
            Street = "DefaultStreet",
            City = "Toronto",
        };
        var oldModel = model;

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Same(oldModel, model);
        Assert.Equal("SomeStreet", model.Street);
        Assert.Equal("Toronto", model.City);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal("Street", entry.Key);
        var state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    private record AddressRecord(string Street, string City)
    {
        public string ZipCode { get; set; }
    }

    [Fact]
    public async Task TryUpdateModel_RecordTypeModel_Throws()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new AddressRecord("DefaultStreet", "Toronto")
        {
            ZipCode = "98001",
        };
        var oldModel = model;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => TryUpdateModelAsync(model, string.Empty, testContext));
        Assert.Equal($"TryUpdateModelAsync cannot update a record type model. If a '{model.GetType()}' must be updated, include it in an object type.", ex.Message);

    }

    private class ModelWithRecordTypeProperty
    {
        public AddressRecord Address { get; set; }
    }

    [Fact]
    public async Task TryUpdateModel_RecordTypeProperty()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address.ZipCode", "98007").Add("Address.Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new ModelWithRecordTypeProperty();
        var oldModel = model;

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Same(oldModel, model);
        Assert.NotNull(model.Address);
        var address = model.Address;
        Assert.Equal("SomeStreet", address.Street);
        Assert.Null(address.City);
        Assert.Equal("98007", address.ZipCode);

        // ModelState
        Assert.True(modelState.IsValid);

        Assert.Equal(2, modelState.Count);
        var entry = Assert.Single(modelState, k => k.Key == "Address.ZipCode");
        var state = entry.Value;
        Assert.Equal("98007", state.AttemptedValue);
        Assert.Equal("98007", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);

        entry = Assert.Single(modelState, k => k.Key == "Address.Street");
        state = entry.Value;
        Assert.Equal("SomeStreet", state.AttemptedValue);
        Assert.Equal("SomeStreet", state.RawValue);
        Assert.Empty(state.Errors);
        Assert.Equal(ModelValidationState.Valid, state.ValidationState);
    }

    [Fact]
    public async Task TryUpdateModel_RecordTypePropertyIsOverwritten()
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = QueryString.Create("Address.ZipCode", "98007").Add("Address.Street", "SomeStreet");
        });

        var modelState = testContext.ModelState;
        var model = new ModelWithRecordTypeProperty
        {
            Address = new AddressRecord("DefaultStreet", "DefaultCity")
            {
                ZipCode = "98056",
            },
        };
        var oldModel = model;

        // Act
        var result = await TryUpdateModelAsync(model, string.Empty, testContext);

        // Assert
        Assert.True(result);

        // Model
        Assert.Same(oldModel, model);
        Assert.NotNull(model.Address);
        var address = model.Address;
        Assert.Equal("SomeStreet", address.Street);
        Assert.Null(address.City);
        Assert.Equal("98007", address.ZipCode);

        // ModelState
        Assert.True(modelState.IsValid);

        Assert.Collection(
            modelState.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Address.Street", kvp.Key);
                var state = kvp.Value;
                Assert.Equal("SomeStreet", state.AttemptedValue);
                Assert.Equal("SomeStreet", state.RawValue);
                Assert.Empty(state.Errors);
                Assert.Equal(ModelValidationState.Valid, state.ValidationState);
            },
            kvp =>
            {
                Assert.Equal("Address.ZipCode", kvp.Key);
                var state = kvp.Value;
                Assert.Equal("98007", state.AttemptedValue);
                Assert.Equal("98007", state.RawValue);
                Assert.Empty(state.Errors);
                Assert.Equal(ModelValidationState.Valid, state.ValidationState);
            });
    }

    private void UpdateRequest(HttpRequest request, string data, string name)
    {
        const string fileName = "text.txt";
        var fileCollection = new FormFileCollection();
        var formCollection = new FormCollection(new Dictionary<string, StringValues>(), fileCollection);

        request.Form = formCollection;
        request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq";

        request.Headers["Content-Disposition"] = $"form-data; name={name}; filename={fileName}";

        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
        fileCollection.Add(new FormFile(memoryStream, 0, data.Length, name, fileName)
        {
            Headers = request.Headers
        });
    }

    private class CustomReadOnlyCollection<T> : ICollection<T>
    {
        private readonly ICollection<T> _original;

        public CustomReadOnlyCollection()
            : this(new List<T>())
        {
        }

        public CustomReadOnlyCollection(ICollection<T> original)
        {
            _original = original;
        }

        public int Count
        {
            get { return _original.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            return _original.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _original.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (T t in _original)
            {
                yield return t;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private async Task<bool> TryUpdateModelAsync(
        object model,
        string prefix,
        ModelBindingTestContext testContext)
    {
        var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
        return await ModelBindingHelper.TryUpdateModelAsync(
            model,
            model.GetType(),
            prefix,
            testContext,
            testContext.MetadataProvider,
            TestModelBinderFactory.CreateDefault(),
            valueProvider,
            ModelBindingTestHelper.GetObjectValidator(testContext.MetadataProvider));
    }
}
