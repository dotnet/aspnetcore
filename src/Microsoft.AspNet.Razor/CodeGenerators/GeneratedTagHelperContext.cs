// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    /// <summary>
    /// Contains necessary information for the tag helper code generation process.
    /// </summary>
    public class GeneratedTagHelperContext
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="GeneratedTagHelperContext"/> with default values.
        /// </summary>
        public GeneratedTagHelperContext()
        {
            BeginAddHtmlAttributeValuesMethodName = "BeginAddHtmlAttributeValues";
            EndAddHtmlAttributeValuesMethodName = "EndAddHtmlAttributeValues";
            AddHtmlAttributeValueMethodName = "AddHtmlAttributeValue";
            CreateTagHelperMethodName = "CreateTagHelper";
            RunnerRunAsyncMethodName = "RunAsync";
            ScopeManagerBeginMethodName = "Begin";
            ScopeManagerEndMethodName = "End";
            ExecutionContextAddMethodName = "Add";
            ExecutionContextAddTagHelperAttributeMethodName = "AddTagHelperAttribute";
            ExecutionContextAddMinimizedHtmlAttributeMethodName = "AddMinimizedHtmlAttribute";
            ExecutionContextAddHtmlAttributeMethodName = "AddHtmlAttribute";
            ExecutionContextOutputPropertyName = "Output";
            FormatInvalidIndexerAssignmentMethodName = "FormatInvalidIndexerAssignment";
            MarkAsHtmlEncodedMethodName = "Html.Raw";
            StartTagHelperWritingScopeMethodName = "StartTagHelperWritingScope";
            EndTagHelperWritingScopeMethodName = "EndTagHelperWritingScope";
            RunnerTypeName = "Microsoft.AspNet.Razor.Runtime.TagHelperRunner";
            ScopeManagerTypeName = "Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager";
            ExecutionContextTypeName = "Microsoft.AspNet.Razor.Runtime.TagHelperExecutionContext";
            TagHelperContentTypeName = "Microsoft.AspNet.Razor.TagHelperContent";
            WriteTagHelperAsyncMethodName = "WriteTagHelperAsync";
            WriteTagHelperToAsyncMethodName = "WriteTagHelperToAsync";
            TagHelperContentGetContentMethodName = "GetContent";
            HtmlEncoderPropertyName = "HtmlEncoder";
        }

        /// <summary>
        /// The name of the method used to begin the addition of unbound, complex tag helper attributes to
        /// TagHelperExecutionContexts.
        /// </summary>
        /// <remarks>
        /// Method signature should be
        /// <code>
        /// public void BeginAddHtmlAttributeValues(
        ///     TagHelperExecutionContext executionContext,
        ///     string attributeName)
        /// </code>
        /// </remarks>
        public string BeginAddHtmlAttributeValuesMethodName { get; set; }

        /// <summary>
        /// Method name used to end addition of unbound, complex tag helper attributes to TagHelperExecutionContexts.
        /// </summary>
        /// <remarks>
        /// Method signature should be
        /// <code>
        /// public void EndAddHtmlAttributeValues(
        ///     TagHelperExecutionContext executionContext)
        /// </code>
        /// </remarks>
        public string EndAddHtmlAttributeValuesMethodName { get; set; }

        /// <summary>
        /// Method name used to add individual components of an unbound, complex tag helper attribute to
        /// TagHelperExecutionContexts.
        /// </summary>
        /// <remarks>
        /// Method signature:
        /// <code>
        /// public void AddHtmlAttributeValues(
        ///     string prefix,
        ///     int prefixOffset,
        ///     string value,
        ///     int valueOffset,
        ///     int valueLength,
        ///     bool isLiteral)
        /// </code>
        /// </remarks>
        public string AddHtmlAttributeValueMethodName { get; set; }

        /// <summary>
        /// The name of the method used to create a tag helper.
        /// </summary>
        public string CreateTagHelperMethodName { get; set; }

        /// <summary>
        /// The name of the <see cref="RunnerTypeName"/> method used to run tag helpers.
        /// </summary>
        public string RunnerRunAsyncMethodName { get; set; }

        /// <summary>
        /// The name of the <see cref="ExecutionContextTypeName"/> method used to start a scope.
        /// </summary>
        public string ScopeManagerBeginMethodName { get; set; }

        /// <summary>
        /// The name of the <see cref="ExecutionContextTypeName"/> method used to end a scope.
        /// </summary>
        public string ScopeManagerEndMethodName { get; set; }

        /// <summary>
        /// The name of the <see cref="ExecutionContextTypeName"/> method used to add tag helper attributes.
        /// </summary>
        public string ExecutionContextAddTagHelperAttributeMethodName { get; set; }

        /// <summary>
        /// The name of the <see cref="ExecutionContextTypeName"/> method used to add minimized HTML attributes.
        /// </summary>
        public string ExecutionContextAddMinimizedHtmlAttributeMethodName { get; set; }

        /// <summary>
        /// The name of the <see cref="ExecutionContextTypeName"/> method used to add HTML attributes.
        /// </summary>
        public string ExecutionContextAddHtmlAttributeMethodName { get; set; }

        /// <summary>
        /// The name of the <see cref="ExecutionContextTypeName"/> method used to add tag helpers.
        /// </summary>
        public string ExecutionContextAddMethodName { get; set; }

        /// <summary>
        /// The property accessor for the tag helper's output.
        /// </summary>
        public string ExecutionContextOutputPropertyName { get; set; }

        /// <summary>
        /// The name of the method used to format an error message about using an indexer when the tag helper property
        /// is <c>null</c>.
        /// </summary>
        /// <remarks>
        /// Method signature should be
        /// <code>
        /// public string FormatInvalidIndexerAssignment(
        ///     string attributeName,       // Name of the HTML attribute associated with the indexer.
        ///     string tagHelperTypeName,   // Full name of the tag helper type.
        ///     string propertyName)        // Dictionary property in the tag helper.
        /// </code>
        /// </remarks>
        public string FormatInvalidIndexerAssignmentMethodName { get; set; }

        /// <summary>
        /// The name of the method used to wrap a <see cref="string"/> value and mark it as HTML-encoded.
        /// </summary>
        /// <remarks>Used together with <see cref="ExecutionContextAddHtmlAttributeMethodName"/>.</remarks>
        public string MarkAsHtmlEncodedMethodName { get; set; }

        /// <summary>
        /// The name of the method used to start a new writing scope.
        /// </summary>
        public string StartTagHelperWritingScopeMethodName { get; set; }

        /// <summary>
        /// The name of the method used to end a writing scope.
        /// </summary>
        public string EndTagHelperWritingScopeMethodName { get; set; }

        /// <summary>
        /// The name of the type used to run tag helpers.
        /// </summary>
        public string RunnerTypeName { get; set; }

        /// <summary>
        /// The name of the type used to create scoped <see cref="ExecutionContextTypeName"/> instances.
        /// </summary>
        public string ScopeManagerTypeName { get; set; }

        /// <summary>
        /// The name of the type describing a specific tag helper scope.
        /// </summary>
        /// <remarks>
        /// Contains information about in-scope tag helpers, HTML attributes, and the tag helpers' output.
        /// </remarks>
        public string ExecutionContextTypeName { get; set; }

        /// <summary>
        /// The name of the type containing tag helper content.
        /// </summary>
        /// <remarks>
        /// Contains the data returned by EndTagHelperWriteScope().
        /// </remarks>
        public string TagHelperContentTypeName { get; set; }

        /// <summary>
        /// The name of the method used to write <see cref="ExecutionContextTypeName"/>.
        /// </summary>
        public string WriteTagHelperAsyncMethodName { get; set; }

        /// <summary>
        /// The name of the method used to write <see cref="ExecutionContextTypeName"/> to a specified
        /// <see cref="System.IO.TextWriter"/>.
        /// </summary>
        public string WriteTagHelperToAsyncMethodName { get; set; }

        /// <summary>
        /// The name of the property containing the <c>IHtmlEncoder</c>.
        /// </summary>
        public string HtmlEncoderPropertyName { get; set; }

        /// <summary>
        /// The name of the method used to convert a <c>TagHelperContent</c> into a <see cref="string"/>.
        /// </summary>
        public string TagHelperContentGetContentMethodName { get; set; }
    }
}