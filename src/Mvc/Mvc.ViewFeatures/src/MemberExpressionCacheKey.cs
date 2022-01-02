// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal readonly struct MemberExpressionCacheKey
{
    public MemberExpressionCacheKey(Type modelType, MemberExpression memberExpression)
    {
        ModelType = modelType;
        MemberExpression = memberExpression;
        Members = null;
    }

    public MemberExpressionCacheKey(Type modelType, MemberInfo[] members)
    {
        ModelType = modelType;
        Members = members;
        MemberExpression = null;
    }

    // We want to avoid caching a MemberExpression since it has references to other instances in the expression tree.
    // We instead store it as a series of MemberInfo items that comprise of the MemberExpression going from right-most
    // expression to left.
    public MemberExpressionCacheKey MakeCacheable()
    {
        var members = new List<MemberInfo>();
        foreach (var member in this)
        {
            members.Add(member);
        }

        return new MemberExpressionCacheKey(ModelType, members.ToArray());
    }

    public MemberExpression MemberExpression { get; }

    public Type ModelType { get; }

    public MemberInfo[] Members { get; }

    public Enumerator GetEnumerator() => new Enumerator(this);

    public struct Enumerator
    {
        private readonly MemberInfo[] _members;
        private int _index;
        private MemberExpression _memberExpression;

        public Enumerator(in MemberExpressionCacheKey key)
        {
            Current = null;
            _members = key.Members;
            _memberExpression = key.MemberExpression;
            _index = -1;
        }

        public MemberInfo Current { get; private set; }

        public bool MoveNext()
        {
            if (_members != null)
            {
                _index++;
                if (_index >= _members.Length)
                {
                    return false;
                }

                Current = _members[_index];
                return true;
            }

            if (_memberExpression == null)
            {
                return false;
            }

            Current = _memberExpression.Member;
            _memberExpression = _memberExpression.Expression as MemberExpression;
            return true;
        }
    }
}
