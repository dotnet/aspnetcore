// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// 
/// </summary>
public interface IComponentSerializationModeHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    public PersistedStateSerializationMode GetComponentSerializationMode(IComponent component);
}
