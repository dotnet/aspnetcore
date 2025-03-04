﻿//HintName: ValidatableInfoResolver.g.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#nullable enable

namespace System.Runtime.CompilerServices
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    file sealed class InterceptsLocationAttribute : System.Attribute
    {
        public InterceptsLocationAttribute(int version, string data)
        {
        }
    }
}

namespace Microsoft.AspNetCore.Http.Validation.Generated
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Http.Validation;

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file sealed class GeneratedValidatablePropertyInfo : global::Microsoft.AspNetCore.Http.Validation.ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public GeneratedValidatablePropertyInfo(
            Type containingType,
            Type propertyType,
            string name,
            string displayName,
            bool isEnumerable,
            bool isNullable,
            bool isRequired,
            bool hasValidatableType,
            ValidationAttribute[] validationAttributes) : base(containingType, propertyType, name, displayName, isEnumerable, isNullable, isRequired, hasValidatableType)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file sealed class GeneratedValidatableTypeInfo : global::Microsoft.AspNetCore.Http.Validation.ValidatableTypeInfo
    {
        public GeneratedValidatableTypeInfo(
            Type type,
            ValidatablePropertyInfo[] members,
            bool implementsIValidatableObject,
            Type[]? validatableSubTypes = null) : base(type, members, implementsIValidatableObject, validatableSubTypes) { }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file class GeneratedValidatableInfoResolver : global::Microsoft.AspNetCore.Http.Validation.IValidatableInfoResolver
    {
        public global::Microsoft.AspNetCore.Http.Validation.ValidatableTypeInfo? GetValidatableTypeInfo(Type type)
        {
                    if (type == typeof(global::SubType))
        {
            return CreateSubType();
        }
        if (type == typeof(global::SubTypeWithInheritance))
        {
            return CreateSubTypeWithInheritance();
        }
        if (type == typeof(global::ComplexType))
        {
            return CreateComplexType();
        }

            return null;
        }

        // No-ops, rely on runtime code for ParameterInfo-based resolution
        public global::Microsoft.AspNetCore.Http.Validation.ValidatableParameterInfo? GetValidatableParameterInfo(global::System.Reflection.ParameterInfo parameterInfo)
        {
            return null;
        }

                    private ValidatableTypeInfo CreateSubType()
            {
                return new GeneratedValidatableTypeInfo(
    type: typeof(global::SubType),
    members: [
                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::SubType),
            propertyType: typeof(string),
            name: "RequiredProperty",
            displayName: "RequiredProperty",
            isEnumerable: false,
            isNullable: false,
            isRequired: true,
            hasValidatableType: false,
            validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RequiredAttribute), Array.Empty<object>(), new Dictionary<string, object>()) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RequiredAttribute")]),
                                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::SubType),
            propertyType: typeof(string),
            name: "StringWithLength",
            displayName: "StringWithLength",
            isEnumerable: false,
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.StringLengthAttribute), new object[] { 10 }, new Dictionary<string, object>()) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.StringLengthAttribute")])
    ],
    implementsIValidatableObject: false);
            }
            private ValidatableTypeInfo CreateSubTypeWithInheritance()
            {
                return new GeneratedValidatableTypeInfo(
    type: typeof(global::SubTypeWithInheritance),
    members: [
                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::SubTypeWithInheritance),
            propertyType: typeof(string),
            name: "EmailString",
            displayName: "EmailString",
            isEnumerable: false,
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.EmailAddressAttribute), Array.Empty<object>(), new Dictionary<string, object>()) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.EmailAddressAttribute")])
    ],
    implementsIValidatableObject: false,
                validatableSubTypes: [
                    typeof(SubType)
                ]);
            }
            private ValidatableTypeInfo CreateComplexType()
            {
                return new GeneratedValidatableTypeInfo(
    type: typeof(global::ComplexType),
    members: [
                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::ComplexType),
            propertyType: typeof(int),
            name: "IntegerWithRange",
            displayName: "IntegerWithRange",
            isEnumerable: false,
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RangeAttribute), new object[] { 10, 100 }, new Dictionary<string, object>()) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RangeAttribute")]),
                                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::ComplexType),
            propertyType: typeof(int),
            name: "IntegerWithRangeAndDisplayName",
            displayName: "Valid identifier",
            isEnumerable: false,
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RangeAttribute), new object[] { 10, 100 }, new Dictionary<string, object>()) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RangeAttribute")]),
                                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::ComplexType),
            propertyType: typeof(global::SubType),
            name: "PropertyWithMemberAttributes",
            displayName: "PropertyWithMemberAttributes",
            isEnumerable: false,
            isNullable: false,
            isRequired: true,
            hasValidatableType: true,
            validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RequiredAttribute), Array.Empty<object>(), new Dictionary<string, object>()) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RequiredAttribute")]),
                                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::ComplexType),
            propertyType: typeof(global::SubType),
            name: "PropertyWithoutMemberAttributes",
            displayName: "PropertyWithoutMemberAttributes",
            isEnumerable: false,
            isNullable: false,
            isRequired: false,
            hasValidatableType: true,
            validationAttributes: []),
                                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::ComplexType),
            propertyType: typeof(global::SubTypeWithInheritance),
            name: "PropertyWithInheritance",
            displayName: "PropertyWithInheritance",
            isEnumerable: false,
            isNullable: false,
            isRequired: false,
            hasValidatableType: true,
            validationAttributes: []),
                                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::ComplexType),
            propertyType: typeof(global::System.Collections.Generic.List<global::SubType>),
            name: "ListOfSubTypes",
            displayName: "ListOfSubTypes",
            isEnumerable: true,
            isNullable: false,
            isRequired: false,
            hasValidatableType: true,
            validationAttributes: []),
                                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::ComplexType),
            propertyType: typeof(int),
            name: "IntegerWithDerivedValidationAttribute",
            displayName: "IntegerWithDerivedValidationAttribute",
            isEnumerable: false,
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::DerivedValidationAttribute), Array.Empty<object>(), new Dictionary<string, object> { { "ErrorMessage", "Value must be an even number" } }) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::DerivedValidationAttribute")]),
                                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::ComplexType),
            propertyType: typeof(int),
            name: "IntegerWithCustomValidation",
            displayName: "IntegerWithCustomValidation",
            isEnumerable: false,
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.CustomValidationAttribute), new object[] { typeof(CustomValidators), "Validate" }, new Dictionary<string, object>()) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.CustomValidationAttribute")]),
                                new GeneratedValidatablePropertyInfo(
            containingType: typeof(global::ComplexType),
            propertyType: typeof(int),
            name: "PropertyWithMultipleAttributes",
            displayName: "PropertyWithMultipleAttributes",
            isEnumerable: false,
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::DerivedValidationAttribute), Array.Empty<object>(), new Dictionary<string, object>()) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::DerivedValidationAttribute"), ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RangeAttribute), new object[] { 10, 100 }, new Dictionary<string, object>()) ?? throw new InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RangeAttribute")])
    ],
    implementsIValidatableObject: false);
            }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file static class GeneratedServiceCollectionExtensions
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "2HbGOie6Bg3kAttRoJz64oEBAABQcm9ncmFtLmNz")]
        public static IServiceCollection AddValidation(this IServiceCollection services, Action<ValidationOptions>? configureOptions = null)
        {
            // Use non-extension method to avoid infinite recursion.
            return ValidationServiceCollectionExtensions.AddValidation(services, options =>
            {
                options.Resolvers.Insert(0, new GeneratedValidatableInfoResolver());
                if (configureOptions is not null)
                {
                    configureOptions(options);
                }
            });
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file static class ValidationAttributeCache
    {
        private sealed record CacheKey(Type AttributeType, object[] Arguments, IReadOnlyDictionary<string, object> NamedArguments);
        private static readonly ConcurrentDictionary<CacheKey, ValidationAttribute> _cache = new();

        public static ValidationAttribute? GetOrCreateValidationAttribute(
            Type attributeType,
            object[] arguments,
            IReadOnlyDictionary<string, object> namedArguments)
        {
            var key = new CacheKey(attributeType, arguments, namedArguments);
            return _cache.GetOrAdd(key, static k =>
            {
                var type = k.AttributeType;
                var args = k.Arguments;

                ValidationAttribute attribute;

                if (args.Length == 0)
                {
                    attribute = type switch
                    {
                        Type t when t == typeof(RequiredAttribute) => new RequiredAttribute(),
                        Type t when t == typeof(EmailAddressAttribute) => new EmailAddressAttribute(),
                        Type t when t == typeof(PhoneAttribute) => new PhoneAttribute(),
                        Type t when t == typeof(UrlAttribute) => new UrlAttribute(),
                        Type t when t == typeof(CreditCardAttribute) => new CreditCardAttribute(),
                        _ when typeof(ValidationAttribute).IsAssignableFrom(type) =>
                            (ValidationAttribute)Activator.CreateInstance(type)!
                    };
                }
                else if (type == typeof(CustomValidationAttribute) && args.Length == 2)
                {
                    // CustomValidationAttribute requires special handling
                    // First argument is a type, second is a method name
                    if (args[0] is Type validatingType && args[1] is string methodName)
                    {
                        attribute = new CustomValidationAttribute(validatingType, methodName);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid arguments for CustomValidationAttribute: Type and method name required");
                    }
                }
                else if (type == typeof(StringLengthAttribute))
                {
                    if (args[0] is int maxLength)
                        attribute = new StringLengthAttribute(maxLength);
                    else
                        throw new ArgumentException($"Invalid maxLength value for StringLengthAttribute: {args[0]}");
                }
                else if (type == typeof(MinLengthAttribute))
                {
                    if (args[0] is int length)
                        attribute = new MinLengthAttribute(length);
                    else
                        throw new ArgumentException($"Invalid length value for MinLengthAttribute: {args[0]}");
                }
                else if (type == typeof(MaxLengthAttribute))
                {
                    if (args[0] is int length)
                        attribute = new MaxLengthAttribute(length);
                    else
                        throw new ArgumentException($"Invalid length value for MaxLengthAttribute: {args[0]}");
                }
                else if (type == typeof(RangeAttribute) && args.Length == 2)
                {
                    if (args[0] is int min && args[1] is int max)
                        attribute = new RangeAttribute(min, max);
                    else if (args[0] is double dmin && args[1] is double dmax)
                        attribute = new RangeAttribute(dmin, dmax);
                    else
                        throw new ArgumentException($"Invalid range values for RangeAttribute: {args[0]}, {args[1]}");
                }
                else if (type == typeof(RegularExpressionAttribute))
                {
                    if (args[0] is string pattern)
                        attribute = new RegularExpressionAttribute(pattern);
                    else
                        throw new ArgumentException($"Invalid pattern for RegularExpressionAttribute: {args[0]}");
                }
                else if (type == typeof(CompareAttribute))
                {
                    if (args[0] is string otherProperty)
                        attribute = new CompareAttribute(otherProperty);
                    else
                        throw new ArgumentException($"Invalid otherProperty for CompareAttribute: {args[0]}");
                }
                else if (typeof(ValidationAttribute).IsAssignableFrom(type))
                {
                    var constructors = type.GetConstructors();
                    var success = false;
                    attribute = null!;

                    foreach (var constructor in constructors)
                    {
                        var parameters = constructor.GetParameters();
                        if (parameters.Length != args.Length)
                            continue;

                        var convertedArgs = new object[args.Length];
                        var canUseConstructor = true;

                        for (var i = 0; i < parameters.Length; i++)
                        {
                            try
                            {
                                if (args[i] != null && args[i].GetType() == parameters[i].ParameterType)
                                {
                                    // Type already matches, use as-is
                                    convertedArgs[i] = args[i];
                                }
                                else
                                {
                                    // Try to convert
                                    convertedArgs[i] = Convert.ChangeType(args[i], parameters[i].ParameterType);
                                }
                            }
                            catch
                            {
                                canUseConstructor = false;
                                break;
                            }
                        }

                        if (canUseConstructor)
                        {
                            attribute = (ValidationAttribute)Activator.CreateInstance(type, convertedArgs)!;
                            success = true;
                            break;
                        }
                    }

                    if (!success)
                    {
                        throw new ArgumentException($"Could not find a suitable constructor for validation attribute type: {type.FullName}");
                    }
                }
                else
                {
                    throw new ArgumentException($"Unsupported validation attribute type: {type.FullName}");
                }

                // Apply named arguments after construction
                foreach (var namedArg in k.NamedArguments)
                {
                    var prop = type.GetProperty(namedArg.Key);
                    if (prop != null && prop.CanWrite)
                    {
                        try
                        {
                            if (namedArg.Value != null && namedArg.Value.GetType() == prop.PropertyType)
                            {
                                // Type already matches, use as-is
                                prop.SetValue(attribute, namedArg.Value);
                            }
                            else
                            {
                                // Try to convert
                                prop.SetValue(attribute, Convert.ChangeType(namedArg.Value, prop.PropertyType));
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException($"Failed to set property {namedArg.Key} on {type.FullName}: {ex.Message}");
                        }
                    }
                }

                return attribute;
            });
        }
    }
}