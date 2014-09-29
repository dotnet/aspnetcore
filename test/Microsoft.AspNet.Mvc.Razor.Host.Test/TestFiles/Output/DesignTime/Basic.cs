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
            PageExecutionContext.BeginContext(0, 4, true);
            WriteLiteral("<div");
            PageExecutionContext.EndContext();
            WriteAttribute("class", Tuple.Create(" class=\"", 4), Tuple.Create("\"", 17), 
            Tuple.Create(Tuple.Create("", 12), Tuple.Create<System.Object, System.Int32>(
#line 1 "TestFiles/Input/Basic.cshtml"
             logo

#line default
#line hidden
            , 12), false));
            PageExecutionContext.BeginContext(18, 24, true);
            WriteLiteral(">\r\n    Hello world\r\n    ");
            PageExecutionContext.EndContext();
            PageExecutionContext.BeginContext(43, 21, false);
            Write(
#line 3 "TestFiles/Input/Basic.cshtml"
     Html.Input("SomeKey")

#line default
#line hidden
            );

            PageExecutionContext.EndContext();
            PageExecutionContext.BeginContext(64, 8, true);
            WriteLiteral("\r\n</div>");
            PageExecutionContext.EndContext();
        }
        #pragma warning restore 1998
    }
}
