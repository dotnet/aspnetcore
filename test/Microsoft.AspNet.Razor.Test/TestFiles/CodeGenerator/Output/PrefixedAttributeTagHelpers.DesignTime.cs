namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class PrefixedAttributeTagHelpers
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
        private global::TestNamespace.InputTagHelper1 __TestNamespace_InputTagHelper1 = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        #line hidden
        public PrefixedAttributeTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
#line 3 "PrefixedAttributeTagHelpers.cshtml"
  
    var literate = "or illiterate";
    var intDictionary = new Dictionary<string, int>
    {
        { "three", 3 },
    };
    var stringDictionary = new SortedDictionary<string, string>
    {
        { "name", "value" },
    };

#line default
#line hidden

            __TestNamespace_InputTagHelper1 = CreateTagHelper<global::TestNamespace.InputTagHelper1>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 16 "PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty = intDictionary;

#line default
#line hidden
            __TestNamespace_InputTagHelper2.IntDictionaryProperty = __TestNamespace_InputTagHelper1.IntDictionaryProperty;
#line 16 "PrefixedAttributeTagHelpers.cshtml"
                  __TestNamespace_InputTagHelper1.StringDictionaryProperty = stringDictionary;

#line default
#line hidden
            __TestNamespace_InputTagHelper2.StringDictionaryProperty = __TestNamespace_InputTagHelper1.StringDictionaryProperty;
            __TestNamespace_InputTagHelper1 = CreateTagHelper<global::TestNamespace.InputTagHelper1>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 17 "PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty = intDictionary;

#line default
#line hidden
            __TestNamespace_InputTagHelper2.IntDictionaryProperty = __TestNamespace_InputTagHelper1.IntDictionaryProperty;
#line 17 "PrefixedAttributeTagHelpers.cshtml"
           __TestNamespace_InputTagHelper1.IntDictionaryProperty["garlic"] = 37;

#line default
#line hidden
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["garlic"] = __TestNamespace_InputTagHelper1.IntDictionaryProperty["garlic"];
#line 17 "PrefixedAttributeTagHelpers.cshtml"
                                                       __TestNamespace_InputTagHelper1.IntProperty = 42;

#line default
#line hidden
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["grabber"] = __TestNamespace_InputTagHelper1.IntProperty;
            __TestNamespace_InputTagHelper1 = CreateTagHelper<global::TestNamespace.InputTagHelper1>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 19 "PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntProperty = 42;

#line default
#line hidden
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["grabber"] = __TestNamespace_InputTagHelper1.IntProperty;
#line 19 "PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty["salt"] = 37;

#line default
#line hidden
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["salt"] = __TestNamespace_InputTagHelper1.IntDictionaryProperty["salt"];
#line 19 "PrefixedAttributeTagHelpers.cshtml"
         __TestNamespace_InputTagHelper1.IntDictionaryProperty["pepper"] = 98;

#line default
#line hidden
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["pepper"] = __TestNamespace_InputTagHelper1.IntDictionaryProperty["pepper"];
            __TestNamespace_InputTagHelper1.StringProperty = "string";
            __TestNamespace_InputTagHelper2.StringDictionaryProperty["grabber"] = __TestNamespace_InputTagHelper1.StringProperty;
            __TestNamespace_InputTagHelper1.StringDictionaryProperty["paprika"] = "another string";
            __TestNamespace_InputTagHelper2.StringDictionaryProperty["paprika"] = __TestNamespace_InputTagHelper1.StringDictionaryProperty["paprika"];
#line 21 "PrefixedAttributeTagHelpers.cshtml"
                                    __o = literate;

#line default
#line hidden
            __TestNamespace_InputTagHelper1.StringDictionaryProperty["cumin"] = string.Empty;
            __TestNamespace_InputTagHelper2.StringDictionaryProperty["cumin"] = __TestNamespace_InputTagHelper1.StringDictionaryProperty["cumin"];
            __TestNamespace_InputTagHelper1 = CreateTagHelper<global::TestNamespace.InputTagHelper1>();
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
#line 22 "PrefixedAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper1.IntDictionaryProperty["value"] = 37;

#line default
#line hidden
            __TestNamespace_InputTagHelper2.IntDictionaryProperty["value"] = __TestNamespace_InputTagHelper1.IntDictionaryProperty["value"];
            __TestNamespace_InputTagHelper1.StringDictionaryProperty["thyme"] = "string";
            __TestNamespace_InputTagHelper2.StringDictionaryProperty["thyme"] = __TestNamespace_InputTagHelper1.StringDictionaryProperty["thyme"];
        }
        #pragma warning restore 1998
    }
}
