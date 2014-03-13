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

        public override async Task ExecuteAsync()
        {
#line 1 "Sections.cshtml"
  
    Layout = "_SectionTestLayout.cshtml"

#line default
#line hidden

            WriteLiteral("\r\n\r\n<div>This is in the Body>\r\n\r\n");
            DefineSection("Section2", () => {
                WriteLiteral("\r\n    <div>This is in Section 2</div>\r\n");
            }
            );
            WriteLiteral("\r\n");
            DefineSection("Section1", () => {
                WriteLiteral("\r\n    <div>This is in Section 1</div>\r\n");
            }
            );
        }
    }
}
