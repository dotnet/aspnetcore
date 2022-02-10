// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to indicate that a something is considered personal data.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PersonalDataAttribute : Attribute
{ }
