#pragma checksum "Sections.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "0bd579cc5b9e6bd12b720003df47899d0a0207cf"
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

            Instrumentation.BeginContext(47, 33, true);
            WriteLiteral("\r\n\r\n<div>This is in the Body>\r\n\r\n");
            Instrumentation.EndContext();
            DefineSection("Section2", new Template((__razor_template_writer) => {
                Instrumentation.BeginContext(99, 39, true);
                WriteLiteralTo(__razor_template_writer, "\r\n    <div>This is in Section 2</div>\r\n");
                Instrumentation.EndContext();
            }
            ));
            Instrumentation.BeginContext(141, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            DefineSection("Section1", new Template((__razor_template_writer) => {
                Instrumentation.BeginContext(162, 39, true);
                WriteLiteralTo(__razor_template_writer, "\r\n    <div>This is in Section 1</div>\r\n");
                Instrumentation.EndContext();
            }
            ));
        }
        #pragma warning restore 1998
    }
}
