#pragma checksum "ResolveUrl.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "ee17a91893cee3c8590202192de89abe32776cc2"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ResolveUrl
    {
        #line hidden
        public ResolveUrl()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(0, 2, true);
            WriteLiteral("<a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 2), Tuple.Create("\"", 14), Tuple.Create(Tuple.Create("", 9), Tuple.Create<System.Object, System.Int32>(Href("~/Foo"), 9), false));
            Instrumentation.BeginContext(15, 12, true);
            WriteLiteral(">Foo</a>\r\n<a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 27), Tuple.Create("\"", 56), Tuple.Create(Tuple.Create("", 34), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 34), false), 
            Tuple.Create(Tuple.Create("", 45), Tuple.Create<System.Object, System.Int32>(product.id, 45), false));
            Instrumentation.BeginContext(57, 1, true);
            WriteLiteral(">");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(59, 12, false);
#line 2 "ResolveUrl.cshtml"
                            Write(product.Name);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(71, 8, true);
            WriteLiteral("</a>\r\n<a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 79), Tuple.Create("\"", 115), Tuple.Create(Tuple.Create("", 86), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 86), false), 
            Tuple.Create(Tuple.Create("", 97), Tuple.Create<System.Object, System.Int32>(product.id, 97), false), Tuple.Create(Tuple.Create("", 108), Tuple.Create("/Detail", 108), true));
            Instrumentation.BeginContext(116, 16, true);
            WriteLiteral(">Details</a>\r\n<a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 132), Tuple.Create("\"", 187), Tuple.Create(Tuple.Create("", 139), Tuple.Create<System.Object, System.Int32>(Href("~/A+Really(Crazy),Url.Is:This/"), 139), false), 
            Tuple.Create(Tuple.Create("", 169), Tuple.Create<System.Object, System.Int32>(product.id, 169), false), Tuple.Create(Tuple.Create("", 180), Tuple.Create("/Detail", 180), true));
            Instrumentation.BeginContext(188, 19, true);
            WriteLiteral(">Crazy Url!</a>\r\n\r\n");
            Instrumentation.EndContext();
#line 6 "ResolveUrl.cshtml"
  

#line default
#line hidden

            Instrumentation.BeginContext(211, 16, true);
            WriteLiteral("    \r\n        <a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 233), Tuple.Create("\"", 245), Tuple.Create(Tuple.Create("", 240), Tuple.Create<System.Object, System.Int32>(Href("~/Foo"), 240), false));
            Instrumentation.BeginContext(246, 20, true);
            WriteLiteral(">Foo</a>\r\n        <a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 266), Tuple.Create("\"", 295), Tuple.Create(Tuple.Create("", 273), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 273), false), 
            Tuple.Create(Tuple.Create("", 284), Tuple.Create<System.Object, System.Int32>(product.id, 284), false));
            Instrumentation.BeginContext(296, 1, true);
            WriteLiteral(">");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(298, 12, false);
#line 9 "ResolveUrl.cshtml"
                                    Write(product.Name);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(310, 16, true);
            WriteLiteral("</a>\r\n        <a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 326), Tuple.Create("\"", 362), Tuple.Create(Tuple.Create("", 333), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 333), false), 
            Tuple.Create(Tuple.Create("", 344), Tuple.Create<System.Object, System.Int32>(product.id, 344), false), Tuple.Create(Tuple.Create("", 355), Tuple.Create("/Detail", 355), true));
            Instrumentation.BeginContext(363, 24, true);
            WriteLiteral(">Details</a>\r\n        <a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 387), Tuple.Create("\"", 442), Tuple.Create(Tuple.Create("", 394), Tuple.Create<System.Object, System.Int32>(Href("~/A+Really(Crazy),Url.Is:This/"), 394), false), 
            Tuple.Create(Tuple.Create("", 424), Tuple.Create<System.Object, System.Int32>(product.id, 424), false), Tuple.Create(Tuple.Create("", 435), Tuple.Create("/Detail", 435), true));
            Instrumentation.BeginContext(443, 23, true);
            WriteLiteral(">Crazy Url!</a>\r\n    \r\n");
            Instrumentation.EndContext();
#line 13 "ResolveUrl.cshtml"

#line default
#line hidden

            Instrumentation.BeginContext(474, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            DefineSection("Foo", async(__razor_template_writer) => {
                Instrumentation.BeginContext(492, 8, true);
                WriteLiteralTo(__razor_template_writer, "\r\n    <a");
                Instrumentation.EndContext();
                WriteAttributeTo(__razor_template_writer, "href", Tuple.Create(" href=\"", 500), Tuple.Create("\"", 512), Tuple.Create(Tuple.Create("", 507), Tuple.Create<System.Object, System.Int32>(Href("~/Foo"), 507), false));
                Instrumentation.BeginContext(513, 16, true);
                WriteLiteralTo(__razor_template_writer, ">Foo</a>\r\n    <a");
                Instrumentation.EndContext();
                WriteAttributeTo(__razor_template_writer, "href", Tuple.Create(" href=\"", 529), Tuple.Create("\"", 558), Tuple.Create(Tuple.Create("", 536), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 536), false), 
                Tuple.Create(Tuple.Create("", 547), Tuple.Create<System.Object, System.Int32>(product.id, 547), false));
                Instrumentation.BeginContext(559, 1, true);
                WriteLiteralTo(__razor_template_writer, ">");
                Instrumentation.EndContext();
                Instrumentation.BeginContext(561, 12, false);
#line 17 "ResolveUrl.cshtml"
     WriteTo(__razor_template_writer, product.Name);

#line default
#line hidden
                Instrumentation.EndContext();
                Instrumentation.BeginContext(573, 12, true);
                WriteLiteralTo(__razor_template_writer, "</a>\r\n    <a");
                Instrumentation.EndContext();
                WriteAttributeTo(__razor_template_writer, "href", Tuple.Create(" href=\"", 585), Tuple.Create("\"", 621), Tuple.Create(Tuple.Create("", 592), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 592), false), 
                Tuple.Create(Tuple.Create("", 603), Tuple.Create<System.Object, System.Int32>(product.id, 603), false), Tuple.Create(Tuple.Create("", 614), Tuple.Create("/Detail", 614), true));
                Instrumentation.BeginContext(622, 20, true);
                WriteLiteralTo(__razor_template_writer, ">Details</a>\r\n    <a");
                Instrumentation.EndContext();
                WriteAttributeTo(__razor_template_writer, "href", Tuple.Create(" href=\"", 642), Tuple.Create("\"", 697), Tuple.Create(Tuple.Create("", 649), Tuple.Create<System.Object, System.Int32>(Href("~/A+Really(Crazy),Url.Is:This/"), 649), false), 
                Tuple.Create(Tuple.Create("", 679), Tuple.Create<System.Object, System.Int32>(product.id, 679), false), Tuple.Create(Tuple.Create("", 690), Tuple.Create("/Detail", 690), true));
                Instrumentation.BeginContext(698, 17, true);
                WriteLiteralTo(__razor_template_writer, ">Crazy Url!</a>\r\n");
                Instrumentation.EndContext();
            }
            );
        }
        #pragma warning restore 1998
    }
}
