// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Specifies the allowed content types which can be used to select the action based on request's content-type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ConsumesAttribute : Attribute, IResourceFilter, IConsumesActionConstraint
    {
        public static readonly int ConsumesActionConstraintOrder = 200;

        /// <summary>
        /// Creates a new instance of <see cref="ConsumesAttribute"/>.
        /// </summary>
        public ConsumesAttribute([NotNull] string contentType, params string[] otherContentTypes)
        {
            ContentTypes = GetContentTypes(contentType, otherContentTypes);
        }

        // The value used is a non default value so that it avoids getting mixed with other action constraints
        // with default order.
        /// <inheritdoc />
        int IActionConstraint.Order { get; } = ConsumesActionConstraintOrder;

        /// <inheritdoc />
        public IList<MediaTypeHeaderValue> ContentTypes { get; set; }

        /// <inheritdoc />
        public void OnResourceExecuting([NotNull] ResourceExecutingContext context)
        {
            // Only execute if the current filter is the one which is closest to the action.
            // Ignore all other filters. This is to ensure we have a overriding behavior.
            if (IsApplicable(context.ActionDescriptor))
            {
                MediaTypeHeaderValue requestContentType = null;
                MediaTypeHeaderValue.TryParse(context.HttpContext.Request.ContentType, out requestContentType);

                // Only execute if this is the last filter before calling the action.
                // This ensures that we only run the filter which is closest to the action.
                if (requestContentType != null &&
                    !ContentTypes.Any(contentType => contentType.IsSubsetOf(requestContentType)))
                {
                    context.Result = new UnsupportedMediaTypeResult();
                }
            }
        }

        /// <inheritdoc />
        public void OnResourceExecuted([NotNull] ResourceExecutedContext context)
        {
        }

        public bool Accept(ActionConstraintContext context)
        {
            // If this constraint is not closest to the action, it will be skipped.
            if (!IsApplicable(context.CurrentCandidate.Action))
            {
                // Since the constraint is to be skipped, returning true here
                // will let the current candidate ignore this constraint and will
                // be selected based on other constraints for this action.
                return true;
            }

            MediaTypeHeaderValue requestContentType = null;
            MediaTypeHeaderValue.TryParse(context.RouteContext.HttpContext.Request.ContentType, out requestContentType);

            // If the request content type is null we need to act like pass through.
            // In case there is a single candidate with a constraint it should be selected.
            // If there are multiple actions with consumes action constraints this should result in ambiguous exception
            // unless there is another action without a consumes constraint.
            if (requestContentType == null)
            {
                var isActionWithoutConsumeConstraintPresent = context.Candidates.Any(
                    candidate => candidate.Constraints == null ||
                    !candidate.Constraints.Any(constraint => constraint is IConsumesActionConstraint));

                return !isActionWithoutConsumeConstraintPresent;
            }

            if (ContentTypes.Any(c => c.IsSubsetOf(requestContentType)))
            {
                return true;
            }

            var firstCandidate = context.Candidates[0];
            if (firstCandidate != context.CurrentCandidate)
            {
                // If the current candidate is not same as the first candidate,
                // we need not probe other candidates to see if they apply.
                // Only the first candidate is allowed to probe other candidates and based on the result select itself.
                return false;
            }

            // Run the matching logic for all IConsumesActionConstraints we can find, and see what matches.
            // 1). If we have a unique best match, then only that constraint should return true.
            // 2). If we have multiple matches, then all constraints that match will return true
            // , resulting in ambiguity(maybe).
            // 3). If we have no matches, then we choose the first constraint to return true.It will later return a 415
            foreach (var candidate in context.Candidates)
            {
                if (candidate == firstCandidate)
                {
                    continue;
                }

                var tempContext = new ActionConstraintContext()
                {
                    Candidates = context.Candidates,
                    RouteContext = context.RouteContext,
                    CurrentCandidate = candidate
                };

                if (candidate.Constraints == null || candidate.Constraints.Count == 0 ||
                    candidate.Constraints.Any(constraint => constraint is IConsumesActionConstraint &&
                                                            constraint.Accept(tempContext)))
                {
                    // There is someone later in the chain which can handle the request.
                    // end the process here.
                    return false;
                }
            }

            // There is no one later in the chain that can handle this content type return a false positive so that
            // later we can detect and return a 415.
            return true;
        }

        private bool IsApplicable(ActionDescriptor actionDescriptor)
        {
            // If there are multiple IConsumeActionConstraints which are defined at the class and
            // at the action level, the one closest to the action overrides the others. To ensure this
            // we take advantage of the fact that ConsumesAttribute is both an IActionFilter and an
            // IConsumeActionConstraint. Since filterdescriptor collection is ordered (the last filter is the one
            // closest to the action), we apply this constraint only if there is no IConsumeActionConstraint after this.
            return actionDescriptor.FilterDescriptors.Last(
                filter => filter.Filter is IConsumesActionConstraint).Filter == this;

        }

        private List<MediaTypeHeaderValue> GetContentTypes(string firstArg, string[] args)
        {
            var completeArgs = new List<string>();
            completeArgs.Add(firstArg);
            completeArgs.AddRange(args);
            var contentTypes = new List<MediaTypeHeaderValue>();
            foreach (var arg in completeArgs)
            {
                var contentType = MediaTypeHeaderValue.Parse(arg);
                if (contentType.MatchesAllSubTypes || contentType.MatchesAllTypes)
                {
                    throw new InvalidOperationException(
                        Resources.FormatMatchAllContentTypeIsNotAllowed(arg));
                }

                contentTypes.Add(contentType);
            }

            return contentTypes;
        }
    }
}