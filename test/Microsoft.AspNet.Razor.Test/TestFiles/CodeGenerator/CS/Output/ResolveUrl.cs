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

        public override async Task ExecuteAsync()
        {
            WriteLiteral("<a");
            WriteAttribute("href", Tuple.Create(" href=\"", 2), Tuple.Create("\"", 14), Tuple.Create(Tuple.Create("", 9), Tuple.Create<System.Object, System.Int32>(Href("~/Foo"), 9), false));
            WriteLiteral(">Foo</a>\r\n<a");
            WriteAttribute("href", Tuple.Create(" href=\"", 27), Tuple.Create("\"", 56), Tuple.Create(Tuple.Create("", 34), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 34), false), 
            Tuple.Create(Tuple.Create("", 45), Tuple.Create<System.Object, System.Int32>(
#line 2 "ResolveUrl.cshtml"
                     product.id

#line default
#line hidden
            , 45), false));
            WriteLiteral(">");
            Write(
#line 2 "ResolveUrl.cshtml"
                                  product.Name

#line default
#line hidden
            );

            WriteLiteral("</a>\r\n<a");
            WriteAttribute("href", Tuple.Create(" href=\"", 79), Tuple.Create("\"", 115), Tuple.Create(Tuple.Create("", 86), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 86), false), 
            Tuple.Create(Tuple.Create("", 97), Tuple.Create<System.Object, System.Int32>(
#line 3 "ResolveUrl.cshtml"
                     product.id

#line default
#line hidden
            , 97), false), Tuple.Create(Tuple.Create("", 108), Tuple.Create("/Detail", 108), true));
            WriteLiteral(">Details</a>\r\n<a");
            WriteAttribute("href", Tuple.Create(" href=\"", 132), Tuple.Create("\"", 187), Tuple.Create(Tuple.Create("", 139), Tuple.Create<System.Object, System.Int32>(Href("~/A+Really(Crazy),Url.Is:This/"), 139), false), 
            Tuple.Create(Tuple.Create("", 169), Tuple.Create<System.Object, System.Int32>(
#line 4 "ResolveUrl.cshtml"
                                        product.id

#line default
#line hidden
            , 169), false), Tuple.Create(Tuple.Create("", 180), Tuple.Create("/Detail", 180), true));
            WriteLiteral(">Crazy Url!</a>\r\n\r\n");
#line 6 "ResolveUrl.cshtml"
  

#line default
#line hidden

            WriteLiteral("    \r\n        <a");
            WriteAttribute("href", Tuple.Create(" href=\"", 233), Tuple.Create("\"", 245), Tuple.Create(Tuple.Create("", 240), Tuple.Create<System.Object, System.Int32>(Href("~/Foo"), 240), false));
            WriteLiteral(">Foo</a>\r\n        <a");
            WriteAttribute("href", Tuple.Create(" href=\"", 266), Tuple.Create("\"", 295), Tuple.Create(Tuple.Create("", 273), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 273), false), 
            Tuple.Create(Tuple.Create("", 284), Tuple.Create<System.Object, System.Int32>(
#line 9 "ResolveUrl.cshtml"
                             product.id

#line default
#line hidden
            , 284), false));
            WriteLiteral(">");
            Write(
#line 9 "ResolveUrl.cshtml"
                                          product.Name

#line default
#line hidden
            );

            WriteLiteral("</a>\r\n        <a");
            WriteAttribute("href", Tuple.Create(" href=\"", 326), Tuple.Create("\"", 362), Tuple.Create(Tuple.Create("", 333), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 333), false), 
            Tuple.Create(Tuple.Create("", 344), Tuple.Create<System.Object, System.Int32>(
#line 10 "ResolveUrl.cshtml"
                             product.id

#line default
#line hidden
            , 344), false), Tuple.Create(Tuple.Create("", 355), Tuple.Create("/Detail", 355), true));
            WriteLiteral(">Details</a>\r\n        <a");
            WriteAttribute("href", Tuple.Create(" href=\"", 387), Tuple.Create("\"", 442), Tuple.Create(Tuple.Create("", 394), Tuple.Create<System.Object, System.Int32>(Href("~/A+Really(Crazy),Url.Is:This/"), 394), false), 
            Tuple.Create(Tuple.Create("", 424), Tuple.Create<System.Object, System.Int32>(
#line 11 "ResolveUrl.cshtml"
                                                product.id

#line default
#line hidden
            , 424), false), Tuple.Create(Tuple.Create("", 435), Tuple.Create("/Detail", 435), true));
            WriteLiteral(">Crazy Url!</a>\r\n    \r\n");
#line 13 "ResolveUrl.cshtml"

#line default
#line hidden

            WriteLiteral("\r\n\r\n");
            DefineSection("Foo", () => {
                WriteLiteral("\r\n    <a");
                WriteAttribute("href", Tuple.Create(" href=\"", 500), Tuple.Create("\"", 512), Tuple.Create(Tuple.Create("", 507), Tuple.Create<System.Object, System.Int32>(Href("~/Foo"), 507), false));
                WriteLiteral(">Foo</a>\r\n    <a");
                WriteAttribute("href", Tuple.Create(" href=\"", 529), Tuple.Create("\"", 558), Tuple.Create(Tuple.Create("", 536), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 536), false), 
                Tuple.Create(Tuple.Create("", 547), Tuple.Create<System.Object, System.Int32>(
#line 17 "ResolveUrl.cshtml"
                         product.id

#line default
#line hidden
                , 547), false));
                WriteLiteral(">");
                Write(
#line 17 "ResolveUrl.cshtml"
                                      product.Name

#line default
#line hidden
                );

                WriteLiteral("</a>\r\n    <a");
                WriteAttribute("href", Tuple.Create(" href=\"", 585), Tuple.Create("\"", 621), Tuple.Create(Tuple.Create("", 592), Tuple.Create<System.Object, System.Int32>(Href("~/Products/"), 592), false), 
                Tuple.Create(Tuple.Create("", 603), Tuple.Create<System.Object, System.Int32>(
#line 18 "ResolveUrl.cshtml"
                         product.id

#line default
#line hidden
                , 603), false), Tuple.Create(Tuple.Create("", 614), Tuple.Create("/Detail", 614), true));
                WriteLiteral(">Details</a>\r\n    <a");
                WriteAttribute("href", Tuple.Create(" href=\"", 642), Tuple.Create("\"", 697), Tuple.Create(Tuple.Create("", 649), Tuple.Create<System.Object, System.Int32>(Href("~/A+Really(Crazy),Url.Is:This/"), 649), false), 
                Tuple.Create(Tuple.Create("", 679), Tuple.Create<System.Object, System.Int32>(
#line 19 "ResolveUrl.cshtml"
                                            product.id

#line default
#line hidden
                , 679), false), Tuple.Create(Tuple.Create("", 690), Tuple.Create("/Detail", 690), true));
                WriteLiteral(">Crazy Url!</a>\r\n");
            }
            );
        }
    }
}
