// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public interface ICommonModel : IPropertyModel
    {
        IReadOnlyList<object> Attributes { get; }
        MemberInfo MemberInfo { get; }
        string Name { get; }
    }
}
