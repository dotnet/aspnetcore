// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorCodeGenerationOptions
    {
        public static RazorCodeGenerationOptions CreateDefault()
        {
            return new DefaultRazorCodeGenerationOptions(
                indentWithTabs: false,
                indentSize: 4,
                designTime: false,
                suppressChecksum: false,
                rootNamespace: null,
                suppressMetadataAttributes: false,
                suppressPrimaryMethodBody: false,
                suppressNullabilityEnforcement: false,
                omitMinimizedComponentAttributeValues: false);
        }

        public static RazorCodeGenerationOptions CreateDesignTimeDefault()
        {
            return new DefaultRazorCodeGenerationOptions(
                indentWithTabs: false,
                indentSize: 4,
                designTime: true,
                rootNamespace: null,
                suppressChecksum: false,
                suppressMetadataAttributes: true,
                suppressPrimaryMethodBody: false,
                suppressNullabilityEnforcement: false,
                omitMinimizedComponentAttributeValues: false);
        }

        public static RazorCodeGenerationOptions Create(Action<RazorCodeGenerationOptionsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new DefaultRazorCodeGenerationOptionsBuilder(designTime: false);
            configure(builder);
            var options = builder.Build();

            return options;
        }

        public static RazorCodeGenerationOptions CreateDesignTime(Action<RazorCodeGenerationOptionsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new DefaultRazorCodeGenerationOptionsBuilder(designTime: true)
            {
                SuppressMetadataAttributes = true,
            };

            configure(builder);
            var options = builder.Build();

            return options;
        }

        public abstract bool DesignTime { get; }

        public abstract bool IndentWithTabs { get; }

        public abstract int IndentSize { get; }

        /// <summary>
        /// Gets the root namespace for the generated code.
        /// </summary>
        public virtual string RootNamespace { get; }

        /// <summary>
        /// Gets a value that indicates whether to suppress the default <c>#pragma checksum</c> directive in the 
        /// generated C# code. If <c>false</c> the checkum directive will be included, otherwise it will not be
        /// generated. Defaults to <c>false</c>, meaning that the checksum will be included.
        /// </summary>
        /// <remarks>
        /// The <c>#pragma checksum</c> is required to enable debugging and should only be supressed for testing
        /// purposes.
        /// </remarks>
        public abstract bool SuppressChecksum { get; }

        /// <summary>
        /// Gets a value that indicates whether to suppress the default metadata attributes in the generated 
        /// C# code. If <c>false</c> the default attributes will be included, otherwise they will not be generated.
        /// Defaults to <c>false</c> at run time, meaning that the attributes will be included. Defaults to
        /// <c>true</c> at design time, meaning that the attributes will not be included.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <c>Microsoft.AspNetCore.Razor.Runtime</c> package includes a default set of attributes intended
        /// for runtimes to discover metadata about the compiled code.
        /// </para>
        /// <para>
        /// The default metadata attributes should be suppressed if code generation targets a runtime without
        /// a reference to <c>Microsoft.AspNetCore.Razor.Runtime</c>, or for testing purposes.
        /// </para>
        /// </remarks>
        public virtual bool SuppressMetadataAttributes { get; protected set; }

        /// <summary>
        /// Gets or sets a value that determines if an empty body is generated for the primary method.
        /// </summary>
        public virtual bool SuppressPrimaryMethodBody { get; protected set; }

        /// <summary>
        /// Gets a value that determines if nullability type enforcement should be suppressed for user code.
        /// </summary>
        public virtual bool SuppressNullabilityEnforcement { get; }

        /// <summary>
        /// Gets a value that determines if the components code writer may omit values for minimized attributes.
        /// </summary>
        public virtual bool OmitMinimizedComponentAttributeValues { get; }

        /// <summary>
        /// Gets or sets a value that determines if special ifdefs are added to the generated code to support
        /// features such as Edit & Continue in the IDE.
        /// </summary>
        internal bool GenerateDesignerIfDefs { get; set; }
    }
}
