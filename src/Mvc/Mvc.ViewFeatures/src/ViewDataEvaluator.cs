// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Static class that helps evaluate expressions. This class cannot be inherited.
/// </summary>
public static class ViewDataEvaluator
{
    /// <summary>
    /// Gets <see cref="ViewDataInfo"/> for named <paramref name="expression"/> in given
    /// <paramref name="viewData"/>.
    /// </summary>
    /// <param name="viewData">
    /// The <see cref="ViewDataDictionary"/> that may contain the <paramref name="expression"/> value.
    /// </param>
    /// <param name="expression">Expression name, relative to <c>viewData.Model</c>.</param>
    /// <returns>
    /// <see cref="ViewDataInfo"/> for named <paramref name="expression"/> in given <paramref name="viewData"/>.
    /// </returns>
    public static ViewDataInfo Eval(ViewDataDictionary viewData, string expression)
    {
        ArgumentNullException.ThrowIfNull(viewData);

        // While it is not valid to generate a field for the top-level model itself because the result is an
        // unnamed input element, do not throw here if full name is null or empty. Support is needed for cases
        // such as Html.Label() and Html.Value(), where the user's code is not creating a name attribute. Checks
        // are in place at higher levels for the invalid cases.
        var fullName = viewData.TemplateInfo.GetFullHtmlFieldName(expression);

        // Given an expression "one.two.three.four" we look up the following (pseudo-code):
        //  this["one.two.three.four"]
        //  this["one.two.three"]["four"]
        //  this["one.two"]["three.four]
        //  this["one.two"]["three"]["four"]
        //  this["one"]["two.three.four"]
        //  this["one"]["two.three"]["four"]
        //  this["one"]["two"]["three.four"]
        //  this["one"]["two"]["three"]["four"]

        // Try to find a matching ViewData entry using the full expression name. If that fails, fall back to
        // ViewData.Model using the expression's relative name.
        var result = EvalComplexExpression(viewData, fullName);
        if (result == null)
        {
            if (string.IsNullOrEmpty(expression))
            {
                // Null or empty expression name means current model even if that model is null.
                result = new ViewDataInfo(container: viewData, value: viewData.Model);
            }
            else
            {
                result = EvalComplexExpression(viewData.Model, expression);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets <see cref="ViewDataInfo"/> for named <paramref name="expression"/> in given
    /// <paramref name="indexableObject"/>.
    /// </summary>
    /// <param name="indexableObject">
    /// The <see cref="object"/> that may contain the <paramref name="expression"/> value.
    /// </param>
    /// <param name="expression">Expression name, relative to <paramref name="indexableObject"/>.</param>
    /// <returns>
    /// <see cref="ViewDataInfo"/> for named <paramref name="expression"/> in given
    /// <paramref name="indexableObject"/>.
    /// </returns>
    public static ViewDataInfo Eval(object indexableObject, string expression)
    {
        // Run through many of the same cases as other Eval() overload.
        return EvalComplexExpression(indexableObject, expression);
    }

    private static ViewDataInfo EvalComplexExpression(object indexableObject, string expression)
    {
        if (indexableObject == null)
        {
            return null;
        }

        if (expression == null)
        {
            // In case a Dictionary indexableObject contains a "" entry, don't short-circuit the logic below.
            expression = string.Empty;
        }

        return InnerEvalComplexExpression(indexableObject, expression);
    }

    private static ViewDataInfo InnerEvalComplexExpression(object indexableObject, string expression)
    {
        Debug.Assert(expression != null);
        var leftExpression = expression;
        do
        {
            var targetInfo = GetPropertyValue(indexableObject, leftExpression);
            if (targetInfo != null)
            {
                if (leftExpression.Length == expression.Length)
                {
                    // Nothing remaining in expression after leftExpression.
                    return targetInfo;
                }

                if (targetInfo.Value != null)
                {
                    var rightExpression = expression.Substring(leftExpression.Length + 1);
                    targetInfo = InnerEvalComplexExpression(targetInfo.Value, rightExpression);
                    if (targetInfo != null)
                    {
                        return targetInfo;
                    }
                }
            }

            leftExpression = GetNextShorterExpression(leftExpression);
        }
        while (!string.IsNullOrEmpty(leftExpression));

        return null;
    }

    // Given "one.two.three.four" initially, calls return
    //  "one.two.three"
    //  "one.two"
    //  "one"
    //  ""
    // Recursion of InnerEvalComplexExpression() further sub-divides these cases to cover the full set of
    // combinations shown in Eval(ViewDataDictionary, string) comments.
    private static string GetNextShorterExpression(string expression)
    {
        if (string.IsNullOrEmpty(expression))
        {
            return string.Empty;
        }

        var lastDot = expression.LastIndexOf('.');
        if (lastDot == -1)
        {
            return string.Empty;
        }

        return expression.Substring(startIndex: 0, length: lastDot);
    }

    private static ViewDataInfo GetIndexedPropertyValue(object indexableObject, string key)
    {
        var dict = indexableObject as IDictionary<string, object>;
        object value = null;
        var success = false;

        if (dict != null)
        {
            success = dict.TryGetValue(key, out value);
        }
        else
        {
            // Fall back to TryGetValue() calls for other Dictionary types.
            var tryDelegate = TryGetValueProvider.CreateInstance(indexableObject.GetType());
            if (tryDelegate != null)
            {
                success = tryDelegate(indexableObject, key, out value);
            }
        }

        if (success)
        {
            return new ViewDataInfo(indexableObject, value);
        }

        return null;
    }

    // This method handles one "segment" of a complex property expression
    private static ViewDataInfo GetPropertyValue(object container, string propertyName)
    {
        // First, try to evaluate the property based on its indexer.
        var value = GetIndexedPropertyValue(container, propertyName);
        if (value != null)
        {
            return value;
        }

        // Do not attempt to find a property with an empty name and or of a ViewDataDictionary.
        if (string.IsNullOrEmpty(propertyName) || container is ViewDataDictionary)
        {
            return null;
        }

        // If the indexer didn't return anything useful, try to use PropertyInfo and treat the expression
        // as a property name.
        var propertyInfo = container.GetType().GetRuntimeProperty(propertyName);
        if (propertyInfo == null)
        {
            return null;
        }

        return new ViewDataInfo(container, propertyInfo);
    }
}
