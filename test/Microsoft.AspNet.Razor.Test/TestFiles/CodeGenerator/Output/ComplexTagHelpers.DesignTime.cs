namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class ComplexTagHelpers
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "ComplexTagHelpers.cshtml"
              "T?g??lpe*, nice"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private PTagHelper __PTagHelper = null;
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
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

            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __InputTagHelper.Type = "text";
            __InputTagHelper2.Type = __InputTagHelper.Type;
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 13 "ComplexTagHelpers.cshtml"
            }
            else
            {

#line default
#line hidden

            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
#line 16 "ComplexTagHelpers.cshtml"
__o = checkbox;

#line default
#line hidden
            __InputTagHelper.Type = string.Empty;
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 16 "ComplexTagHelpers.cshtml"
                                   __InputTagHelper2.Checked = true;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
#line 17 "ComplexTagHelpers.cshtml"
__o = true ? "checkbox" : "anything";

#line default
#line hidden
            __InputTagHelper.Type = string.Empty;
            __InputTagHelper2.Type = __InputTagHelper.Type;
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
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

            __InputTagHelper.Type = string.Empty;
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 19 "ComplexTagHelpers.cshtml"
            }

#line default
#line hidden

            __PTagHelper = CreateTagHelper<PTagHelper>();
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

            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
#line 23 "ComplexTagHelpers.cshtml"
__InputTagHelper2.Checked = (@object);

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 21 "ComplexTagHelpers.cshtml"
                   __PTagHelper.Age = DateTimeOffset.Now.Year - 1970;

#line default
#line hidden
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
#line 26 "ComplexTagHelpers.cshtml"
                                __InputTagHelper2.Checked = (DateTimeOffset.Now.Year > 2014);

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 25 "ComplexTagHelpers.cshtml"
__PTagHelper.Age = -1970 + @DateTimeOffset.Now.Year;

#line default
#line hidden
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
#line 29 "ComplexTagHelpers.cshtml"
__InputTagHelper2.Checked = DateTimeOffset.Now.Year > 2014;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 28 "ComplexTagHelpers.cshtml"
__PTagHelper.Age = DateTimeOffset.Now.Year - 1970;

#line default
#line hidden
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
#line 32 "ComplexTagHelpers.cshtml"
__InputTagHelper2.Checked =    @(  DateTimeOffset.Now.Year  ) > 2014   ;

#line default
#line hidden
            __PTagHelper = CreateTagHelper<PTagHelper>();
#line 31 "ComplexTagHelpers.cshtml"
__PTagHelper.Age = ("My age is this long.".Length);

#line default
#line hidden
#line 35 "ComplexTagHelpers.cshtml"
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
