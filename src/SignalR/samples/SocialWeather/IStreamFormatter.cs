// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SocialWeather;

public interface IStreamFormatter<T>
{
    Task<T> ReadAsync(Stream stream);
    Task WriteAsync(T value, Stream stream);
}
