// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

#if !COMPONENTS
/// <summary>
/// A marker interface for types that are associated with route parameters.
/// </summary>
public interface IParameterPolicy
#else
internal interface IParameterPolicy
#endif
{
}
