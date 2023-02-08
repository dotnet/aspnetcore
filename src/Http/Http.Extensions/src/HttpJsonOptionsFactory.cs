// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed class HttpJsonOptionsFactory : IOptionsFactory<JsonOptions>
{
    private readonly OptionsFactory<JsonOptions> _innerOptionsFactory;

    /// <summary>
    /// Initializes a new instance with the specified options configurations.
    /// </summary>
    /// <param name="setups">The configuration actions to run.</param>
    /// <param name="postConfigures">The initialization actions to run.</param>
    public HttpJsonOptionsFactory(IEnumerable<IConfigureOptions<JsonOptions>> setups, IEnumerable<IPostConfigureOptions<JsonOptions>> postConfigures)
        : this(setups, postConfigures, validations: Array.Empty<IValidateOptions<JsonOptions>>())
    { }

    /// <summary>
    /// Initializes a new instance with the specified options configurations.
    /// </summary>
    /// <param name="setups">The configuration actions to run.</param>
    /// <param name="postConfigures">The initialization actions to run.</param>
    /// <param name="validations">The validations to run.</param>
    public HttpJsonOptionsFactory(IEnumerable<IConfigureOptions<JsonOptions>> setups, IEnumerable<IPostConfigureOptions<JsonOptions>> postConfigures, IEnumerable<IValidateOptions<JsonOptions>> validations)
    {
        _innerOptionsFactory = new OptionsFactory<JsonOptions>(setups, postConfigures, validations);
    }

    public JsonOptions Create(string name)
    {
        var options = _innerOptionsFactory.Create(name);

        // After the options is completed created
        // we need to ensure we have it configured for
        // reflection (if needed) and marked as read-only
        options.EnsureConfigured();

        return options;
    }
}
