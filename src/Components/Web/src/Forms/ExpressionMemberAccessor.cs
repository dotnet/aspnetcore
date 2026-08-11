// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
            var accessorBody = lambdaExpression.Body;

            if (accessorBody is UnaryExpression unaryExpression
                && unaryExpression.NodeType == ExpressionType.Convert
                && unaryExpression.Type == typeof(object))
            {
                accessorBody = unaryExpression.Operand;
            }

            if (accessorBody is not MemberExpression memberExpression)
            {
                throw new ArgumentException(
                    $"The provided expression contains a {accessorBody.GetType().Name} which is not supported. " +
                    $"Only simple member accessors (fields, properties) of an object are supported.");
            }

            return memberExpression.Member;
        });
    }

    public static string GetDisplayName(MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);

        // The cache key includes the current UI culture name because DisplayAttribute.GetName()
        // resolves resource based names against it. Keying on the member alone would freeze
        // the first resolved culture for the lifetime of the process, which is shared by every
        // user when components render on the server. The name is used rather than the CultureInfo
        // so that the cache does not retain culture objects.
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

    private static void ClearCache()
    {
        _memberInfoCache.Clear();
        _displayNameCache.Clear();
    }
}
