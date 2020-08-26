using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// a class helper that help you dynamicly add or remove html class
    /// </summary>
    [HtmlTargetElement(Attributes = ClassDicCatch)]
    [HtmlTargetElement(Attributes = ClassDicAll)]
    public class ClassHelperTagHelper : TagHelper
    {
        private const string ClassDicPrefix = "class.";
        private const string ClassDicCatch = "class.*"; 
        private const string ClassDicAll = "classes";
        private readonly HtmlEncoder _htmlEncoder;
        public ClassHelperTagHelper(HtmlEncoder encoder)
        {
            _htmlEncoder = encoder;
        }

        [HtmlAttributeName(ClassDicAll, DictionaryAttributePrefix = ClassDicPrefix)]
        public IDictionary<string, bool> Classes { get; set; } = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>
        /// we need to run after other <see cref="TagHelper"/> execute
        /// </summary>
        public override int Order => 1000;
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var classes = this.Classes.Where(x => x.Value == true).Select(x => x.Key);
            foreach (var key in classes)
            {
                output.AddClass(key, _htmlEncoder);
            }

            var notClasses = this.Classes.Where(x => x.Value == false).Select(x => x.Key);
            foreach (var c in notClasses)
            {
                output.RemoveClass(c, _htmlEncoder);
            }
        }
    }
}
