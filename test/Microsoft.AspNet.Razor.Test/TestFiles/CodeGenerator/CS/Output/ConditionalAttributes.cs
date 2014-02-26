namespace TestOutput
{
    using System;

    public class ConditionalAttributes
    {
        #line hidden
        public ConditionalAttributes()
        {
        }

        public override void Execute()
        {
#line 1 "ConditionalAttributes.cshtml"
  
    var ch = true;
    var cls = "bar";

#line default
#line hidden

            WriteLiteral("    <a href=\"Foo\" />\r\n    <p");
            WriteAttribute("class", Tuple.Create(" class=\"", 74), Tuple.Create("\"", 86), 
            Tuple.Create(Tuple.Create("", 82), Tuple.Create<System.Object, System.Int32>(
#line 5 "ConditionalAttributes.cshtml"
               cls

#line default
#line hidden
            , 82), false));
            WriteLiteral(" />\r\n    <p");
            WriteAttribute("class", Tuple.Create(" class=\"", 98), Tuple.Create("\"", 114), Tuple.Create(Tuple.Create("", 106), Tuple.Create("foo", 106), true), 
            Tuple.Create(Tuple.Create(" ", 109), Tuple.Create<System.Object, System.Int32>(
#line 6 "ConditionalAttributes.cshtml"
                   cls

#line default
#line hidden
            , 110), false));
            WriteLiteral(" />\r\n    <p");
            WriteAttribute("class", Tuple.Create(" class=\"", 126), Tuple.Create("\"", 142), 
            Tuple.Create(Tuple.Create("", 134), Tuple.Create<System.Object, System.Int32>(
#line 7 "ConditionalAttributes.cshtml"
               cls

#line default
#line hidden
            , 134), false), Tuple.Create(Tuple.Create(" ", 138), Tuple.Create("foo", 139), true));
            WriteLiteral(" />\r\n    <input type=\"checkbox\"");
            WriteAttribute("checked", Tuple.Create(" checked=\"", 174), Tuple.Create("\"", 187), 
            Tuple.Create(Tuple.Create("", 184), Tuple.Create<System.Object, System.Int32>(
#line 8 "ConditionalAttributes.cshtml"
                                     ch

#line default
#line hidden
            , 184), false));
            WriteLiteral(" />\r\n    <input type=\"checkbox\"");
            WriteAttribute("checked", Tuple.Create(" checked=\"", 219), Tuple.Create("\"", 236), Tuple.Create(Tuple.Create("", 229), Tuple.Create("foo", 229), true), 
            Tuple.Create(Tuple.Create(" ", 232), Tuple.Create<System.Object, System.Int32>(
#line 9 "ConditionalAttributes.cshtml"
                                         ch

#line default
#line hidden
            , 233), false));
            WriteLiteral(" />\r\n    <p");
            WriteAttribute("class", Tuple.Create(" class=\"", 248), Tuple.Create("\"", 281), 
            Tuple.Create(Tuple.Create("", 256), Tuple.Create<System.Object, System.Int32>(new Template((__razor_attribute_value_writer) => {
#line 10 "ConditionalAttributes.cshtml"
               if(cls != null) { 

#line default
#line hidden

                WriteTo(__razor_attribute_value_writer, 
#line 10 "ConditionalAttributes.cshtml"
                                  cls

#line default
#line hidden
                );

#line 10 "ConditionalAttributes.cshtml"
                                      }

#line default
#line hidden

            }
            ), 256), false));
            WriteLiteral(" />\r\n    <a");
            WriteAttribute("href", Tuple.Create(" href=\"", 293), Tuple.Create("\"", 305), Tuple.Create(Tuple.Create("", 300), Tuple.Create<System.Object, System.Int32>(Href("~/Foo"), 300), false));
            WriteLiteral(" />\r\n    <script");
            WriteAttribute("src", Tuple.Create(" src=\"", 322), Tuple.Create("\"", 373), 
            Tuple.Create(Tuple.Create("", 328), Tuple.Create<System.Object, System.Int32>(
#line 12 "ConditionalAttributes.cshtml"
                  Url.Content("~/Scripts/jquery-1.6.2.min.js")

#line default
#line hidden
            , 328), false));
            WriteLiteral(" type=\"text/javascript\"></script>\r\n    <script");
            WriteAttribute("src", Tuple.Create(" src=\"", 420), Tuple.Create("\"", 487), 
            Tuple.Create(Tuple.Create("", 426), Tuple.Create<System.Object, System.Int32>(
#line 13 "ConditionalAttributes.cshtml"
                  Url.Content("~/Scripts/modernizr-2.0.6-development-only.js")

#line default
#line hidden
            , 426), false));
            WriteLiteral(" type=\"text/javascript\"></script>\r\n    <script src=\"http://ajax.aspnetcdn.com/aja" +
"x/jquery.ui/1.8.16/jquery-ui.min.js\" type=\"text/javascript\"></script>\r\n");
#line 15 "ConditionalAttributes.cshtml"

#line default
#line hidden

        }
    }
}
