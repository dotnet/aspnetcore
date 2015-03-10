// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Generator
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
            CreateTagHelperMethodName = "CreateTagHelper";
            RunnerRunAsyncMethodName = "RunAsync";
            ScopeManagerBeginMethodName = "Begin";
            ScopeManagerEndMethodName = "End";
            ExecutionContextAddMethodName = "Add";
            ExecutionContextAddTagHelperAttributeMethodName = "AddTagHelperAttribute";
            ExecutionContextAddHtmlAttributeMethodName = "AddHtmlAttribute";
            ExecutionContextOutputPropertyName = "Output";
            StartTagHelperWritingScopeMethodName = "StartTagHelperWritingScope";
            EndTagHelperWritingScopeMethodName = "EndTagHelperWritingScope";
            RunnerTypeName = "TagHelperRunner";
            ScopeManagerTypeName = "TagHelperScopeManager";
            ExecutionContextTypeName = "TagHelperExecutionContext";
            TagHelperContentTypeName = "TagHelperContent";
            WriteTagHelperAsyncMethodName = "WriteTagHelperAsync";
            WriteTagHelperToAsyncMethodName = "WriteTagHelperToAsync";
        }

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
    }
}