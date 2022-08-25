// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Indicates that the specified method requires dynamic access to code that is not referenced
/// statically, for example through <see cref="System.Reflection"/>.
/// </summary>
/// <remarks>
/// This allows tools to understand which methods are unsafe to call when removing unreferenced
/// code from an application.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
internal sealed class RequiresUnreferencedCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresUnreferencedCodeAttribute"/> class
    /// with the specified message.
    /// </summary>
    /// <param name="message">
    /// A message that contains information about the usage of unreferenced code.
    /// </param>
    public RequiresUnreferencedCodeAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets a message that contains information about the usage of unreferenced code.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets or sets an optional URL that contains more information about the method,
    /// why it requires unreferenced code, and what options a consumer has to deal with it.
    /// </summary>
    public string? Url { get; set; }
}

/// <summary>
/// Suppresses reporting of a specific rule violation, allowing multiple suppressions on a
/// single code artifact.
/// </summary>
/// <remarks>
/// <see cref="UnconditionalSuppressMessageAttribute"/> is different than
/// <see cref="SuppressMessageAttribute"/> in that it doesn't have a
/// <see cref="ConditionalAttribute"/>. So it is always preserved in the compiled assembly.
/// </remarks>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
internal sealed class UnconditionalSuppressMessageAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnconditionalSuppressMessageAttribute"/>
    /// class, specifying the category of the tool and the identifier for an analysis rule.
    /// </summary>
    /// <param name="category">The category for the attribute.</param>
    /// <param name="checkId">The identifier of the analysis rule the attribute applies to.</param>
    public UnconditionalSuppressMessageAttribute(string category, string checkId)
    {
        Category = category;
        CheckId = checkId;
    }

    /// <summary>
    /// Gets the category identifying the classification of the attribute.
    /// </summary>
    /// <remarks>
    /// The <see cref="Category"/> property describes the tool or tool analysis category
    /// for which a message suppression attribute applies.
    /// </remarks>
    public string Category { get; }

    /// <summary>
    /// Gets the identifier of the analysis tool rule to be suppressed.
    /// </summary>
    /// <remarks>
    /// Concatenated together, the <see cref="Category"/> and <see cref="CheckId"/>
    /// properties form a unique check identifier.
    /// </remarks>
    public string CheckId { get; }

    /// <summary>
    /// Gets or sets the scope of the code that is relevant for the attribute.
    /// </summary>
    /// <remarks>
    /// The Scope property is an optional argument that specifies the metadata scope for which
    /// the attribute is relevant.
    /// </remarks>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets a fully qualified path that represents the target of the attribute.
    /// </summary>
    /// <remarks>
    /// The <see cref="Target"/> property is an optional argument identifying the analysis target
    /// of the attribute. An example value is "System.IO.Stream.ctor():System.Void".
    /// Because it is fully qualified, it can be long, particularly for targets such as parameters.
    /// The analysis tool user interface should be capable of automatically formatting the parameter.
    /// </remarks>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets an optional argument expanding on exclusion criteria.
    /// </summary>
    /// <remarks>
    /// The <see cref="MessageId "/> property is an optional argument that specifies additional
    /// exclusion where the literal metadata target is not sufficiently precise. For example,
    /// the <see cref="UnconditionalSuppressMessageAttribute"/> cannot be applied within a method,
    /// and it may be desirable to suppress a violation against a statement in the method that will
    /// give a rule violation, but not against all statements in the method.
    /// </remarks>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the justification for suppressing the code analysis message.
    /// </summary>
    public string? Justification { get; set; }
}

[AttributeUsage(
        AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
        AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
        AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
        Inherited = false)]
internal sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicallyAccessedMembersAttribute"/> class
    /// with the specified member types.
    /// </summary>
    /// <param name="memberTypes">The types of members dynamically accessed.</param>
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
    {
        MemberTypes = memberTypes;
    }

    /// <summary>
    /// Gets the <see cref="DynamicallyAccessedMemberTypes"/> which specifies the type
    /// of members dynamically accessed.
    /// </summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

/// <summary>
/// Specifies the types of members that are dynamically accessed.
///
/// This enumeration has a <see cref="FlagsAttribute"/> attribute that allows a
/// bitwise combination of its member values.
/// </summary>
[Flags]
internal enum DynamicallyAccessedMemberTypes
{
    /// <summary>
    /// Specifies no members.
    /// </summary>
    None = 0,

    /// <summary>
    /// Specifies the default, parameterless public constructor.
    /// </summary>
    PublicParameterlessConstructor = 0x0001,

    /// <summary>
    /// Specifies all public constructors.
    /// </summary>
    PublicConstructors = 0x0002 | PublicParameterlessConstructor,

    /// <summary>
    /// Specifies all non-public constructors.
    /// </summary>
    NonPublicConstructors = 0x0004,

    /// <summary>
    /// Specifies all public methods.
    /// </summary>
    PublicMethods = 0x0008,

    /// <summary>
    /// Specifies all non-public methods.
    /// </summary>
    NonPublicMethods = 0x0010,

    /// <summary>
    /// Specifies all public fields.
    /// </summary>
    PublicFields = 0x0020,

    /// <summary>
    /// Specifies all non-public fields.
    /// </summary>
    NonPublicFields = 0x0040,

    /// <summary>
    /// Specifies all public nested types.
    /// </summary>
    PublicNestedTypes = 0x0080,

    /// <summary>
    /// Specifies all non-public nested types.
    /// </summary>
    NonPublicNestedTypes = 0x0100,

    /// <summary>
    /// Specifies all public properties.
    /// </summary>
    PublicProperties = 0x0200,

    /// <summary>
    /// Specifies all non-public properties.
    /// </summary>
    NonPublicProperties = 0x0400,

    /// <summary>
    /// Specifies all public events.
    /// </summary>
    PublicEvents = 0x0800,

    /// <summary>
    /// Specifies all non-public events.
    /// </summary>
    NonPublicEvents = 0x1000,

    /// <summary>
    /// Specifies all interfaces implemented by the type.
    /// </summary>
    Interfaces = 0x2000,

    /// <summary>
    /// Specifies all members.
    /// </summary>
    All = ~None
}
