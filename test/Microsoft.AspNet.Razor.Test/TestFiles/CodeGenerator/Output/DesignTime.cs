namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class DesignTime
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            #pragma warning restore 219
        }
        #line hidden
        public DesignTime()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 2 "DesignTime.cshtml"
            for(int i = 1; i <= 10; i++) {
    

#line default
#line hidden

#line 3 "DesignTime.cshtml"
                __o = i;

#line default
#line hidden
#line 3 "DesignTime.cshtml"
                           
            }

#line default
#line hidden

#line 8 "DesignTime.cshtml"
__o = Foo(Bar.Baz);

#line default
#line hidden
#line 9 "DesignTime.cshtml"
__o = Foo(item => new Template((__razor_template_writer) => {
#line 9 "DesignTime.cshtml"
        __o = baz;

#line default
#line hidden
}
)
);

#line default
#line hidden
            DefineSection("Footer", async(__razor_template_writer) => {
#line 14 "DesignTime.cshtml"
__o = bar;

#line default
#line hidden
            }
            );
        }
        #pragma warning restore 1998
    }
}
