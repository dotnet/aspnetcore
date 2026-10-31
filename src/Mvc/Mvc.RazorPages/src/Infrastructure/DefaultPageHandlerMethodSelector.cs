// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class DefaultPageHandlerMethodSelector : IPageHandlerMethodSelector
{
    private const string Handler = "handler";

    public HandlerMethodDescriptor? Select(PageContext context)
    {
        var handlers = SelectHandlers(context);
        if (handlers == null || handlers.Count == 0)
        {
            return null;
        }

        List<HandlerMethodDescriptor>? ambiguousMatches = null;
        HandlerMethodDescriptor? bestMatch = null;
        for (var score = 2; score >= 0; score--)
        {
            for (var i = 0; i < handlers.Count; i++)
            {
                var handler = handlers[i];
                if (GetScore(handler) == score)
                {
                    if (bestMatch == null)
                    {
                        bestMatch = handler;
                        continue;
                    }

                    if (ambiguousMatches == null)
                    {
                        ambiguousMatches = new List<HandlerMethodDescriptor>
                            {
                                bestMatch
                            };
                    }

                    ambiguousMatches.Add(handler);
                }
            }

            if (ambiguousMatches != null)
            {
                var ambiguousMethods = string.Join(", ", ambiguousMatches.Select(m => m.MethodInfo));
                throw new InvalidOperationException(Resources.FormatAmbiguousHandler(Environment.NewLine, ambiguousMethods));
            }

            if (bestMatch != null)
            {
                return bestMatch;
            }
        }

        return null;
    }

    private static List<HandlerMethodDescriptor> SelectHandlers(PageContext context)
    {
        var handlers = context.ActionDescriptor.HandlerMethods;
        var candidates = new List<HandlerMethodDescriptor>();

        // Name is optional, may not be provided.
        var handlerName = GetHandlerName(context);

        // The handler selection process considers handlers according to a few criteria. Handlers
        // have a defined HTTP method that they handle, and also optionally a 'name'.
        //
        // We don't really have a scenario for handler methods without a verb (we don't provide a way
        // to create one). If we see one, it will just never match.
        //
        // The verb must match (with some fuzzy matching) and the handler name must match if
        // there is one.
        //
        // The process is like this:
        //
        //  1. Match the possible candidates on HTTP method
        //  1a. **Added in 2.1** if no candidates matched in 1, then do *fuzzy matching*
        //  2. Match the candidates from 1 or 1a on handler name.

        // Step 1: match on HTTP method.
        var httpMethod = context.HttpContext.Request.Method;
        for (var i = 0; i < handlers.Count; i++)
        {
            var handler = handlers[i];
            if (handler.HttpMethod != null &&
                string.Equals(handler.HttpMethod, httpMethod, StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(handler);
            }
        }

        // Step 1a: do fuzzy HTTP method matching if needed.
        if (candidates.Count == 0)
        {
            var fuzzyHttpMethod = GetFuzzyMatchHttpMethod(context);
            if (fuzzyHttpMethod != null)
            {
                for (var i = 0; i < handlers.Count; i++)
                {
                    var handler = handlers[i];
                    if (handler.HttpMethod != null &&
                        string.Equals(handler.HttpMethod, fuzzyHttpMethod, StringComparison.OrdinalIgnoreCase))
                    {
                        candidates.Add(handler);
                    }
                }
            }
        }

        // Step 2: remove candidates with non-matching handlers.
        for (var i = candidates.Count - 1; i >= 0; i--)
        {
            var handler = candidates[i];
            if (handler.Name != null &&
                !handler.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase))
            {
                candidates.RemoveAt(i);
            }
        }

        return candidates;
    }

    private static int GetScore(HandlerMethodDescriptor descriptor)
    {
        if (descriptor.Name != null)
        {
            return 2;
        }
        else if (descriptor.HttpMethod != null)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    private static string? GetHandlerName(PageContext context)
    {
        var handlerName = Convert.ToString(context.RouteData.Values[Handler], CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(handlerName))
        {
            return handlerName;
        }

        if (context.HttpContext.Request.Query.TryGetValue(Handler, out var queryValues))
        {
            return queryValues[0];
        }

        return null;
    }

    private static string? GetFuzzyMatchHttpMethod(PageContext context)
    {
        // Map HEAD to get.
        return HttpMethods.IsHead(context.HttpContext.Request.Method) ? HttpMethods.Get : null;
    }
}
