namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

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

        public override async Task ExecuteAsync()
        {
#line 2 "DesignTime.cshtml"
            for(int i = 1; i <= 10; i++) {
    

#line default
#line hidden

#line 1 "------------------------------------------"
                __o = i;

#line default
#line hidden
#line 3 "DesignTime.cshtml"
                           
            }

#line default
#line hidden

#line 1 "------------------------------------------"
__o = Foo(Bar.Baz);

#line default
#line hidden
#line 1 "------------------------------------------"
__o = Foo(item => new Template((__razor_template_writer) => {
#line 1 "------------------------------------------"
        __o = baz;

#line default
#line hidden
}
)
#line 9 "DesignTime.cshtml"
                         )

#line default
#line hidden
;

#line default
#line hidden
            DefineSection("Footer", () => {
#line 1 "------------------------------------------"
__o = bar;

#line default
#line hidden
            }
            );
        }
    }
}
