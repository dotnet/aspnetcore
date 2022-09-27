// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A filter that specifies the supported request content types. <see cref="ContentTypes"/> is used to select an
/// action when there would otherwise be multiple matches.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ConsumesAttribute :
    Attribute,
    IResourceFilter,
    IConsumesActionConstraint,
    IApiRequestMetadataProvider,
    IAcceptsMetadata
{
    /// <summary>
    /// The order for consumes attribute.
    /// </summary>
    /// <value>Defaults to 200</value>
    public static readonly int ConsumesActionConstraintOrder = 200;

    /// <summary>
    /// Creates a new instance of <see cref="ConsumesAttribute"/>.
    /// <param name="contentType">The request content type.</param>
    /// <param name="otherContentTypes">The additional list of allowed request content types.</param>
    /// </summary>
    public ConsumesAttribute(string contentType, params string[] otherContentTypes)
    {
        if (contentType == null)
        {
            throw new ArgumentNullException(nameof(contentType));
        }

        // We want to ensure that the given provided content types are valid values, so
        // we validate them using the semantics of MediaTypeHeaderValue.
        MediaTypeHeaderValue.Parse(contentType);

        for (var i = 0; i < otherContentTypes.Length; i++)
        {
            MediaTypeHeaderValue.Parse(otherContentTypes[i]);
        }

        ContentTypes = GetContentTypes(contentType, otherContentTypes);
        _contentTypes = GetAllContentTypes(contentType, otherContentTypes);
    }

    /// <summary>
    /// Creates a new instance of <see cref="ConsumesAttribute"/>.
    /// <param name="requestType">The type being read from the request.</param>
    /// <param name="contentType">The request content type.</param>
    /// <param name="otherContentTypes">The additional list of allowed request content types.</param>
    /// </summary>
    public ConsumesAttribute(Type requestType, string contentType, params string[] otherContentTypes)
    {
        if (contentType == null)
        {
            throw new ArgumentNullException(nameof(contentType));
        }

        // We want to ensure that the given provided content types are valid values, so
        // we validate them using the semantics of MediaTypeHeaderValue.
        MediaTypeHeaderValue.Parse(contentType);

        for (var i = 0; i < otherContentTypes.Length; i++)
        {
            MediaTypeHeaderValue.Parse(otherContentTypes[i]);
        }

        ContentTypes = GetContentTypes(contentType, otherContentTypes);
        _contentTypes = GetAllContentTypes(contentType, otherContentTypes);
        _requestType = requestType;
    }

    // The value used is a non default value so that it avoids getting mixed with other action constraints
    // with default order.
    /// <inheritdoc />
    int IActionConstraint.Order => ConsumesActionConstraintOrder;

    /// <summary>
    /// Gets or sets the supported request content types. Used to select an action when there would otherwise be
    /// multiple matches.
    /// </summary>
    public MediaTypeCollection ContentTypes { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if the request body is optional.
    /// This value is only used to specify if the request body is required in API explorer.
    /// </summary>
    public bool IsOptional { get; set; }

    readonly Type? _requestType;

    readonly List<string> _contentTypes = new();

    Type? IAcceptsMetadata.RequestType => _requestType;

    IReadOnlyList<string> IAcceptsMetadata.ContentTypes => _contentTypes;

    /// <inheritdoc />
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Only execute if the current filter is the one which is closest to the action.
        // Ignore all other filters. This is to ensure we have a overriding behavior.
        if (IsApplicable(context.ActionDescriptor))
        {
            var requestContentType = context.HttpContext.Request.ContentType;

            // Confirm the request's content type is more specific than a media type this action supports e.g. OK
            // if client sent "text/plain" data and this action supports "text/*".
            //
            // Requests without a content type do not return a 415. It is a common pattern to place [Consumes] on
            // a controller and have GET actions
            if (!string.IsNullOrEmpty(requestContentType) && !IsSubsetOfAnyContentType(requestContentType))
            {
                context.Result = new UnsupportedMediaTypeResult();
            }
        }
    }

    private bool IsSubsetOfAnyContentType(string requestMediaType)
    {
        var parsedRequestMediaType = new MediaType(requestMediaType);
        for (var i = 0; i < ContentTypes.Count; i++)
        {
            var contentTypeMediaType = new MediaType(ContentTypes[i]);
            if (parsedRequestMediaType.IsSubsetOf(contentTypeMediaType))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public void OnResourceExecuted(ResourceExecutedContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
    }

    /// <inheritdoc />
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

        var requestContentType = context.RouteContext.HttpContext.Request.ContentType;

        // If the request content type is null we need to act like pass through.
        // In case there is a single candidate with a constraint it should be selected.
        // If there are multiple actions with consumes action constraints this should result in ambiguous exception
        // unless there is another action without a consumes constraint.
        if (string.IsNullOrEmpty(requestContentType))
        {
            var isActionWithoutConsumeConstraintPresent = context.Candidates.Any(
                candidate => candidate.Constraints == null ||
                !candidate.Constraints.Any(constraint => constraint is IConsumesActionConstraint));

            return !isActionWithoutConsumeConstraintPresent;
        }

        // Confirm the request's content type is more specific than (a media type this action supports e.g. OK
        // if client sent "text/plain" data and this action supports "text/*".
        if (IsSubsetOfAnyContentType(requestContentType))
        {
            return true;
        }

        var firstCandidate = context.Candidates[0];
        if (firstCandidate.Action != context.CurrentCandidate.Action)
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
            if (candidate.Action == firstCandidate.Action)
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
        // IConsumeActionConstraint. Since FilterDescriptor collection is ordered (the last filter is the one
        // closest to the action), we apply this constraint only if there is no IConsumeActionConstraint after this.
        return actionDescriptor.FilterDescriptors.Last(
            filter => filter.Filter is IConsumesActionConstraint).Filter == this;
    }

    private static MediaTypeCollection GetContentTypes(string firstArg, string[] args)
    {
        var completeArgs = new List<string>(args.Length + 1);
        completeArgs.Add(firstArg);
        completeArgs.AddRange(args);
        var contentTypes = new MediaTypeCollection();
        foreach (var arg in completeArgs)
        {
            var mediaType = new MediaType(arg);
            if (mediaType.MatchesAllSubTypes ||
                mediaType.MatchesAllTypes)
            {
                throw new InvalidOperationException(
                    Resources.FormatMatchAllContentTypeIsNotAllowed(arg));
            }

            contentTypes.Add(arg);
        }

        return contentTypes;
    }

    private static List<string> GetAllContentTypes(string contentType, string[] additionalContentTypes)
    {
        var allContentTypes = new List<string>()
            {
                contentType
            };
        allContentTypes.AddRange(additionalContentTypes);
        return allContentTypes;
    }

    /// <inheritdoc />
    public void SetContentTypes(MediaTypeCollection contentTypes)
    {
        contentTypes.Clear();
        foreach (var contentType in ContentTypes)
        {
            contentTypes.Add(contentType);
        }
    }
}
