namespace Asp
{
    using System.Threading.Tasks;

    public class ASPV_TestFiles_Input_InjectWithModel_cshtml : Microsoft.AspNet.Mvc.Razor.RazorPage<
#line 1 "TestFiles/Input/InjectWithModel.cshtml"
       MyModel

#line default
#line hidden
    >
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            #pragma warning restore 219
        }
        #line hidden
        public ASPV_TestFiles_Input_InjectWithModel_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public
#line 2 "TestFiles/Input/InjectWithModel.cshtml"
        MyApp MyPropertyName

#line default
#line hidden
        { get; private set; }
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public
#line 3 "TestFiles/Input/InjectWithModel.cshtml"
        MyService<MyModel> Html

#line default
#line hidden
        { get; private set; }
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public Microsoft.AspNet.Mvc.IViewComponentHelper Component { get; private set; }
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public Microsoft.AspNet.Mvc.IUrlHelper Url { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
        }
        #pragma warning restore 1998
    }
}
