// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure
{
    public class DefaultTempDataSerializerTest : TempDataSerializerTestBase
    {
        protected override TempDataSerializer GetTempDataSerializer() => new DefaultTempDataSerializer();
    }
}
