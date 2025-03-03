// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
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
                CreatePropertyInfo(typeof(Person), typeof(string), "Name", "Name", false, false, true, false,
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Person), typeof(int), "Age", "Age", false, false, false, false,
                    [new RangeAttribute(0, 120)]),
                CreatePropertyInfo(typeof(Person), typeof(Address), "Address", "Address", false, false, false, true,
                    [])
            ],
            false
        );

        var addressType = new TestValidatableTypeInfo(
            typeof(Address),
            [
                CreatePropertyInfo(typeof(Address), typeof(string), "Street", "Street", false, false, true, false,
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Address), typeof(string), "City", "City", false, false, true, false,
                    [new RequiredAttribute()])
            ],
            false
        );

        var validationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(Person), personType },
            { typeof(Address), addressType }
        });

        var context = new ValidatableContext
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
        await personType.Validate(personWithMissingRequiredFields, context);

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
                CreatePropertyInfo(typeof(Employee), typeof(string), "Name", "Name", false, false, true, false,
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Employee), typeof(string), "Department", "Department", false, false, false, false,
                    []),
                CreatePropertyInfo(typeof(Employee), typeof(decimal), "Salary", "Salary", false, false, false, false,
                    [])
            ],
            true // Implements IValidatableObject
        );

        var context = new ValidatableContext
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
        await employeeType.Validate(employee, context);

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
                CreatePropertyInfo(typeof(Vehicle), typeof(string), "Make", "Make", false, false, true, false,
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Vehicle), typeof(string), "Model", "Model", false, false, true, false,
                    [new RequiredAttribute()])
            ],
            false
        );

        var derivedType = new TestValidatableTypeInfo(
            typeof(Car),
            [
                CreatePropertyInfo(typeof(Car), typeof(int), "Doors", "Doors", false, false, false, false,
                    [new RangeAttribute(2, 5)])
            ],
            false,
            [typeof(Vehicle)] // validatableSubTypes
        );

        var context = new ValidatableContext
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
        await derivedType.Validate(car, context);

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
                CreatePropertyInfo(typeof(OrderItem), typeof(string), "ProductName", "ProductName", false, false, true, false,
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(OrderItem), typeof(int), "Quantity", "Quantity", false, false, false, false,
                    [new RangeAttribute(1, 100)])
            ],
            false
        );

        var orderType = new TestValidatableTypeInfo(
            typeof(Order),
            [
                CreatePropertyInfo(typeof(Order), typeof(string), "OrderNumber", "OrderNumber", false, false, true, false,
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(Order), typeof(List<OrderItem>), "Items", "Items", true, false, false, true,
                    [])
            ],
            false
        );

        var context = new ValidatableContext
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
        await orderType.Validate(order, context);

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
                CreatePropertyInfo(typeof(Person), typeof(string), "Name", "Name", false, true, false, false,
                    []),
                CreatePropertyInfo(typeof(Person), typeof(Address), "Address", "Address", false, true, false, true,
                    [])
            ],
            false
        );

        var context = new ValidatableContext
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
        await personType.Validate(person, context);

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
                CreatePropertyInfo(typeof(TreeNode), typeof(string), "Name", "Name", false, false, true, false,
                    [new RequiredAttribute()]),
                CreatePropertyInfo(typeof(TreeNode), typeof(TreeNode), "Parent", "Parent", false, true, false, true,
                    []),
                CreatePropertyInfo(typeof(TreeNode), typeof(List<TreeNode>), "Children", "Children", true, false, false, true,
                    [])
            ],
            false
        );

        // Create a validation options with a small max depth
        var validationOptions = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(TreeNode), nodeType }
        });
        validationOptions.MaxDepth = 3; // Set a small max depth to trigger the limit

        var context = new ValidatableContext
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
        async () => await nodeType.Validate(rootNode, context));

        Assert.NotNull(exception);
        Assert.Contains("Maximum validation depth of 3 exceeded at 'Children[0].Parent.Children[0]'. This is likely caused by a circular reference in the object graph. Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.", exception.Message);
    }

    private ValidatablePropertyInfo CreatePropertyInfo(
        Type containingType,
        Type propertyType,
        string name,
        string displayName,
        bool isEnumerable,
        bool isNullable,
        bool isRequired,
        bool hasValidatableType,
        ValidationAttribute[] validationAttributes)
    {
        return new TestValidatablePropertyInfo(
            containingType,
            propertyType,
            name,
            displayName,
            isEnumerable,
            isNullable,
            isRequired,
            hasValidatableType,
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

    // Test implementations
    private class TestValidatablePropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatablePropertyInfo(
            Type containingType,
            Type propertyType,
            string name,
            string displayName,
            bool isEnumerable,
            bool isNullable,
            bool isRequired,
            bool hasValidatableType,
            ValidationAttribute[] validationAttributes)
            : base(containingType, propertyType, name, displayName, isEnumerable, isNullable, isRequired, hasValidatableType)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableTypeInfo : ValidatableTypeInfo
    {
        public TestValidatableTypeInfo(
            Type type,
            ValidatablePropertyInfo[] members,
            bool implementsIValidatableObject,
            Type[]? validatableSubTypes = null)
            : base(type, members, implementsIValidatableObject, validatableSubTypes)
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

            public ValidatableTypeInfo? GetValidatableTypeInfo(Type type)
            {
                _typeInfoMappings.TryGetValue(type, out var info);
                return info;
            }

            public ValidatableParameterInfo? GetValidatableParameterInfo(ParameterInfo parameterInfo)
            {
                // Not implemented in the test
                return null;
            }
        }
    }
}
