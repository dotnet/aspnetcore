// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// ICommonModel interface.
    /// </summary>
    public interface ICommonModel : IPropertyModel
    {
        /// <summary>
        /// The attributes.
        /// </summary>
        IReadOnlyList<object> Attributes { get; }

        /// <summary>
        /// The MemberInfo.
        /// </summary>
        MemberInfo MemberInfo { get; }

        /// <summary>
        /// The name.
        /// </summary>
        string Name { get; }
    }
}
