// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageHandlerMethodSelector : IPageHandlerMethodSelector
    {
        public HandlerMethodDescriptor Select(PageContext context)
        {
            var handlers = new List<HandlerMethodAndMetadata>(context.ActionDescriptor.HandlerMethods.Count);
            for (var i = 0; i < context.ActionDescriptor.HandlerMethods.Count; i++)
            {
                handlers.Add(HandlerMethodAndMetadata.Create(context.ActionDescriptor.HandlerMethods[i]));
            }

            for (var i = handlers.Count - 1; i >= 0; i--)
            {
                var handler = handlers[i];

                if (handler.HttpMethod != null &&
                    !string.Equals(handler.HttpMethod, context.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    handlers.RemoveAt(i);
                }
            }

            var formaction = Convert.ToString(context.RouteData.Values["formaction"]);

            for (var i = handlers.Count - 1; i >= 0; i--)
            {
                var handler = handlers[i];

                if (handler.Formaction != null &&
                    !string.Equals(handler.Formaction, formaction, StringComparison.OrdinalIgnoreCase))
                {
                    handlers.RemoveAt(i);
                }
            }

            var ambiguousMatches = (List<HandlerMethodDescriptor>)null;
            var best = (HandlerMethodAndMetadata?)null;
            for (var i = 2; i >= 0; i--)
            {
                for (var j = 0; j < handlers.Count; j++)
                {
                    var handler = handlers[j];
                    if (handler.GetScore() == i)
                    {
                        if (best == null)
                        {
                            best = handler;
                            continue;
                        }

                        if (ambiguousMatches == null)
                        {
                            ambiguousMatches = new List<HandlerMethodDescriptor>();
                            ambiguousMatches.Add(best.Value.Handler);
                        }

                        ambiguousMatches.Add(handler.Handler);
                    }
                }

                if (ambiguousMatches != null)
                {
                    throw new InvalidOperationException($"Selecting a handler is ambiguous! Matches: {string.Join(", ", ambiguousMatches)}");
                }

                if (best != null)
                {
                    return best.Value.Handler;
                }
            }

            return null;
        }

        // Bad prototype substring implementation :)
        private struct HandlerMethodAndMetadata
        {
            public static HandlerMethodAndMetadata Create(HandlerMethodDescriptor handler)
            {
                var name = handler.Method.Name;

                string httpMethod;
                if (name.StartsWith("OnGet", StringComparison.Ordinal))
                {
                    httpMethod = "GET";
                }
                else if (name.StartsWith("OnPost", StringComparison.Ordinal))
                {
                    httpMethod = "POST";
                }
                else
                {
                    httpMethod = null;
                }

                var formactionStart = httpMethod?.Length + 2 ?? 0;
                var formactionLength = name.EndsWith("Async", StringComparison.Ordinal)
                    ? name.Length - formactionStart - "Async".Length
                    : name.Length - formactionStart;

                var formaction = formactionLength == 0 ? null : name.Substring(formactionStart, formactionLength);

                return new HandlerMethodAndMetadata(handler, httpMethod, formaction);
            }

            public HandlerMethodAndMetadata(HandlerMethodDescriptor handler, string httpMethod, string formaction)
            {
                Handler = handler;
                HttpMethod = httpMethod;
                Formaction = formaction;
            }

            public HandlerMethodDescriptor Handler { get; }

            public string HttpMethod { get; }

            public string Formaction { get; }

            public int GetScore()
            {
                if (Formaction != null)
                {
                    return 2;
                }
                else if (HttpMethod != null)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}