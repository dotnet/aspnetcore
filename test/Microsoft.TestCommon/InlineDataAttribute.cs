// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
