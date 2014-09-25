#pragma checksum "ConditionalAttributes.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "f0a97e23ecfbaaaa77b528650156cb123ebdbe60"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ConditionalAttributes
    {
        #line hidden
        public ConditionalAttributes()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "ConditionalAttributes.cshtml"
  
    var ch = true;
    var cls = "bar";

#line default
#line hidden

            Instrumentation.BeginContext(46, 28, true);
            WriteLiteral("    <a href=\"Foo\" />\r\n    <p");
            Instrumentation.EndContext();
            WriteAttribute("class", Tuple.Create(" class=\"", 74), Tuple.Create("\"", 86), 
            Tuple.Create(Tuple.Create("", 82), Tuple.Create<System.Object, System.Int32>(cls, 82), false));
            Instrumentation.BeginContext(87, 11, true);
            WriteLiteral(" />\r\n    <p");
            Instrumentation.EndContext();
            WriteAttribute("class", Tuple.Create(" class=\"", 98), Tuple.Create("\"", 114), Tuple.Create(Tuple.Create("", 106), Tuple.Create("foo", 106), true), 
            Tuple.Create(Tuple.Create(" ", 109), Tuple.Create<System.Object, System.Int32>(cls, 110), false));
            Instrumentation.BeginContext(115, 11, true);
            WriteLiteral(" />\r\n    <p");
            Instrumentation.EndContext();
            WriteAttribute("class", Tuple.Create(" class=\"", 126), Tuple.Create("\"", 142), 
            Tuple.Create(Tuple.Create("", 134), Tuple.Create<System.Object, System.Int32>(cls, 134), false), Tuple.Create(Tuple.Create(" ", 138), Tuple.Create("foo", 139), true));
            Instrumentation.BeginContext(143, 31, true);
            WriteLiteral(" />\r\n    <input type=\"checkbox\"");
            Instrumentation.EndContext();
            WriteAttribute("checked", Tuple.Create(" checked=\"", 174), Tuple.Create("\"", 187), 
            Tuple.Create(Tuple.Create("", 184), Tuple.Create<System.Object, System.Int32>(ch, 184), false));
            Instrumentation.BeginContext(188, 31, true);
            WriteLiteral(" />\r\n    <input type=\"checkbox\"");
            Instrumentation.EndContext();
            WriteAttribute("checked", Tuple.Create(" checked=\"", 219), Tuple.Create("\"", 236), Tuple.Create(Tuple.Create("", 229), Tuple.Create("foo", 229), true), 
            Tuple.Create(Tuple.Create(" ", 232), Tuple.Create<System.Object, System.Int32>(ch, 233), false));
            Instrumentation.BeginContext(237, 11, true);
            WriteLiteral(" />\r\n    <p");
            Instrumentation.EndContext();
            WriteAttribute("class", Tuple.Create(" class=\"", 248), Tuple.Create("\"", 281), 
            Tuple.Create(Tuple.Create("", 256), Tuple.Create<System.Object, System.Int32>(new Template((__razor_attribute_value_writer) => {
#line 10 "ConditionalAttributes.cshtml"
               if(cls != null) { 

#line default
#line hidden

                Instrumentation.BeginContext(276, 3, false);
#line 10 "ConditionalAttributes.cshtml"
WriteTo(__razor_attribute_value_writer, cls);

#line default
#line hidden
                Instrumentation.EndContext();
#line 10 "ConditionalAttributes.cshtml"
                                      }

#line default
#line hidden

            }
            ), 256), false));
            Instrumentation.BeginContext(282, 11, true);
            WriteLiteral(" />\r\n    <a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 293), Tuple.Create("\"", 305), Tuple.Create(Tuple.Create("", 300), Tuple.Create<System.Object, System.Int32>(Href("~/Foo"), 300), false));
            Instrumentation.BeginContext(306, 16, true);
            WriteLiteral(" />\r\n    <script");
            Instrumentation.EndContext();
            WriteAttribute("src", Tuple.Create(" src=\"", 322), Tuple.Create("\"", 373), 
            Tuple.Create(Tuple.Create("", 328), Tuple.Create<System.Object, System.Int32>(Url.Content("~/Scripts/jquery-1.6.2.min.js"), 328), false));
            Instrumentation.BeginContext(374, 46, true);
            WriteLiteral(" type=\"text/javascript\"></script>\r\n    <script");
            Instrumentation.EndContext();
            WriteAttribute("src", Tuple.Create(" src=\"", 420), Tuple.Create("\"", 487), 
            Tuple.Create(Tuple.Create("", 426), Tuple.Create<System.Object, System.Int32>(Url.Content("~/Scripts/modernizr-2.0.6-development-only.js"), 426), false));
            Instrumentation.BeginContext(488, 152, true);
            WriteLiteral(" type=\"text/javascript\"></script>\r\n    <script src=\"http://ajax.aspnetcdn.com/aja" +
"x/jquery.ui/1.8.16/jquery-ui.min.js\" type=\"text/javascript\"></script>\r\n");
            Instrumentation.EndContext();
#line 15 "ConditionalAttributes.cshtml"

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
