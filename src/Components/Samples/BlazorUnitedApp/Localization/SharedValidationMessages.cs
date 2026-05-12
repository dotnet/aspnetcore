// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorUnitedApp.Localization;

/// <summary>
/// Marker type identifying the shared resource source for all validation messages
/// in this sample. Resource files are looked up by computing
/// <c>{RootNamespace}.{ResourcesPath}.{TypeFullNameWithoutRootNamespace}.{culture}.resx</c>:
///   Resources/Localization.SharedValidationMessages.resx (en, fallback)
///   Resources/Localization.SharedValidationMessages.fr.resx
///   Resources/Localization.SharedValidationMessages.de.resx
///
/// Used by AddValidationLocalization&lt;SharedValidationMessages&gt;() to resolve every
/// validation lookup against this single resource scope.
/// </summary>
public sealed class SharedValidationMessages
{
}
