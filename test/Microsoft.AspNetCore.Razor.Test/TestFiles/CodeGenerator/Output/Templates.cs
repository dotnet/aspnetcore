#pragma checksum "Templates.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "ecd19ba0b3ae7ac597e19534c93fd2023208ae59"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Templates
    {
#line 1 "Templates.cshtml"

    public HelperResult Repeat(int times, Func<int, object> template) {
        return new HelperResult((writer) => {
            for(int i = 0; i < times; i++) {
                ((HelperResult)template(i)).WriteTo(writer);
            }
        });
    }

#line default
#line hidden
        #line hidden
        public Templates()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(280, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 11 "Templates.cshtml"
  
    Func<dynamic, object> foo = 

#line default
#line hidden

            item => new Template(async(__razor_template_writer) => {
                Instrumentation.BeginContext(325, 11, true);
                WriteLiteralTo(__razor_template_writer, "This works ");
                Instrumentation.EndContext();
                Instrumentation.BeginContext(337, 4, false);
#line 12 "Templates.cshtml"
                  WriteTo(__razor_template_writer, item);

#line default
#line hidden
                Instrumentation.EndContext();
                Instrumentation.BeginContext(341, 1, true);
                WriteLiteralTo(__razor_template_writer, "!");
                Instrumentation.EndContext();
            }
            )
#line 12 "Templates.cshtml"
                                                               ;
    

#line default
#line hidden

            Instrumentation.BeginContext(357, 7, false);
#line 13 "Templates.cshtml"
Write(foo(""));

#line default
#line hidden
            Instrumentation.EndContext();
#line 13 "Templates.cshtml"
            

#line default
#line hidden

            Instrumentation.BeginContext(369, 8, true);
            WriteLiteral("\r\n<ul>\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(379, 11, false);
#line 17 "Templates.cshtml"
Write(Repeat(10, item => new Template(async(__razor_template_writer) => {
    Instrumentation.BeginContext(391, 10, true);
    WriteLiteralTo(__razor_template_writer, "<li>Item #");
    Instrumentation.EndContext();
    Instrumentation.BeginContext(402, 4, false);
#line 17 "Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    Instrumentation.EndContext();
    Instrumentation.BeginContext(406, 5, true);
    WriteLiteralTo(__razor_template_writer, "</li>");
    Instrumentation.EndContext();
}
)
));

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(413, 16, true);
            WriteLiteral("\r\n</ul>\r\n\r\n<p>\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(430, 16, false);
#line 21 "Templates.cshtml"
Write(Repeat(10,
    item => new Template(async(__razor_template_writer) => {
    Instrumentation.BeginContext(448, 14, true);
    WriteLiteralTo(__razor_template_writer, " This is line#");
    Instrumentation.EndContext();
    Instrumentation.BeginContext(463, 4, false);
#line 22 "Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    Instrumentation.EndContext();
    Instrumentation.BeginContext(467, 17, true);
    WriteLiteralTo(__razor_template_writer, " of markup<br/>\r\n");
    Instrumentation.EndContext();
}
)
));

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(485, 15, true);
            WriteLiteral("\r\n</p>\r\n\r\n<p>\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(501, 16, false);
#line 27 "Templates.cshtml"
Write(Repeat(10,
    item => new Template(async(__razor_template_writer) => {
    Instrumentation.BeginContext(519, 15, true);
    WriteLiteralTo(__razor_template_writer, ": This is line#");
    Instrumentation.EndContext();
    Instrumentation.BeginContext(535, 4, false);
#line 28 "Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    Instrumentation.EndContext();
    Instrumentation.BeginContext(539, 18, true);
    WriteLiteralTo(__razor_template_writer, " of markup<br />\r\n");
    Instrumentation.EndContext();
}
)
));

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(558, 15, true);
            WriteLiteral("\r\n</p>\r\n\r\n<p>\r\n");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(574, 16, false);
#line 33 "Templates.cshtml"
Write(Repeat(10,
    item => new Template(async(__razor_template_writer) => {
    Instrumentation.BeginContext(592, 16, true);
    WriteLiteralTo(__razor_template_writer, ":: This is line#");
    Instrumentation.EndContext();
    Instrumentation.BeginContext(609, 4, false);
#line 34 "Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    Instrumentation.EndContext();
    Instrumentation.BeginContext(613, 18, true);
    WriteLiteralTo(__razor_template_writer, " of markup<br />\r\n");
    Instrumentation.EndContext();
}
)
));

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(632, 22, true);
            WriteLiteral("\r\n</p>\r\n\r\n\r\n<ul>\r\n    ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(655, 11, false);
#line 40 "Templates.cshtml"
Write(Repeat(10, item => new Template(async(__razor_template_writer) => {
    Instrumentation.BeginContext(667, 20, true);
    WriteLiteralTo(__razor_template_writer, "<li>\r\n        Item #");
    Instrumentation.EndContext();
    Instrumentation.BeginContext(688, 4, false);
#line 41 "Templates.cshtml"
WriteTo(__razor_template_writer, item);

#line default
#line hidden
    Instrumentation.EndContext();
    Instrumentation.BeginContext(692, 2, true);
    WriteLiteralTo(__razor_template_writer, "\r\n");
    Instrumentation.EndContext();
#line 42 "Templates.cshtml"
        

#line default
#line hidden

#line 42 "Templates.cshtml"
          var parent = item;

#line default
#line hidden

    Instrumentation.BeginContext(725, 77, true);
    WriteLiteralTo(__razor_template_writer, "        <ul>\r\n            <li>Child Items... ?</li>\r\n        </ul>\r\n    </li>");
    Instrumentation.EndContext();
}
)
));

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(864, 8, true);
            WriteLiteral("\r\n</ul> ");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
