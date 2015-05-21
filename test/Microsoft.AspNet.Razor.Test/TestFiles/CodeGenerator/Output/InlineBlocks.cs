#pragma checksum "InlineBlocks.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "e827e93343a95c7254a19287b095dfba9390d29f"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class InlineBlocks
    {
        #line hidden
        public InlineBlocks()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            DefineSection("Link", async(__razor_template_writer) => {
            }
            );
            Instrumentation.BeginContext(13, 23, true);
            WriteLiteral("(string link) {\r\n    <a");
            Instrumentation.EndContext();
            WriteAttribute("href", Tuple.Create(" href=\"", 36), Tuple.Create("\"", 94), 
            Tuple.Create(Tuple.Create("", 43), Tuple.Create<System.Object, System.Int32>(new Template((__razor_attribute_value_writer) => {
#line 2 "InlineBlocks.cshtml"
              if(link != null) { 

#line default
#line hidden

                Instrumentation.BeginContext(64, 4, false);
#line 2 "InlineBlocks.cshtml"
WriteTo(__razor_attribute_value_writer, link);

#line default
#line hidden
                Instrumentation.EndContext();
#line 2 "InlineBlocks.cshtml"
                                       } else {

#line default
#line hidden

                Instrumentation.BeginContext(84, 1, true);
                WriteLiteralTo(__razor_attribute_value_writer, "#");
                Instrumentation.EndContext();
#line 2 "InlineBlocks.cshtml"
                                                              

#line default
#line hidden

#line 2 "InlineBlocks.cshtml"
                                                              }

#line default
#line hidden

            }
            ), 43), false));
            Instrumentation.BeginContext(95, 6, true);
            WriteLiteral(" />\r\n}");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
