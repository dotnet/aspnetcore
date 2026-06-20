#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Validation.Tests;

public class AsyncValidationTests
{
    [Fact]
    public async Task AsyncValidationAttribute_ValidatesAsynchronously_FailsWhenInvalid()
    {
        // Arrange
        var userType = new TestValidatableTypeInfo(
            typeof(UserWithAsyncValidation),
            [
                CreatePropertyInfo(typeof(UserWithAsyncValidation), typeof(string), "Email", "Email",
                    [new EmailExistsAttribute()])
            ]);

        var user = new UserWithAsyncValidation { Email = "duplicate@example.com" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(UserWithAsyncValidation), userType }
            }),
            ValidationContext = new ValidationContext(user)
        };

        // Act
        await userType.ValidateAsync(user, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Email", error.Key);
        Assert.Equal("Email already exists", error.Value.First());
    }

    [Fact]
    public async Task AsyncValidationAttribute_PassesWhenValid()
    {
        // Arrange
        var userType = new TestValidatableTypeInfo(
            typeof(UserWithAsyncValidation),
            [
                CreatePropertyInfo(typeof(UserWithAsyncValidation), typeof(string), "Email", "Email",
                    [new EmailExistsAttribute()])
            ]);

        var user = new UserWithAsyncValidation { Email = "unique@example.com" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(UserWithAsyncValidation), userType }
            }),
            ValidationContext = new ValidationContext(user)
        };

        // Act
        await userType.ValidateAsync(user, context, default);

        // Assert
        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0);
    }

    [Fact]
    public async Task MixedSyncAndAsyncAttributes_AllValidate()
    {
        // Arrange
        var productType = new TestValidatableTypeInfo(
            typeof(Product),
            [
                CreatePropertyInfo(typeof(Product), typeof(string), "Name", "Name",
                    [
                        new RequiredAttribute { ErrorMessage = "Name is required" },
                        new StringLengthAttribute(100) { ErrorMessage = "Name too long" }
                    ]),
                CreatePropertyInfo(typeof(Product), typeof(string), "SKU", "SKU",
                    [
                        new RequiredAttribute { ErrorMessage = "SKU is required" },
                        new SkuValidationAttribute { ErrorMessage = "SKU already exists" }
                    ])
            ]);

        var product = new Product { Name = "", SKU = "DUPLICATE-SKU" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Product), productType }
            }),
            ValidationContext = new ValidationContext(product)
        };

        // Act
        await productType.ValidateAsync(product, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(2, context.ValidationErrors.Count);
        Assert.Contains("Name", context.ValidationErrors.Keys);
        Assert.Contains("SKU", context.ValidationErrors.Keys);
        Assert.Equal("Name is required", context.ValidationErrors["Name"].First());
        Assert.Equal("SKU already exists", context.ValidationErrors["SKU"].First());
    }

    [Fact]
    public async Task AsyncValidation_RespectsValidationOrder()
    {
        // Arrange
        var validationOrder = new ConcurrentQueue<string>();
        var orderType = new TestValidatableTypeInfo(
            typeof(OrderWithTracking),
            [
                CreatePropertyInfo(typeof(OrderWithTracking), typeof(string), "Field1", "Field1",
                    [
                        new TrackingSyncAttribute(validationOrder, "Field1-Sync"),
                        new TrackingAsyncAttribute(validationOrder, "Field1-Async")
                    ]),
                CreatePropertyInfo(typeof(OrderWithTracking), typeof(string), "Field2", "Field2",
                    [
                        new TrackingSyncAttribute(validationOrder, "Field2-Sync"),
                        new TrackingAsyncAttribute(validationOrder, "Field2-Async")
                    ])
            ]);

        var order = new OrderWithTracking();
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(OrderWithTracking), orderType }
            }),
            ValidationContext = new ValidationContext(order)
        };

        // Act
        await orderType.ValidateAsync(order, context, default);

        // Assert - Within each property, sync validations run before async
        Assert.Equal(4, validationOrder.Count);

        var validationOrderArray = validationOrder.ToArray();
        Assert.Equal("Field1-Sync", validationOrderArray[0]);
        Assert.Equal("Field2-Sync", validationOrderArray[1]);

        if (validationOrderArray[2] == "Field1-Async")
        {
            Assert.Equal("Field2-Async", validationOrderArray[3]);
        }
        else
        {
            Assert.Equal("Field2-Async", validationOrderArray[2]);
            Assert.Equal("Field1-Async", validationOrderArray[3]);
        }
    }

    [Fact]
    public async Task IAsyncValidatableObject_ValidatesAsynchronously()
    {
        // Arrange
        var accountType = new TestValidatableTypeInfo(typeof(AsyncValidatableAccount), []);

        var account = new AsyncValidatableAccount
        {
            Username = "taken",
            Email = "duplicate@example.com"
        };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(AsyncValidatableAccount), accountType }
            }),
            ValidationContext = new ValidationContext(account)
        };

        // Act
        await accountType.ValidateAsync(account, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(2, context.ValidationErrors.Count);
        Assert.Contains("Username", context.ValidationErrors.Keys);
        Assert.Contains("Email", context.ValidationErrors.Keys);
    }

    [Fact]
    public async Task IAsyncValidatableObject_WithMultipleMemberNames()
    {
        // Arrange
        var registrationType = new TestValidatableTypeInfo(typeof(RegistrationForm), []);

        var registration = new RegistrationForm
        {
            Password = "password123",
            ConfirmPassword = "different"
        };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(RegistrationForm), registrationType }
            }),
            ValidationContext = new ValidationContext(registration)
        };

        // Act
        await registrationType.ValidateAsync(registration, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(2, context.ValidationErrors.Count);
        Assert.Contains("Password", context.ValidationErrors.Keys);
        Assert.Contains("ConfirmPassword", context.ValidationErrors.Keys);
    }

    [Fact]
    public async Task AsyncValidation_OnNestedObjects()
    {
        // Arrange
        var addressType = new TestValidatableTypeInfo(
            typeof(AddressWithAsyncValidation),
            [
                CreatePropertyInfo(typeof(AddressWithAsyncValidation), typeof(string), "ZipCode", "Zip Code",
                    [new ZipCodeExistsAttribute { ErrorMessage = "Invalid zip code" }])
            ]);

        var customerType = new TestValidatableTypeInfo(
            typeof(CustomerWithAsyncAddress),
            [
                CreatePropertyInfo(typeof(CustomerWithAsyncAddress), typeof(string), "Name", "Name",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(CustomerWithAsyncAddress), typeof(AddressWithAsyncValidation), "Address", "Address", [])
            ]);

        var customer = new CustomerWithAsyncAddress
        {
            Name = "John Doe",
            Address = new AddressWithAsyncValidation { ZipCode = "INVALID" }
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(CustomerWithAsyncAddress), customerType },
                { typeof(AddressWithAsyncValidation), addressType }
            }),
            ValidationContext = new ValidationContext(customer)
        };

        // Act
        await customerType.ValidateAsync(customer, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Address.ZipCode", error.Key);
        Assert.Equal("Invalid zip code", error.Value.First());
    }

    [Fact]
    public async Task AsyncValidation_OnNestedIAsyncValidatableObjectProperty()
    {
        var profileType = new TestValidatableTypeInfo(typeof(AsyncValidatableProfile), []);

        var customerType = new TestValidatableTypeInfo(
            typeof(CustomerWithAsyncProfile),
            [
                CreatePropertyInfo(typeof(CustomerWithAsyncProfile), typeof(string), "Name", "Name",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(CustomerWithAsyncProfile), typeof(AsyncValidatableProfile), "Profile", "Profile", [])
            ]);

        var customer = new CustomerWithAsyncProfile
        {
            Name = "John Doe",
            Profile = new AsyncValidatableProfile { Bio = "short" }
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(CustomerWithAsyncProfile), customerType },
                { typeof(AsyncValidatableProfile), profileType }
            }),
            ValidationContext = new ValidationContext(customer)
        };

        // Act
        await customerType.ValidateAsync(customer, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Profile.Bio", error.Key);
        Assert.Equal("Bio must be at least 10 characters", error.Value.First());
    }

    [Fact]
    public async Task AsyncValidation_OnNestedIAsyncValidatableObjectProperty_UsesIsolatedContext()
    {
        // Arrange
        // A nested IAsyncValidatableObject must be validated on an isolated (cloned) context so that
        // its asynchronous validation - which runs in parallel with sibling members - never observes
        // mutations made to the ValidationContext by those siblings. If the nested async object were
        // wrongly treated as "guaranteed synchronous", it would be validated on the shared context
        // and would observe the sibling 'Name' property overwriting ValidationContext.MemberName.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var profile = new GatedAsyncValidatableProfile { Gate = gate.Task };

        var profileType = new TestValidatableTypeInfo(typeof(GatedAsyncValidatableProfile), []);
        var customerType = new TestValidatableTypeInfo(
            typeof(CustomerWithGatedAsyncProfile),
            [
                // Profile is validated first and parks on the gate; Name runs afterwards and mutates
                // ValidationContext.MemberName on the shared context while the gate is still closed.
                CreatePropertyInfo(typeof(CustomerWithGatedAsyncProfile), typeof(GatedAsyncValidatableProfile), "Profile", "Profile", []),
                CreatePropertyInfo(typeof(CustomerWithGatedAsyncProfile), typeof(string), "Name", "Name",
                    [new StringLengthAttribute(100)])
            ]);

        var customer = new CustomerWithGatedAsyncProfile
        {
            Profile = profile,
            Name = "John Doe"
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(CustomerWithGatedAsyncProfile), customerType },
                { typeof(GatedAsyncValidatableProfile), profileType }
            }),
            ValidationContext = new ValidationContext(customer)
        };

        // Act
        var validationTask = customerType.ValidateAsync(customer, context, default);

        // All synchronous member validation has run by now (Name has set MemberName on its context).
        // Releasing the gate lets the nested async validation resume and capture the MemberName.
        gate.SetResult();

        await validationTask.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert - the nested async object must not have observed the sibling's MemberName mutation.
        Assert.NotEqual("Name", profile.CapturedMemberName);

        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Profile.Bio", error.Key);
    }

    [Fact]
    public async Task SiblingMember_WithSyncError_HasCorrectPath_WhenPrecedingMemberSuspendsAsync()
    {
        // Regression test for ValidateContext clone state leaking between sibling members.
        //
        // Members are validated sequentially, but when a member's validation does not complete
        // synchronously, the validator clones the ValidateContext for the subsequent members so
        // they don't race on shared mutable state. That clone must not inherit the *in-progress*
        // CurrentValidationPath of the suspended sibling.
        //
        // Here 'First' parks on a gate while CurrentValidationPath has been mutated to "First"
        // (its finally block that restores the path hasn't run yet). 'Second' is then validated
        // on a clone and fails synchronously. Because 'Second' is a root-level sibling its error
        // path must be "Second" - not "First.Second" - regardless of where 'First' is suspended.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var modelType = new TestValidatableTypeInfo(
            typeof(TwoStringModel),
            [
                CreatePropertyInfo(typeof(TwoStringModel), typeof(string), "First", "First",
                    [new GatedSuccessAsyncAttribute(gate.Task)]),
                CreatePropertyInfo(typeof(TwoStringModel), typeof(string), "Second", "Second",
                    [new StringLengthAttribute(3) { ErrorMessage = "Second is too long" }])
            ]);

        var model = new TwoStringModel { First = "ok", Second = "way too long" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(TwoStringModel), modelType }
            }),
            ValidationContext = new ValidationContext(model)
        };

        // Act - 'First' suspends on the gate during the synchronous portion of validation, then
        // 'Second' is validated (and fails) on the cloned context. Release the gate afterwards.
        var validateTask = modelType.ValidateAsync(model, context, default);
        gate.SetResult();
        await validateTask.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Second", error.Key);
        Assert.Equal("Second is too long", error.Value.First());
    }

    [Fact]
    public async Task SiblingMember_WithSyncError_HasCorrectPath_WhenPrecedingComplexMemberSuspendsAsync()
    {
        // Same clone-state-leak regression as above, but the suspended sibling is a *complex*
        // member whose nested async validation parks while CurrentValidationPath has been pushed
        // even deeper (and CurrentDepth has been incremented). The failing root-level sibling
        // 'Title' must still report its error under "Title".
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var innerType = new TestValidatableTypeInfo(
            typeof(GatedInner),
            [
                CreatePropertyInfo(typeof(GatedInner), typeof(string), "Value", "Value",
                    [new GatedSuccessAsyncAttribute(gate.Task)])
            ]);

        var modelType = new TestValidatableTypeInfo(
            typeof(ComplexThenStringModel),
            [
                CreatePropertyInfo(typeof(ComplexThenStringModel), typeof(GatedInner), "Inner", "Inner", []),
                CreatePropertyInfo(typeof(ComplexThenStringModel), typeof(string), "Title", "Title",
                    [new StringLengthAttribute(3) { ErrorMessage = "Title is too long" }])
            ]);

        var model = new ComplexThenStringModel
        {
            Inner = new GatedInner { Value = "ok" },
            Title = "way too long"
        };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(ComplexThenStringModel), modelType },
                { typeof(GatedInner), innerType }
            }),
            ValidationContext = new ValidationContext(model)
        };

        // Act
        var validateTask = modelType.ValidateAsync(model, context, default);
        gate.SetResult();
        await validateTask.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Title", error.Key);
        Assert.Equal("Title is too long", error.Value.First());
    }

    [Fact]
    public async Task AsyncValidation_WithCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var documentType = new TestValidatableTypeInfo(
            typeof(Document),
            [
                CreatePropertyInfo(typeof(Document), typeof(string), "Content", "Content",
                    [new SlowAsyncValidationAttribute()])
            ]);

        var document = new Document { Content = "test" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Document), documentType }
            }),
            ValidationContext = new ValidationContext(document)
        };

        // Cancel after a short delay
        cts.CancelAfter(100);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await documentType.ValidateAsync(document, context, cts.Token);
        });
    }

    [Fact]
    public async Task MultipleAsyncValidation_WithCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var documentType = new TestValidatableTypeInfo(
            typeof(Document),
            [
                CreatePropertyInfo(typeof(Document), typeof(string), "Content", "Content",
                    [new SlowAsyncValidationAttribute(), new SlowAsyncValidationAttribute()])
            ]);

        var document = new Document { Content = "test" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Document), documentType }
            }),
            ValidationContext = new ValidationContext(document)
        };

        // Cancel after a short delay
        cts.CancelAfter(100);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await documentType.ValidateAsync(document, context, cts.Token);
        });
    }

    [Fact]
    public async Task AsyncValidation_CombinedWithIAsyncValidatableObject()
    {
        // Arrange
        var profileType = new TestValidatableTypeInfo(
            typeof(UserProfile),
            [
                CreatePropertyInfo(typeof(UserProfile), typeof(string), "Username", "Username",
                    [new UsernameAvailableAttribute { ErrorMessage = "Username taken" }])
            ]);

        var profile = new UserProfile
        {
            Username = "taken",
            Bio = "short" // Will fail IAsyncValidatableObject validation
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(UserProfile), profileType }
            }),
            ValidationContext = new ValidationContext(profile)
        };

        // Act
        await profileType.ValidateAsync(profile, context, default);

        // Assert - IAsyncValidatableObject is not called when property validation fails
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Username", error.Key);
        Assert.Equal("Username taken", error.Value.First());
    }

    [Fact]
    public async Task IAsyncValidatableObject_RunsWhenPropertyValidationPasses()
    {
        // Arrange
        var profileType = new TestValidatableTypeInfo(
            typeof(UserProfile),
            [
                CreatePropertyInfo(typeof(UserProfile), typeof(string), "Username", "Username",
                    [new UsernameAvailableAttribute()])
            ]);

        var profile = new UserProfile
        {
            Username = "available",
            Bio = "short" // Will fail IAsyncValidatableObject validation
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(UserProfile), profileType }
            }),
            ValidationContext = new ValidationContext(profile)
        };

        // Act
        await profileType.ValidateAsync(profile, context, default);

        // Assert - IAsyncValidatableObject runs and reports error
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Bio", error.Key);
        Assert.Equal("Bio must be at least 10 characters", error.Value.First());
    }

    [Fact]
    public async Task AsyncValidation_TypeLevelAttribute()
    {
        // Arrange
        var orderType = new TestValidatableTypeInfo(
            typeof(OrderWithTypeValidation),
            [
                CreatePropertyInfo(typeof(OrderWithTypeValidation), typeof(decimal), "SubTotal", "SubTotal", []),
                CreatePropertyInfo(typeof(OrderWithTypeValidation), typeof(decimal), "Tax", "Tax", []),
                CreatePropertyInfo(typeof(OrderWithTypeValidation), typeof(decimal), "Total", "Total", [])
            ],
            [new OrderTotalValidationAttribute()]);

        var order = new OrderWithTypeValidation
        {
            SubTotal = 100,
            Tax = 10,
            Total = 99 // Wrong total
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(OrderWithTypeValidation), orderType }
            }),
            ValidationContext = new ValidationContext(order)
        };

        // Act
        await orderType.ValidateAsync(order, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Contains("Total", error.Value.First());
    }

    [Fact]
    public async Task AsyncValidation_InCollection()
    {
        const int itemCount = 3;
        var allEnteredSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var attribute = new ParallelBarrierAsyncValidationAttribute(allEnteredSignal, itemCount)
        {
            ErrorMessage = "Item code exists"
        };

        var itemType = new TestValidatableTypeInfo(
            typeof(ItemWithAsyncValidation),
            [
                CreatePropertyInfo(typeof(ItemWithAsyncValidation), typeof(string), "Code", "Code", [attribute])
            ]);

        var listType = new TestValidatableTypeInfo(
            typeof(ItemList),
            [
                CreatePropertyInfo(typeof(ItemList), typeof(List<ItemWithAsyncValidation>), "Items", "Items", [])
            ]);

        var list = new ItemList
        {
            Items = new List<ItemWithAsyncValidation>
            {
                new ItemWithAsyncValidation { Code = "VALID" },
                new ItemWithAsyncValidation { Code = "DUPLICATE" },
                new ItemWithAsyncValidation { Code = "VALID2" }
            }
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(ItemList), listType },
                { typeof(ItemWithAsyncValidation), itemType }
            }),
            ValidationContext = new ValidationContext(list)
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await listType.ValidateAsync(list, context, cts.Token);

        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Items[1].Code", error.Key);
        Assert.Equal("Item code exists", error.Value.First());
    }

    [Fact]
    public async Task AsyncValidation_HandlesExceptions()
    {
        // Arrange
        var documentType = new TestValidatableTypeInfo(
            typeof(Document),
            [
                CreatePropertyInfo(typeof(Document), typeof(string), "Content", "Content",
                    [new ThrowingAsyncValidationAttribute()])
            ]);

        var document = new Document { Content = "test" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Document), documentType }
            }),
            ValidationContext = new ValidationContext(document)
        };

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => documentType.ValidateAsync(document, context, default));

        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0);
        Assert.Contains("Async validation failed", ex.Message);
    }

    [Fact]
    public async Task AsyncValidation_ShortCircuitsOnPropertyError()
    {
        // Arrange
        var validated = false;
        var entityType = new TestValidatableTypeInfo(
            typeof(EntityWithValidation),
            [
                CreatePropertyInfo(typeof(EntityWithValidation), typeof(string), "Name", "Name",
                    [new RequiredAttribute { ErrorMessage = "Name required" }])
            ],
            [new TrackingTypeLevelAttribute(() => validated = true)]);

        var entity = new EntityWithValidation { Name = null };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(EntityWithValidation), entityType }
            }),
            ValidationContext = new ValidationContext(entity)
        };

        // Act
        await entityType.ValidateAsync(entity, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.False(validated); // Type-level validation should not run due to property error
    }

    [Fact]
    public async Task AsyncValidation_PropertyWithAsyncFailure_ShortCircuitsTypeLevelAttribute()
    {
        // A failing member (here a property with a failing async attribute) short-circuits
        // object-level validation: the type-level attribute must not run.
        var typeLevelValidated = false;
        var entityType = new TestValidatableTypeInfo(
            typeof(UserWithAsyncValidation),
            [
                CreatePropertyInfo(typeof(UserWithAsyncValidation), typeof(string), "Email", "Email",
                    [new EmailExistsAttribute()])
            ],
            [new TrackingTypeLevelAttribute(() => typeLevelValidated = true)]);

        var user = new UserWithAsyncValidation { Email = "duplicate@example.com" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(UserWithAsyncValidation), entityType }
            }),
            ValidationContext = new ValidationContext(user)
        };

        await entityType.ValidateAsync(user, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Contains("Email", context.ValidationErrors.Keys);
        Assert.False(typeLevelValidated);
    }

    [Fact]
    public async Task AsyncValidation_WithDelayedValidation()
    {
        // Arrange
        var recordType = new TestValidatableTypeInfo(
            typeof(Record),
            [
                CreatePropertyInfo(typeof(Record), typeof(string), "Value", "Value",
                    [
                        new DelayedAsyncValidationAttribute { ShouldFail = false },
                        new DelayedAsyncValidationAttribute { ShouldFail = true, ErrorMessage = "Delayed validation failed" }
                    ])
            ]);

        var record = new Record { Value = "test" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Record), recordType }
            }),
            ValidationContext = new ValidationContext(record)
        };

        // Act
        await recordType.ValidateAsync(record, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Value", error.Key);
        Assert.Equal("Delayed validation failed", error.Value.First());
    }

    [Fact]
    public async Task AsyncValidation_MultipleAttributesOnSameProperty_ShortCircuitsAfterFirstError()
    {
        // Arrange
        // The second DelayedAsyncValidationAttribute waits on a signal that is only set when the
        // first validation error is reached, allowing the second validation to proceed. By then the
        // first error has short-circuited validation and cancelled the sibling's (linked)
        // cancellation token, so the second attribute is cancelled and its error is NOT collected.
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recordType = new TestValidatableTypeInfo(
            typeof(Record),
            [
                CreatePropertyInfo(typeof(Record), typeof(string), "Value", "Value",
                    [
                        new DelayedAsyncValidationAttribute { ShouldFail = true, ErrorMessage = "First async error" },
                        new DelayedAsyncValidationAttribute(signal.Task) { ShouldFail = true, ErrorMessage = "Second async error" }
                    ])
            ]);

        var record = new Record { Value = "test" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Record), recordType }
            }),
            ValidationContext = new ValidationContext(record)
        };

        context.OnValidationError += context =>
        {
            if (context.Errors.Count == 1 && context.Errors[0] == "First async error")
            {
                signal.TrySetResult();
            }
        };

        // Act
        await recordType.ValidateAsync(record, context, default);

        // Assert - the first error short-circuits the sibling async attribute on the same property.
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Value", error.Key);
        var message = Assert.Single(error.Value);
        Assert.Equal("First async error", message);
    }

    [Fact]
    public async Task AsyncValidation_MultipleAttributesOnSameProperty_RunInParallel()
    {
        // Two async attributes on the same property must run in parallel. The shared barrier only
        // releases once BOTH attributes have entered validation; if they run sequentially the first
        // blocks forever waiting for the second, the timeout cancels validation, and this test fails.
        const int attributeCount = 2;
        var allEnteredSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var barrier = new ParallelBarrierAsyncValidationAttribute(allEnteredSignal, attributeCount);

        var recordType = new TestValidatableTypeInfo(
            typeof(Record),
            [
                // The same barrier instance is applied twice, so it must be entered concurrently.
                CreatePropertyInfo(typeof(Record), typeof(string), "Value", "Value", [barrier, barrier])
            ]);

        var record = new Record { Value = "VALID" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Record), recordType }
            }),
            ValidationContext = new ValidationContext(record)
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await recordType.ValidateAsync(record, context, cts.Token);

        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0);
    }

    [Fact]
    public async Task MultipleAsyncAttributesOnSameProperty_FailingConcurrently_RecordAllErrors()
    {
        // Two async attributes on one property fail concurrently. A shared barrier forces both to be
        // in-flight simultaneously, so both errors are written to the same context error collection
        // (same key) at the same time. This exercises the concurrent write path; a non-thread-safe
        // error collection would intermittently lose a write (or throw). Stressed over many iterations
        // because data races are non-deterministic.
        const int attributeCount = 100;
        const int iterations = 1000;

        for (var i = 0; i < iterations; i++)
        {
            var allEnteredSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var barrier = new ConcurrentFailingBarrierAttribute(allEnteredSignal, attributeCount);

            var propertyAttributes = new ValidationAttribute[attributeCount];
            Array.Fill(propertyAttributes, barrier);

            var recordType = new TestValidatableTypeInfo(
                typeof(Record),
                [
                    // Same barrier instance applied twice: both invocations must be in-flight together,
                    // then both fail and write to the "Value" entry concurrently.
                    CreatePropertyInfo(typeof(Record), typeof(string), "Value", "Value", propertyAttributes)
                ]);

            var record = new Record { Value = "DUPLICATE" }; // makes both attributes fail
            var context = new ValidateContext
            {
                ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
                {
                    { typeof(Record), recordType }
                }),
                ValidationContext = new ValidationContext(record)
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await recordType.ValidateAsync(record, context, cts.Token);

            Assert.NotNull(context.ValidationErrors);
            Assert.True(context.ValidationErrors.ContainsKey("Value"));
            Assert.Equal(attributeCount, context.ValidationErrors["Value"].Count());
        }
    }

    [Fact]
    public async Task AsyncValidation_DeepNestedObjects_ValidateInParallel()
    {
        // Arrange
        // Build a three-level object graph (DeepRoot -> DeepBranch -> DeepInner -> DeepLeaf)
        // with three branches. Each leaf carries the same async validator instance, gated
        // by a CountdownEvent that requires every leaf validator to be in-flight at the
        // same time before any of them can complete. If deep validation ran sequentially,
        // only the first validator would reach the gate and the others would never signal,
        // causing the WaitAsync inside the gate to time out and surface a validation error.
        const int ExpectedParallelValidators = 3;

        using var startedLatch = new CountdownEvent(ExpectedParallelValidators);
        var allStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new ParallelGateAsyncAttribute(ExpectedParallelValidators, startedLatch, allStartedTcs);

        var leafType = new TestValidatableTypeInfo(
            typeof(DeepLeaf),
            [
                CreatePropertyInfo(typeof(DeepLeaf), typeof(string), "Value", "Value", [gate])
            ]);

        var innerType = new TestValidatableTypeInfo(
            typeof(DeepInner),
            [
                CreatePropertyInfo(typeof(DeepInner), typeof(DeepLeaf), "Leaf", "Leaf", [])
            ]);

        var branchType = new TestValidatableTypeInfo(
            typeof(DeepBranch),
            [
                CreatePropertyInfo(typeof(DeepBranch), typeof(DeepInner), "Inner", "Inner", [])
            ]);

        var rootType = new TestValidatableTypeInfo(
            typeof(DeepRoot),
            [
                CreatePropertyInfo(typeof(DeepRoot), typeof(DeepBranch), "BranchA", "BranchA", []),
                CreatePropertyInfo(typeof(DeepRoot), typeof(DeepBranch), "BranchB", "BranchB", []),
                CreatePropertyInfo(typeof(DeepRoot), typeof(DeepBranch), "BranchC", "BranchC", [])
            ]);

        var root = new DeepRoot
        {
            BranchA = new DeepBranch { Inner = new DeepInner { Leaf = new DeepLeaf { Value = "a" } } },
            BranchB = new DeepBranch { Inner = new DeepInner { Leaf = new DeepLeaf { Value = "b" } } },
            BranchC = new DeepBranch { Inner = new DeepInner { Leaf = new DeepLeaf { Value = "c" } } }
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(DeepRoot), rootType },
                { typeof(DeepBranch), branchType },
                { typeof(DeepInner), innerType },
                { typeof(DeepLeaf), leafType }
            }),
            ValidationContext = new ValidationContext(root)
        };

        // Act
        await rootType.ValidateAsync(root, context, default);

        // Assert
        Assert.Equal(0, startedLatch.CurrentCount);
        Assert.True(allStartedTcs.Task.IsCompletedSuccessfully);
        Assert.True(
            context.ValidationErrors is null || context.ValidationErrors.Count == 0,
            "Deep validators did not run in parallel: " + string.Join(
                "; ",
                (context.ValidationErrors ?? (IReadOnlyDictionary<string, IEnumerable<string>>)new Dictionary<string, IEnumerable<string>>())
                    .SelectMany(e => e.Value.Select(v => $"{e.Key}: {v}"))));
    }

    [Fact]
    public async Task IAsyncValidatableObject_DoesNotRunInParallelWithPropertyValidation()
    {
        // Arrange
        // Track when property async validation completes and when IAsyncValidatableObject starts.
        // If they ran in parallel, the property validator (which has a delay) would not have
        // completed by the time IAsyncValidatableObject.ValidateAsync begins.
        var propertyValidationCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var propertyValidationWasCompleteWhenObjectValidationStarted = false;

        var model = new ParallelTrackingModel(
            onObjectValidateStarted: () =>
            {
                propertyValidationWasCompleteWhenObjectValidationStarted = propertyValidationCompleted.Task.IsCompletedSuccessfully;
            })
        {
            Name = "test"
        };

        var modelType = new TestValidatableTypeInfo(
            typeof(ParallelTrackingModel),
            [
                CreatePropertyInfo(typeof(ParallelTrackingModel), typeof(string), "Name", "Name",
                    [new CompletionTrackingAsyncAttribute(propertyValidationCompleted)])
            ]);

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(ParallelTrackingModel), modelType }
            }),
            ValidationContext = new ValidationContext(model)
        };

        // Act
        await modelType.ValidateAsync(model, context, default);

        // Assert - IAsyncValidatableObject.ValidateAsync starts only after property validation completes
        Assert.True(propertyValidationCompleted.Task.IsCompletedSuccessfully);
        Assert.True(propertyValidationWasCompleteWhenObjectValidationStarted,
            "IAsyncValidatableObject.ValidateAsync should not run in parallel with property validation; " +
            "it should wait for all property validation to complete first.");
    }

    [Fact]
    public async Task ValidateMembers_PropertyGetterThrows_ExceptionShouldPropagate()
    {
        var typeInfo = new TestValidatableTypeInfo(
            typeof(TypeWithThrowingGetter),
            [
                CreatePropertyInfo(typeof(TypeWithThrowingGetter), typeof(string), "ThrowingProp", "Throwing Prop",
                    [new RequiredAttribute()])
            ]);

        var instance = new TypeWithThrowingGetter();
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(TypeWithThrowingGetter), typeInfo }
            }),
            ValidationContext = new ValidationContext(instance)
        };

        var ex = await Assert.ThrowsAsync<System.Reflection.TargetInvocationException>(
            () => typeInfo.ValidateAsync(instance, context, default));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("Getter throws", ex.InnerException.Message);
    }

    [Fact]
    public async Task AsyncValidation_OnParameterCollection_AwaitsAsyncValidatorsOnItems()
    {
        // Arrange
        var completionTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var itemType = new TestValidatableTypeInfo(
            typeof(ItemWithAsyncValidation),
            [
                CreatePropertyInfo(typeof(ItemWithAsyncValidation), typeof(string), "Code", "Code",
                    [new CompletionTrackingAsyncAttribute(completionTcs) { ErrorMessage = "async error" }])
            ]);

        var paramInfo = new TestValidatableParameterInfo(
            parameterType: typeof(IEnumerable<ItemWithAsyncValidation>),
            name: "items",
            displayNameInfo: new TestLiteralDisplayName("Items"),
            validationAttributes: []);

        var items = new List<ItemWithAsyncValidation>
        {
            new() { Code = "test" }
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(ItemWithAsyncValidation), itemType }
            }),
            ValidationContext = new ValidationContext(items)
        };

        // Act
        await paramInfo.ValidateAsync(items, context, default);

        // Assert — the async validator must have been awaited before ValidateAsync returned
        Assert.True(completionTcs.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task TypeLevelAttributeValidation_RunsOnlyAfterMemberValidationCompletes()
    {
        // Type-level attribute validation must not run in parallel with member validation; all
        // member tasks must complete first. (Mirrors
        // IAsyncValidatableObject_DoesNotRunInParallelWithPropertyValidation for type-level attributes.)
        var propertyValidationCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var propertyWasCompleteWhenTypeLevelRan = false;

        var modelType = new TestValidatableTypeInfo(
            typeof(Record),
            [
                CreatePropertyInfo(typeof(Record), typeof(string), "Value", "Value",
                    [new CompletionTrackingAsyncAttribute(propertyValidationCompleted)])
            ],
            [new TrackingTypeLevelAttribute(
                () => propertyWasCompleteWhenTypeLevelRan = propertyValidationCompleted.Task.IsCompletedSuccessfully)]);

        var model = new Record { Value = "test" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Record), modelType }
            }),
            ValidationContext = new ValidationContext(model)
        };

        await modelType.ValidateAsync(model, context, default);

        Assert.True(propertyValidationCompleted.Task.IsCompletedSuccessfully);
        Assert.True(
            propertyWasCompleteWhenTypeLevelRan,
            "Type-level attribute validation should run only after all member validation completes.");
    }

    [Fact]
    public async Task SiblingSubtreeError_DoesNotSuppress_UnrelatedTypesValidatableObject()
    {
        // Regression test for cross-subtree contamination of the early-return error check.
        // ValidatableTypeInfo.ValidateAsync captures `originalErrorCount` per invocation but compares
        // it against the GLOBAL, shared ValidationErrors dictionary. When two sibling subtrees are
        // validated in parallel, an error produced by one subtree inflates the global count seen by
        // the other, causing the unrelated subtree to wrongly skip its own IValidatableObject
        // validation even though none of *its* members failed.
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstReadDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReadDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // The first sibling's member fails; the second sibling's member passes.
        var firstMember = new GatedContextCapturingAsyncAttribute(firstEntered, releaseFirst.Task, firstReadDone)
        {
            ReportFailure = true
        };
        var secondMember = new GatedContextCapturingAsyncAttribute(secondEntered, releaseSecond.Task, secondReadDone);

        var secondObjectValidateRan = false;

        var firstType = new TestValidatableTypeInfo(
            typeof(CrossTalkFirst),
            [CreatePropertyInfo(typeof(CrossTalkFirst), typeof(string), "Value", "Value", [firstMember])]);
        var secondType = new TestValidatableTypeInfo(
            typeof(CrossTalkSecond),
            [CreatePropertyInfo(typeof(CrossTalkSecond), typeof(string), "Value", "Value", [secondMember])]);
        var rootType = new TestValidatableTypeInfo(
            typeof(CrossTalkRoot),
            [
                CreatePropertyInfo(typeof(CrossTalkRoot), typeof(CrossTalkFirst), "First", "First", []),
                CreatePropertyInfo(typeof(CrossTalkRoot), typeof(CrossTalkSecond), "Second", "Second", [])
            ]);

        var model = new CrossTalkRoot
        {
            First = new CrossTalkFirst { Value = "a" },
            Second = new CrossTalkSecond
            {
                Value = "b",
                OnValidateCalled = () => secondObjectValidateRan = true
            }
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(CrossTalkRoot), rootType },
                { typeof(CrossTalkFirst), firstType },
                { typeof(CrossTalkSecond), secondType }
            }),
            ValidationContext = new ValidationContext(model)
        };

        var firstErrorRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        context.OnValidationError += errorContext =>
        {
            if (errorContext.Path == "First.Value")
            {
                firstErrorRecorded.TrySetResult();
            }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        var validateTask = rootType.ValidateAsync(model, context, cts.Token);

        // Both sibling subtrees have begun validating their members and are now suspended.
        await firstEntered.Task.WaitAsync(cts.Token);
        await secondEntered.Task.WaitAsync(cts.Token);

        // Let the first sibling fail and record its error into the shared dictionary.
        releaseFirst.SetResult();
        await firstErrorRecorded.Task.WaitAsync(cts.Token);

        // Now let the second sibling finish. Its own members produced no errors, so its
        // IValidatableObject validation must still run.
        releaseSecond.SetResult();
        await validateTask;

        Assert.True(
            secondObjectValidateRan,
            "CrossTalkSecond has no member-level errors, so its IValidatableObject.Validate must run; " +
            "a sibling subtree's error must not short-circuit it via the shared error count.");
    }

    [Fact]
    public async Task CollectionItemError_DoesNotSuppress_SiblingItemsValidatableObject()
    {
        // Same root cause as SiblingSubtreeError_* but via the collection/enumerable parallel path in
        // ValidatablePropertyInfo (item[0] uses the shared context, item[1..] use clones). A failing
        // item must not suppress a sibling item's IValidatableObject validation through the shared,
        // global ValidationErrors count.
        var failingEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFailing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var passingEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePassing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var itemMember = new CollectionItemGatedAsyncAttribute(
            failingEntered, releaseFailing.Task, passingEntered, releasePassing.Task);

        var itemType = new TestValidatableTypeInfo(
            typeof(CrossTalkItem),
            [CreatePropertyInfo(typeof(CrossTalkItem), typeof(string), "Value", "Value", [itemMember])]);
        var listType = new TestValidatableTypeInfo(
            typeof(CrossTalkItemList),
            [CreatePropertyInfo(typeof(CrossTalkItemList), typeof(List<CrossTalkItem>), "Items", "Items", [])]);

        var passingItemValidateRan = false;
        var list = new CrossTalkItemList
        {
            Items =
            [
                new CrossTalkItem { Value = "fail" },
                new CrossTalkItem { Value = "pass", OnValidateCalled = () => passingItemValidateRan = true }
            ]
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(CrossTalkItemList), listType },
                { typeof(CrossTalkItem), itemType }
            }),
            ValidationContext = new ValidationContext(list)
        };

        var failingItemErrorRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        context.OnValidationError += errorContext =>
        {
            if (errorContext.Path == "Items[0].Value")
            {
                failingItemErrorRecorded.TrySetResult();
            }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        var validateTask = listType.ValidateAsync(list, context, cts.Token);

        // Both collection items are validating their members in parallel and are suspended.
        await failingEntered.Task.WaitAsync(cts.Token);
        await passingEntered.Task.WaitAsync(cts.Token);

        // Let the first item fail and record its error into the shared dictionary.
        releaseFailing.SetResult();
        await failingItemErrorRecorded.Task.WaitAsync(cts.Token);

        // Now let the valid sibling item finish; its IValidatableObject must still run.
        releasePassing.SetResult();
        await validateTask;

        Assert.True(
            passingItemValidateRan,
            "The valid collection item has no member-level errors, so its IValidatableObject.Validate must run; " +
            "a sibling item's error must not short-circuit it via the shared error count.");
    }

    [Fact]
    public async Task FirstMemberSubtree_NotSuppressed_ByLaterSiblingError()
    {
        // Variant of the cross-talk bug targeting member[0], which is validated on the *shared*
        // parent context (no clone). A later sibling (member[1], validated on a clone) records an
        // error that propagates UP into the shared parent counter via _parentContext. The first
        // member's subtree reads that same counter, so it must not be fooled into short-circuiting
        // its own IValidatableObject validation.
        var victimEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseVictim = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var victimReadDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var badEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBad = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var badReadDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var victimMember = new GatedContextCapturingAsyncAttribute(victimEntered, releaseVictim.Task, victimReadDone);
        var badMember = new GatedContextCapturingAsyncAttribute(badEntered, releaseBad.Task, badReadDone)
        {
            ReportFailure = true
        };

        var victimType = new TestValidatableTypeInfo(
            typeof(VictimChild),
            [CreatePropertyInfo(typeof(VictimChild), typeof(string), "Value", "Value", [victimMember])]);
        var badType = new TestValidatableTypeInfo(
            typeof(BadChild),
            [CreatePropertyInfo(typeof(BadChild), typeof(string), "Value", "Value", [badMember])]);

        // Victim is member[0] (validated on the shared context); Bad is member[1] (validated on a clone).
        var rootType = new TestValidatableTypeInfo(
            typeof(FirstMemberVictimRoot),
            [
                CreatePropertyInfo(typeof(FirstMemberVictimRoot), typeof(VictimChild), "Victim", "Victim", []),
                CreatePropertyInfo(typeof(FirstMemberVictimRoot), typeof(BadChild), "Bad", "Bad", [])
            ]);

        var victimValidateRan = false;
        var model = new FirstMemberVictimRoot
        {
            Victim = new VictimChild { Value = "v", OnValidateCalled = () => victimValidateRan = true },
            Bad = new BadChild { Value = "b" }
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(FirstMemberVictimRoot), rootType },
                { typeof(VictimChild), victimType },
                { typeof(BadChild), badType }
            }),
            ValidationContext = new ValidationContext(model)
        };

        var badErrorRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        context.OnValidationError += errorContext =>
        {
            if (errorContext.Path == "Bad.Value")
            {
                badErrorRecorded.TrySetResult();
            }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        var validateTask = rootType.ValidateAsync(model, context, cts.Token);

        await victimEntered.Task.WaitAsync(cts.Token);
        await badEntered.Task.WaitAsync(cts.Token);

        // Let the later sibling fail first and record its error (which propagates up the parent chain).
        releaseBad.SetResult();
        await badErrorRecorded.Task.WaitAsync(cts.Token);

        // Now let the first member finish; its own members produced no errors, so its
        // IValidatableObject must still run.
        releaseVictim.SetResult();
        await validateTask;

        Assert.True(
            victimValidateRan,
            "VictimChild (member[0]) has no member-level errors, so its IValidatableObject.Validate must run; " +
            "a later sibling's error propagated into the shared parent counter must not short-circuit it.");
    }

    [Fact]
    public async Task MixedSyncAsyncMembers_DoNotReuseAParkedContext()
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstReadDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstAttr = new GatedContextCapturingAsyncAttribute(firstEntered, releaseFirst.Task, firstReadDone);

        var modelType = new TestValidatableTypeInfo(
            typeof(MixedSyncAsyncModel),
            [
                // member[0]: async -> parks on the original (shared) context.
                CreatePropertyInfo(typeof(MixedSyncAsyncModel), typeof(string), "First", "First", [firstAttr]),
                // member[1]: synchronous -> validated on a clone (member[0] already went async).
                CreatePropertyInfo(typeof(MixedSyncAsyncModel), typeof(string), "Second", "Second", []),
                // member[2]: synchronous -> must NOT reuse the parked original context.
                CreatePropertyInfo(typeof(MixedSyncAsyncModel), typeof(string), "Third", "Third", [])
            ]);

        var model = new MixedSyncAsyncModel { First = "a", Second = "b", Third = "c" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(MixedSyncAsyncModel), modelType }
            }),
            ValidationContext = new ValidationContext(model)
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act - member[0] parks; member[1] and member[2] run synchronously during the loop.
        var validateTask = modelType.ValidateAsync(model, context, cts.Token);

        await firstEntered.Task.WaitAsync(cts.Token);
        releaseFirst.SetResult();
        await firstReadDone.Task.WaitAsync(cts.Token);
        await validateTask;

        // member[0]'s context must still reflect "First"; a later synchronous member reusing/mutating
        // the parked context would leave it showing "Third".
        Assert.Equal("First", firstAttr.CapturedMemberName);
        Assert.Equal("First", firstAttr.CapturedDisplayName);
    }

    [Fact]
    public async Task PropertyErrorOnClonedMember_StillShortCircuitsTypeLevelValidation()
    {
        // The property-error short-circuit (skip type-level/IValidatableObject when a member fails)
        // must work regardless of which member fails. After member[0] goes async, member[1] is
        // validated on a CLONED context. If the parent only inspects its own context's error count
        // (no roll-up from clones), member[1]'s failure becomes invisible and type-level validation
        // wrongly runs. This mirrors AsyncValidation_ShortCircuitsOnPropertyError but with the
        // failing property as a (cloned) non-first member.
        var releaseAsyncMember = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var typeLevelRan = false;
        var modelType = new TestValidatableTypeInfo(
            typeof(ShortCircuitModel),
            [
                // member[0]: async + passing -> forces member[1] onto a cloned context.
                CreatePropertyInfo(typeof(ShortCircuitModel), typeof(string), "AsyncOk", "AsyncOk",
                    [new GatedSuccessAsyncAttribute(releaseAsyncMember.Task)]),
                // member[1]: synchronously fails Required (validated on a clone).
                CreatePropertyInfo(typeof(ShortCircuitModel), typeof(string), "BadName", "BadName",
                    [new RequiredAttribute { ErrorMessage = "BadName is required" }])
            ],
            [new TrackingTypeLevelAttribute(() => typeLevelRan = true)]);

        var model = new ShortCircuitModel { AsyncOk = "ok", BadName = null };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(ShortCircuitModel), modelType }
            }),
            ValidationContext = new ValidationContext(model)
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        var validateTask = modelType.ValidateAsync(model, context, cts.Token);
        releaseAsyncMember.SetResult();
        await validateTask;

        // Assert - BadName (a cloned member) failed, so type-level validation must be skipped.
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("BadName"));
        Assert.False(
            typeLevelRan,
            "A failing property must short-circuit type-level validation even when that property is " +
            "validated on a cloned member context.");
    }

    // Test model classes
    private class ShortCircuitModel
    {
        public string? AsyncOk { get; set; }
        public string? BadName { get; set; }
    }

    private class FirstMemberVictimRoot
    {
        public VictimChild? Victim { get; set; }
        public BadChild? Bad { get; set; }
    }

    private class VictimChild : IValidatableObject
    {
        public string? Value { get; set; }

        public Action? OnValidateCalled { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            OnValidateCalled?.Invoke();
            return Array.Empty<ValidationResult>();
        }
    }

    private class BadChild
    {
        public string? Value { get; set; }
    }

    private class MixedSyncAsyncModel
    {
        public string? First { get; set; }
        public string? Second { get; set; }
        public string? Third { get; set; }
    }

    private class CrossTalkRoot
    {
        public CrossTalkFirst? First { get; set; }
        public CrossTalkSecond? Second { get; set; }
    }

    private class CrossTalkFirst
    {
        public string? Value { get; set; }
    }

    private class CrossTalkSecond : IValidatableObject
    {
        public string? Value { get; set; }

        public Action? OnValidateCalled { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            OnValidateCalled?.Invoke();
            return Array.Empty<ValidationResult>();
        }
    }

    private class CrossTalkItemList
    {
        public List<CrossTalkItem>? Items { get; set; }
    }

    private class CrossTalkItem : IValidatableObject
    {
        public string? Value { get; set; }

        public Action? OnValidateCalled { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            OnValidateCalled?.Invoke();
            return Array.Empty<ValidationResult>();
        }
    }

    private class TypeWithThrowingGetter
    {
        public string ThrowingProp => throw new InvalidOperationException("Getter throws");
    }

    private class UserWithAsyncValidation
    {
        public string? Email { get; set; }
    }

    private class Product
    {
        public string? Name { get; set; }
        public string? SKU { get; set; }
    }

    private class OrderWithTracking
    {
        public string? Field1 { get; set; }
        public string? Field2 { get; set; }
        public string? Field3 { get; set; }
        public string? Field4 { get; set; }
    }

    private class AsyncValidatableAccount : IAsyncValidatableObject
    {
        public string? Username { get; set; }
        public string? Email { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (Username == "taken")
            {
                yield return new ValidationResult("Username is already taken", [nameof(Username)]);
            }

            if (Email == "duplicate@example.com")
            {
                yield return new ValidationResult("Email is already registered", [nameof(Email)]);
            }
        }
    }

    private class RegistrationForm : IAsyncValidatableObject
    {
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (Password != ConfirmPassword)
            {
                yield return new ValidationResult(
                    "Passwords do not match",
                    [nameof(Password), nameof(ConfirmPassword)]);
            }
        }
    }

    private class AddressWithAsyncValidation
    {
        public string? ZipCode { get; set; }
    }

    private class CustomerWithAsyncAddress
    {
        public string? Name { get; set; }
        public AddressWithAsyncValidation? Address { get; set; }
    }

    private class CustomerWithAsyncProfile
    {
        public string? Name { get; set; }
        public AsyncValidatableProfile? Profile { get; set; }
    }

    private class AsyncValidatableProfile : IAsyncValidatableObject
    {
        public string? Bio { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            => Array.Empty<ValidationResult>();

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (Bio is null || Bio.Length < 10)
            {
                yield return new ValidationResult("Bio must be at least 10 characters", [nameof(Bio)]);
            }
        }
    }

    private class CustomerWithGatedAsyncProfile
    {
        public string? Name { get; set; }

        public GatedAsyncValidatableProfile? Profile { get; set; }
    }

    private class GatedAsyncValidatableProfile : IAsyncValidatableObject
    {
        public Task Gate { get; set; } = Task.CompletedTask;

        public string? Bio { get; set; }

        public string? CapturedMemberName { get; private set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            => Array.Empty<ValidationResult>();

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Gate.WaitAsync(cancellationToken);

            // Capture the MemberName after resuming. When validated on an isolated (cloned) context
            // this reflects only this object's validation; on a shared context it leaks sibling state.
            CapturedMemberName = validationContext.MemberName;

            yield return new ValidationResult("Bio must be at least 10 characters", [nameof(Bio)]);
        }
    }

    private class Document
    {
        public string? Content { get; set; }
    }

    private class TwoStringModel
    {
        public string? First { get; set; }
        public string? Second { get; set; }
    }

    private class GatedInner
    {
        public string? Value { get; set; }
    }

    private class ComplexThenStringModel
    {
        public GatedInner? Inner { get; set; }
        public string? Title { get; set; }
    }

    private class UserProfile : IAsyncValidatableObject
    {
        public string? Username { get; set; }
        public string? Bio { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (Bio is not null && Bio.Length < 10)
            {
                yield return new ValidationResult("Bio must be at least 10 characters", [nameof(Bio)]);
            }
        }
    }

    private class OrderWithTypeValidation
    {
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    private class ItemWithAsyncValidation
    {
        public string? Code { get; set; }
    }

    private class ItemList
    {
        public List<ItemWithAsyncValidation>? Items { get; set; }
    }

    private class EntityWithValidation : IValidatableObject
    {
        public string? Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }
    }

    private class Record
    {
        public string? Value { get; set; }
    }

    private class DeepRoot
    {
        public DeepBranch? BranchA { get; set; }
        public DeepBranch? BranchB { get; set; }
        public DeepBranch? BranchC { get; set; }
    }

    private class DeepBranch
    {
        public DeepInner? Inner { get; set; }
    }

    private class DeepInner
    {
        public DeepLeaf? Leaf { get; set; }
    }

    private class DeepLeaf
    {
        public string? Value { get; set; }
    }

    private class ParallelTrackingModel : IAsyncValidatableObject
    {
        private readonly Action _onObjectValidateStarted;

        public ParallelTrackingModel(Action onObjectValidateStarted)
        {
            _onObjectValidateStarted = onObjectValidateStarted;
        }

        public string? Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Array.Empty<ValidationResult>();
        }

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _onObjectValidateStarted();
            await Task.CompletedTask;
            yield break;
        }
    }

    // Test validation attributes
    private class EmailExistsAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (value is string email && email == "duplicate@example.com")
            {
                return new ValidationResult("Email already exists");
            }

            return ValidationResult.Success;
        }
    }

    private class SkuValidationAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (value is string sku && sku.Contains("DUPLICATE"))
            {
                return new ValidationResult(ErrorMessage ?? "SKU already exists");
            }

            return ValidationResult.Success;
        }
    }

    private class TrackingAsyncAttribute : AsyncValidationAttribute
    {
        private readonly ConcurrentQueue<string> _validationOrder;
        private readonly string _name;

        public TrackingAsyncAttribute(ConcurrentQueue<string> validationOrder, string name)
        {
            _validationOrder = validationOrder;
            _name = name;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            _validationOrder.Enqueue(_name);
            return ValidationResult.Success;
        }
    }

    private class TrackingSyncAttribute : ValidationAttribute
    {
        private readonly ConcurrentQueue<string> _validationOrder;
        private readonly string _name;

        public TrackingSyncAttribute(ConcurrentQueue<string> validationOrder, string name)
        {
            _validationOrder = validationOrder;
            _name = name;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            _validationOrder.Enqueue(_name);
            return ValidationResult.Success;
        }
    }

    private class ZipCodeExistsAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (value is string zip && zip == "INVALID")
            {
                return new ValidationResult(ErrorMessage ?? "Invalid zip code");
            }

            return ValidationResult.Success;
        }
    }

    private class SlowAsyncValidationAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            // Block until cancellation is requested (deterministic; no fixed delay).
            await new TaskCompletionSource().Task.WaitAsync(cancellationToken);
            return ValidationResult.Success;
        }
    }

    private class UsernameAvailableAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (value is string username && username == "taken")
            {
                return new ValidationResult(ErrorMessage ?? "Username is taken");
            }

            return ValidationResult.Success;
        }
    }

    private class OrderTotalValidationAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (value is OrderWithTypeValidation order)
            {
                if (order.Total != order.SubTotal + order.Tax)
                {
                    return new ValidationResult("Total does not match SubTotal + Tax", [nameof(OrderWithTypeValidation.Total)]);
                }
            }

            return ValidationResult.Success;
        }
    }

    private class ItemCodeExistsAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (value is string code && code == "DUPLICATE")
            {
                return new ValidationResult(ErrorMessage ?? "Item code exists");
            }

            return ValidationResult.Success;
        }
    }

    private class ParallelBarrierAsyncValidationAttribute : AsyncValidationAttribute
    {
        private readonly TaskCompletionSource _allEnteredSignal;
        private readonly int _expectedCount;
        private int _enteredCount;

        public ParallelBarrierAsyncValidationAttribute(
            TaskCompletionSource allEnteredSignal,
            int expectedCount)
        {
            _allEnteredSignal = allEnteredSignal;
            _expectedCount = expectedCount;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            // Signal that this item has entered validation.
            // When all items have entered, signal completion.
            if (Interlocked.Increment(ref _enteredCount) == _expectedCount)
            {
                _allEnteredSignal.SetResult();
            }

            // Wait until all items have entered - this will deadlock/timeout if run sequentially.
            await _allEnteredSignal.Task.WaitAsync(cancellationToken);

            if (value is string code && code == "DUPLICATE")
            {
                return new ValidationResult(ErrorMessage ?? "Item code exists");
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Barrier attribute that waits until every expected invocation is in-flight and then fails.
    /// It waits with a timeout (not the cancellation token), so the short-circuit cancellation from a
    /// sibling's first failure does NOT cancel it - forcing every failure to be reported concurrently
    /// to the same error key, which exercises the concurrent error-collection write path.
    /// </summary>
    private sealed class ConcurrentFailingBarrierAttribute : AsyncValidationAttribute
    {
        private readonly TaskCompletionSource _allEnteredSignal;
        private readonly int _expectedCount;
        private int _enteredCount;

        public ConcurrentFailingBarrierAttribute(TaskCompletionSource allEnteredSignal, int expectedCount)
        {
            _allEnteredSignal = allEnteredSignal;
            _expectedCount = expectedCount;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _enteredCount) == _expectedCount)
            {
                _allEnteredSignal.SetResult();
            }

            await _allEnteredSignal.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return new ValidationResult("Concurrent error");
        }
    }

    private class ThrowingAsyncValidationAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("Async validation failed");
        }
    }

    private class TrackingTypeLevelAttribute : ValidationAttribute
    {
        private readonly Action _callback;

        public TrackingTypeLevelAttribute(Action callback)
        {
            _callback = callback;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            _callback();
            return ValidationResult.Success;
        }
    }

    private class DelayedAsyncValidationAttribute : AsyncValidationAttribute
    {
        private readonly Task? _signal;

        public DelayedAsyncValidationAttribute()
        {
        }

        public DelayedAsyncValidationAttribute(Task signal)
        {
            _signal = signal;
        }

        public bool ShouldFail { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            if (_signal is not null)
            {
                await _signal.WaitAsync(cancellationToken);

                // The sibling attribute's first error short-circuits validation by cancelling the
                // linked token. Block until that cancellation is observed so this attribute's error
                // is not collected (deterministic; no fixed delay).
                await new TaskCompletionSource().Task.WaitAsync(cancellationToken);
            }
            else
            {
                await Task.Yield();
            }

            if (ShouldFail)
            {
                return new ValidationResult(ErrorMessage ?? "Validation failed");
            }

            return ValidationResult.Success;
        }
    }

    private class ParallelGateAsyncAttribute : AsyncValidationAttribute
    {
        private readonly int _expected;
        private readonly CountdownEvent _startedLatch;
        private readonly TaskCompletionSource _allStartedTcs;

        public ParallelGateAsyncAttribute(int expected, CountdownEvent startedLatch, TaskCompletionSource allStartedTcs)
        {
            _expected = expected;
            _startedLatch = startedLatch;
            _allStartedTcs = allStartedTcs;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            // Signal that this validator has started. The last one to signal sets the TCS,
            // releasing every validator that is already waiting on it.
            if (_startedLatch.Signal())
            {
                _allStartedTcs.TrySetResult();
            }

            try
            {
                // Wait until every expected deep validator has also started. If deep
                // validation does not run in parallel, only the first validator reaches
                // this point and WaitAsync throws TimeoutException, which we translate
                // into a clear validation error.
                await _allStartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            catch (TimeoutException)
            {
                var started = _expected - _startedLatch.CurrentCount;
                return new ValidationResult(
                    $"Expected {_expected} deep validators to run in parallel, but only {started} started before the timeout elapsed.");
            }

            return ValidationResult.Success;
        }
    }

    private class CompletionTrackingAsyncAttribute : AsyncValidationAttribute
    {
        private readonly TaskCompletionSource _completionTcs;

        public CompletionTrackingAsyncAttribute(TaskCompletionSource completionTcs)
        {
            _completionTcs = completionTcs;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            _completionTcs.TrySetResult();
            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Async attribute that parks on an external gate before succeeding. Used to deterministically
    /// hold a member's validation in a suspended state (with the ValidateContext still mutated)
    /// while sibling members are validated.
    /// </summary>
    private class GatedSuccessAsyncAttribute : AsyncValidationAttribute
    {
        private readonly Task _gate;

        public GatedSuccessAsyncAttribute(Task gate)
        {
            _gate = gate;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Property-level async attribute that, after being released from an external gate, captures the
    /// MemberName/DisplayName it observes on the (shared) ValidationContext. Used to detect whether a
    /// concurrently-running type-level validation corrupted the in-flight property's context.
    /// </summary>
    private sealed class GatedContextCapturingAsyncAttribute : AsyncValidationAttribute
    {
        private readonly TaskCompletionSource _entered;
        private readonly Task _release;
        private readonly TaskCompletionSource _readDone;

        public GatedContextCapturingAsyncAttribute(TaskCompletionSource entered, Task release, TaskCompletionSource readDone)
        {
            _entered = entered;
            _release = release;
            _readDone = readDone;
        }

        public string? CapturedMemberName { get; private set; }

        public string? CapturedDisplayName { get; private set; }

        /// <summary>
        /// When <see langword="true"/>, the attribute reports a failure whose message embeds the
        /// display name observed on the context, surfacing context corruption as a user-visible error.
        /// </summary>
        public bool ReportFailure { get; init; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value, ValidationContext validationContext, CancellationToken cancellationToken)
        {
            _entered.SetResult();
            await _release.WaitAsync(cancellationToken);

            CapturedMemberName = validationContext.MemberName;
            CapturedDisplayName = validationContext.DisplayName;
            _readDone.TrySetResult();

            if (ReportFailure)
            {
                return new ValidationResult(
                    $"{validationContext.DisplayName} is invalid",
                    [validationContext.MemberName ?? string.Empty]);
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Async attribute for collection items that gates the "fail" and "pass" items independently
    /// (keyed off the value being validated), so a test can deterministically order a failing item's
    /// error before a sibling item completes.
    /// </summary>
    private sealed class CollectionItemGatedAsyncAttribute : AsyncValidationAttribute
    {
        private readonly TaskCompletionSource _failingEntered;
        private readonly Task _releaseFailing;
        private readonly TaskCompletionSource _passingEntered;
        private readonly Task _releasePassing;

        public CollectionItemGatedAsyncAttribute(
            TaskCompletionSource failingEntered,
            Task releaseFailing,
            TaskCompletionSource passingEntered,
            Task releasePassing)
        {
            _failingEntered = failingEntered;
            _releaseFailing = releaseFailing;
            _passingEntered = passingEntered;
            _releasePassing = releasePassing;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value, ValidationContext validationContext, CancellationToken cancellationToken)
        {
            if ((string?)value == "fail")
            {
                _failingEntered.SetResult();
                await _releaseFailing.WaitAsync(cancellationToken);
                return new ValidationResult("item failed", [validationContext.MemberName ?? string.Empty]);
            }

            _passingEntered.SetResult();
            await _releasePassing.WaitAsync(cancellationToken);
            return ValidationResult.Success;
        }
    }

    // Test helper methods
    private ValidatablePropertyInfo CreatePropertyInfo(
        Type containingType,
        Type propertyType,
        string name,
        string displayName,
        ValidationAttribute[] validationAttributes)
    {
        return new TestValidatablePropertyInfo(
            containingType,
            propertyType,
            name,
            displayName,
            validationAttributes);
    }

    private class TestValidatablePropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatablePropertyInfo(
            Type containingType,
            Type propertyType,
            string name,
            string displayName,
            ValidationAttribute[] validationAttributes)
            : base(containingType, propertyType, name, new TestLiteralDisplayName(displayName))
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableParameterInfo : ValidatableParameterInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatableParameterInfo(
            Type parameterType,
            string name,
            DisplayNameInfo? displayNameInfo,
            ValidationAttribute[] validationAttributes)
            : base(parameterType, name, displayNameInfo)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableTypeInfo : ValidatableTypeInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatableTypeInfo(
            Type type,
            IReadOnlyList<ValidatablePropertyInfo> members,
            ValidationAttribute[]? validationAttributes = null)
            : base(type, members)
        {
            _validationAttributes = validationAttributes ?? Array.Empty<ValidationAttribute>();
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestLiteralDisplayName : DisplayNameInfo
    {
        private readonly string _displayName;

        public TestLiteralDisplayName(string displayName)
        {
            _displayName = displayName;
        }

        public override string GetDisplayName(ValidateContext context, string memberName, Type? declaringType)
        {
            return _displayName;
        }
    }

    private class TestValidationOptions : ValidationOptions
    {
        public TestValidationOptions(Dictionary<Type, ValidatableTypeInfo> typeInfoMappings)
        {
            var resolver = new DictionaryBasedResolver(typeInfoMappings);
            Resolvers.Add(resolver);
        }

        private class DictionaryBasedResolver : IValidatableInfoResolver
        {
            private readonly Dictionary<Type, ValidatableTypeInfo> _typeInfoMappings;

            public DictionaryBasedResolver(Dictionary<Type, ValidatableTypeInfo> typeInfoMappings)
            {
                _typeInfoMappings = typeInfoMappings;
            }

            public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo? validatableTypeInfo)
            {
                if (_typeInfoMappings.TryGetValue(type, out var info))
                {
                    validatableTypeInfo = info;
                    return true;
                }
                validatableTypeInfo = null;
                return false;
            }

            public bool TryGetValidatableParameterInfo(System.Reflection.ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo? validatableParameterInfo)
            {
                validatableParameterInfo = null;
                return false;
            }
        }
    }
}
