// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns.MenuOptions;

/// <summary>
/// Enumeration of filter options for string type fields.
/// </summary>
internal enum StringFilterOptions
{
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la chaîne contient une sous-chaîne spécifiée.
    /// </summary>
    Contains,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la chaîne commence par une sous-chaîne spécifiée.
    /// </summary>
    StartsWith,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la chaîne se termine par une sous-chaîne spécifiée.
    /// </summary>
    EndsWith,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la chaîne est égale à une chaîne spécifiée.
    /// </summary>
    Equal,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la chaîne n'est pas égale à une chaîne spécifiée.
    /// </summary>
    NotEqual
}

/// <summary>
/// Enumeration of filter options for data type fields.
/// </summary>
internal enum DataFilterOptions
{
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la donnée n'est pas égale à une donnée spécifiée.
    /// </summary>
    NotEqual,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la donnée est supérieure à une donnée spécifiée.
    /// </summary>
    GreaterThan,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la donnée est supérieure ou égale à une donnée spécifiée.
    /// </summary>
    GreaterThanOrEqual,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la donnée est inférieure à une donnée spécifiée.
    /// </summary>
    LessThan,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la donnée est inférieure ou égale à une donnée spécifiée.
    /// </summary>
    LessThanOrEqual,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si la donnée est égale à une donnée spécifiée.
    /// </summary>
    Equal
}

/// <summary>
/// Enumeration of filter options for enumeration type fields.
/// </summary>
internal enum EnumFilterOptions
{
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si l'énumération n'est pas égale à une énumération spécifiée.
    /// </summary>
    NotEqual,
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si l'énumération est égale à une énumération spécifiée.
    /// </summary>
    Equal
}

/// <summary>
/// Enumeration of filter options for boolean type fields.
/// </summary>
internal enum BoolFilterOptions
{
    /// <summary xml:lang="fr">
    /// Option de filtre pour vérifier si le booléen est égal à un booléen spécifié.
    /// </summary>
    Equal
}
