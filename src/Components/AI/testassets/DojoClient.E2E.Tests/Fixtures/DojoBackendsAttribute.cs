// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using DojoAgent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Fixtures;

/// <summary>
/// Supplies one independently discovered data row per <see cref="DojoBackendKind"/> member to a
/// data-driven test method, in place of a <c>[DataRow]</c> pair per backend.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DojoBackendsAttribute : Attribute, ITestDataSource
{
    /// <inheritdoc />
    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        foreach (var backend in Enum.GetValues<DojoBackendKind>())
        {
            yield return [backend];
        }
    }

    /// <inheritdoc />
    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
        => data is [DojoBackendKind backend] ? $"{methodInfo.Name} ({backend})" : null;
}
