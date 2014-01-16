using System;
using Microsoft.AspNet.Mvc.Razor;

namespace MvcSample.Views
{
    [VirtualPath("~/Views/Shared/_Layout.cshtml")]
    public class Layout : RazorView
    {
        protected override void Execute()
        {
            WriteLiteral("<html>");
            WriteLiteral("<body>");
            WriteLiteral("<h1>Hello world</h1>");
            WriteLiteral("<div id=\"main\"");
            RenderBody();
            WriteLiteral("</div>");
            WriteLiteral("</body></html>");
        }
    }
}