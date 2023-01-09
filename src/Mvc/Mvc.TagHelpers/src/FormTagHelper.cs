// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;form&gt; elements.
/// </summary>
[HtmlTargetElement("form")]
public class FormTagHelper : TagHelper
{
    private const string ActionAttributeName = "asp-action";
    private const string AntiforgeryAttributeName = "asp-antiforgery";
    private const string AreaAttributeName = "asp-area";
    private const string PageAttributeName = "asp-page";
    private const string PageHandlerAttributeName = "asp-page-handler";
    private const string FragmentAttributeName = "asp-fragment";
    private const string ControllerAttributeName = "asp-controller";
    private const string RouteAttributeName = "asp-route";
    private const string RouteValuesDictionaryName = "asp-all-route-data";
    private const string RouteValuesPrefix = "asp-route-";
    private const string HtmlActionAttributeName = "action";
    private IDictionary<string, string> _routeValues;

    /// <summary>
    /// Creates a new <see cref="FormTagHelper"/>.
    /// </summary>
    /// <param name="generator">The <see cref="IHtmlGenerator"/>.</param>
    public FormTagHelper(IHtmlGenerator generator)
    {
        Generator = generator;
    }

    // This TagHelper's order must be lower than the RenderAtEndOfFormTagHelper. I.e it must be executed before
    // RenderAtEndOfFormTagHelper does.
    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Gets the <see cref="Rendering.ViewContext"/> of the executing view.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Gets the <see cref="IHtmlGenerator"/> used to generate the <see cref="FormTagHelper"/>'s output.
    /// </summary>
    protected IHtmlGenerator Generator { get; }

    /// <summary>
    /// The name of the action method.
    /// </summary>
    [HtmlAttributeName(ActionAttributeName)]
    public string Action { get; set; }

    /// <summary>
    /// The name of the controller.
    /// </summary>
    [HtmlAttributeName(ControllerAttributeName)]
    public string Controller { get; set; }

    /// <summary>
    /// The name of the area.
    /// </summary>
    [HtmlAttributeName(AreaAttributeName)]
    public string Area { get; set; }

    /// <summary>
    /// The name of the page.
    /// </summary>
    [HtmlAttributeName(PageAttributeName)]
    public string Page { get; set; }

    /// <summary>
    /// The name of the page handler.
    /// </summary>
    [HtmlAttributeName(PageHandlerAttributeName)]
    public string PageHandler { get; set; }

    /// <summary>
    /// Whether the antiforgery token should be generated.
    /// </summary>
    /// <value>Defaults to <c>false</c> if user provides an <c>action</c> attribute
    /// or if the <c>method</c> is <see cref="FormMethod.Get"/>; <c>true</c> otherwise.</value>
    [HtmlAttributeName(AntiforgeryAttributeName)]
    public bool? Antiforgery { get; set; }

    /// <summary>
    /// Gets or sets the URL fragment.
    /// </summary>
    [HtmlAttributeName(FragmentAttributeName)]
    public string Fragment { get; set; }

    /// <summary>
    /// Name of the route.
    /// </summary>
    /// <remarks>
    /// Must be <c>null</c> if <see cref="Action"/> or <see cref="Controller"/> is non-<c>null</c>.
    /// </remarks>
    [HtmlAttributeName(RouteAttributeName)]
    public string Route { get; set; }

    /// <summary>
    /// The HTTP method to use.
    /// </summary>
    /// <remarks>Passed through to the generated HTML in all cases.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string Method { get; set; }

    /// <summary>
    /// Additional parameters for the route.
    /// </summary>
    [HtmlAttributeName(RouteValuesDictionaryName, DictionaryAttributePrefix = RouteValuesPrefix)]
    public IDictionary<string, string> RouteValues
    {
        get
        {
            if (_routeValues == null)
            {
                _routeValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return _routeValues;
        }
        set
        {
            _routeValues = value;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Does nothing if user provides an <c>action</c> attribute and <see cref="Antiforgery"/> is <c>null</c> or
    /// <c>false</c>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <c>action</c> attribute is provided and <see cref="Action"/>, <see cref="Controller"/> or <see cref="Fragment"/> are
    /// non-<c>null</c> or if the user provided <c>asp-route-*</c> attributes.
    /// </exception>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (Method != null)
        {
            output.CopyHtmlAttribute(nameof(Method), context);
        }

        var antiforgeryDefault = true;
        var routeableParametersProvided = Action != null ||
            Controller != null ||
            Area != null ||
            Page != null ||
            PageHandler != null ||
            Fragment != null ||
            Route != null ||
            (_routeValues != null && _routeValues.Count > 0);

        // If "action" is already set, it means the user is attempting to use a normal <form>.
        if (output.Attributes.TryGetAttribute(HtmlActionAttributeName, out var actionAttribute))
        {
            if (routeableParametersProvided)
            {
                // User also specified bound attributes we cannot use.
                throw new InvalidOperationException(
                    Resources.FormatFormTagHelper_CannotOverrideAction(
                        HtmlActionAttributeName,
                        "<form>",
                        RouteValuesPrefix,
                        ActionAttributeName,
                        ControllerAttributeName,
                        FragmentAttributeName,
                        AreaAttributeName,
                        RouteAttributeName,
                        PageAttributeName,
                        PageHandlerAttributeName));
            }

            string attributeValue = null;
            switch (actionAttribute.Value)
            {
                case HtmlString htmlString:
                    attributeValue = htmlString.ToString();
                    break;
                case string stringValue:
                    attributeValue = stringValue;
                    break;
            }

            if (string.IsNullOrEmpty(attributeValue))
            {
                // User is using the FormTagHelper like a normal <form> tag that has an empty or complex IHtmlContent action attribute.
                // e.g. <form action="" method="..."> or <form action="@CustomUrlIHtmlContent" method="...">

                if (string.Equals(Method ?? "get", "get", StringComparison.OrdinalIgnoreCase))
                {
                    antiforgeryDefault = false;
                }
                else
                {
                    // Antiforgery default is already set to true
                }
            }
            else
            {
                // User is likely using the <form> element to submit to another site. Do not send an antiforgery token to unknown sites.
                antiforgeryDefault = false;
            }
        }
        else
        {
            var routeLink = Route != null;
            var actionLink = Controller != null || Action != null;
            var pageLink = Page != null || PageHandler != null;

            if ((routeLink && actionLink) || (routeLink && pageLink) || (actionLink && pageLink))
            {
                var message = string.Join(
                    Environment.NewLine,
                    Resources.FormatCannotDetermineAttributeFor(HtmlActionAttributeName, "<form>"),
                    RouteAttributeName,
                    ControllerAttributeName + ", " + ActionAttributeName,
                    PageAttributeName);

                throw new InvalidOperationException(message);
            }

            RouteValueDictionary routeValues = null;
            if (_routeValues != null && _routeValues.Count > 0)
            {
                routeValues = new RouteValueDictionary(_routeValues);
            }

            if (Area != null)
            {
                if (routeValues == null)
                {
                    routeValues = new RouteValueDictionary();
                }

                // Unconditionally replace any value from asp-route-area.
                routeValues["area"] = Area;
            }

            TagBuilder tagBuilder = null;
            if (!routeableParametersProvided &&
                _routeValues == null &&
                // Antiforgery will sometime be set globally via TagHelper Initializers, verify it was provided in the cshtml.
                !context.AllAttributes.ContainsName(AntiforgeryAttributeName))
            {
                // A <form> tag that doesn't utilize asp-* attributes. Let it flow to the output.
                Method = Method ?? "get";
            }
            else if (pageLink)
            {
                tagBuilder = Generator.GeneratePageForm(
                    ViewContext,
                    Page,
                    PageHandler,
                    routeValues,
                    Fragment,
                    method: null,
                    htmlAttributes: null);
            }
            else if (routeLink)
            {
                tagBuilder = Generator.GenerateRouteForm(
                    ViewContext,
                    Route,
                    routeValues,
                    Fragment,
                    method: null,
                    htmlAttributes: null);
            }
            else
            {
                tagBuilder = Generator.GenerateForm(
                    ViewContext,
                    Action,
                    Controller,
                    Fragment,
                    routeValues,
                    method: null,
                    htmlAttributes: null);
            }

            if (tagBuilder != null)
            {
                output.MergeAttributes(tagBuilder);
                if (tagBuilder.HasInnerHtml)
                {
                    output.PostContent.AppendHtml(tagBuilder.InnerHtml);
                }
            }

            if (string.Equals(Method, "get", StringComparison.OrdinalIgnoreCase))
            {
                antiforgeryDefault = false;
            }
        }

        if (Antiforgery ?? antiforgeryDefault)
        {
            var antiforgeryTag = Generator.GenerateAntiforgery(ViewContext);
            if (antiforgeryTag != null)
            {
                output.PostContent.AppendHtml(antiforgeryTag);
            }
        }
    }
}
