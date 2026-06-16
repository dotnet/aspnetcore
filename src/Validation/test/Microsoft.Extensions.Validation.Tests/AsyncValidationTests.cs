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
    public async Task AsyncValidation_PropertyWithAsyncFailure_CanRunInParallelWithTypeLevelAttribute()
    {
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
        Assert.True(typeLevelValidated);
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
                        new DelayedAsyncValidationAttribute(50) { ShouldFail = false },
                        new DelayedAsyncValidationAttribute(100) { ShouldFail = true, ErrorMessage = "Delayed validation failed" }
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
    public async Task AsyncValidation_MultipleAttributesOnSameProperty_CollectsAllErrors()
    {
        // Arrange
        // The second DelayedAsyncValidationAttribute is going to wait until the semaphore can be entered.
        // The semaphore initially can't be entered (initialCount = 0).
        // Only when the first validation error is reached, it will release the semaphore, allowing
        // the second validation to proceed and report its error as well.
        // This ensures that we get the event as soon as validation results are available.
        using var signal = new SemaphoreSlim(0, 1);
        var recordType = new TestValidatableTypeInfo(
            typeof(Record),
            [
                CreatePropertyInfo(typeof(Record), typeof(string), "Value", "Value",
                    [
                        new DelayedAsyncValidationAttribute(10) { ShouldFail = true, ErrorMessage = "First async error" },
                        new DelayedAsyncValidationAttribute(10, signal) { ShouldFail = true, ErrorMessage = "Second async error" }
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
                signal.Release();
            }
        };

        // Act
        await recordType.ValidateAsync(record, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Value", error.Key);
        var errors = error.Value.ToArray();
        Assert.Equal(2, errors.Length);
        Assert.Contains("First async error", errors);
        Assert.Contains("Second async error", errors);
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
    public async Task AsyncAttributeOnProperty_WithComplexValue_SeesCorrectDisplayName()
    {
        using var typeValidationStarted = new SemaphoreSlim(0, 1);
        var capturedDisplayNames = new ConcurrentBag<string>();

        var innerType = new TestValidatableTypeInfo(
            typeof(InnerComplexType),
            [
                CreatePropertyInfo(typeof(InnerComplexType), typeof(string), "Data", "Data", [])
            ],
            [new SignalingAsyncTypeLevelAttribute(typeValidationStarted)]);

        // The outer type has a property "Inner" whose display name is "Shipping Address".
        // The property carries an async attribute that waits for the type-level validation
        // to start (ensuring DisplayName has been changed), then captures whatever
        // DisplayName it sees on the ValidationContext.
        var outerType = new TestValidatableTypeInfo(
            typeof(OuterWithComplexProp),
            [
                new TestValidatablePropertyInfo(
                    typeof(OuterWithComplexProp),
                    typeof(InnerComplexType),
                    "Inner",
                    "Shipping Address",
                    [new DisplayNameCapturingAsyncAttribute(typeValidationStarted, capturedDisplayNames)])
            ]);

        var instance = new OuterWithComplexProp
        {
            Inner = new InnerComplexType { Data = "test" }
        };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(OuterWithComplexProp), outerType },
                { typeof(InnerComplexType), innerType }
            }),
            ValidationContext = new ValidationContext(instance)
        };

        await outerType.ValidateAsync(instance, context, default);

        // The async attribute on the property should see the property's display name
        // ("Shipping Address"), not the type's display name ("InnerComplexType").
        var captured = Assert.Single(capturedDisplayNames);
        Assert.Equal("Shipping Address", captured);
    }

    [Fact]
    public async Task AsyncValidation_OnParameterCollection_AwaitsAsyncValidatorsOnItems()
    {
        // Arrange
        // Validates that Clone(null) in ValidatableParameterInfo for enumerable items
        // keeps async tasks in the parameter's shared bag so they are properly awaited.
        // If Clone(this) were used instead, item-level async validation tasks would be
        // orphaned in a separate bag and errors would not be reported.
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
        Assert.True(completionTcs.Task.IsCompletedSuccessfully,
            "Async validation on parameter collection items must be awaited. " +
            "If Clone(null) were changed to Clone(this), the async task would be orphaned.");
    }

    // Test model classes
    private class TypeWithThrowingGetter
    {
        public string ThrowingProp => throw new InvalidOperationException("Getter throws");
    }

    private class InnerComplexType
    {
        public string? Data { get; set; }
    }

    private class OuterWithComplexProp
    {
        public InnerComplexType? Inner { get; set; }
    }

    /// <summary>
    /// Type-level async attribute that signals a semaphore when it starts running,
    /// then delays to hold the ValidationContext in a modified state.
    /// </summary>
    private class SignalingAsyncTypeLevelAttribute : AsyncValidationAttribute
    {
        private readonly SemaphoreSlim _signal;

        public SignalingAsyncTypeLevelAttribute(SemaphoreSlim signal) => _signal = signal;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value, ValidationContext validationContext, CancellationToken cancellationToken)
        {
            // Signal that type-level validation has started (DisplayName is now the type name)
            _signal.Release();
            await Task.Delay(200, cancellationToken);
            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Async attribute that waits for a signal, then captures validationContext.DisplayName.
    /// Used to observe what DisplayName the property's async attribute sees when a
    /// concurrent type-level validation is modifying the shared ValidationContext.
    /// </summary>
    private class DisplayNameCapturingAsyncAttribute : AsyncValidationAttribute
    {
        private readonly SemaphoreSlim _waitForSignal;
        private readonly System.Collections.Concurrent.ConcurrentBag<string> _capturedNames;

        public DisplayNameCapturingAsyncAttribute(
            SemaphoreSlim waitForSignal,
            System.Collections.Concurrent.ConcurrentBag<string> capturedNames)
        {
            _waitForSignal = waitForSignal;
            _capturedNames = capturedNames;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value, ValidationContext validationContext, CancellationToken cancellationToken)
        {
            // Wait for the type-level validation to start (which changes DisplayName)
            await _waitForSignal.WaitAsync(cancellationToken);
            // Small delay to ensure we read AFTER the type modifies DisplayName
            await Task.Delay(10, cancellationToken);
            // Capture whatever DisplayName is on the context right now
            _capturedNames.Add(validationContext.DisplayName);
            return ValidationResult.Success;
        }
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
            await Task.Delay(10, cancellationToken);

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
            await Task.Delay(10, cancellationToken);

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

    private class Document
    {
        public string? Content { get; set; }
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
            await Task.Delay(10, cancellationToken);

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
            await Task.Delay(50, cancellationToken);

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
            await Task.Delay(30, cancellationToken);

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
            await Task.Delay(10, cancellationToken);
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
            await Task.Delay(20, cancellationToken);

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
            await Task.Delay(5000, cancellationToken);
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
            await Task.Delay(30, cancellationToken);

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
            await Task.Delay(20, cancellationToken);

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
            await Task.Delay(20, cancellationToken);

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

    private class ThrowingAsyncValidationAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => throw new UnreachableException();

        protected override async Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
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
        private readonly int _delayMs;
        private readonly SemaphoreSlim? _signal;

        public DelayedAsyncValidationAttribute(int delayMs)
        {
            _delayMs = delayMs;
        }

        public DelayedAsyncValidationAttribute(int delayMs, SemaphoreSlim signal)
        {
            _delayMs = delayMs;
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
                await _signal.WaitAsync();
            }

            await Task.Delay(_delayMs, cancellationToken);

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
            await Task.Delay(50, cancellationToken);
            _completionTcs.TrySetResult();
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
