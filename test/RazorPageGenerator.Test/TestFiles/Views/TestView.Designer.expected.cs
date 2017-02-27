namespace Microsoft.AspNetCore.TestGenerated
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    internal class TestView : Microsoft.Extensions.RazorViews.BaseView
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("The time is ");
#line 1 "TestView.cshtml"
       Write(DateTime.UtcNow);

#line default
#line hidden
            WriteLiteral("\r\nwindow.alert(\"Hello world\");\r\nFooter goes here.");
        }
        #pragma warning restore 1998
    }
}
