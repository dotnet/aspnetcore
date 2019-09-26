using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    [HtmlTargetElement("component", Attributes = ComponentTypeName)]
    public class ComponentTagHelper : TagHelper
    {
        private const string ComponentParameterName = "parameters";
        private const string ComponentParameterPrefix = "parameter-";
        private const string ComponentTypeName = "component-type";
        private const string RenderModeName = "render-mode";
        private readonly IHtmlHelper _htmlHelper;
        private IDictionary<string, object> _parameters;

        public ComponentTagHelper(IHtmlHelper htmlHelper)
        {
            _htmlHelper = htmlHelper;
        }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViwContext { get; set; }

        [HtmlAttributeName(ComponentParameterName, DictionaryAttributePrefix = ComponentParameterPrefix)]
        public IDictionary<string, object> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    _parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                }

                return _parameters;
            }
            set
            {
                _parameters = value;
            }
        }
        
        [HtmlAttributeName(ComponentTypeName)]
        public Type ComponentType { get; set; }

        [HtmlAttributeName(RenderModeName)]
        public RenderMode RenderMode { get; set; }

        public async override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            (_htmlHelper as IViewContextAware).Contextualize(ViwContext);
            var result = await _htmlHelper.RenderComponentAsync(ComponentType, RenderMode, Parameters);
            output.Content = output.Content.SetHtmlContent(result);
        }
    }
}
