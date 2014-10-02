#pragma checksum "TestFiles/Input/Basic.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "63d2634be31f68aa89a0c1561d67c73cc446f3d4"
namespace Asp
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.Rendering;
    using System.Threading.Tasks;

    public class ASPV_TestFiles_Input_Basic_cshtml : Microsoft.AspNet.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        public ASPV_TestFiles_Input_Basic_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public Microsoft.AspNet.Mvc.IViewComponentHelper Component { get; private set; }
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public Microsoft.AspNet.Mvc.IUrlHelper Url { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            WriteLiteral("<div");
            WriteAttribute("class", Tuple.Create(" class=\"", 4), Tuple.Create("\"", 17), 
            Tuple.Create(Tuple.Create("", 12), Tuple.Create<System.Object, System.Int32>(logo, 12), false));
            WriteLiteral(">\r\n    Hello world\r\n    ");
#line 3 "TestFiles/Input/Basic.cshtml"
Write(Html.Input("SomeKey"));

#line default
#line hidden
            WriteLiteral("\r\n</div>");
        }
        #pragma warning restore 1998
    }
}
