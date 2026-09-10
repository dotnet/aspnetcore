// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components.HotReload;

namespace Microsoft.AspNetCore.Components.Forms;

internal static class ExpressionMemberAccessor
{
    private static readonly ConcurrentDictionary<Expression, MemberInfo> _memberInfoCache = new();
    private static readonly ConcurrentDictionary<(MemberInfo Member, string CultureName), string> _displayNameCache = new();

    static ExpressionMemberAccessor()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearCache;
        }
    }

    private static MemberInfo GetMemberInfo<TValue>(Expression<Func<TValue>> accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        return _memberInfoCache.GetOrAdd(accessor, static expr =>
        {
            var lambdaExpression = (LambdaExpression)expr;
            var member = GetMemberInfo(lambdaExpression.Body, out var accessorBody);
            if (member is null)
            {
                throw new ArgumentException(
                    $"The provided expression contains a {accessorBody.GetType().Name} which is not supported. " +
                    $"Only simple member accessors (fields, properties) of an object are supported.");
            }

            return member;
        });
    }

    private static MemberInfo? GetMemberInfo(Expression accessorBody, out Expression normalizedAccessorBody)
    {
        normalizedAccessorBody = accessorBody;

        if (normalizedAccessorBody is UnaryExpression
            {
                NodeType: ExpressionType.Convert,
                Type: var type
            } unaryExpression &&
            type == typeof(object))
        {
            normalizedAccessorBody = unaryExpression.Operand;
        }

        return normalizedAccessorBody is MemberExpression memberExpression
            ? memberExpression.Member
            : null;
    }

    public static string GetDisplayName(MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);

        return _displayNameCache.GetOrAdd((member, CultureInfo.CurrentUICulture.Name), static key =>
        {
            var displayAttribute = key.Member.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute is not null)
            {
                var name = displayAttribute.GetName();
                if (name is not null)
                {
                    return name;
                }
            }

            var displayNameAttribute = key.Member.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttribute?.DisplayName is not null)
            {
                return displayNameAttribute.DisplayName;
            }

            return key.Member.Name;
        });
    }

    public static string GetDisplayName<TValue>(Expression<Func<TValue>> accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        var member = GetMemberInfo(accessor);
        return GetDisplayName(member);
    }

    public static bool TryGetDisplayName<TValue>(
        Expression<Func<TValue>> accessor,
        [NotNullWhen(true)] out string? displayName)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        var member = GetMemberInfo(accessor.Body, out _);
        if (member is null)
        {
            displayName = null;
            return false;
        }

        displayName = GetDisplayName(member);
        return true;
    }

    private static void ClearCache()
    {
        _memberInfoCache.Clear();
        _displayNameCache.Clear();
    }
}
