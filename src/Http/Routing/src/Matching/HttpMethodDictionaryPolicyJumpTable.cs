// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class HttpMethodDictionaryPolicyJumpTable : PolicyJumpTable
{
    internal struct HttpMethodsJumpTable
    {
        public HttpMethodsJumpTable(int exitDestination)
        {
            ConnectDestination = exitDestination;
            DeleteDestination = exitDestination;
            GetDestination = exitDestination;
            HeadDestination = exitDestination;
            OptionsDestination = exitDestination;
            PatchDestination = exitDestination;
            PostDestination = exitDestination;
            PutDestination = exitDestination;
            TraceDestination = exitDestination;
        }

        public int ConnectDestination { get; private set; }
        public int DeleteDestination { get; private set; }
        public int GetDestination { get; private set; }
        public int HeadDestination { get; private set; }
        public int OptionsDestination { get; private set; }
        public int PatchDestination { get; private set; }
        public int PostDestination { get; private set; }
        public int PutDestination { get; private set; }
        public int TraceDestination { get; private set; }

        public bool TryGetKnownValue(string method, out int destination)
        {
            // Implementation skeleton taken from https://github.com/dotnet/runtime/blob/13b43155c31beb844b1b04766fea65235ccd8363/src/libraries/System.Net.Http/src/System/Net/Http/HttpMethod.cs#L179
            if (method.Length >= 3) // 3 == smallest known method
            {
                (string? matchedMethod, destination) = (method[0] | 0x20) switch
                {
                    'c' => (HttpMethods.Connect, ConnectDestination),
                    'd' => (HttpMethods.Delete, DeleteDestination),
                    'g' => (HttpMethods.Get, GetDestination),
                    'h' => (HttpMethods.Head, HeadDestination),
                    'o' => (HttpMethods.Options, OptionsDestination),
                    'p' => method.Length switch
                    {
                        3 => (HttpMethods.Put, PutDestination),
                        4 => (HttpMethods.Post, PostDestination),
                        _ => (HttpMethods.Patch, PatchDestination),
                    },
                    't' => (HttpMethods.Trace, TraceDestination),
                    _ => (null, 0),
                };

                return matchedMethod is not null && method.Equals(matchedMethod, StringComparison.OrdinalIgnoreCase);
            }
            destination = 0;
            return false;
        }

        public void Set(string method, int destination)
        {
            if (method.Length < 3) // 3 == smallest known method
            {
                return;
            }
            switch (method[0] | 0x20)
            {
                case 'c' when method.Equals(HttpMethods.Connect, StringComparison.OrdinalIgnoreCase):
                    ConnectDestination = destination;
                    break;
                case 'd' when method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase):
                    DeleteDestination = destination;
                    break;
                case 'g' when method.Equals(HttpMethods.Get, StringComparison.OrdinalIgnoreCase):
                    GetDestination = destination;
                    break;
                case 'h' when method.Equals(HttpMethods.Head, StringComparison.OrdinalIgnoreCase):
                    HeadDestination = destination;
                    break;
                case 'o' when method.Equals(HttpMethods.Options, StringComparison.OrdinalIgnoreCase):
                    OptionsDestination = destination;
                    break;
                case 'p' when method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase):
                    PutDestination = destination;
                    break;
                case 'p' when method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase):
                    PostDestination = destination;
                    break;
                case 'p' when method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase):
                    PatchDestination = destination;
                    break;
                case 't' when method.Equals(HttpMethods.Trace, StringComparison.OrdinalIgnoreCase):
                    TraceDestination = destination;
                    break;
            };
        }
    }

    private readonly int _exitDestination;
    private readonly HttpMethodsJumpTable _knownHttpMethodDestinations;
    private readonly Dictionary<string, int>? _destinations;
    private readonly int _corsPreflightExitDestination;
    private readonly Dictionary<string, int>? _corsPreflightDestinations;
    private readonly HttpMethodsJumpTable _corsPreflightHttpMethodDestinations;
    private readonly bool _supportsCorsPreflight;

    public HttpMethodDictionaryPolicyJumpTable(
        int exitDestination,
        Dictionary<string, int>? destinations,
        int corsPreflightExitDestination,
        Dictionary<string, int>? corsPreflightDestinations)
    {
        _exitDestination = exitDestination;
        _knownHttpMethodDestinations = new HttpMethodsJumpTable(exitDestination);
        _corsPreflightHttpMethodDestinations = new HttpMethodsJumpTable(corsPreflightExitDestination);
        if (destinations != null)
        {
            foreach (var item in destinations)
            {
                _knownHttpMethodDestinations.Set(item.Key, item.Value);
            }
        }
        if (corsPreflightDestinations != null)
        {
            foreach (var item in corsPreflightDestinations)
            {
                _corsPreflightHttpMethodDestinations.Set(item.Key, item.Value);
            }
        }

        _destinations = destinations;
        _corsPreflightExitDestination = corsPreflightExitDestination;
        _corsPreflightDestinations = corsPreflightDestinations;
        _supportsCorsPreflight = _corsPreflightDestinations != null && _corsPreflightDestinations.Count > 0;
    }

    public override int GetDestination(HttpContext httpContext)
    {
        int destination;
        var httpMethod = httpContext.Request.Method;
        if (_supportsCorsPreflight && HttpMethodMatcherPolicy.IsCorsPreflightRequest(httpContext, httpMethod, out var accessControlRequestMethod))
        {
            var corsHttpMethod = accessControlRequestMethod.ToString();
            if (_corsPreflightHttpMethodDestinations.TryGetKnownValue(corsHttpMethod, out destination))
            {
                return destination;
            }
            return _corsPreflightDestinations!.TryGetValue(corsHttpMethod, out destination)
                ? destination
                : _corsPreflightExitDestination;
        }
        if (_knownHttpMethodDestinations.TryGetKnownValue(httpMethod, out destination))
        {
            return destination;
        }

        return _destinations != null &&
            _destinations.TryGetValue(httpMethod, out destination) ? destination : _exitDestination;
    }
}
