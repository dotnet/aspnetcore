// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.TestCommon
{
    public class ForceGCAttribute : Xunit.BeforeAfterTestAttribute
    {
        public override void After(MethodInfo methodUnderTest)
        {
            GC.Collect(99);
            GC.Collect(99);
            GC.Collect(99);
        }
    }
}
