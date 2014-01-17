using System;
using Microsoft.AspNet.Mvc.Razor;

namespace MvcSample.Views
{
    [VirtualPath("~/Views/Home/MyView.cshtml")]
    public class MyView : RazorView<User>
    {
        public override void Execute()
        {
            Layout = "~/Views/Shared/_Layout.cshtml";
            WriteLiteral("<div style=\"border: 1px solid black\">The time is now");
            Write(new HtmlString(DateTime.UtcNow.ToString()));
            WriteLiteral("<em>");
            Write(Model.Name);
            Write("</em>");
            WriteLiteral("</div>");
        }
    }
}