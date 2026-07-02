#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Extensions.Validation.Tests;

public class ValidatableTypeInfoTests : ValidationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_ValidatesComplexType_WithNestedProperties(bool useAsync)
    {
        // Arrange
        List<ValidationErrorContext> validationErrors = [];

        var personType = new TestValidatableTypeInfo(
            typeof(Person),
            [
                CreatePropertyInfo(typeof(Person), typeof(string), "Name", "Name",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Person), typeof(int), "Age", "Age",
                    [new RangeAttribute(0, 120)]),
                CreatePropertyInfo(typeof(Person), typeof(Address), "Address", "Address",
                    [])
            ]);

        var addressType = new TestValidatableTypeInfo(
            typeof(Address),
            [
                CreatePropertyInfo(typeof(Address), typeof(string), "Street", "Street",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Address), typeof(string), "City", "City",
                    [new RequiredAttribute()])
            ]);

        var validationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(Person), personType },
            { typeof(Address), addressType }
        });

        var personWithMissingRequiredFields = new Person
        {
            Age = 150, // Invalid age
            Address = new Address() // Missing required City and Street
        };
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ServiceProvider = null,
        };

        context.OnValidationError += validationErrors.Add;

        // Act
        await ValidateAsync(personType, personWithMissingRequiredFields, context, useAsync, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Age", kvp.Key);
                Assert.Equal("The field Age must be between 0 and 120.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Address.Street", kvp.Key);
                Assert.Equal("The Street field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Address.City", kvp.Key);
                Assert.Equal("The City field is required.", kvp.Value.First());
            });

        Assert.Collection(validationErrors,
            context =>
            {
                Assert.Equal("Name", context.Name);
                Assert.Equal("Name", context.Path);
                Assert.Equal("The Name field is required.", context.Errors.Single());
                Assert.Same(context.Container, personWithMissingRequiredFields);
            },
            context =>
            {
                Assert.Equal("Age", context.Name);
                Assert.Equal("Age", context.Path);
                Assert.Equal("The field Age must be between 0 and 120.", context.Errors.Single());
                Assert.Same(context.Container, personWithMissingRequiredFields);
            },
            context =>
            {
                Assert.Equal("Street", context.Name);
                Assert.Equal("Address.Street", context.Path);
                Assert.Equal("The Street field is required.", context.Errors.Single());
                Assert.Same(context.Container, personWithMissingRequiredFields.Address);
            },
            context =>
            {
                Assert.Equal("City", context.Name);
                Assert.Equal("Address.City", context.Path);
                Assert.Equal("The City field is required.", context.Errors.Single());
                Assert.Same(context.Container, personWithMissingRequiredFields.Address);
            });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesIValidatableObject_Implementation(bool useAsync)
    {
        // Arrange
        var validationErrors = new List<ValidationErrorContext>();
        var employeeType = new TestValidatableTypeInfo(
            typeof(Employee),
            [
                CreatePropertyInfo(typeof(Employee), typeof(string), "Name", "Name",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Employee), typeof(string), "Department", "Department",
                    []),
                CreatePropertyInfo(typeof(Employee), typeof(decimal), "Salary", "Salary",
                    [])
            ]);

        var employee = new Employee
        {
            Name = "John Doe",
            Department = "IT",
            Salary = -5000 // Negative salary will trigger IValidatableObject validation
        };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Employee), employeeType }
            }),
            ServiceProvider = null,
        };

        context.OnValidationError += validationErrors.Add;

        // Act
        await ValidateAsync(employeeType, employee, context, useAsync, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Salary", error.Key);
        Assert.Equal("Salary must be a positive value.", error.Value.First());

        var errorContext = Assert.Single(validationErrors);
        Assert.Equal("Salary", errorContext.Name);
        Assert.Equal("Salary", errorContext.Path);
        Assert.Equal("Salary must be a positive value.", errorContext.Errors.Single());
        Assert.Same(errorContext.Container, employee);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesPolymorphicTypes_WithSubtypes(bool useAsync)
    {
        // Arrange
        var baseType = new TestValidatableTypeInfo(
            typeof(Vehicle),
            [
                CreatePropertyInfo(typeof(Vehicle), typeof(string), "Make", "Make",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Vehicle), typeof(string), "Model", "Model",
                    [new RequiredAttribute()])
            ]);

        var derivedType = new TestValidatableTypeInfo(
            typeof(Car),
            [
                CreatePropertyInfo(typeof(Car), typeof(int), "Doors", "Doors",
                    [new RangeAttribute(2, 5)])
            ]);

        var car = new Car
        {
            // Missing Make and Model (required in base type)
            Doors = 7 // Invalid number of doors
        };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Vehicle), baseType },
                { typeof(Car), derivedType }
            }),
            ServiceProvider = null,
        };

        // Act
        await ValidateAsync(derivedType, car, context, useAsync, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Doors", kvp.Key);
                Assert.Equal("The field Doors must be between 2 and 5.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Make", kvp.Key);
                Assert.Equal("The Make field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Model", kvp.Key);
                Assert.Equal("The Model field is required.", kvp.Value.First());
            });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesCollections_OfValidatableTypes(bool useAsync)
    {
        // Arrange
        var itemType = new TestValidatableTypeInfo(
            typeof(OrderItem),
            [
                CreatePropertyInfo(typeof(OrderItem), typeof(string), "ProductName", "ProductName",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(OrderItem), typeof(int), "Quantity", "Quantity",
                    [new RangeAttribute(1, 100)])
            ]);

        var orderType = new TestValidatableTypeInfo(
            typeof(Order),
            [
                CreatePropertyInfo(typeof(Order), typeof(string), "OrderNumber", "OrderNumber",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Order), typeof(List<OrderItem>), "Items", "Items",
                    [])
            ]);

        var order = new Order
        {
            OrderNumber = "ORD-12345",
            Items =
            [
                new OrderItem { ProductName = "Valid Product", Quantity = 5 },
                new OrderItem { /* Missing ProductName (required) */ Quantity = 0 /* Invalid quantity */ },
                new OrderItem { ProductName = "Another Product", Quantity = 200 /* Invalid quantity */ }
            ]
        };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(OrderItem), itemType },
                { typeof(Order), orderType }
            }),
            ServiceProvider = null,
        };

        // Act
        await ValidateAsync(orderType, order, context, useAsync, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Items[1].ProductName", kvp.Key);
                Assert.Equal("The ProductName field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Items[1].Quantity", kvp.Key);
                Assert.Equal("The field Quantity must be between 1 and 100.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Items[2].Quantity", kvp.Key);
                Assert.Equal("The field Quantity must be between 1 and 100.", kvp.Value.First());
            });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesNullValues_Appropriately(bool useAsync)
    {
        // Arrange
        var personType = new TestValidatableTypeInfo(
            typeof(Person),
            [
                CreatePropertyInfo(typeof(Person), typeof(string), "Name", "Name",
                    []),
                CreatePropertyInfo(typeof(Person), typeof(Address), "Address", "Address",
                    [])
            ]);

        var person = new Person
        {
            Name = null,
            Address = null
        };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Person), personType }
            }),
            ServiceProvider = null,
        };

        // Act
        await ValidateAsync(personType, person, context, useAsync, default);

        // Assert
        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0); // No validation errors for nullable properties with null values
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_RespectsMaxDepthOption_ForCircularReferences(bool useAsync)
    {
        // Arrange
        // Create a type that can contain itself (circular reference)
        var nodeType = new TestValidatableTypeInfo(
            typeof(TreeNode),
            [
                CreatePropertyInfo(typeof(TreeNode), typeof(string), "Name", "Name",
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(TreeNode), typeof(TreeNode), "Parent", "Parent",
                    []),
                CreatePropertyInfo(typeof(TreeNode), typeof(List<TreeNode>), "Children", "Children",
                    [])
            ]);

        // Create a validation options with a small max depth
        var validationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(TreeNode), nodeType }
        });
        validationOptions.MaxDepth = 3; // Set a small max depth to trigger the limit

        // Create a deep tree with circular references
        var rootNode = new TreeNode { Name = "Root" };
        var level1 = new TreeNode { Name = "Level1", Parent = rootNode };
        var level2 = new TreeNode { Name = "Level2", Parent = level1 };
        var level3 = new TreeNode { Name = "Level3", Parent = level2 };
        var level4 = new TreeNode { Name = "" }; // Invalid: missing required name
        var level5 = new TreeNode { Name = "" }; // Invalid but beyond max depth, should not be validated

        rootNode.Children.Add(level1);
        level1.Children.Add(level2);
        level2.Children.Add(level3);
        level3.Children.Add(level4);
        level4.Children.Add(level5);

        // Add a circular reference
        level5.Children.Add(rootNode);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ServiceProvider = null,
        };

        // Act + Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ValidateAsync(nodeType, rootNode, context, useAsync, default));

        Assert.NotNull(exception);
        Assert.Equal("Maximum validation depth of 3 exceeded at 'Children[0].Parent.Children[0]' in 'TreeNode'. This is likely caused by a circular reference in the object graph. Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.", exception.Message);
        Assert.Equal(0, context.CurrentDepth);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesCustomValidationAttributes(bool useAsync)
    {
        // Arrange
        var productType = new TestValidatableTypeInfo(
            typeof(Product),
            [
                CreatePropertyInfo(typeof(Product), typeof(string), "SKU", "SKU", [new RequiredAttribute(), new CustomSkuValidationAttribute()]),
            ]);

        var product = new Product { SKU = "INVALID-SKU" };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Product), productType }
            }),
            ServiceProvider = null,
        };

        // Act
        await ValidateAsync(productType, product, context, useAsync, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("SKU", error.Key);
        Assert.Equal("SKU must start with 'PROD-'.", error.Value.First());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesMultipleErrorsOnSameProperty(bool useAsync)
    {
        // Arrange
        var userType = new TestValidatableTypeInfo(
            typeof(User),
            [
                CreatePropertyInfo(typeof(User), typeof(string), "Password", "Password",
                    [
                        new RequiredAttribute(),
                        new MinLengthAttribute(8) { ErrorMessage = "Password must be at least 8 characters." },
                        new PasswordComplexityAttribute()
                    ])
            ]);

        var user = new User { Password = "abc" };  // Too short and not complex enough
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(User), userType }
            }),
            ServiceProvider = null,
        };

        // Act
        await ValidateAsync(userType, user, context, useAsync, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Single(context.ValidationErrors.Keys); // Only the "Password" key
        var passwordErrors = context.ValidationErrors["Password"].ToArray();
        Assert.Equal(2, passwordErrors.Length); // But with 2 errors
        Assert.Contains("Password must be at least 8 characters.", passwordErrors);
        Assert.Contains("Password must contain at least one number and one special character.", passwordErrors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HandlesMultiLevelInheritance(bool useAsync)
    {
        // Arrange
        var baseType = new TestValidatableTypeInfo(
            typeof(BaseEntity),
            [
                CreatePropertyInfo(typeof(BaseEntity), typeof(Guid), "Id", "Id", [])
            ]);

        var intermediateType = new TestValidatableTypeInfo(
            typeof(IntermediateEntity),
            [
                CreatePropertyInfo(typeof(IntermediateEntity), typeof(DateTime), "CreatedAt", "CreatedAt", [new PastDateAttribute()])
            ]);

        var derivedType = new TestValidatableTypeInfo(
            typeof(DerivedEntity),
            [
                CreatePropertyInfo(typeof(DerivedEntity), typeof(string), "Name", "Name", [new RequiredAttribute()])
            ]);

        var entity = new DerivedEntity
        {
            Name = "",  // Invalid: required
            CreatedAt = DateTime.Now.AddDays(1) // Invalid: future date
        };
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(BaseEntity), baseType },
                { typeof(IntermediateEntity), intermediateType },
                { typeof(DerivedEntity), derivedType }
            }),
            ServiceProvider = null,
        };

        // Act
        await ValidateAsync(derivedType, entity, context, useAsync, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("CreatedAt", kvp.Key);
                Assert.Equal("Date must be in the past.", kvp.Value.First());
            });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_RequiredOnPropertyShortCircuitsOtherValidations(bool useAsync)
    {
        // Arrange
        var userType = new TestValidatableTypeInfo(
            typeof(User),
            [
                CreatePropertyInfo(typeof(User), typeof(string), "Password", "Password",
                    [new RequiredAttribute(), new PasswordComplexityAttribute()])
            ]);

        var user = new User { Password = null }; // Invalid: required
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(User), userType }
            }),
            ServiceProvider = null, // Invalid: required
        };

        // Act
        await ValidateAsync(userType, user, context, useAsync, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Single(context.ValidationErrors.Keys);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Password", error.Key);
        Assert.Equal("The Password field is required.", error.Value.Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_IValidatableObject_WithZeroAndMultipleMemberNames_BehavesAsExpected(bool useAsync)
    {
        var globalType = new TestValidatableTypeInfo(
            typeof(GlobalErrorObject),
            []); // no properties – nothing sets MemberName
        var globalErrorInstance = new GlobalErrorObject { Data = -1 };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(GlobalErrorObject), globalType }
            }),
            ServiceProvider = null,
        };

        await ValidateAsync(globalType, globalErrorInstance, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        var globalError = Assert.Single(context.ValidationErrors);
        Assert.Equal(string.Empty, globalError.Key);
        Assert.Equal("Data must be positive.", globalError.Value.Single());

        var multiType = new TestValidatableTypeInfo(
            typeof(MultiMemberErrorObject),
            [
                CreatePropertyInfo(typeof(MultiMemberErrorObject), typeof(string), "FirstName", "FirstName", []),
                CreatePropertyInfo(typeof(MultiMemberErrorObject), typeof(string), "LastName",  "LastName",  [])
            ]);

        var multiErrorInstance = new MultiMemberErrorObject { FirstName = "", LastName = "" };

        context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(MultiMemberErrorObject), multiType }
            }),
            ServiceProvider = null,
        };

        await ValidateAsync(multiType, multiErrorInstance, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("FirstName", kvp.Key);
                Assert.Equal("FirstName and LastName are required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("LastName", kvp.Key);
                Assert.Equal("FirstName and LastName are required.", kvp.Value.First());
            });
    }

    // The expected order of validation is:
    // 1. Attributes on properties
    // 2. Attributes on the type
    // 3. IValidatableObject implementation
    // If any of these steps report an error, the later steps are skipped.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_IValidatableObject_WithPropertyErrors_ShortCircuitsProperly(bool useAsync)
    {
        var testTypeInfo = new TestValidatableTypeInfo(
            typeof(PropertyAndTypeLevelErrorObject),
            [
                CreatePropertyInfo(typeof(PropertyAndTypeLevelErrorObject), typeof(int), "Value", "Value",
                    [new RangeAttribute(0, int.MaxValue) {  ErrorMessage = "Property attribute error" }])
            ],
            [
                new CustomValidationAttribute()
            ]);

        // First case:
        var testTypeInstance = new PropertyAndTypeLevelErrorObject { Value = 15 };

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(PropertyAndTypeLevelErrorObject), testTypeInfo }
            }),
            ServiceProvider = null,
        };

        await ValidateAsync(testTypeInfo, testTypeInstance, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        var interfaceError = Assert.Single(context.ValidationErrors);
        Assert.Equal(string.Empty, interfaceError.Key);
        Assert.Equal("IValidatableObject error", interfaceError.Value.Single());

        // Second case:
        testTypeInstance.Value = 5;
        context = new ValidateContext()
        {
            ServiceProvider = null,
            ValidationOptions = context.ValidationOptions,
        };

        await ValidateAsync(testTypeInfo, testTypeInstance, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        var classAttributeError = Assert.Single(context.ValidationErrors);
        Assert.Equal(string.Empty, classAttributeError.Key);
        Assert.Equal("Class attribute error", classAttributeError.Value.Single());

        // Third case:
        testTypeInstance.Value = -5;
        context = new ValidateContext()
        {
            ServiceProvider = null,
            ValidationOptions = context.ValidationOptions
        };

        await ValidateAsync(testTypeInfo, testTypeInstance, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        var propertyAttributeError = Assert.Single(context.ValidationErrors);
        Assert.Equal("Value", propertyAttributeError.Key);
        Assert.Equal("Property attribute error", propertyAttributeError.Value.Single());
    }

    [Fact]
    public void TryFindProperty_ReturnsFalse_WhenPropertyNotFound()
    {
        var typeInfo = new TestValidatableTypeInfo(typeof(Person), []);
        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(Person), typeInfo },
        });

        Assert.False(typeInfo.TryFindProperty("NonExistent", options, out var validatablePropertyInfo));
        Assert.Null(validatablePropertyInfo);
    }

    [Fact]
    public void TryFindProperty_ReturnsMatchingProperty_WhenPresent()
    {
        var nameProperty = CreatePropertyInfo(typeof(Person), typeof(string), "Name", "Name", []);
        var typeInfo = new TestValidatableTypeInfo(typeof(Person), [nameProperty]);
        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(Person), typeInfo },
        });

        Assert.True(typeInfo.TryFindProperty("Name", options, out var retrievedNameProperty));
        Assert.NotNull(retrievedNameProperty);
        Assert.Same(nameProperty, retrievedNameProperty);
    }

    [Fact]
    public void TryFindProperty_ReturnsInheritedProperty_FromSuperType()
    {
        // BaseEntity declares Id; DerivedEntity declares Name. Looking up Id on the derived type
        // should resolve through the super-type info via the validation options resolver.
        var idProperty = CreatePropertyInfo(typeof(BaseEntity), typeof(Guid), "Id", "Id", []);
        var baseType = new TestValidatableTypeInfo(typeof(BaseEntity), [idProperty]);
        var nameProperty = CreatePropertyInfo(typeof(DerivedEntity), typeof(string), "Name", "Name", []);
        var derivedType = new TestValidatableTypeInfo(typeof(DerivedEntity), [nameProperty]);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(BaseEntity), baseType },
            { typeof(DerivedEntity), derivedType },
        });

        Assert.True(derivedType.TryFindProperty("Name", options, out var localProperty));
        Assert.NotNull(localProperty);
        Assert.Same(nameProperty, localProperty);

        Assert.True(derivedType.TryFindProperty("Id", options, out var inheritedProperty));
        Assert.NotNull(inheritedProperty);
        Assert.Same(idProperty, inheritedProperty);

        Assert.False(derivedType.TryFindProperty("NonExistent", options, out var missingProperty));
        Assert.Null(missingProperty);
    }

    [Fact]
    public void TryFindProperty_LocalDeclarationShadowsInheritedProperty()
    {
        // If both base and derived declare a property with the same name, the derived (local)
        // declaration is returned, matching how ValidateAsync would visit derived members first.
        var baseNameProperty = CreatePropertyInfo(typeof(BaseEntity), typeof(string), "Name", "Name", []);
        var baseType = new TestValidatableTypeInfo(typeof(BaseEntity), [baseNameProperty]);
        var derivedNameProperty = CreatePropertyInfo(typeof(DerivedEntity), typeof(string), "Name", "Name", []);
        var derivedType = new TestValidatableTypeInfo(typeof(DerivedEntity), [derivedNameProperty]);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(BaseEntity), baseType },
            { typeof(DerivedEntity), derivedType },
        });

        Assert.True(derivedType.TryFindProperty("Name", options, out var propertyInfo));
        Assert.NotNull(propertyInfo);
        Assert.Same(derivedNameProperty, propertyInfo);
    }

    [Fact]
    public void TryFindProperty_ReturnsFalseForInheritedMember_WhenSuperTypeNotResolvable()
    {
        // Only the derived type is registered. Local lookup still works; inherited members
        // remain unresolved and the method returns false without throwing.
        var nameProperty = CreatePropertyInfo(typeof(DerivedEntity), typeof(string), "Name", "Name", []);
        var derivedType = new TestValidatableTypeInfo(typeof(DerivedEntity), [nameProperty]);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(DerivedEntity), derivedType },
        });

        Assert.True(derivedType.TryFindProperty("Name", options, out var localProperty));
        Assert.NotNull(localProperty);
        Assert.Same(nameProperty, localProperty);

        Assert.False(derivedType.TryFindProperty("Id", options, out var inheritedProperty));
        Assert.Null(inheritedProperty);
    }

    [Fact]
    public void TryFindProperty_WalksMultipleInheritanceLevels()
    {
        // Three-level chain: BaseEntity (Id) <- IntermediateEntity (CreatedAt) <- DerivedEntity (Name).
        // A lookup on DerivedEntity must reach members declared at every level of the chain.
        var idProperty = CreatePropertyInfo(typeof(BaseEntity), typeof(Guid), "Id", "Id", []);
        var createdAtProperty = CreatePropertyInfo(typeof(IntermediateEntity), typeof(DateTime), "CreatedAt", "CreatedAt", []);
        var nameProperty = CreatePropertyInfo(typeof(DerivedEntity), typeof(string), "Name", "Name", []);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(BaseEntity), new TestValidatableTypeInfo(typeof(BaseEntity), [idProperty]) },
            { typeof(IntermediateEntity), new TestValidatableTypeInfo(typeof(IntermediateEntity), [createdAtProperty]) },
            { typeof(DerivedEntity), new TestValidatableTypeInfo(typeof(DerivedEntity), [nameProperty]) },
        });

        Assert.True(options.TryGetValidatableTypeInfo(typeof(DerivedEntity), out var derivedEntityInfo));

        Assert.True(derivedEntityInfo.TryFindProperty("Name", options, out var fromDerived));
        Assert.NotNull(fromDerived);
        Assert.Same(nameProperty, fromDerived);

        Assert.True(derivedEntityInfo.TryFindProperty("CreatedAt", options, out var fromIntermediate));
        Assert.NotNull(fromIntermediate);
        Assert.Same(createdAtProperty, fromIntermediate);

        Assert.True(derivedEntityInfo.TryFindProperty("Id", options, out var fromBase));
        Assert.NotNull(fromBase);
        Assert.Same(idProperty, fromBase);
    }

    [Fact]
    public void TryFindProperty_ResolvesInterfaceDeclaredProperty()
    {
        // Property declared on an interface implemented by the target type. ValidatableTypeInfo's
        // _superTypes list is populated by GetAllImplementedTypes(), which includes interfaces.
        var auditedProperty = CreatePropertyInfo(typeof(IAuditable), typeof(DateTime), "CreatedAt", "CreatedAt", []);
        var auditableTypeInfo = new TestValidatableTypeInfo(typeof(IAuditable), [auditedProperty]);
        var nameProperty = CreatePropertyInfo(typeof(AuditableThing), typeof(string), "Name", "Name", []);
        var thingTypeInfo = new TestValidatableTypeInfo(typeof(AuditableThing), [nameProperty]);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(IAuditable), auditableTypeInfo },
            { typeof(AuditableThing), thingTypeInfo },
        });

        Assert.True(thingTypeInfo.TryFindProperty("CreatedAt", options, out var resolved));
        Assert.NotNull(resolved);
        Assert.Same(auditedProperty, resolved);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HiddenPropertyOnDerivedType_UsesDeclaredProperty(bool useAsync)
    {
        var queryOptions = new DerivedQueryOptions
        {
            IfMatch = "etag",
        };
        var propertyInfo = CreatePropertyInfo(typeof(DerivedQueryOptions), typeof(string), nameof(DerivedQueryOptions.IfMatch), nameof(DerivedQueryOptions.IfMatch), []);
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions([]),
            ServiceProvider = null,
        };

        await ValidateAsync(propertyInfo, queryOptions, context, useAsync, default);

        Assert.Null(context.ValidationErrors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_HiddenGenericPropertyOnDerivedType_UsesDeclaredProperty(bool useAsync)
    {
        var queryOptions = new GenericDerivedQueryOptions<int>
        {
            IfMatch = new GenericETag<int>(),
        };
        var propertyName = nameof(GenericDerivedQueryOptions<int>.IfMatch);
        var propertyInfo = CreatePropertyInfo(typeof(GenericDerivedQueryOptions<int>), typeof(GenericETag<int>), propertyName, propertyName, []);
        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions([]),
            ServiceProvider = null,
        };

        await ValidateAsync(propertyInfo, queryOptions, context, useAsync, default);

        Assert.Null(context.ValidationErrors);
    }

    private interface IAuditable
    {
        DateTime CreatedAt { get; }
    }

    private class AuditableThing : IAuditable
    {
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class QueryOptions
    {
        public object? IfMatch { get; set; }
    }

    private class DerivedQueryOptions : QueryOptions
    {
        public new string? IfMatch { get; set; }
    }

    private class GenericETag
    {
    }

    private class GenericETag<T> : GenericETag
    {
    }

    private class GenericQueryOptions
    {
        public virtual GenericETag? IfMatch { get; set; }
    }

    private class GenericDerivedQueryOptions<T> : GenericQueryOptions
    {
        public new GenericETag<T>? IfMatch
        {
            get => base.IfMatch as GenericETag<T>;
            set => base.IfMatch = value;
        }
    }

    // Returns no member names to validate https://github.com/dotnet/aspnetcore/issues/61739
    private class GlobalErrorObject : IValidatableObject
    {
        public int Data { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Data <= 0)
            {
                yield return new ValidationResult("Data must be positive.");
            }
        }
    }

    // Returns multiple member names to validate https://github.com/dotnet/aspnetcore/issues/61739
    private class MultiMemberErrorObject : IValidatableObject
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName))
            {
                // MULTIPLE member names
                yield return new ValidationResult(
                    "FirstName and LastName are required.",
                    [nameof(FirstName), nameof(LastName)]);
            }
        }
    }

    [CustomValidation]
    private class PropertyAndTypeLevelErrorObject : IValidatableObject
    {
        [Range(0, int.MaxValue, ErrorMessage = "Property attribute error")]
        public int Value { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Value < 20)
            {
                yield return new ValidationResult($"IValidatableObject error");
            }
        }
    }

    private class CustomValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is PropertyAndTypeLevelErrorObject instance)
            {
                if (instance.Value < 10)
                {
                    return new ValidationResult($"Class attribute error");
                }
            }
            return ValidationResult.Success;
        }
    }

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
    }    // Test model classes
    private class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public Address? Address { get; set; }
    }

    private class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }

    private class Employee : IValidatableObject
    {
        public string? Name { get; set; }
        public string? Department { get; set; }
        public decimal Salary { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Salary < 0)
            {
                yield return new ValidationResult("Salary must be a positive value.", ["Salary"]);
            }
        }
    }

    private class Vehicle
    {
        public string? Make { get; set; }
        public string? Model { get; set; }
    }

    private class Car : Vehicle
    {
        public int Doors { get; set; }
    }

    private class Order
    {
        public string? OrderNumber { get; set; }
        public List<OrderItem> Items { get; set; } = [];
    }

    private class OrderItem
    {
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
    }

    private class TreeNode
    {
        public string Name { get; set; } = string.Empty;
        public TreeNode? Parent { get; set; }
        public List<TreeNode> Children { get; set; } = [];
    }

    private class Product
    {
        public string SKU { get; set; } = string.Empty;
    }

    private class User
    {
        public string? Password { get; set; } = string.Empty;
    }

    private class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    private class IntermediateEntity : BaseEntity
    {
        public DateTime CreatedAt { get; set; }
    }

    private class DerivedEntity : IntermediateEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private class PastDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime date && date > DateTime.Now)
            {
                return new ValidationResult("Date must be in the past.");
            }

            return ValidationResult.Success;
        }
    }

    private class CustomSkuValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string sku && !sku.StartsWith("PROD-", StringComparison.Ordinal))
            {
                return new ValidationResult("SKU must start with 'PROD-'.");
            }

            return ValidationResult.Success;
        }
    }

    private class PasswordComplexityAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string password)
            {
                var hasDigit = password.Any(c => char.IsDigit(c));
                var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

                if (!hasDigit || !hasSpecial)
                {
                    return new ValidationResult("Password must contain at least one number and one special character.");
                }
            }

            return ValidationResult.Success;
        }
    }

    // Test implementations
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

    private class TestValidationOptions : ValidationOptions
    {
        public TestValidationOptions(Dictionary<Type, ValidatableTypeInfo> typeInfoMappings)
        {
            // Create a custom resolver that uses the dictionary
            var resolver = new DictionaryBasedResolver(typeInfoMappings);

            // Add it to the resolvers collection
            Resolvers.Add(resolver);
        }

        // Private resolver implementation that uses a dictionary lookup
        private class DictionaryBasedResolver : IValidatableInfoResolver
        {
            private readonly Dictionary<Type, ValidatableTypeInfo> _typeInfoMappings;

            public DictionaryBasedResolver(Dictionary<Type, ValidatableTypeInfo> typeInfoMappings)
            {
                _typeInfoMappings = typeInfoMappings;
            }

            public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo? validatableInfo)
            {
                if (_typeInfoMappings.TryGetValue(type, out var info))
                {
                    validatableInfo = info;
                    return true;
                }
                validatableInfo = null;
                return false;
            }

            public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo? validatableInfo)
            {
                validatableInfo = null;
                return false;
            }
        }
    }
}
