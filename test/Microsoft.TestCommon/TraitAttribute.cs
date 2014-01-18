// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.TestCommon
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class TraitAttribute : Xunit.TraitAttribute
    {
        public TraitAttribute(string name, string value)
            : base(name, value)
        {
        }
    }
}
