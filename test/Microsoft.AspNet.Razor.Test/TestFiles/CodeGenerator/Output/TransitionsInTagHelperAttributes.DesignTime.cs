namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class TransitionsInTagHelperAttributes
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "TransitionsInTagHelperAttributes.cshtml"
              "something, nice"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private PTagHelper __PTagHelper = null;
        #line hidden
        public TransitionsInTagHelperAttributes()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 2 "TransitionsInTagHelperAttributes.cshtml"
   
    var @class = "container-fluid";
    var @int = 1;

#line default
#line hidden

            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 7 "TransitionsInTagHelperAttributes.cshtml"
    __PTagHelper.Age = 1337;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 8 "TransitionsInTagHelperAttributes.cshtml"
__o = @class;

#line default
#line hidden
#line 8 "TransitionsInTagHelperAttributes.cshtml"
       __PTagHelper.Age = 42;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 9 "TransitionsInTagHelperAttributes.cshtml"
  __PTagHelper.Age = 42 + @int;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 10 "TransitionsInTagHelperAttributes.cshtml"
  __PTagHelper.Age = int;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 11 "TransitionsInTagHelperAttributes.cshtml"
  __PTagHelper.Age = (@int);

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 12 "TransitionsInTagHelperAttributes.cshtml"
__o = @class;

#line default
#line hidden
#line 12 "TransitionsInTagHelperAttributes.cshtml"
              __PTagHelper.Age = 4 * @(@int + 2);

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
