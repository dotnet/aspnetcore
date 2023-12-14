// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// From https://github.com/dotnet/runtime/blob/ef72b95937703e485fdbbb75f3251fedfd1a0ef9/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs

namespace System.Diagnostics.CodeAnalysis;

// Attributes added in netstandard2.1
#if !NETSTANDARD2_1_OR_GREATER
/// <summary>Specifies that null is allowed as an input even if the corresponding type disallows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class AllowNullAttribute : Attribute
{ }

/// <summary>Specifies that null is disallowed as an input even if the corresponding type allows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class DisallowNullAttribute : Attribute
{ }

/// <summary>Specifies that an output may be null even if the corresponding type disallows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class MaybeNullAttribute : Attribute
{ }

/// <summary>Specifies that an output will not be null even if the corresponding type allows it. Specifies that an input argument was not null when the call returns.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class NotNullAttribute : Attribute
{ }

/// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter may be null even if the corresponding type disallows it.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class MaybeNullWhenAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified return value condition.</summary>
    /// <param name="returnValue">
    /// The return value condition. If the method returns this value, the associated parameter may be null.
    /// </param>
    public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

    /// <summary>Gets the return value condition.</summary>
    public bool ReturnValue { get; }
}

/// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the corresponding type allows it.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class NotNullWhenAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified return value condition.</summary>
    /// <param name="returnValue">
    /// The return value condition. If the method returns this value, the associated parameter will not be null.
    /// </param>
    public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

    /// <summary>Gets the return value condition.</summary>
    public bool ReturnValue { get; }
}

/// <summary>Specifies that the output will be non-null if the named parameter is non-null.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class NotNullIfNotNullAttribute : Attribute
{
    /// <summary>Initializes the attribute with the associated parameter name.</summary>
    /// <param name="parameterName">
    /// The associated parameter name.  The output will be non-null if the argument to the parameter specified is non-null.
    /// </param>
    public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;

    /// <summary>Gets the associated parameter name.</summary>
    public string ParameterName { get; }
}

/// <summary>Applied to a method that will never return under any circumstance.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class DoesNotReturnAttribute : Attribute
{ }

/// <summary>Specifies that the method will not return if the associated Boolean parameter is passed the specified value.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class DoesNotReturnIfAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified parameter value.</summary>
    /// <param name="parameterValue">
    /// The condition parameter value. Code after the method will be considered unreachable by diagnostics if the argument to
    /// the associated parameter matches this value.
    /// </param>
    public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;

    /// <summary>Gets the condition parameter value.</summary>
    public bool ParameterValue { get; }
}
#endif

// Attributes added in 5.0
#if NETSTANDARD || NETFRAMEWORK
/// <summary>Specifies that the method or property will ensure that the listed field and property members have not-null values.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class MemberNotNullAttribute : Attribute
{
    /// <summary>Initializes the attribute with a field or property member.</summary>
    /// <param name="member">
    /// The field or property member that is promised to be not-null.
    /// </param>
    public MemberNotNullAttribute(string member) => Members = new[] { member };

    /// <summary>Initializes the attribute with the list of field and property members.</summary>
    /// <param name="members">
    /// The list of field and property members that are promised to be not-null.
    /// </param>
    public MemberNotNullAttribute(params string[] members) => Members = members;

    /// <summary>Gets field or property member names.</summary>
    public string[] Members { get; }
}

/// <summary>Specifies that the method or property will ensure that the listed field and property members have not-null values when returning with the specified return value condition.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
        sealed class MemberNotNullWhenAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
    /// <param name="returnValue">
    /// The return value condition. If the method returns this value, the associated parameter will not be null.
    /// </param>
    /// <param name="member">
    /// The field or property member that is promised to be not-null.
    /// </param>
    public MemberNotNullWhenAttribute(bool returnValue, string member)
    {
        ReturnValue = returnValue;
        Members = new[] { member };
    }

    /// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
    /// <param name="returnValue">
    /// The return value condition. If the method returns this value, the associated parameter will not be null.
    /// </param>
    /// <param name="members">
    /// The list of field and property members that are promised to be not-null.
    /// </param>
    public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
    {
        ReturnValue = returnValue;
        Members = members;
    }

    /// <summary>Gets the return value condition.</summary>
    public bool ReturnValue { get; }

    /// <summary>Gets field or property member names.</summary>
    public string[] Members { get; }
}
#endif
