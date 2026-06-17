// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Marks a test as "Quarantined" so that the build will sequester it and ignore failures.
/// </summary>
/// <remarks>
/// <para>
/// This attribute works by applying xUnit.net "Traits" based on the criteria specified in the attribute
/// properties. Once these traits are applied, build scripts can include/exclude tests based on them.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Fact]
/// [QuarantinedTest("Github Url")]
/// public void FlakyTest()
/// {
///     // Flakiness
/// }
/// </code>
///
/// <para>
/// The above example generates the following facet:
/// </para>
///
/// <list type="bullet">
/// <item>
///     <description><c>Quarantined</c> = <c>true</c></description>
/// </item>
/// </list>
/// </example>
[TraitDiscoverer("Microsoft.AspNetCore.InternalTesting." + nameof(QuarantinedTestTraitDiscoverer), "Microsoft.AspNetCore.InternalTesting")]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class QuarantinedTestAttribute : Attribute, ITraitAttribute
{
    /// <summary>
    /// Gets an optional reason for the quarantining, such as a link to a GitHub issue URL with more details as to why the test is quarantined.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuarantinedTestAttribute"/> class with an optional <see cref="Reason"/>.
    /// </summary>
    /// <param name="reason">A reason that this test is quarantined. Preferably a Github issue Url.</param>
    public QuarantinedTestAttribute(string reason)
    {
        Reason = reason;
    }
}
