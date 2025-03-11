﻿//HintName: ValidatableInfoResolver.g.cs
#nullable enable annotations
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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file sealed class GeneratedValidatablePropertyInfo : global::Microsoft.AspNetCore.Http.Validation.ValidatablePropertyInfo
    {
        private readonly global::System.ComponentModel.DataAnnotations.ValidationAttribute[] _validationAttributes;

        public GeneratedValidatablePropertyInfo(
            global::System.Type containingType,
            global::System.Type propertyType,
            string name,
            string displayName,
            global::System.ComponentModel.DataAnnotations.ValidationAttribute[] validationAttributes) : base(containingType, propertyType, name, displayName)
        {
            _validationAttributes = validationAttributes;
        }

        protected override global::System.ComponentModel.DataAnnotations.ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file sealed class GeneratedValidatableTypeInfo : global::Microsoft.AspNetCore.Http.Validation.ValidatableTypeInfo
    {
        public GeneratedValidatableTypeInfo(
            global::System.Type type,
            ValidatablePropertyInfo[] members) : base(type, members) { }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file class GeneratedValidatableInfoResolver : global::Microsoft.AspNetCore.Http.Validation.IValidatableInfoResolver
    {
        public bool TryGetValidatableTypeInfo(global::System.Type type, [global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out global::Microsoft.AspNetCore.Http.Validation.IValidatableInfo? validatableInfo)
        {
            validatableInfo = null;
        if (type == typeof(global::SubType))
        {
            validatableInfo = CreateSubType();
            return true;
        }
        if (type == typeof(global::SubTypeWithInheritance))
        {
            validatableInfo = CreateSubTypeWithInheritance();
            return true;
        }
        if (type == typeof(global::ComplexType))
        {
            validatableInfo = CreateComplexType();
            return true;
        }

            return false;
        }

        // No-ops, rely on runtime code for ParameterInfo-based resolution
        public bool TryGetValidatableParameterInfo(global::System.Reflection.ParameterInfo parameterInfo, [global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out global::Microsoft.AspNetCore.Http.Validation.IValidatableInfo? validatableInfo)
        {
            validatableInfo = null;
            return false;
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
                        validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RequiredAttribute), [], []) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RequiredAttribute")]
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::SubType),
                        propertyType: typeof(string),
                        name: "StringWithLength",
                        displayName: "StringWithLength",
                        validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.StringLengthAttribute), [10], []) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.StringLengthAttribute")]
                    ),
                ]
            );
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
                        validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.EmailAddressAttribute), [], []) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.EmailAddressAttribute")]
                    ),
                ]
            );
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
                        validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RangeAttribute), [10, 100], []) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RangeAttribute")]
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::ComplexType),
                        propertyType: typeof(int),
                        name: "IntegerWithRangeAndDisplayName",
                        displayName: "Valid identifier",
                        validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RangeAttribute), [10, 100], []) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RangeAttribute")]
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::ComplexType),
                        propertyType: typeof(global::SubType),
                        name: "PropertyWithMemberAttributes",
                        displayName: "PropertyWithMemberAttributes",
                        validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RequiredAttribute), [], []) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RequiredAttribute")]
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::ComplexType),
                        propertyType: typeof(global::SubType),
                        name: "PropertyWithoutMemberAttributes",
                        displayName: "PropertyWithoutMemberAttributes",
                        validationAttributes: []
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::ComplexType),
                        propertyType: typeof(global::SubTypeWithInheritance),
                        name: "PropertyWithInheritance",
                        displayName: "PropertyWithInheritance",
                        validationAttributes: []
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::ComplexType),
                        propertyType: typeof(global::System.Collections.Generic.List<global::SubType>),
                        name: "ListOfSubTypes",
                        displayName: "ListOfSubTypes",
                        validationAttributes: []
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::ComplexType),
                        propertyType: typeof(int),
                        name: "IntegerWithDerivedValidationAttribute",
                        displayName: "IntegerWithDerivedValidationAttribute",
                        validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::DerivedValidationAttribute), [], new global::System.Collections.Generic.Dictionary<string, object> { { "ErrorMessage", "Value must be an even number" } }) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::DerivedValidationAttribute")]
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::ComplexType),
                        propertyType: typeof(int),
                        name: "IntegerWithCustomValidation",
                        displayName: "IntegerWithCustomValidation",
                        validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.CustomValidationAttribute), [typeof(CustomValidators), "Validate"], []) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.CustomValidationAttribute")]
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::ComplexType),
                        propertyType: typeof(int),
                        name: "PropertyWithMultipleAttributes",
                        displayName: "PropertyWithMultipleAttributes",
                        validationAttributes: [ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::DerivedValidationAttribute), [], []) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::DerivedValidationAttribute"), ValidationAttributeCache.GetOrCreateValidationAttribute(typeof(global::System.ComponentModel.DataAnnotations.RangeAttribute), [10, 100], []) ?? throw new global::System.InvalidOperationException(@"Failed to create validation attribute global::System.ComponentModel.DataAnnotations.RangeAttribute")]
                    ),
                ]
            );
        }

    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file static class GeneratedServiceCollectionExtensions
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "I71YCOnkIuFyp29JNyKEXIEBAABQcm9ncmFtLmNz")]
        public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddValidation(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::System.Action<ValidationOptions>? configureOptions = null)
        {
            // Use non-extension method to avoid infinite recursion.
            return global::Microsoft.Extensions.DependencyInjection.ValidationServiceCollectionExtensions.AddValidation(services, options =>
            {
                options.Resolvers.Insert(0, new GeneratedValidatableInfoResolver());
                if (configureOptions is not null)
                {
                    configureOptions(options);
                }
            });
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file static class ValidationAttributeCache
    {
        private sealed record CacheKey(global::System.Type AttributeType, object[] Arguments, global::System.Collections.Generic.Dictionary<string, object> NamedArguments);
        private static readonly global::System.Collections.Concurrent.ConcurrentDictionary<CacheKey, global::System.ComponentModel.DataAnnotations.ValidationAttribute> _cache = new();

        public static global::System.ComponentModel.DataAnnotations.ValidationAttribute? GetOrCreateValidationAttribute(
            global::System.Type attributeType,
            object[] arguments,
            global::System.Collections.Generic.Dictionary<string, object> namedArguments)
        {
            var key = new CacheKey(attributeType, arguments, namedArguments);
            return _cache.GetOrAdd(key, static k =>
            {
                var type = k.AttributeType;
                var args = k.Arguments;

                global::System.ComponentModel.DataAnnotations.ValidationAttribute? attribute;

                if (args.Length == 0)
                {
                    attribute = type switch
                    {
                        global::System.Type t when t == typeof(global::System.ComponentModel.DataAnnotations.RequiredAttribute) => new global::System.ComponentModel.DataAnnotations.RequiredAttribute(),
                        global::System.Type t when t == typeof(global::System.ComponentModel.DataAnnotations.EmailAddressAttribute) => new global::System.ComponentModel.DataAnnotations.EmailAddressAttribute(),
                        global::System.Type t when t == typeof(global::System.ComponentModel.DataAnnotations.PhoneAttribute) => new global::System.ComponentModel.DataAnnotations.PhoneAttribute(),
                        global::System.Type t when t == typeof(global::System.ComponentModel.DataAnnotations.UrlAttribute) => new global::System.ComponentModel.DataAnnotations.UrlAttribute(),
                        global::System.Type t when t == typeof(global::System.ComponentModel.DataAnnotations.CreditCardAttribute) => new global::System.ComponentModel.DataAnnotations.CreditCardAttribute(),
                        _ when typeof(global::System.ComponentModel.DataAnnotations.ValidationAttribute).IsAssignableFrom(type) =>
                            (global::System.ComponentModel.DataAnnotations.ValidationAttribute)global::System.Activator.CreateInstance(type)!,
                        _ => throw new global::System.ArgumentException($"Unsupported validation attribute type: {type.FullName}")
                    };
                }
                else if (type == typeof(global::System.ComponentModel.DataAnnotations.CustomValidationAttribute) && args.Length == 2)
                {
                    // CustomValidationAttribute requires special handling
                    // First argument is a type, second is a method name
                    if (args[0] is global::System.Type validatingType && args[1] is string methodName)
                    {
                        attribute = new global::System.ComponentModel.DataAnnotations.CustomValidationAttribute(validatingType, methodName);
                    }
                    else
                    {
                        throw new global::System.ArgumentException($"Invalid arguments for CustomValidationAttribute: Type and method name required");
                    }
                }
                else if (type == typeof(global::System.ComponentModel.DataAnnotations.StringLengthAttribute))
                {
                    if (args.Length > 0 && args[0] is int maxLength)
                    {
                        attribute = new global::System.ComponentModel.DataAnnotations.StringLengthAttribute(maxLength);
                    }
                    else
                    {
                        throw new global::System.ArgumentException($"Invalid maxLength value for StringLengthAttribute: {args[0]}");
                    }
                }
                else if (type == typeof(global::System.ComponentModel.DataAnnotations.MinLengthAttribute))
                {
                    if (args[0] is int length)
                    {
                        attribute = new global::System.ComponentModel.DataAnnotations.MinLengthAttribute(length);
                    }
                    else
                    {
                        throw new global::System.ArgumentException($"Invalid length value for MinLengthAttribute: {args[0]}");
                    }
                }
                else if (type == typeof(global::System.ComponentModel.DataAnnotations.MaxLengthAttribute))
                {
                    if (args[0] is int length)
                    {
                        attribute = new global::System.ComponentModel.DataAnnotations.MaxLengthAttribute(length);
                    }
                    else
                    {
                        throw new global::System.ArgumentException($"Invalid length value for MaxLengthAttribute: {args[0]}");
                    }
                }
                else if (type == typeof(global::System.ComponentModel.DataAnnotations.RangeAttribute) && args.Length == 2)
                {
                    if (args[0] is int min && args[1] is int max)
                    {
                        attribute = new global::System.ComponentModel.DataAnnotations.RangeAttribute(min, max);
                    }
                    else if (args[0] is double dmin && args[1] is double dmax)
                    {
                        attribute = new global::System.ComponentModel.DataAnnotations.RangeAttribute(dmin, dmax);
                    }
                    else
                    {
                        throw new global::System.ArgumentException($"Invalid range values for RangeAttribute: {args[0]}, {args[1]}");
                    }
                }
                else if (type == typeof(global::System.ComponentModel.DataAnnotations.RegularExpressionAttribute))
                {
                    if (args[0] is string pattern)
                    {
                        attribute = new global::System.ComponentModel.DataAnnotations.RegularExpressionAttribute(pattern);
                    }
                    else
                    {
                        throw new global::System.ArgumentException($"Invalid pattern for RegularExpressionAttribute: {args[0]}");
                    }
                }
                else if (type == typeof(global::System.ComponentModel.DataAnnotations.CompareAttribute))
                {
                    if (args[0] is string otherProperty)
                    {
                        attribute = new global::System.ComponentModel.DataAnnotations.CompareAttribute(otherProperty);
                    }
                    else
                    {
                        throw new global::System.ArgumentException($"Invalid otherProperty for CompareAttribute: {args[0]}");
                    }
                }
                else if (typeof(global::System.ComponentModel.DataAnnotations.ValidationAttribute).IsAssignableFrom(type))
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
                                    convertedArgs[i] = global::System.Convert.ChangeType(args[i], parameters[i].ParameterType);
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
                            attribute = (global::System.ComponentModel.DataAnnotations.ValidationAttribute)global::System.Activator.CreateInstance(type, convertedArgs)!;
                            success = true;
                            break;
                        }
                    }

                    if (!success)
                    {
                        throw new global::System.ArgumentException($"Could not find a suitable constructor for validation attribute type: {type.FullName}");
                    }
                }
                else
                {
                    throw new global::System.ArgumentException($"Unsupported validation attribute type: {type.FullName}");
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
                                prop.SetValue(attribute, global::System.Convert.ChangeType(namedArg.Value, prop.PropertyType));
                            }
                        }
                        catch (global::System.Exception ex)
                        {
                            throw new global::System.ArgumentException($"Failed to set property {namedArg.Key} on {type.FullName}: {ex.Message}");
                        }
                    }
                }

                return attribute;
            });
        }
    }
}