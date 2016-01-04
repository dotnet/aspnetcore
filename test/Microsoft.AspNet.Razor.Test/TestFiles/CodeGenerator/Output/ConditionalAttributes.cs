#pragma checksum "ConditionalAttributes.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "088be4e50958bcab0f1d1ac04d2c28dcd8049bf5"
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
            BeginWriteAttribute("class", " class=\"", 74, "\"", 86, 1);
#line 5 "ConditionalAttributes.cshtml"
WriteAttributeValue("", 82, cls, 82, 4, false);

#line default
#line hidden
            EndWriteAttribute();
            Instrumentation.BeginContext(87, 11, true);
            WriteLiteral(" />\r\n    <p");
            Instrumentation.EndContext();
            BeginWriteAttribute("class", " class=\"", 98, "\"", 114, 2);
            WriteAttributeValue("", 106, "foo", 106, 3, true);
#line 6 "ConditionalAttributes.cshtml"
WriteAttributeValue(" ", 109, cls, 110, 5, false);

#line default
#line hidden
            EndWriteAttribute();
            Instrumentation.BeginContext(115, 11, true);
            WriteLiteral(" />\r\n    <p");
            Instrumentation.EndContext();
            BeginWriteAttribute("class", " class=\"", 126, "\"", 142, 2);
#line 7 "ConditionalAttributes.cshtml"
WriteAttributeValue("", 134, cls, 134, 4, false);

#line default
#line hidden
            WriteAttributeValue(" ", 138, "foo", 139, 4, true);
            EndWriteAttribute();
            Instrumentation.BeginContext(143, 31, true);
            WriteLiteral(" />\r\n    <input type=\"checkbox\"");
            Instrumentation.EndContext();
            BeginWriteAttribute("checked", " checked=\"", 174, "\"", 187, 1);
#line 8 "ConditionalAttributes.cshtml"
WriteAttributeValue("", 184, ch, 184, 3, false);

#line default
#line hidden
            EndWriteAttribute();
            Instrumentation.BeginContext(188, 31, true);
            WriteLiteral(" />\r\n    <input type=\"checkbox\"");
            Instrumentation.EndContext();
            BeginWriteAttribute("checked", " checked=\"", 219, "\"", 236, 2);
            WriteAttributeValue("", 229, "foo", 229, 3, true);
#line 9 "ConditionalAttributes.cshtml"
WriteAttributeValue(" ", 232, ch, 233, 4, false);

#line default
#line hidden
            EndWriteAttribute();
            Instrumentation.BeginContext(237, 11, true);
            WriteLiteral(" />\r\n    <p");
            Instrumentation.EndContext();
            BeginWriteAttribute("class", " class=\"", 248, "\"", 281, 1);
            WriteAttributeValue("", 256, new Template(async(__razor_attribute_value_writer) => {
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
            ), 256, 25, false);
            EndWriteAttribute();
            Instrumentation.BeginContext(282, 40, true);
            WriteLiteral(" />\r\n    <a href=\"~/Foo\" />\r\n    <script");
            Instrumentation.EndContext();
            BeginWriteAttribute("src", " src=\"", 322, "\"", 373, 1);
#line 12 "ConditionalAttributes.cshtml"
WriteAttributeValue("", 328, Url.Content("~/Scripts/jquery-1.6.2.min.js"), 328, 45, false);

#line default
#line hidden
            EndWriteAttribute();
            Instrumentation.BeginContext(374, 46, true);
            WriteLiteral(" type=\"text/javascript\"></script>\r\n    <script");
            Instrumentation.EndContext();
            BeginWriteAttribute("src", " src=\"", 420, "\"", 487, 1);
#line 13 "ConditionalAttributes.cshtml"
WriteAttributeValue("", 426, Url.Content("~/Scripts/modernizr-2.0.6-development-only.js"), 426, 61, false);

#line default
#line hidden
            EndWriteAttribute();
            Instrumentation.BeginContext(488, 152, true);
            WriteLiteral(" type=\"text/javascript\"></script>\r\n    <script src=\"http://ajax.aspnetcdn.com/ajax/jquery.ui/1.8.16/jquery-ui.min.js\" type=\"text/javascript\"></script>\r\n");
            Instrumentation.EndContext();
#line 15 "ConditionalAttributes.cshtml"

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
