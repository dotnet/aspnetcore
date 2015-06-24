#pragma checksum "TestFiles/Input/Basic.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "4901c9b2325c62315b9048c930cac987201ccbab"
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
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.IViewComponentHelper Component { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.IUrlHelper Url { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            BeginContext(0, 4, true);
            WriteLiteral("<div");
            EndContext();
            WriteAttribute("class", Tuple.Create(" class=\"", 4), Tuple.Create("\"", 17), 
            Tuple.Create(Tuple.Create("", 12), Tuple.Create<System.Object, System.Int32>(logo, 12), false));
            BeginContext(18, 22, true);
            WriteLiteral(">\n    Hello world\n    ");
            EndContext();
            BeginContext(41, 21, false);
#line 3 "TestFiles/Input/Basic.cshtml"
Write(Html.Input("SomeKey"));

#line default
#line hidden
            EndContext();
            BeginContext(62, 7, true);
            WriteLiteral("\n</div>");
            EndContext();
        }
        #pragma warning restore 1998
    }
}
