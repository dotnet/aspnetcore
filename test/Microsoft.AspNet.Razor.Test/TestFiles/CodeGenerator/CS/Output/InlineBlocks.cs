#pragma checksum "InlineBlocks.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "85fc1bd0306a5a6164d3d866bd690ff95cba0a8e"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class InlineBlocks
    {
public static Template 
#line 1 "InlineBlocks.cshtml"
Link(string link) {

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 1 "InlineBlocks.cshtml"
                           

#line default
#line hidden

            Instrumentation.BeginContext(29, 6, true);
            WriteLiteralTo(__razor_helper_writer, "    <a");
            Instrumentation.EndContext();
            WriteAttributeTo(__razor_helper_writer, "href", Tuple.Create(" href=\"", 35), Tuple.Create("\"", 93), 
            Tuple.Create(Tuple.Create("", 42), Tuple.Create<System.Object, System.Int32>(new Template((__razor_attribute_value_writer) => {
#line 2 "InlineBlocks.cshtml"
              if(link != null) { 

#line default
#line hidden

                Instrumentation.BeginContext(63, 4, false);
#line 2 "InlineBlocks.cshtml"
WriteTo(__razor_attribute_value_writer, link);

#line default
#line hidden
                Instrumentation.EndContext();
#line 2 "InlineBlocks.cshtml"
                                       } else {

#line default
#line hidden

                Instrumentation.BeginContext(76, 3, true);
                WriteLiteralTo(__razor_attribute_value_writer, " # ");
                Instrumentation.EndContext();
#line 2 "InlineBlocks.cshtml"
                                                               }

#line default
#line hidden

            }
            ), 42), false));
            Instrumentation.BeginContext(94, 5, true);
            WriteLiteralTo(__razor_helper_writer, " />\r\n");
            Instrumentation.EndContext();
#line 3 "InlineBlocks.cshtml"

#line default
#line hidden

        }
        );
#line 3 "InlineBlocks.cshtml"
}

#line default
#line hidden

        #line hidden
        public InlineBlocks()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
        }
        #pragma warning restore 1998
    }
}
