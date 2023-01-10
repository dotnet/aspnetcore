// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Wraps the object of type <see cref="Microsoft.AspNetCore.Mvc.SerializableError"/>.
/// </summary>
public class SerializableErrorWrapperProvider : IWrapperProvider
{
    /// <inheritdoc />
    public Type WrappingType => typeof(SerializableErrorWrapper);

    /// <inheritdoc />
    public object? Wrap(object? original)
    {
        ArgumentNullException.ThrowIfNull(original);

        var error = original as SerializableError;
        if (error == null)
        {
            throw new ArgumentException(
                Resources.FormatWrapperProvider_MismatchType(
                    typeof(SerializableErrorWrapper).Name,
                    original.GetType().Name),
                nameof(original));
        }

        return new SerializableErrorWrapper(error);
    }
}
