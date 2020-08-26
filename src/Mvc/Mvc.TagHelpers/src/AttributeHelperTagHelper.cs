using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// a <see cref="TagHelper"/> that help you dynamicly add or remove html class
    /// </summary>
    [HtmlTargetElement(Attributes = AttributeDicCatch)]
    [HtmlTargetElement(Attributes = AttributeDicAll)]
    public class AttributeHelperTagHelper : TagHelper
    {
        private const string AttributeDicPrefix = "attr.";
        private const string AttributeDicCatch = "attr.*";
        private const string AttributeDicAll = "attrs";
        private readonly HtmlEncoder _htmlEncoder;
        public AttributeHelperTagHelper(HtmlEncoder encoder)
        {
            _htmlEncoder = encoder;
        }

        [HtmlAttributeName(AttributeDicAll, DictionaryAttributePrefix = AttributeDicPrefix)]
        public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>
        /// we need to run after other <see cref="TagHelper"/> execute
        /// </summary>
        public override int Order => 1000;
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {

            foreach (var attr in Attributes)
            {
                if (attr.Value is bool v)
                {
                    if (v == true)
                    {
                        // it seems only this way we can add HtmlAttributeValueStyle.Minimized
                        output.Attributes.SetAttribute(new TagHelperAttribute(attr.Key));
                    }
                    else
                    {
                        output.Attributes.RemoveAll(attr.Key);
                    }
                }
                else
                {
                    output.Attributes.SetAttribute(attr.Key, attr.Value);
                }
            }
        }
    }
}
