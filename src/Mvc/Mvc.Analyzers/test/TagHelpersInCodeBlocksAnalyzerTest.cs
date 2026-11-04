// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

public class TagHelpersInCodeBlocksAnalyzerTest
{
    private readonly DiagnosticDescriptor DiagnosticDescriptor = DiagnosticDescriptors.MVC1006_FunctionsContainingTagHelpersMustBeAsyncAndReturnTask;

    private static readonly DiagnosticResult CS4033Result = new("CS4033", DiagnosticSeverity.Error);
    private static readonly DiagnosticResult CS4034Result = new("CS4034", DiagnosticSeverity.Error);

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfTagHelpersInActions()
    {
        var source = @"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfTagHelpersInActions : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(""\r\n"");
            WriteLiteral(""\r\n"");

    Action sometMethod = {|#0:() =>
    {

            WriteLiteral(""        "");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""d946c1cd0bbc2f1bde41a0f3eb6596ec7dca5c342925"", async() => {
                WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

                WriteLiteral(""</p>\r\n        "");
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
            {|#1:await __tagHelperRunner.RunAsync(__tagHelperExecutionContext)|};
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                {|#2:await __tagHelperExecutionContext.SetOutputContentAsync()|};
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral(""\r\n"");
    }|};

    sometMethod();

        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0)
            .WithArguments("lambda expression", "System.Func<System.Threading.Tasks.Task>");

        return VerifyAnalyzerAsync(source, diagnosticResult, CS4034Result.WithLocation(1), CS4034Result.WithLocation(2));
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfTagHelpersInNonAsyncFunc()
    {
        var source = @"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfTagHelpersInNonAsyncFunc : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(""\r\n"");
            WriteLiteral(""\r\n"");

    Func<Task> sometMethod = {|#0:() =>
    {

            WriteLiteral(""        "");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""452e9bab960ded6307b3903ebf73278bb1b2bec32959"", async() => {
                WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

                WriteLiteral(""</p>\r\n        "");
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
            {|#1:await __tagHelperRunner.RunAsync(__tagHelperExecutionContext)|};
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                {|#2:await __tagHelperExecutionContext.SetOutputContentAsync()|};
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral(""\r\n"");
            return Task.CompletedTask;
    }|};

    sometMethod();

        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0);

        return VerifyAnalyzerAsync(source, diagnosticResult, CS4034Result.WithLocation(1), CS4034Result.WithLocation(2));
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfTagHelpersInVoidClassMethods()
    {
        var source = @"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@""SHA1"", @""186d75f4b59675515143af9ff43784abaabd24a9"", @""/DiagnosticsAreReturned_ForUseOfTagHelpersInVoidClassMethods.cshtml"")]
    public class DiagnosticsAreReturned_ForUseOfTagHelpersInVoidClassMethods : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(""\r\n"");

    SometMethod();

            WriteLiteral(""\r\n"");
        }
        #pragma warning restore 1998

    {|#0:void SometMethod()
    {

        WriteLiteral(""        "");
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""186d75f4b59675515143af9ff43784abaabd24a93243"", async() => {
            WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

            WriteLiteral(""</p>\r\n        "");
        }
        );
        __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
        __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
        BeginWriteTagHelperAttribute();
        __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
        __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
        {|#1:await __tagHelperRunner.RunAsync(__tagHelperExecutionContext)|};
        if (!__tagHelperExecutionContext.Output.IsContentModified)
        {
            {|#2:await __tagHelperExecutionContext.SetOutputContentAsync()|};
        }
        Write(__tagHelperExecutionContext.Output);
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
        WriteLiteral(""\r\n"");
    }|}

        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0);

        return VerifyAnalyzerAsync(source, diagnosticResult, CS4033Result.WithLocation(1), CS4033Result.WithLocation(2));
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfTagHelpersInVoidDelegates()
    {
        var source = @"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfTagHelpersInVoidDelegates : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(""\r\n"");
            WriteLiteral(""\r\n"");

    TestDelegate sometMethod = {|#0:delegate ()
    {

            WriteLiteral(""        "");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""c63f59d4d7a4db461bc9f052a7833885a8d13dcf2972"", async() => {
                WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

                WriteLiteral(""</p>\r\n        "");
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
            {|#1:await __tagHelperRunner.RunAsync(__tagHelperExecutionContext)|};
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                {|#2:await __tagHelperExecutionContext.SetOutputContentAsync()|};
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral(""\r\n"");
    }|};

    sometMethod();

            WriteLiteral(""\r\n"");
        }
        #pragma warning restore 1998

    delegate void TestDelegate();

        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0);

        return VerifyAnalyzerAsync(source, diagnosticResult, CS4034Result.WithLocation(1), CS4034Result.WithLocation(2));
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfTagHelpersInVoidLocalFunctions()
    {
        var source = @"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfTagHelpersInVoidLocalFunctions : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(""\r\n"");

    {|#0:void SometMethod()
    {

            WriteLiteral(""        "");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""5a4c42fadc056422c6be6ab14a6bd877e2fa382d2948"", async() => {
                WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

                WriteLiteral(""</p>\r\n        "");
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
            {|#1:await __tagHelperRunner.RunAsync(__tagHelperExecutionContext)|};
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                {|#2:await __tagHelperExecutionContext.SetOutputContentAsync()|};
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral(""\r\n"");
    }|}

    SometMethod();

        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0);

        return VerifyAnalyzerAsync(source, diagnosticResult, CS4033Result.WithLocation(1), CS4033Result.WithLocation(2));
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncClassMethods()
    {
        var source = @"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncClassMethods_ : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(""\r\n"");

    await SometMethod();

            WriteLiteral(""\r\n"");
        }
        #pragma warning restore 1998

    async Task SometMethod()
    {

        WriteLiteral(""        "");
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""8a14df240fb31e930618408064cdb1e343ff38f43283"", async() => {
            WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

            WriteLiteral(""</p>\r\n        "");
        }
        );
        __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
        __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
        BeginWriteTagHelperAttribute();
        __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
        __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
        await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
        if (!__tagHelperExecutionContext.Output.IsContentModified)
        {
            await __tagHelperExecutionContext.SetOutputContentAsync();
        }
        Write(__tagHelperExecutionContext.Output);
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
        WriteLiteral(""\r\n"");
    }

        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";

        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncDelegates()
    {
        var source = @"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncDelegates_ : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(""\r\n"");
            WriteLiteral(""\r\n"");

    TestDelegate sometMethod = async delegate ()
    {

            WriteLiteral(""        "");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""028e6be2d2e1a766b4ea9e3949a9e9797bb524083002"", async() => {
                WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

                WriteLiteral(""</p>\r\n        "");
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral(""\r\n"");
    };

    await sometMethod();

            WriteLiteral(""\r\n"");
        }
        #pragma warning restore 1998

    delegate Task TestDelegate();

        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncLocalFunctions()
    {
        var source = @"
#pragma checksum ""C:\Users\nimullen\Documents\Projects\AnalyzerTest\NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncLocalFunctions..cshtml"" ""{ff1816ec-aa5e-4d10-87f7-6f4963833460}"" ""d5d5a28106f24c1bb31f07635157a3503dfdcb2d""
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncLocalFunctions_), @""mvc.1.0.view"", @""/NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncLocalFunctions..cshtml"")]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@""SHA1"", @""d5d5a28106f24c1bb31f07635157a3503dfdcb2d"", @""/NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncLocalFunctions..cshtml"")]
    public class NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncLocalFunctions_ : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(""\r\n"");

    async Task SometMethod()
    {

            WriteLiteral(""        "");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""d5d5a28106f24c1bb31f07635157a3503dfdcb2d2978"", async() => {
                WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

                WriteLiteral(""</p>\r\n        "");
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral(""\r\n"");
    }

    await SometMethod();

        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task SingleDiagnosticIsReturned_ForMultipleTagHelpersInVoidMethod()
    {
        var source = @"
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class SingleDiagnosticIsReturned_ForMultipleTagHelpersInVoidMethod : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(""\r\n"");

    SometMethod();

            WriteLiteral(""\r\n"");
        }
        #pragma warning restore 1998

    {|#0:void SometMethod()
    {

        WriteLiteral(""        "");
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""a49ad3383d0802d134e8f9ec401bad4c2f47d14d3250"", async() => {
            WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

            WriteLiteral(""</p>\r\n        "");
        }
        );
        __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
        __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
        BeginWriteTagHelperAttribute();
        __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
        __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
        {|#1:await __tagHelperRunner.RunAsync(__tagHelperExecutionContext)|};
        if (!__tagHelperExecutionContext.Output.IsContentModified)
        {
            {|#2:await __tagHelperExecutionContext.SetOutputContentAsync()|};
        }
        Write(__tagHelperExecutionContext.Output);
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
        WriteLiteral(""\r\n"");
        WriteLiteral(""        "");
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""a49ad3383d0802d134e8f9ec401bad4c2f47d14d4821"", async() => {
            WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

            WriteLiteral(""</p>\r\n        "");
        }
        );
        __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
        __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
        BeginWriteTagHelperAttribute();
        __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
        __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
        {|#3:await __tagHelperRunner.RunAsync(__tagHelperExecutionContext)|};
        if (!__tagHelperExecutionContext.Output.IsContentModified)
        {
            {|#4:await __tagHelperExecutionContext.SetOutputContentAsync()|};
        }
        Write(__tagHelperExecutionContext.Output);
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
        WriteLiteral(""\r\n"");
        WriteLiteral(""        "");
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin(""cache"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, ""a49ad3383d0802d134e8f9ec401bad4c2f47d14d6392"", async() => {
            WriteLiteral(""\r\n            <p>The current time is "");
                              Write(DateTime.Now);

            WriteLiteral(""</p>\r\n        "");
        }
        );
        __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
        __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
        BeginWriteTagHelperAttribute();
        __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
        __tagHelperExecutionContext.AddHtmlAttribute(""asp-vary-by-user"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
        {|#5:await __tagHelperRunner.RunAsync(__tagHelperExecutionContext)|};
        if (!__tagHelperExecutionContext.Output.IsContentModified)
        {
            {|#6:await __tagHelperExecutionContext.SetOutputContentAsync()|};
        }
        Write(__tagHelperExecutionContext.Output);
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
        WriteLiteral(""\r\n"");
    }|}

        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0);

        return VerifyAnalyzerAsync(source,
            diagnosticResult,
            CS4033Result.WithLocation(1),
            CS4033Result.WithLocation(2),
            CS4033Result.WithLocation(3),
            CS4033Result.WithLocation(4),
            CS4033Result.WithLocation(5),
            CS4033Result.WithLocation(6));
    }

    private static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new TagHelpersInCodeBlocksCSharpAnalyzerTest(TestReferences.MetadataReferences)
        {
            TestCode = source,
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    private sealed class TagHelpersInCodeBlocksCSharpAnalyzerTest : CSharpAnalyzerTest<AttributesShouldNotBeAppliedToPageModelAnalyzer, DefaultVerifier>
    {
        public TagHelpersInCodeBlocksCSharpAnalyzerTest(ImmutableArray<MetadataReference> metadataReferences)
        {
            TestState.AdditionalReferences.AddRange(metadataReferences);
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => new[] { new TagHelpersInCodeBlocksAnalyzer() };
    }
}
