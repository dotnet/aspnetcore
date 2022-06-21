// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint
/// that contains a <see cref="StatusCode"/>.
/// </summary>
public interface IStatusCodeHttpResult
{
    int StatusCode { get; }
}

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint
/// that contains an object <see cref="RawValue"/>.
/// </summary>
public interface IValueHttpResult
{
    object? RawValue { get; }
}

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint
/// that contains an <see cref="Value"/>.
/// </summary>
public interface IValueHttpResult<out TValue>
{
    TValue? Value { get; }
}

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint
/// that constains an <see cref="ContentType"/>.
/// </summary>
public interface IContentHttpResult
{
    string? ContentType { get; }
}

/// <summary>
/// Defines a contract that represents the file result of an HTTP endpoint.
/// </summary>
public interface IFileHttpResult
{
    string? ContentType { get; }
    string? FileDownloadName { get; }
}

/// <summary>
/// Defines a contract that represents the a multi <see cref="IResult"/> types.
/// </summary>
public interface INestedHttpResult
{
    IResult Result { get; }
}
