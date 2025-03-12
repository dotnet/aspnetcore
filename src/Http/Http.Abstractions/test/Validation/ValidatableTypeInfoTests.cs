// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.Validation.Tests;

public class ValidatableTypeInfoTests
{
    [Fact]
    public async Task Validate_ValidatesComplexType_WithNestedProperties()
    {
        // Arrange
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

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
        };

        var personWithMissingRequiredFields = new Person
        {
            Age = 150, // Invalid age
            Address = new Address() // Missing required City and Street
        };
        context.ValidationContext = new ValidationContext(personWithMissingRequiredFields);

        // Act
        await personType.ValidateAsync(personWithMissingRequiredFields, context, default);

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
    }

    [Fact]
    public async Task Validate_HandlesIValidatableObject_Implementation()
    {
        // Arrange
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

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Employee), employeeType }
            })
        };

        var employee = new Employee
        {
            Name = "John Doe",
            Department = "IT",
            Salary = -5000 // Negative salary will trigger IValidatableObject validation
        };
        context.ValidationContext = new ValidationContext(employee);

        // Act
        await employeeType.ValidateAsync(employee, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Salary", error.Key);
        Assert.Equal("Salary must be a positive value.", error.Value.First());
    }

    [Fact]
    public async Task Validate_HandlesPolymorphicTypes_WithSubtypes()
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

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Vehicle), baseType },
                { typeof(Car), derivedType }
            })
        };

        var car = new Car
        {
            // Missing Make and Model (required in base type)
            Doors = 7 // Invalid number of doors
        };
        context.ValidationContext = new ValidationContext(car);

        // Act
        await derivedType.ValidateAsync(car, context, default);

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

    [Fact]
    public async Task Validate_HandlesCollections_OfValidatableTypes()
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

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(OrderItem), itemType },
                { typeof(Order), orderType }
            })
        };

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
        context.ValidationContext = new ValidationContext(order);

        // Act
        await orderType.ValidateAsync(order, context, default);

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

    [Fact]
    public async Task Validate_HandlesNullValues_Appropriately()
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

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Person), personType }
            })
        };

        var person = new Person
        {
            Name = null,
            Address = null
        };
        context.ValidationContext = new ValidationContext(person);

        // Act
        await personType.ValidateAsync(person, context, default);

        // Assert
        Assert.Null(context.ValidationErrors); // No validation errors for nullable properties with null values
    }

    [Fact]
    public async Task Validate_RespectsMaxDepthOption_ForCircularReferences()
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

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationErrors = []
        };

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

        context.ValidationContext = new ValidationContext(rootNode);

        // Act + Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
        async () => await nodeType.ValidateAsync(rootNode, context, default));

        Assert.NotNull(exception);
        Assert.Equal("Maximum validation depth of 3 exceeded at 'Children[0].Parent.Children[0]' in 'TreeNode'. This is likely caused by a circular reference in the object graph. Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.", exception.Message);
        Assert.Equal(0, context.CurrentDepth);
    }

    [Fact]
    public async Task Validate_HandlesCustomValidationAttributes()
    {
        // Arrange
        var productType = new TestValidatableTypeInfo(
            typeof(Product),
            [
                CreatePropertyInfo(typeof(Product), typeof(string), "SKU", "SKU", [new RequiredAttribute(), new CustomSkuValidationAttribute()]),
            ]);

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(Product), productType }
            })
        };

        var product = new Product { SKU = "INVALID-SKU" };
        context.ValidationContext = new ValidationContext(product);

        // Act
        await productType.ValidateAsync(product, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("SKU", error.Key);
        Assert.Equal("SKU must start with 'PROD-'.", error.Value.First());
    }

    [Fact]
    public async Task Validate_HandlesMultipleErrorsOnSameProperty()
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

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(User), userType }
            })
        };

        var user = new User { Password = "abc" };  // Too short and not complex enough
        context.ValidationContext = new ValidationContext(user);

        // Act
        await userType.ValidateAsync(user, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Single(context.ValidationErrors.Keys); // Only the "Password" key
        Assert.Equal(2, context.ValidationErrors["Password"].Length); // But with 2 errors
        Assert.Contains("Password must be at least 8 characters.", context.ValidationErrors["Password"]);
        Assert.Contains("Password must contain at least one number and one special character.", context.ValidationErrors["Password"]);
    }

    [Fact]
    public async Task Validate_HandlesMultiLevelInheritance()
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

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(BaseEntity), baseType },
                { typeof(IntermediateEntity), intermediateType },
                { typeof(DerivedEntity), derivedType }
            })
        };

        var entity = new DerivedEntity
        {
            Name = "",  // Invalid: required
            CreatedAt = DateTime.Now.AddDays(1) // Invalid: future date
        };
        context.ValidationContext = new ValidationContext(entity);

        // Act
        await derivedType.ValidateAsync(entity, context, default);

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

    [Fact]
    public async Task Validate_RequiredOnPropertyShortCircuitsOtherValidations()
    {
        // Arrange
        var userType = new TestValidatableTypeInfo(
            typeof(User),
            [
                CreatePropertyInfo(typeof(User), typeof(string), "Password", "Password",
                    [new RequiredAttribute(), new PasswordComplexityAttribute()])
            ]);

        var context = new ValidateContext
        {
            ValidationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
            {
                { typeof(User), userType }
            })
        };

        var user = new User { Password = null };  // Invalid: required
        context.ValidationContext = new ValidationContext(user);

        // Act
        await userType.ValidateAsync(user, context, default);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Single(context.ValidationErrors.Keys);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Password", error.Key);
        Assert.Equal("The Password field is required.", error.Value.Single());
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
    }

    // Test model classes
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
                yield return new ValidationResult("Salary must be a positive value.", new[] { nameof(Salary) });
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
            : base(containingType, propertyType, name, displayName)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableTypeInfo : ValidatableTypeInfo
    {
        public TestValidatableTypeInfo(
            Type type,
            ValidatablePropertyInfo[] members)
            : base(type, members)
        {
        }
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

            public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
            {
                if (_typeInfoMappings.TryGetValue(type, out var info))
                {
                    validatableInfo = info;
                    return true;
                }
                validatableInfo = null;
                return false;
            }

            public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
            {
                validatableInfo = null;
                return false;
            }
        }
    }
}
