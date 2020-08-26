using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// a <see cref="TagHelper"/> that implement the `asp-if` keyword
    /// </summary>
    [HtmlTargetElement(Attributes= IfAttributeName)]
    public class IfTagHelper : TagHelper
    {
        private const string IfAttributeName = "asp-if";

        /// <summary>
        /// if <see cref="false"/> then the TagHelper will be remove and not be execute (including the C# inside this)
        /// </summary>
        [HtmlAttributeName(IfAttributeName)]
        public bool Condition { get; set; }

        /// <summary>
        /// we need to run before almost every TagHelpers and C# , and  we not use <see cref="int.MinValue"/> so if some taghelper do
        /// need to execute before us, it can use an order smaller than -1000
        /// </summary>
        public override int Order { get; } = -1000;
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!Condition)
            {
                output.SuppressOutput();
            }

        }
    }
}
