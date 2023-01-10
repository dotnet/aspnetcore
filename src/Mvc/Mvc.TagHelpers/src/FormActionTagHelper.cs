// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;button&gt; elements and &lt;input&gt; elements with
/// their <c>type</c> attribute set to <c>image</c> or <c>submit</c>.
/// </summary>
[HtmlTargetElement("button", Attributes = ActionAttributeName)]
[HtmlTargetElement("button", Attributes = ControllerAttributeName)]
[HtmlTargetElement("button", Attributes = AreaAttributeName)]
[HtmlTargetElement("button", Attributes = PageAttributeName)]
[HtmlTargetElement("button", Attributes = PageHandlerAttributeName)]
[HtmlTargetElement("button", Attributes = FragmentAttributeName)]
[HtmlTargetElement("button", Attributes = RouteAttributeName)]
[HtmlTargetElement("button", Attributes = RouteValuesDictionaryName)]
[HtmlTargetElement("button", Attributes = RouteValuesPrefix + "*")]
[HtmlTargetElement("input", Attributes = ImageActionAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = ImageControllerAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = ImageAreaAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = ImagePageAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = ImagePageHandlerAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = ImageFragmentAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = ImageRouteAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = ImageRouteValuesDictionarySelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = ImageRouteValuesSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = SubmitActionAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = SubmitControllerAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = SubmitAreaAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = SubmitPageAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = SubmitPageHandlerAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = SubmitFragmentAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = SubmitRouteAttributeSelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = SubmitRouteValuesDictionarySelector, TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = SubmitRouteValuesSelector, TagStructure = TagStructure.WithoutEndTag)]
public class FormActionTagHelper : TagHelper
{
    private const string ActionAttributeName = "asp-action";
    private const string AreaAttributeName = "asp-area";
    private const string ControllerAttributeName = "asp-controller";
    private const string PageAttributeName = "asp-page";
    private const string PageHandlerAttributeName = "asp-page-handler";
    private const string FragmentAttributeName = "asp-fragment";
    private const string RouteAttributeName = "asp-route";
    private const string RouteValuesDictionaryName = "asp-all-route-data";
    private const string RouteValuesPrefix = "asp-route-";
    private const string FormAction = "formaction";

    private const string ImageTypeSelector = "[type=image], ";
    private const string ImageActionAttributeSelector = ImageTypeSelector + ActionAttributeName;
    private const string ImageAreaAttributeSelector = ImageTypeSelector + AreaAttributeName;
    private const string ImagePageAttributeSelector = ImageTypeSelector + PageAttributeName;
    private const string ImagePageHandlerAttributeSelector = ImageTypeSelector + PageHandlerAttributeName;
    private const string ImageFragmentAttributeSelector = ImageTypeSelector + FragmentAttributeName;
    private const string ImageControllerAttributeSelector = ImageTypeSelector + ControllerAttributeName;
    private const string ImageRouteAttributeSelector = ImageTypeSelector + RouteAttributeName;
    private const string ImageRouteValuesDictionarySelector = ImageTypeSelector + RouteValuesDictionaryName;
    private const string ImageRouteValuesSelector = ImageTypeSelector + RouteValuesPrefix + "*";

    private const string SubmitTypeSelector = "[type=submit], ";
    private const string SubmitActionAttributeSelector = SubmitTypeSelector + ActionAttributeName;
    private const string SubmitAreaAttributeSelector = SubmitTypeSelector + AreaAttributeName;
    private const string SubmitPageAttributeSelector = SubmitTypeSelector + PageAttributeName;
    private const string SubmitPageHandlerAttributeSelector = SubmitTypeSelector + PageHandlerAttributeName;
    private const string SubmitFragmentAttributeSelector = SubmitTypeSelector + FragmentAttributeName;
    private const string SubmitControllerAttributeSelector = SubmitTypeSelector + ControllerAttributeName;
    private const string SubmitRouteAttributeSelector = SubmitTypeSelector + RouteAttributeName;
    private const string SubmitRouteValuesDictionarySelector = SubmitTypeSelector + RouteValuesDictionaryName;
    private const string SubmitRouteValuesSelector = SubmitTypeSelector + RouteValuesPrefix + "*";

    private IDictionary<string, string> _routeValues;

    /// <summary>
    /// Creates a new <see cref="FormActionTagHelper"/>.
    /// </summary>
    /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
    public FormActionTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        UrlHelperFactory = urlHelperFactory;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Gets or sets the <see cref="Rendering.ViewContext"/> for the current request.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Gets the <see cref="IUrlHelperFactory"/> used to create an <see cref="IUrlHelper"/> to generate links.
    /// </summary>
    protected IUrlHelperFactory UrlHelperFactory { get; }

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
    /// <remarks>Does nothing if user provides an <c>FormAction</c> attribute.</remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <c>FormAction</c> attribute is provided and <see cref="Action"/>, <see cref="Controller"/>,
    /// <see cref="Fragment"/> or <see cref="Route"/> are non-<c>null</c> or if the user provided <c>asp-route-*</c> attributes.
    /// Also thrown if <see cref="Route"/> and one or both of <see cref="Action"/> and <see cref="Controller"/>
    /// are non-<c>null</c>
    /// </exception>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        // If "FormAction" is already set, it means the user is attempting to use a normal button or input element.
        if (output.Attributes.ContainsName(FormAction))
        {
            if (Action != null ||
                Controller != null ||
                Area != null ||
                Page != null ||
                PageHandler != null ||
                Fragment != null ||
                Route != null ||
                (_routeValues != null && _routeValues.Count > 0))
            {
                // User specified a FormAction and one of the bound attributes; can't override that FormAction
                // attribute.
                throw new InvalidOperationException(
                    Resources.FormatFormActionTagHelper_CannotOverrideFormAction(
                        FormAction,
                        output.TagName,
                        RouteValuesPrefix,
                        ActionAttributeName,
                        ControllerAttributeName,
                        AreaAttributeName,
                        FragmentAttributeName,
                        RouteAttributeName,
                        PageAttributeName,
                        PageHandlerAttributeName));
            }

            return;
        }

        var routeLink = Route != null;
        var actionLink = Controller != null || Action != null;
        var pageLink = Page != null || PageHandler != null;

        if ((routeLink && actionLink) || (routeLink && pageLink) || (actionLink && pageLink))
        {
            var message = string.Join(
                Environment.NewLine,
                Resources.FormatCannotDetermineAttributeFor(FormAction, '<' + output.TagName + '>'),
                RouteAttributeName,
                ControllerAttributeName + ", " + ActionAttributeName,
                PageAttributeName + ", " + PageHandlerAttributeName);

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

        var urlHelper = UrlHelperFactory.GetUrlHelper(ViewContext);
        string url;
        if (pageLink)
        {
            url = urlHelper.Page(Page, PageHandler, routeValues, protocol: null, host: null, fragment: Fragment);
        }
        else if (routeLink)
        {
            url = urlHelper.RouteUrl(Route, routeValues, protocol: null, host: null, fragment: Fragment);
        }
        else
        {
            url = urlHelper.Action(Action, Controller, routeValues, protocol: null, host: null, fragment: Fragment);
        }

        output.Attributes.SetAttribute(FormAction, url);
    }
}
