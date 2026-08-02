// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <inheritdoc />
/// <typeparam name="T">The <see cref="Type"/> of object that is going to be written in the response.</typeparam>
/// <remarks>
/// This is a derived generic variant of the <see cref="ProducesAttribute"/>.
/// Ensure that only one instance of either attribute is provided on the target.
/// </remarks>
public class ProducesAttribute<T> : ProducesAttribute
{
    /// <summary>
    /// Initializes an instance of <see cref="ProducesAttribute"/>.
    /// </summary>
    public ProducesAttribute() : base(typeof(T)) { }
}
