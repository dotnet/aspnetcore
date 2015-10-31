namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ComplexTagHelpers
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
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        #line hidden
        public ComplexTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 3 "ComplexTagHelpers.cshtml"
if (true)
{
    var checkbox = "checkbox";


#line default
#line hidden

#line 10 "ComplexTagHelpers.cshtml"
            

#line default
#line hidden

#line 10 "ComplexTagHelpers.cshtml"
            if (false)
            {

#line default
#line hidden

            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __TestNamespace_InputTagHelper.Type = "text";
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 13 "ComplexTagHelpers.cshtml"
            }
            else
            {

#line default
#line hidden

            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 16 "ComplexTagHelpers.cshtml"
                                        __o = checkbox;

#line default
#line hidden
            __TestNamespace_InputTagHelper.Type = string.Empty;
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 16 "ComplexTagHelpers.cshtml"
                     __TestNamespace_InputTagHelper2.Checked = true;

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 17 "ComplexTagHelpers.cshtml"
                         __o = true ? "checkbox" : "anything";

#line default
#line hidden
            __TestNamespace_InputTagHelper.Type = string.Empty;
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 18 "ComplexTagHelpers.cshtml"
                             if(true) {

#line default
#line hidden

#line 18 "ComplexTagHelpers.cshtml"
                                                              

#line default
#line hidden

#line 18 "ComplexTagHelpers.cshtml"
                                                              } else {

#line default
#line hidden

#line 18 "ComplexTagHelpers.cshtml"
                                                                                            

#line default
#line hidden

#line 18 "ComplexTagHelpers.cshtml"
                                                                                            }

#line default
#line hidden

            __TestNamespace_InputTagHelper.Type = string.Empty;
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 19 "ComplexTagHelpers.cshtml"
            }

#line default
#line hidden

            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 8 "ComplexTagHelpers.cshtml"
                          __o = DateTime.Now;

#line default
#line hidden
#line 22 "ComplexTagHelpers.cshtml"
            

#line default
#line hidden

#line 22 "ComplexTagHelpers.cshtml"
               var @object = false;

#line default
#line hidden

            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 23 "ComplexTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = (@object);

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 21 "ComplexTagHelpers.cshtml"
     __TestNamespace_PTagHelper.Age = DateTimeOffset.Now.Year - 1970;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 26 "ComplexTagHelpers.cshtml"
                  __TestNamespace_InputTagHelper2.Checked = (DateTimeOffset.Now.Year > 2014);

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 25 "ComplexTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = -1970 + @DateTimeOffset.Now.Year;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 29 "ComplexTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = DateTimeOffset.Now.Year > 2014;

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 28 "ComplexTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = DateTimeOffset.Now.Year - 1970;

#line default
#line hidden
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 32 "ComplexTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked =    @(  DateTimeOffset.Now.Year  ) > 2014   ;

#line default
#line hidden
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 31 "ComplexTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = ("My age is this long.".Length);

#line default
#line hidden
#line 34 "ComplexTagHelpers.cshtml"
   __o = someMethod(item => new Template(async(__razor_template_writer) => {
    __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
    __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 34 "ComplexTagHelpers.cshtml"
                     __TestNamespace_InputTagHelper2.Checked = checked;

#line default
#line hidden
    __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
#line 34 "ComplexTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = 123;

#line default
#line hidden
}
)
);

#line default
#line hidden
#line 36 "ComplexTagHelpers.cshtml"
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
