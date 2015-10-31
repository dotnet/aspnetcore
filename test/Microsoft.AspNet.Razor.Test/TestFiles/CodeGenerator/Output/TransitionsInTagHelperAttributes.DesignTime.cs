namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class TransitionsInTagHelperAttributes
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = "something, nice";
            #pragma warning restore 219
        }
        #line hidden
        private global::TestNamespace.PTagHelper __TestNamespace_PTagHelper = null;
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

            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 7 "TransitionsInTagHelperAttributes.cshtml"
__TestNamespace_PTagHelper.Age = 1337;

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 8 "TransitionsInTagHelperAttributes.cshtml"
      __o = @class;

#line default
#line hidden
#line 8 "TransitionsInTagHelperAttributes.cshtml"
__TestNamespace_PTagHelper.Age = 42;

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 9 "TransitionsInTagHelperAttributes.cshtml"
__TestNamespace_PTagHelper.Age = 42 + @int;

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 10 "TransitionsInTagHelperAttributes.cshtml"
__TestNamespace_PTagHelper.Age = int;

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 11 "TransitionsInTagHelperAttributes.cshtml"
__TestNamespace_PTagHelper.Age = (@int);

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 12 "TransitionsInTagHelperAttributes.cshtml"
             __o = @class;

#line default
#line hidden
#line 12 "TransitionsInTagHelperAttributes.cshtml"
__TestNamespace_PTagHelper.Age = 4 * @(@int + 2);

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
