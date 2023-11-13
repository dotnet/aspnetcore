// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints.FormMapping;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Provides options for configuring server-side rendering of Razor Components.
/// </summary>
public sealed class RazorComponentsServiceOptions
{
    // Fairly long default lifetime to allow for clock skew across servers
    private TimeSpan _temporaryRedirectionUrlValidityDuration = TimeSpan.FromMinutes(5);

    internal FormDataMapperOptions _formMappingOptions = new();

    /// <summary>
    /// Gets or sets a value that determines whether to include detailed information on errors.
    /// </summary>
    public bool DetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of elements allowed in a form collection.
    /// </summary>
    public int MaxFormMappingCollectionSize
    {
        get => _formMappingOptions.MaxCollectionSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _formMappingOptions.MaxCollectionSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum depth allowed when recursively mapping form data.
    /// </summary>
    public int MaxFormMappingRecursionDepth
    {
        get => _formMappingOptions.MaxRecursionDepth;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 1);
            _formMappingOptions.MaxRecursionDepth = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of errors allowed when mapping form data.
    /// </summary>
    public int MaxFormMappingErrorCount
    {
        get => _formMappingOptions.MaxErrorCount;
        set
        {
            _formMappingOptions.MaxErrorCount = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum size of the buffer used to read form data keys.
    /// </summary>
    public int MaxFormMappingKeySize
    {
        get => _formMappingOptions.MaxKeyBufferSize;
        set => _formMappingOptions.MaxKeyBufferSize = value;
    }

    /// <summary>
    /// Gets or sets the lifetime of data protection validity for temporary redirection URLs
    /// emitted by Blazor server-side rendering. These are only used transiently so the lifetime
    /// only needs to be long enough for a client to receive the URL and begin navigation to it.
    /// However, it should also be long enough to allow for clock skew across servers.
    /// </summary>
    public TimeSpan TemporaryRedirectionUrlValidityDuration
    {
        get => _temporaryRedirectionUrlValidityDuration;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value.TotalMilliseconds, 0);
            _temporaryRedirectionUrlValidityDuration = value;
        }
    }

    internal string? JavaScriptInitializers { get; set; }
}
