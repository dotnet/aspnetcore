namespace TestOutput
{
    using System;

    public class DesignTime
    {
        private static object @__o;
public static Template 
#line 17 "DesignTime.cshtml"
Foo() {
#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 17 "DesignTime.cshtml"
               
    if(true) {
        
#line default
#line hidden

#line 19 "DesignTime.cshtml"
                  
    }
#line default
#line hidden

        }
        );
#line 21 "DesignTime.cshtml"
}
#line default
#line hidden

        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            #pragma warning restore 219
        }
        #line hidden
        public DesignTime()
        {
        }

        public override void Execute()
        {
#line 2 "DesignTime.cshtml"
             for(int i = 1; i <= 10; i++) {
    
#line default
#line hidden

            __o = 
#line 3 "DesignTime.cshtml"
                      i
#line default
#line hidden
            ;
#line 3 "DesignTime.cshtml"
                           
            }
#line default
#line hidden

            __o = 
#line 8 "DesignTime.cshtml"
  Foo(Bar.Baz)
#line default
#line hidden
            ;
            __o = 
#line 9 "DesignTime.cshtml"
 Foo(
#line default
#line hidden
            item => new Template((__razor_template_writer) => {
                __o = 
#line 9 "DesignTime.cshtml"
              baz
#line default
#line hidden
                ;
            }
            )
#line 9 "DesignTime.cshtml"
                         )
#line default
#line hidden
            ;
            DefineSection("Footer", () => {
                __o = 
#line 14 "DesignTime.cshtml"
     bar
#line default
#line hidden
                ;
            }
            );
        }
    }
}
