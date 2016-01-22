#pragma checksum "NestedCSharp.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "2b9e8dcf7c08153c15ac84973938a7c0254f2369"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class NestedCSharp
    {
        #line hidden
        public NestedCSharp()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 1 "NestedCSharp.cshtml"
  
    

#line default
#line hidden

#line 2 "NestedCSharp.cshtml"
     foreach (var result in (dynamic)Url)
    {

#line default
#line hidden

            Instrumentation.BeginContext(54, 27, true);
            WriteLiteral("        <div>\r\n            ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(82, 16, false);
#line 5 "NestedCSharp.cshtml"
       Write(result.SomeValue);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(98, 19, true);
            WriteLiteral(".\r\n        </div>\r\n");
            Instrumentation.EndContext();
#line 7 "NestedCSharp.cshtml"
    }

#line default
#line hidden

#line 7 "NestedCSharp.cshtml"
     

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
