// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.TestCommon
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class InlineDataAttribute : Xunit.Extensions.InlineDataAttribute
    {
        public InlineDataAttribute(params object[] dataValues)
            : base(dataValues)
        {
        }
    }
}
