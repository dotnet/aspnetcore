namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class EnumTagHelpers
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 1 "EnumTagHelpers.cshtml"
              "something, nice"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private InputTagHelper __InputTagHelper = null;
        private CatchAllTagHelper __CatchAllTagHelper = null;
        #line hidden
        public EnumTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 3 "EnumTagHelpers.cshtml"
  
    var enumValue = MyEnum.MyValue;

#line default
#line hidden

            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
#line 7 "EnumTagHelpers.cshtml"
__InputTagHelper.Value = MyEnum.MyValue;

#line default
#line hidden
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
#line 8 "EnumTagHelpers.cshtml"
         __o = MyEnum.MySecondValue;

#line default
#line hidden
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
#line 9 "EnumTagHelpers.cshtml"
__InputTagHelper.Value = Microsoft.AspNet.Razor.Test.Generator.MyEnum.MyValue;

#line default
#line hidden
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
#line 10 "EnumTagHelpers.cshtml"
__InputTagHelper.Value = Microsoft.AspNet.Razor.Test.Generator.MyEnum.MySecondValue;

#line default
#line hidden
#line 10 "EnumTagHelpers.cshtml"
         __CatchAllTagHelper.CatchAll = Microsoft.AspNet.Razor.Test.Generator.MyEnum.MyValue;

#line default
#line hidden
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
#line 11 "EnumTagHelpers.cshtml"
__InputTagHelper.Value = enumValue;

#line default
#line hidden
#line 11 "EnumTagHelpers.cshtml"
      __CatchAllTagHelper.CatchAll = enumValue;

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
