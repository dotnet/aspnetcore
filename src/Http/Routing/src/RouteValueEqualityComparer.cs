// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// An <see cref="IEqualityComparer{Object}"/> implementation that compares objects as-if
/// they were route value strings.
/// </summary>
/// <remarks>
/// Values that are are not strings are converted to strings using
/// <c>Convert.ToString(x, CultureInfo.InvariantCulture)</c>. <c>null</c> values are converted
/// to the empty string.
///
/// strings are compared using <see cref="StringComparison.OrdinalIgnoreCase"/>.
/// </remarks>
#if !COMPONENTS
public class RouteValueEqualityComparer : IEqualityComparer<object?>
#else
internal class RouteValueEqualityComparer : IEqualityComparer<object?>
#endif
{
    /// <summary>
    /// A default instance of the <see cref="RouteValueEqualityComparer"/>.
    /// </summary>
    public static readonly RouteValueEqualityComparer Default = new RouteValueEqualityComparer();

    /// <inheritdoc />
    public new bool Equals(object? x, object? y)
    {
        var stringX = x as string ?? Convert.ToString(x, CultureInfo.InvariantCulture);
        var stringY = y as string ?? Convert.ToString(y, CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(stringX) && string.IsNullOrEmpty(stringY))
        {
            return true;
        }
        else
        {
            return string.Equals(stringX, stringY, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <inheritdoc />
    public int GetHashCode(object obj)
    {
        var stringObj = obj as string ?? Convert.ToString(obj, CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(stringObj))
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(string.Empty);
        }
        else
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(stringObj);
        }
    }
}
