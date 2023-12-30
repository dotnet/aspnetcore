// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

// Guide to making breaking behavior changes in MVC:
//
// Hello, if you're reading this, you're probably trying to make a change in behavior in MVC in a minor
// version. Every change in behavior is a breaking change to someone, even if a feature was buggy or
// broken in some scenarios.
//
// To help make things easier for current users, we don't automatically opt users into breaking changes when
// they upgrade applications to a new minor version of ASP.NET Core. It's a separate choice to opt in to new
// behaviors in a minor release.
//
// To make things better for future users, we also want to provide an easy way for applications to get
// access to the new behaviors. We make changes when they are improvements, and if we're changing something
// we've already shipped, it must add value for all of our users (eventually). To this end, new applications
// created using the template are always opted in to the 'current' version.
//
// This means that all changes in behavior should be opt-in.
//
// -----
//
// Moving on from general philosophy, here's how to implement a behavior change and corresponding
// compatibility switch.
//
// Add a new property on options that uses a CompatibilitySwitch<T> as a backing field. Make sure the
// new switch is exposed by implementing IEnumerable<ICompatibilitySwitch> on the options class. Pass the
// property name to the CompatibilitySwitch constructor using nameof.
//
// Choose a boolean value or a new enum type as the 'value' of the property.
//
// If the new property has a boolean value, it should be named something like `SuppressFoo`
// (if the new value deactivates some behavior) or like `AllowFoo` (if the new value enables some behavior).
// Choose a name so that the old behavior equates to 'false'.
//
// If it's an enum, make sure you initialize the compatibility switch using the
// CompatibilitySwitch(string, value) constructor to make it obvious the correct value is passed in. It's
// a good idea to equate the original behavior with the default enum value as well.
//
// Then create (or modify) a subclass of ConfigureCompatibilityOptions appropriate for your options type.
// Override the DefaultValues property and provide appropriate values based on the value of the Version
// property. If you just added this class, register it as an IPostConfigureOptions<TOptions> in DI.
//
/// <summary>
/// Infrastructure supporting the implementation of <see cref="CompatibilityVersion"/>. This is an
/// implementation of <see cref="ICompatibilitySwitch"/> suitable for use with the <see cref="IOptions{T}"/>
/// pattern. This is framework infrastructure and should not be used by application code.
/// </summary>
/// <typeparam name="TValue">The type of value associated with the compatibility switch.</typeparam>
public class CompatibilitySwitch<TValue> : ICompatibilitySwitch where TValue : struct
{
    private TValue _value;

    /// <summary>
    /// Creates a new compatibility switch with the provided name.
    /// </summary>
    /// <param name="name">
    /// The compatibility switch name. The name must match a property name on an options type.
    /// </param>
    public CompatibilitySwitch(string name)
        : this(name, default)
    {
    }

    /// <summary>
    /// Creates a new compatibility switch with the provided name and initial value.
    /// </summary>
    /// <param name="name">
    /// The compatibility switch name. The name must match a property name on an options type.
    /// </param>
    /// <param name="initialValue">
    /// The initial value to assign to the switch.
    /// </param>
    public CompatibilitySwitch(string name, TValue initialValue)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        _value = initialValue;
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="Value"/> property has been set.
    /// </summary>
    /// <remarks>
    /// This is used by the compatibility infrastructure to determine whether the application developer
    /// has set explicitly set the value associated with this switch.
    /// </remarks>
    public bool IsValueSet { get; private set; }

    /// <summary>
    /// Gets the name of the compatibility switch.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or set the value associated with the compatibility switch.
    /// </summary>
    /// <remarks>
    /// Setting the switch value using <see cref="Value"/> will set <see cref="IsValueSet"/> to <c>true</c>.
    /// As a consequence, the compatibility infrastructure will consider this switch explicitly configured by
    /// the application developer, and will not apply a default value based on the compatibility version.
    /// </remarks>
    public TValue Value
    {
        get => _value;
        set
        {
            IsValueSet = true;
            _value = value;
        }
    }

    // Called by the compatibility infrastructure to set a default value when IsValueSet is false.
    object ICompatibilitySwitch.Value
    {
        get => Value;
        set => Value = (TValue)value;
    }
}
