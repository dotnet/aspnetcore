// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    internal class InternalType
    {
    }

    public class PublicType
    {
        private class NestedPrivateType
        {
        }
    }

    public class ContainerType
    {
        public class NestedType
        {

        }
    }

    public class GenericType<TVal>
    {
    }
}
