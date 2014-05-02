// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

namespace Microsoft.AspNet.Razor.Generator
{
    public enum ExpressionRenderingMode
    {
        /// <summary>
        /// Indicates that expressions should be written to the output stream
        /// </summary>
        /// <example>
        /// If @foo is rendered with WriteToOutput, the code generator would output the following code:
        /// 
        /// Write(foo);
        /// </example>
        WriteToOutput,

        /// <summary>
        /// Indicates that expressions should simply be placed as-is in the code, and the context in which
        /// the code exists will be used to render it
        /// </summary>
        /// <example>
        /// If @foo is rendered with InjectCode, the code generator would output the following code:
        /// 
        /// foo
        /// </example>
        InjectCode
    }
}
