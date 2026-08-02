// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class HttpMethodDestinationsLookup
{
    private readonly int _exitDestination;

    private readonly int _connectDestination;
    private readonly int _deleteDestination;
    private readonly int _getDestination;
    private readonly int _headDestination;
    private readonly int _optionsDestination;
    private readonly int _patchDestination;
    private readonly int _postDestination;
    private readonly int _putDestination;
    private readonly int _traceDestination;
    private readonly Dictionary<string, int>? _extraDestinations;

    public HttpMethodDestinationsLookup(List<KeyValuePair<string, int>> destinations, int exitDestination)
    {
        _exitDestination = exitDestination;

        int? connectDestination = null;
        int? deleteDestination = null;
        int? getDestination = null;
        int? headDestination = null;
        int? optionsDestination = null;
        int? patchDestination = null;
        int? postDestination = null;
        int? putDestination = null;
        int? traceDestination = null;

        foreach (var (method, destination) in destinations)
        {
            if (method.Length >= 3) // 3 == smallest known method
            {
                switch (method[0] | 0x20)
                {
                    case 'c' when method.Equals(HttpMethods.Connect, StringComparison.OrdinalIgnoreCase):
                        connectDestination = destination;
                        continue;
                    case 'd' when method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase):
                        deleteDestination = destination;
                        continue;
                    case 'g' when method.Equals(HttpMethods.Get, StringComparison.OrdinalIgnoreCase):
                        getDestination = destination;
                        continue;
                    case 'h' when method.Equals(HttpMethods.Head, StringComparison.OrdinalIgnoreCase):
                        headDestination = destination;
                        continue;
                    case 'o' when method.Equals(HttpMethods.Options, StringComparison.OrdinalIgnoreCase):
                        optionsDestination = destination;
                        continue;
                    case 'p':
                        if (method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase))
                        {
                            putDestination = destination;
                            continue;
                        }
                        else if (method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
                        {
                            postDestination = destination;
                            continue;
                        }
                        else if (method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase))
                        {
                            patchDestination = destination;
                            continue;
                        }
                        break;
                    case 't' when method.Equals(HttpMethods.Trace, StringComparison.OrdinalIgnoreCase):
                        traceDestination = destination;
                        continue;
                }
            }

            _extraDestinations ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _extraDestinations.Add(method, destination);
        }

        _connectDestination = connectDestination ?? _exitDestination;
        _deleteDestination = deleteDestination ?? _exitDestination;
        _getDestination = getDestination ?? _exitDestination;
        _headDestination = headDestination ?? _exitDestination;
        _optionsDestination = optionsDestination ?? _exitDestination;
        _patchDestination = patchDestination ?? _exitDestination;
        _postDestination = postDestination ?? _exitDestination;
        _putDestination = putDestination ?? _exitDestination;
        _traceDestination = traceDestination ?? _exitDestination;
    }

    public int GetDestination(string method)
    {
        // Implementation skeleton taken from https://github.com/dotnet/runtime/blob/13b43155c31beb844b1b04766fea65235ccd8363/src/libraries/System.Net.Http/src/System/Net/Http/HttpMethod.cs#L179
        if (method.Length >= 3) // 3 == smallest known method
        {
            (var matchedMethod, var destination) = (method[0] | 0x20) switch
            {
                'c' => (HttpMethods.Connect, _connectDestination),
                'd' => (HttpMethods.Delete, _deleteDestination),
                'g' => (HttpMethods.Get, _getDestination),
                'h' => (HttpMethods.Head, _headDestination),
                'o' => (HttpMethods.Options, _optionsDestination),
                'p' => method.Length switch
                {
                    3 => (HttpMethods.Put, _putDestination),
                    4 => (HttpMethods.Post, _postDestination),
                    _ => (HttpMethods.Patch, _patchDestination),
                },
                't' => (HttpMethods.Trace, _traceDestination),
                _ => (null, 0),
            };

            if (matchedMethod is not null && method.Equals(matchedMethod, StringComparison.OrdinalIgnoreCase))
            {
                return destination;
            }
        }

        if (_extraDestinations != null && _extraDestinations.TryGetValue(method, out var extraDestination))
        {
            return extraDestination;
        }

        return _exitDestination;
    }
}
