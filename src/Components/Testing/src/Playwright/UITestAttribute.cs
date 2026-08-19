// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Testing.Playwright;

/// <summary>
/// Marks a <c>partial</c> E2E UI test class for source generation. The bundled generator
/// emits the test-framework binding onto the annotated class — for MSTest that is
/// <c>[TestClass]</c>, a <c>TestContext</c> property, and the
/// <c>[TestInitialize]</c>/<c>[TestCleanup]</c> hooks that drive the
/// <see cref="UITest.InitializeCoreAsync"/> / <see cref="UITest.CleanupCoreAsync"/>
/// lifecycle and attach diagnostics.
/// </summary>
/// <remarks>
/// Apply to a class that derives from <see cref="PlaywrightTest"/> (or one of its
/// subclasses) and is declared <c>partial</c>. Using this attribute lets the test class
/// stay free of any hand-written test-framework wiring while the library itself takes no
/// dependency on a specific test framework.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UITestAttribute : Attribute
{
}
