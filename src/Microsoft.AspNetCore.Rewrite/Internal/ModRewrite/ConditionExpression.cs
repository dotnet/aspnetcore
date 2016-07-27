// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite.Operands;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    /// Represents the ConditionPattern for a mod_rewrite rule.
    /// </summary>
    public class ConditionExpression
    {
        public Operand Operand { get; set; }
        public bool Invert { get; set; }
       
        /// <summary>
        /// Checks if a condition matches the context.
        /// </summary>
        /// <param name="context">The UrlRewriteContext.</param>
        /// <param name="previous">The previous condition results (for backreferences).</param>
        /// <param name="testString">The testString created from the <see cref="Pattern"/>.</param>
        /// <returns>If the testString satisfies the condition</returns>
        public bool? CheckConditionExpression(RewriteContext context, Match previous, string testString)
        {
            return Operand.CheckOperation(previous, testString, context.FileProvider) ^ Invert;
        }
    }
}
