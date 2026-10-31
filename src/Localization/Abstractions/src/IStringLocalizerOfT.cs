// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Localization;

/// <summary>
/// Represents an <see cref="IStringLocalizer"/> that provides strings for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The <see cref="System.Type"/> to provide strings for.</typeparam>
public interface IStringLocalizer<out T> : IStringLocalizer
{
}
