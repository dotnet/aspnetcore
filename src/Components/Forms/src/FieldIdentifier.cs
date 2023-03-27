// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Uniquely identifies a single field that can be edited. This may correspond to a property on a
/// model object, or can be any other named value.
/// </summary>
public readonly struct FieldIdentifier : IEquatable<FieldIdentifier>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldIdentifier"/> structure.
    /// </summary>
    /// <param name="accessor">An expression that identifies an object member.</param>
    /// <typeparam name="TField">The field <see cref="Type"/>.</typeparam>
    public static FieldIdentifier Create<TField>(Expression<Func<TField>> accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        ParseAccessor(accessor, out var model, out var fieldName);
        return new FieldIdentifier(model, fieldName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldIdentifier"/> structure.
    /// </summary>
    /// <param name="model">The object that owns the field.</param>
    /// <param name="fieldName">The name of the editable field.</param>
    public FieldIdentifier(object model, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.GetType().IsValueType)
        {
            throw new ArgumentException("The model must be a reference-typed object.", nameof(model));
        }

        Model = model;

        // Note that we do allow an empty string. This is used by some validation systems
        // as a place to store object-level (not per-property) messages.
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
    }

    /// <summary>
    /// Gets the object that owns the editable field.
    /// </summary>
    public object Model { get; }

    /// <summary>
    /// Gets the name of the editable field.
    /// </summary>
    public string FieldName { get; }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // We want to compare Model instances by reference. RuntimeHelpers.GetHashCode returns identical hashes for equal object references (ignoring any `Equals`/`GetHashCode` overrides) which is what we want.
        var modelHash = RuntimeHelpers.GetHashCode(Model);
        var fieldHash = StringComparer.Ordinal.GetHashCode(FieldName);
        return (
            modelHash,
            fieldHash
        )
        .GetHashCode();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is FieldIdentifier otherIdentifier
        && Equals(otherIdentifier);

    /// <inheritdoc />
    public bool Equals(FieldIdentifier otherIdentifier)
    {
        return
            ReferenceEquals(otherIdentifier.Model, Model) &&
            string.Equals(otherIdentifier.FieldName, FieldName, StringComparison.Ordinal);
    }

    private static void ParseAccessor<T>(Expression<Func<T>> accessor, out object model, out string fieldName)
    {
        var accessorBody = accessor.Body;

        // Unwrap casts to object
        if (accessorBody is UnaryExpression unaryExpression
            && unaryExpression.NodeType == ExpressionType.Convert
            && unaryExpression.Type == typeof(object))
        {
            accessorBody = unaryExpression.Operand;
        }

        if (!(accessorBody is MemberExpression memberExpression))
        {
            throw new ArgumentException($"The provided expression contains a {accessorBody.GetType().Name} which is not supported. {nameof(FieldIdentifier)} only supports simple member accessors (fields, properties) of an object.");
        }

        // Identify the field name. We don't mind whether it's a property or field, or even something else.
        fieldName = GetFullName(memberExpression);

        // Get a reference to the model object
        // i.e., given a value like "(something).MemberName", determine the runtime value of "(something)",
        if (memberExpression.Expression is ConstantExpression constantExpression)
        {
            if (constantExpression.Value is null)
            {
                throw new ArgumentException("The provided expression must evaluate to a non-null value.");
            }
            model = constantExpression.Value;
        }
        else if (memberExpression.Expression != null)
        {
            // It would be great to cache this somehow, but it's unclear there's a reasonable way to do
            // so, given that it embeds captured values such as "this". We could consider special-casing
            // for "() => something.Member" and building a cache keyed by "something.GetType()" with values
            // of type Func<object, object> so we can cheaply map from "something" to "something.Member".
            var modelLambda = Expression.Lambda(memberExpression.Expression);
            var modelLambdaCompiled = (Func<object?>)modelLambda.Compile();
            var result = modelLambdaCompiled();
            if (result is null)
            {
                throw new ArgumentException("The provided expression must evaluate to a non-null value.");
            }
            model = result;
        }
        else
        {
            throw new ArgumentException($"The provided expression contains a {accessorBody.GetType().Name} which is not supported. {nameof(FieldIdentifier)} only supports simple member accessors (fields, properties) of an object.");
        }
    }

    private static string GetFullName(MemberExpression root)
    {
        return ExpressionHelper.GetUncachedExpressionText(LambdaExpression.Lambda(root));
        //    Span<char> result = stackalloc char[1024];

        //    var initialExpression = root.Expression as MemberExpression;
        //    initialExpression.Member.Name.CopyTo(result);
        //    var currentSpan = result.Slice(initialExpression.Member.Name.Length);

        //    for (var expression = initialExpression.Expression as MemberExpression;
        //        expression != null && currentSpan.Length > 1;
        //        expression = expression.Expression as MemberExpression)
        //    {
        //        currentSpan[0] = '.';
        //        expression.Member.Name.CopyTo(currentSpan.Slice(1));
        //        currentSpan = currentSpan.Slice(expression.Member.Name.Length + 1);
        //    }

        //    return result.Slice(0, result.Length - currentSpan.Length).ToString();
    }
}

internal static class ExpressionHelper
{
    public static string GetUncachedExpressionText(LambdaExpression expression)
        => GetExpressionText(expression, expressionTextCache: null);

    public static string GetExpressionText(LambdaExpression expression, ConcurrentDictionary<LambdaExpression, string> expressionTextCache)
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (expressionTextCache != null &&
            expressionTextCache.TryGetValue(expression, out var expressionText))
        {
            return expressionText;
        }

        // Determine size of string needed (length) and number of segments it contains (segmentCount). Put another
        // way, segmentCount tracks the number of times the loop below should iterate. This avoids adding ".model"
        // and / or an extra leading "." and then removing them after the loop. Other information collected in this
        // first loop helps with length and segmentCount adjustments. doNotCache is somewhat separate: If
        // true, expression strings are not cached for the expression.
        //
        // After the corrections below the first loop, length is usually exactly the size of the returned string.
        // However when containsIndexers is true, the calculation is approximate because either evaluating indexer
        // expressions multiple times or saving indexer strings can get expensive. Optimizing for the common case
        // of a collection (not a dictionary) with less than 100 elements. If that assumption proves to be
        // incorrect, the StringBuilder will be enlarged but hopefully just once.
        var doNotCache = false;
        var lastIsModel = false;
        var length = 0;
        var segmentCount = 0;
        var trailingMemberExpressions = 0;

        var part = expression.Body;
        while (part != null)
        {
            switch (part.NodeType)
            {
                case ExpressionType.Call:
                    // Will exit loop if at Method().Property or [i,j].Property. In that case (like [i].Property),
                    // don't cache and don't remove ".Model" (if that's .Property).
                    doNotCache = true;
                    lastIsModel = false;

                    var methodExpression = (MethodCallExpression)part;
                    if (IsSingleArgumentIndexer(methodExpression))
                    {
                        length += "[99]".Length;
                        part = methodExpression.Object;
                        segmentCount++;
                        trailingMemberExpressions = 0;
                    }
                    else
                    {
                        // Unsupported.
                        part = null;
                    }
                    break;

                case ExpressionType.ArrayIndex:
                    var binaryExpression = (BinaryExpression)part;

                    doNotCache = true;
                    lastIsModel = false;
                    length += "[99]".Length;
                    part = binaryExpression.Left;
                    segmentCount++;
                    trailingMemberExpressions = 0;
                    break;

                case ExpressionType.MemberAccess:
                    var memberExpressionPart = (MemberExpression)part;
                    var name = memberExpressionPart.Member.Name;

                    // If identifier contains "__", it is "reserved for use by the implementation" and likely
                    // compiler- or Razor-generated e.g. the name of a field in a delegate's generated class.
                    if (name.Contains("__"))
                    {
                        // Exit loop.
                        part = null;
                    }
                    else
                    {
                        lastIsModel = string.Equals("model", name, StringComparison.OrdinalIgnoreCase);
                        length += name.Length + 1;
                        part = memberExpressionPart.Expression;
                        segmentCount++;
                        trailingMemberExpressions++;
                    }
                    break;

                case ExpressionType.Parameter:
                    // Unsupported but indicates previous member access was not the view's Model.
                    lastIsModel = false;
                    part = null;
                    break;

                default:
                    // Unsupported.
                    part = null;
                    break;
            }
        }

        // If name would start with ".model", then strip that part away.
        if (lastIsModel)
        {
            length -= ".model".Length;
            segmentCount--;
            trailingMemberExpressions--;
        }

        // Trim the leading "." if present. The loop below special-cases the last property to avoid this addition.
        if (trailingMemberExpressions > 0)
        {
            length--;
        }

        Debug.Assert(segmentCount >= 0);
        if (segmentCount == 0)
        {
            Debug.Assert(!doNotCache);
            expressionTextCache?.TryAdd(expression, string.Empty);

            return string.Empty;
        }

        var builder = new StringBuilder(length);
        part = expression.Body;
        while (part != null && segmentCount > 0)
        {
            segmentCount--;
            switch (part.NodeType)
            {
                case ExpressionType.Call:
                    Debug.Assert(doNotCache);
                    var methodExpression = (MethodCallExpression)part;

                    InsertIndexerInvocationText(builder, methodExpression.Arguments.Single(), expression);

                    part = methodExpression.Object;
                    break;

                case ExpressionType.ArrayIndex:
                    Debug.Assert(doNotCache);
                    var binaryExpression = (BinaryExpression)part;

                    InsertIndexerInvocationText(builder, binaryExpression.Right, expression);

                    part = binaryExpression.Left;
                    break;

                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)part;
                    var name = memberExpression.Member.Name;
                    Debug.Assert(!name.Contains("__"));

                    builder.Insert(0, name);
                    if (segmentCount > 0)
                    {
                        // One or more parts to the left of this part are coming.
                        builder.Insert(0, '.');
                    }

                    part = memberExpression.Expression;
                    break;

                default:
                    // Should be unreachable due to handling in above loop.
                    Debug.Assert(false);
                    break;
            }
        }

        Debug.Assert(segmentCount == 0);
        expressionText = builder.ToString();
        if (expressionTextCache != null && !doNotCache)
        {
            expressionTextCache.TryAdd(expression, expressionText);
        }

        return expressionText;
    }

    private static void InsertIndexerInvocationText(
        StringBuilder builder,
        Expression indexExpression,
        LambdaExpression parentExpression)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(indexExpression);
        ArgumentNullException.ThrowIfNull(parentExpression);

        if (parentExpression.Parameters == null)
        {
            throw new ArgumentException($"Argument Error {nameof(parentExpression.Parameters)}",
                nameof(parentExpression));
        }

        var converted = Expression.Convert(indexExpression, typeof(object));
        var fakeParameter = Expression.Parameter(typeof(object), null);
        var lambda = Expression.Lambda<Func<object, object>>(converted, fakeParameter);
        Func<object, object> func;

        try
        {
            func = CachedExpressionCompiler.Process(lambda) ?? lambda.Compile();
        }
        catch (InvalidOperationException ex)
        {
            var parameters = parentExpression.Parameters.ToArray();
            throw new InvalidOperationException(
                $"Invalid indexer {indexExpression} at {parameters[0].Name}",
                ex);
        }

        builder.Insert(0, ']');
        builder.Insert(0, Convert.ToString(func(null), CultureInfo.InvariantCulture));
        builder.Insert(0, '[');
    }

    public static bool IsSingleArgumentIndexer(Expression expression)
    {
        if (!(expression is MethodCallExpression methodExpression) || methodExpression.Arguments.Count != 1)
        {
            return false;
        }

        // Check whether GetDefaultMembers() (if present in CoreCLR) would return a member of this type. Compiler
        // names the indexer property, if any, in a generated [DefaultMember] attribute for the containing type.
        var declaringType = methodExpression.Method.DeclaringType;
        var defaultMember = declaringType.GetCustomAttribute<DefaultMemberAttribute>(inherit: true);
        if (defaultMember == null)
        {
            return false;
        }

        // Find default property (the indexer) and confirm its getter is the method in this expression.
        var runtimeProperties = declaringType.GetRuntimeProperties();
        foreach (var property in runtimeProperties)
        {
            if ((string.Equals(defaultMember.MemberName, property.Name, StringComparison.Ordinal) &&
                property.GetMethod == methodExpression.Method))
            {
                return true;
            }
        }

        return false;
    }
}

internal static class CachedExpressionCompiler
{
    private static readonly Expression NullExpression = Expression.Constant(value: null);

    /// <remarks>
    /// This is the entry point to the expression compilation system. The system
    /// a) Will rewrite the expression to avoid null refs when any part of the expression tree is evaluated  to null
    /// b) Attempt to cache the result, or an intermediate part of the result.
    /// If the provided expression is particularly obscure and the system doesn't know how to handle it, it will
    /// return null.
    /// </remarks>
    public static Func<TModel, object> Process<TModel, TResult>(
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return Compiler<TModel, TResult>.Compile(expression);
    }

    private static class Compiler<TModel, TResult>
    {
        private static Func<TModel, object> _identityFunc;

        private static readonly ConcurrentDictionary<MemberInfo, Func<TModel, object>> _simpleMemberAccessCache =
            new ConcurrentDictionary<MemberInfo, Func<TModel, object>>();

        private static readonly ConcurrentDictionary<MemberExpressionCacheKey, Func<TModel, object>> _chainedMemberAccessCache =
            new ConcurrentDictionary<MemberExpressionCacheKey, Func<TModel, object>>(MemberExpressionCacheKeyComparer.Instance);

        private static readonly ConcurrentDictionary<MemberInfo, Func<object, TResult>> _constMemberAccessCache =
            new ConcurrentDictionary<MemberInfo, Func<object, TResult>>();

        public static Func<TModel, object> Compile(Expression<Func<TModel, TResult>> expression)
        {
            Debug.Assert(expression != null);

            var root = expression.Body switch
            {
                UnaryExpression unary when unary.NodeType == ExpressionType.Convert => unary.Operand,
                _ => expression.Body
            };

            switch (root)
            {
                // model => model
                case var body when body == expression.Parameters[0]:
                    return CompileFromIdentityFunc(expression);

                // model => (object){const}
                case ConstantExpression constantExpression:
                    return CompileFromConstLookup(constantExpression);

                // model => CapturedConstant
                case MemberExpression memberExpression when memberExpression.Expression is ConstantExpression constantExpression:
                    return CompileCapturedConstant(memberExpression, constantExpression);

                // model => ModelType.StaticMember
                case MemberExpression memberExpression when memberExpression.Expression == null:
                    return CompileFromStaticMemberAccess(expression, memberExpression);

                // model => model.Member
                case MemberExpression memberExpression when memberExpression.Expression == expression.Parameters[0]:
                    return CompileFromSimpleMemberAccess(expression, memberExpression);

                // model => model.Member1.Member2
                case MemberExpression memberExpression when IsChainedPropertyAccessor(memberExpression):
                    return CompileForChainedMemberAccess(expression, memberExpression);

                default:
                    return null;
            }

            bool IsChainedPropertyAccessor(MemberExpression memberExpression)
            {
                while (memberExpression.Expression != null)
                {
                    if (memberExpression.Expression is MemberExpression leftExpression)
                    {
                        memberExpression = leftExpression;
                        continue;
                    }
                    else if (memberExpression.Expression == expression.Parameters[0])
                    {
                        return true;
                    }

                    break;
                }

                return false;
            }
        }

        private static Func<TModel, object> CompileFromConstLookup(
            ConstantExpression constantExpression)
        {
            // model => {const}
            var constantValue = constantExpression.Value;
            return _ => constantValue;
        }

        private static Func<TModel, object> CompileFromIdentityFunc(
            Expression<Func<TModel, TResult>> expression)
        {
            // model => model
            // Don't need to lock, as all identity funcs are identical.
            if (_identityFunc == null)
            {
                var identityFuncCore = expression.Compile();
                _identityFunc = model => identityFuncCore(model);
            }

            return _identityFunc;
        }

        private static Func<TModel, object> CompileFromStaticMemberAccess(
            Expression<Func<TModel, TResult>> expression,
            MemberExpression memberExpression)
        {
            // model => ModelType.StaticMember
            if (_simpleMemberAccessCache.TryGetValue(memberExpression.Member, out var result))
            {
                return result;
            }

            var func = expression.Compile();
            result = model => func(model);
            result = _simpleMemberAccessCache.GetOrAdd(memberExpression.Member, result);

            return result;
        }

        private static Func<TModel, object> CompileFromSimpleMemberAccess(
            Expression<Func<TModel, TResult>> expression,
            MemberExpression memberExpression)
        {
            // Input: () => m.Member
            // Output: () => (m == null) ? null : m.Member
            if (_simpleMemberAccessCache.TryGetValue(memberExpression.Member, out var result))
            {
                return result;
            }

            result = _simpleMemberAccessCache.GetOrAdd(memberExpression.Member, Rewrite(expression, memberExpression));
            return result;
        }

        private static Func<TModel, object> CompileForChainedMemberAccess(
            Expression<Func<TModel, TResult>> expression,
            MemberExpression memberExpression)
        {
            // Input: () => m.Member1.Member2
            // Output: () => (m == null || m.Member1 == null) ? null : m.Member1.Member2
            var key = new MemberExpressionCacheKey(typeof(TModel), memberExpression);
            if (_chainedMemberAccessCache.TryGetValue(key, out var result))
            {
                return result;
            }

            var cacheableKey = key.MakeCacheable();
            result = _chainedMemberAccessCache.GetOrAdd(cacheableKey, Rewrite(expression, memberExpression));
            return result;
        }

        private static Func<TModel, object> CompileCapturedConstant(MemberExpression memberExpression, ConstantExpression constantExpression)
        {
            // model => {const} (captured local variable)
            if (!_constMemberAccessCache.TryGetValue(memberExpression.Member, out var result))
            {
                // rewrite as capturedLocal => ((TDeclaringType)capturedLocal)
                var parameterExpression = Expression.Parameter(typeof(object), "capturedLocal");
                var castExpression =
                    Expression.Convert(parameterExpression, memberExpression.Member.DeclaringType);
                var replacementMemberExpression = memberExpression.Update(castExpression);
                var replacementExpression = Expression.Lambda<Func<object, TResult>>(
                    UnaryExpression.Convert(replacementMemberExpression,typeof(object)),
                    parameterExpression);

                result = replacementExpression.Compile();
                result = _constMemberAccessCache.GetOrAdd(memberExpression.Member, result);
            }

            var capturedLocal = constantExpression.Value;
            return _ => result(capturedLocal);
        }

        private static Func<TModel, object> Rewrite(
            Expression<Func<TModel, TResult>> expression,
            MemberExpression memberExpression)
        {
            Expression combinedNullTest = null;
            var currentExpression = memberExpression;

            while (currentExpression != null)
            {
                AddNullCheck(currentExpression.Expression, ref combinedNullTest);

                if (currentExpression.Expression is MemberExpression leftExpression)
                {
                    currentExpression = leftExpression;
                }
                else
                {
                    break;
                }
            }

            var body = expression.Body;

            // Cast the entire expression to object in case Member is a value type. This is required for us to be able to
            // express the null conditional statement m == null ? null : (object)m.IntValue
            if (body.Type.IsValueType)
            {
                body = Expression.Convert(body, typeof(object));
            }

            if (combinedNullTest != null)
            {
                Debug.Assert(combinedNullTest.Type == typeof(bool));
                body = Expression.Condition(
                    combinedNullTest,
                    Expression.Constant(value: null, body.Type),
                    body);
            }

            var rewrittenExpression = Expression.Lambda<Func<TModel, object>>(body, expression.Parameters);
            return rewrittenExpression.Compile();
        }

        private static void AddNullCheck(Expression invokingExpression, ref Expression combinedNullTest)
        {
            var type = invokingExpression.Type;
            var isNullableValueType = type.IsValueType && Nullable.GetUnderlyingType(type) != null;
            if (type.IsValueType && !isNullableValueType)
            {
                // struct.Member where struct is not nullable. Do nothing.
                return;
            }

            // NullableStruct.Member or Class.Member
            // type is Nullable ? (value == null) : object.ReferenceEquals(value, null)
            var nullTest = isNullableValueType ?
                Expression.Equal(invokingExpression, NullExpression) :
                Expression.ReferenceEqual(invokingExpression, NullExpression);

            if (combinedNullTest == null)
            {
                combinedNullTest = nullTest;
            }
            else
            {
                // m == null || m.Member == null
                combinedNullTest = Expression.OrElse(nullTest, combinedNullTest);
            }
        }
    }
}

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

internal sealed class MemberExpressionCacheKeyComparer : IEqualityComparer<MemberExpressionCacheKey>
{
    public static readonly MemberExpressionCacheKeyComparer Instance = new MemberExpressionCacheKeyComparer();

    public bool Equals(MemberExpressionCacheKey x, MemberExpressionCacheKey y)
    {
        if (x.ModelType != y.ModelType)
        {
            return false;
        }

        var xEnumerator = x.GetEnumerator();
        var yEnumerator = y.GetEnumerator();

        while (xEnumerator.MoveNext())
        {
            if (!yEnumerator.MoveNext())
            {
                return false;
            }

            // Current is a MemberInfo instance which has a good comparer.
            if (xEnumerator.Current != yEnumerator.Current)
            {
                return false;
            }
        }

        return !yEnumerator.MoveNext();
    }

    public int GetHashCode(MemberExpressionCacheKey obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.ModelType);

        foreach (var member in obj)
        {
            hashCode.Add(member);
        }

        return hashCode.ToHashCode();
    }
}
