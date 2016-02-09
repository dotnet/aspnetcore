#pragma checksum "Sections.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "bf45c2508895b4b9b3579bc193e7a0167ccdbdfb"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Sections
    {
        #line hidden
        public Sections()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "Sections.cshtml"
  
    Layout = "_SectionTestLayout.cshtml"

#line default
#line hidden

            Instrumentation.BeginContext(49, 31, true);
            WriteLiteral("\r\n<div>This is in the Body>\r\n\r\n");
            Instrumentation.EndContext();
            DefineSection("Section2", async(__razor_section_writer) => {
                Instrumentation.BeginContext(99, 39, true);
                WriteLiteralTo(__razor_section_writer, "\r\n    <div>This is in Section 2</div>\r\n");
                Instrumentation.EndContext();
            }
            );
            Instrumentation.BeginContext(141, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            DefineSection("Section1", async(__razor_section_writer) => {
                Instrumentation.BeginContext(162, 39, true);
                WriteLiteralTo(__razor_section_writer, "\r\n    <div>This is in Section 1</div>\r\n");
                Instrumentation.EndContext();
            }
            );
            Instrumentation.BeginContext(204, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            DefineSection("NestedDelegates", async(__razor_section_writer) => {
                Instrumentation.BeginContext(232, 2, true);
                WriteLiteralTo(__razor_section_writer, "\r\n");
                Instrumentation.EndContext();
#line 16 "Sections.cshtml"
    

#line default
#line hidden

#line 16 "Sections.cshtml"
       Func<dynamic, object> f = 

#line default
#line hidden

                item => new Template(async(__razor_template_writer) => {
                    Instrumentation.BeginContext(268, 6, true);
                    WriteLiteralTo(__razor_template_writer, "<span>");
                    Instrumentation.EndContext();
                    Instrumentation.BeginContext(275, 4, false);
#line 16 "Sections.cshtml"
        WriteTo(__razor_template_writer, item);

#line default
#line hidden
                    Instrumentation.EndContext();
                    Instrumentation.BeginContext(279, 7, true);
                    WriteLiteralTo(__razor_template_writer, "</span>");
                    Instrumentation.EndContext();
                }
                )
#line 16 "Sections.cshtml"
                                                    ; 

#line default
#line hidden

            }
            );
        }
        #pragma warning restore 1998
    }
}
