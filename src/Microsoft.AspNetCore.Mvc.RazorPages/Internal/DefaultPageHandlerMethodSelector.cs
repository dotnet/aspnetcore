// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageHandlerMethodSelector : IPageHandlerMethodSelector
    {
        private const string Handler = "handler";

        public HandlerMethodDescriptor Select(PageContext context)
        {
            var handlers = SelectHandlers(context);
            if (handlers == null || handlers.Count == 0)
            {
                return null;
            }

            List<HandlerMethodDescriptor> ambiguousMatches = null;
            HandlerMethodDescriptor bestMatch = null;
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
                            ambiguousMatches = new List<HandlerMethodDescriptor>();
                            ambiguousMatches.Add(bestMatch);
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
            List<HandlerMethodDescriptor> handlersToConsider = null;

            var handlerName = Convert.ToString(context.RouteData.Values[Handler]);

            if (string.IsNullOrEmpty(handlerName) &&
                context.HttpContext.Request.Query.TryGetValue(Handler, out StringValues queryValues))
            {
                handlerName = queryValues[0];
            }

            for (var i = 0; i < handlers.Count; i++)
            {
                var handler = handlers[i];
                if (handler.HttpMethod != null &&
                    !string.Equals(handler.HttpMethod, context.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                else if (handler.Name != null &&
                    !handler.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (handlersToConsider == null)
                {
                    handlersToConsider = new List<HandlerMethodDescriptor>();
                }

                handlersToConsider.Add(handler);
            }

            return handlersToConsider;
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
    }
}