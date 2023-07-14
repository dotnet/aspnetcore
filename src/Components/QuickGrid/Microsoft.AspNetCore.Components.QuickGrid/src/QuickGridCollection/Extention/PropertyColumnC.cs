// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;

// should be moved to another extension project, if the developer wants to use DataAnnotation they should download the extension
// doit être déplace verre un autre Project d'extension, si le développeur veux utilise DataAnnotation il devrai téléchargé extension
public partial class PropertyColumnC<TGridItem, TProp>
{
    /// <summary>
    /// Partial method to get the column title from data annotations.
    /// If the extension is installed, this method uses the <see cref="DisplayNameAttribute"/> and <see cref="DisplayAttribute"/> attributes to set the column title <see cref="ColumnCBase{TGridItem}.Title"/>.
    /// </summary>
    partial void GetTitleFromDataAnnotations(MemberExpression memberExpression)
    {
        var memberInfo = memberExpression.Member;
        var displayName = memberInfo.GetCustomAttribute(typeof(DisplayNameAttribute)) as DisplayNameAttribute;
        var display = memberInfo.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute;
        Title = displayName?.DisplayName ?? display?.Name ?? memberInfo.Name ?? "";
    }

    /// <summary>
    /// Partial method to get the format to use for displaying the property from data annotations.
    /// If the extension is installed, this method uses the <see cref="DisplayFormatAttribute"/> attribute to set the column format <see cref="PropertyColumnC{TGridItem, TProp}.DisplayFormat"/>.
    /// </summary>
    partial void GetDisplayFormatFromDataAnnotations(MemberExpression memberExpression)
    {
        var memberInfo = memberExpression.Member;
        var displayFormat = memberInfo.GetCustomAttribute(typeof(DisplayFormatAttribute)) as DisplayFormatAttribute;
        DisplayFormat = displayFormat?.DataFormatString;
    }
}

