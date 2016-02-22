#pragma checksum "Sections.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "ec9a74381c339244a887565526c11056ece494a3"
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
                Instrumentation.BeginContext(99, 10, true);
                WriteLiteralTo(__razor_section_writer, "\r\n    <div");
                Instrumentation.EndContext();
                BeginWriteAttributeTo(__razor_section_writer, "class", " class=\"", 109, "\"", 128, 2);
                WriteAttributeValueTo(__razor_section_writer, "", 117, "some", 117, 4, true);
#line 8 "Sections.cshtml"
WriteAttributeValueTo(__razor_section_writer, " ", 121, thing, 122, 7, false);

#line default
#line hidden
                EndWriteAttributeTo(__razor_section_writer);
                Instrumentation.BeginContext(129, 29, true);
                WriteLiteralTo(__razor_section_writer, ">This is in Section 2</div>\r\n");
                Instrumentation.EndContext();
            }
            );
            Instrumentation.BeginContext(161, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            DefineSection("Section1", async(__razor_section_writer) => {
                Instrumentation.BeginContext(182, 39, true);
                WriteLiteralTo(__razor_section_writer, "\r\n    <div>This is in Section 1</div>\r\n");
                Instrumentation.EndContext();
            }
            );
            Instrumentation.BeginContext(224, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            DefineSection("NestedDelegates", async(__razor_section_writer) => {
                Instrumentation.BeginContext(252, 2, true);
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
                    Instrumentation.BeginContext(288, 6, true);
                    WriteLiteralTo(__razor_template_writer, "<span>");
                    Instrumentation.EndContext();
                    Instrumentation.BeginContext(295, 4, false);
#line 16 "Sections.cshtml"
        WriteTo(__razor_template_writer, item);

#line default
#line hidden
                    Instrumentation.EndContext();
                    Instrumentation.BeginContext(299, 7, true);
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
