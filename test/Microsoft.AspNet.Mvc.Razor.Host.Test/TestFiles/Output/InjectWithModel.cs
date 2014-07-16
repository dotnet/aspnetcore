namespace Razor
{
    using System.Threading.Tasks;

    public class __CompiledTemplate : RazorView<
#line 1 ""
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
        public __CompiledTemplate()
        {
        }
        #line hidden
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public
#line 2 ""
        MyApp MyPropertyName

#line default
#line hidden
        { get; private set; }
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public
#line 3 ""
        MyService<TModel> Html

#line default
#line hidden
        { get; private set; }
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public Microsoft.AspNet.Mvc.IViewComponentHelper Component { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
        }
        #pragma warning restore 1998
    }
}
