// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Testing
{
    public static class ResponseAssert
    {
        public static Uri IsRedirect(HttpResponseMessage responseMessage)
        {
            Assert.Equal(HttpStatusCode.Redirect, responseMessage.StatusCode);
            return responseMessage.Headers.Location;
        }

        public static void IsOK(HttpResponseMessage responseMessage)
        {
            Assert.Equal(HttpStatusCode.OK, responseMessage.StatusCode);
        }

        public static SetCookieHeaderValue HasCookie(string name, HttpResponseMessage response, CookieComparison comparison)
        {
            var setCookieHeaderValue = new SetCookieHeaderValue(new StringSegment(name));
            var foundCookie = HasCookieCore(setCookieHeaderValue, response, new SetCookieComparer(comparison));

            if (foundCookie != null)
            {
                return foundCookie;
            }

            var suffix = comparison.HasFlag(CookieComparison.NameStartsWith) ? "starting with" : "";
            Assert.True(false, $"Couldn't find a cookie with a name {suffix} '{name}'");

            return null;
        }

        public static SetCookieHeaderValue HasCookie(
            SetCookieHeaderValue expectedSetCookieHeader,
            HttpResponseMessage response)
        {
            return HasCookie(expectedSetCookieHeader, response, SetCookieComparer.Default);
        }

        public static SetCookieHeaderValue HasCookie(
            SetCookieHeaderValue expectedSetCookieHeader,
            HttpResponseMessage response,
            params CookieComparison[] criteria)
        {
            return HasCookie(expectedSetCookieHeader, response, new SetCookieComparer(criteria.Aggregate((l, r) => l | r)));
        }

        public static TCaptured LocationHasQueryParameters<TCaptured>(HttpResponseMessage response, params ValueSpecification[] values)
            where TCaptured : new()
        {
            var queryParameters = LocationHasQueryParameters(response, values);

            var result = new TCaptured();

            return SimpleBind(result, queryParameters.ToDictionary(kvp => kvp.Key, kvp => (string[])kvp.Value, StringComparer.OrdinalIgnoreCase));
        }

        private static TCaptured SimpleBind<TCaptured>(TCaptured result, IDictionary<string, string[]> values)
        {
            foreach (var property in result.GetType().GetProperties())
            {
                if (values.TryGetValue(property.Name, out var value))
                {
                    if (property.PropertyType == typeof(string))
                    {
                        property.SetValue(result, value.FirstOrDefault());
                    }
                    else if (property.PropertyType == typeof(string[]))
                    {
                        property.SetValue(result, value);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            return result;
        }

        public static IDictionary<string, StringValues> LocationHasQueryParameters(HttpResponseMessage response, params ValueSpecification[] values)
        {
            var location = IsRedirect(response);

            var queryParameters = QueryHelpers.ParseNullableQuery(location.Query);
            foreach (var parameter in values)
            {
                if (!queryParameters.TryGetValue(parameter.Name, out var parameterValues))
                {
                    Assert.True(false, $"Missing parameter '{parameter}'");
                }

                if (!parameter.MatchValue)
                {
                    continue;
                }

                foreach (var expectedValue in parameter.Values)
                {
                    Assert.Contains(expectedValue, parameterValues, parameter.ValueComparer);
                }
            }

            return queryParameters;
        }

        public class ValueSpecification
        {
            public string Name { get; set; }
            public bool MatchValue { get; set; } = true;
            public string[] Values { get; set; }
            public IEqualityComparer<string> ValueComparer { get; set; }

            public static implicit operator ValueSpecification(string name)
            {
                return new ValueSpecification
                {
                    Name = name,
                    MatchValue = false
                };
            }

            public static implicit operator ValueSpecification((string name, string value) tuple) => new ValueSpecification()
            {
                Name = tuple.name,
                Values = new string[] { tuple.value },
                ValueComparer = StringComparer.Ordinal
            };

            public static implicit operator ValueSpecification((string name, string[] values) tuple) => new ValueSpecification()
            {
                Name = tuple.name,
                Values = tuple.values,
                ValueComparer = StringComparer.Ordinal
            };
        }

        public static void IsHtmlDocument(HttpResponseMessage response)
        {
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
        }

        public static SetCookieHeaderValue HasCookie(
            SetCookieHeaderValue expectedCookieHeader,
            HttpResponseMessage response,
            IEqualityComparer<SetCookieHeaderValue> equalityComparer)
        {
            var foundValue = HasCookieCore(expectedCookieHeader, response, equalityComparer);
            Assert.True(null != foundValue, $"Couldn't find a matching cookie.");
            return foundValue;
        }

        private static SetCookieHeaderValue HasCookieCore(
            SetCookieHeaderValue expectedCookieHeader,
            HttpResponseMessage response,
            IEqualityComparer<SetCookieHeaderValue> equalityComparer)
        {
            var values = SetCookieHeaderValue.ParseList(response.Headers.GetValues(HeaderNames.SetCookie).ToList());
            foreach (var setCookieValue in values)
            {
                if (equalityComparer.Equals(expectedCookieHeader, setCookieValue))
                {
                    return setCookieValue;
                }
            }

            return null;
        }
    }

    public class SetCookieComparer : IEqualityComparer<SetCookieHeaderValue>
    {
        public static SetCookieComparer Default = new SetCookieComparer(CookieComparison.Default);
        private readonly CookieComparison _comparisonCriteria;
        private readonly TimeSpan _skewAllowance;

        public SetCookieComparer(CookieComparison comparisonCriteria, TimeSpan skewAllowance)
        {
            _comparisonCriteria = comparisonCriteria;
            _skewAllowance = skewAllowance;
        }

        public SetCookieComparer(CookieComparison comparisonCriteria)
            : this(comparisonCriteria, TimeSpan.FromMinutes(1))
        {
        }

        public bool Equals(SetCookieHeaderValue expected, SetCookieHeaderValue candidate)
        {
            var matchesAllCriteria = true;
            if (_comparisonCriteria.HasFlag(CookieComparison.NameStartsWith))
            {
                matchesAllCriteria = matchesAllCriteria && candidate.Name.StartsWith(expected.Name.ToString(), StringComparison.Ordinal);
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.NameEquals))
            {
                matchesAllCriteria = matchesAllCriteria && candidate.Name.Equals(expected.Name, StringComparison.Ordinal);
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.PathEquals))
            {
                matchesAllCriteria = matchesAllCriteria && candidate.Path.Equals(expected.Path, StringComparison.Ordinal);
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.DomainEquals))
            {
                matchesAllCriteria = matchesAllCriteria && candidate.Domain.Equals(expected.Domain, StringComparison.Ordinal);
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.ExpiresEquals))
            {
                matchesAllCriteria = matchesAllCriteria &&
                    ((expected.Expires.HasValue &&
                    candidate.Expires.HasValue &&
                    expected.Expires - _skewAllowance <= candidate.Expires &&
                    candidate.Expires <= expected.Expires + _skewAllowance) ||
                        expected.Expires == candidate.Expires);
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.MaxAgeEquals))
            {
                matchesAllCriteria = matchesAllCriteria &&
                    ((expected.MaxAge.HasValue &&
                    candidate.MaxAge.HasValue &&
                    expected.MaxAge - _skewAllowance <= candidate.MaxAge &&
                    candidate.MaxAge <= expected.MaxAge + _skewAllowance) ||
                        expected.MaxAge == candidate.MaxAge);
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.HttpOnly))
            {
                matchesAllCriteria = matchesAllCriteria && expected.HttpOnly == candidate.HttpOnly;
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.SameSite))
            {
                matchesAllCriteria = matchesAllCriteria && expected.SameSite == candidate.SameSite;
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.Secure))
            {
                matchesAllCriteria = matchesAllCriteria && expected.Secure == candidate.Secure;
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.ValueEquals))
            {
                matchesAllCriteria = matchesAllCriteria && expected.Value.Equals(candidate.Value, StringComparison.Ordinal);
            }

            if (_comparisonCriteria.HasFlag(CookieComparison.ValueStartsWith))
            {
                matchesAllCriteria = matchesAllCriteria && candidate.Value.StartsWith(expected.Value.ToString(), StringComparison.Ordinal);
            }

            return matchesAllCriteria;
        }

        public int GetHashCode(SetCookieHeaderValue obj)
        {
            return 1;
        }
    }
}
