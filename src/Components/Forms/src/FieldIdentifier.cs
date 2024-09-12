// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.HotReload;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Uniquely identifies a single field that can be edited. This may correspond to a property on a
/// model object, or can be any other named value.
/// </summary>
public readonly struct FieldIdentifier : IEquatable<FieldIdentifier>
{
    private static readonly ConcurrentDictionary<(Type ModelType, MemberInfo Member), Func<object, object>> _fieldAccessors = new();

    static FieldIdentifier()
    {
        HotReloadManager.Default.OnDeltaApplied += ClearCache;
    }

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
        return ReferenceEquals(otherIdentifier.Model, Model) &&
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

        switch (accessorBody)
        {
            case MemberExpression memberExpression:
                // Identify the field name. We don't mind whether it's a property or field, or even something else.
                fieldName = memberExpression.Member.Name;
                // Get a reference to the model object
                // i.e., given a value like "(something).MemberName", determine the runtime value of "(something)",
                switch (memberExpression.Expression)
                {
                    case ConstantExpression constant when constant.Value == null:
                        throw new ArgumentException("The provided expression must evaluate to a non-null value.");
                    case ConstantExpression constant when constant.Value != null:
                        model = constant.Value;
                        break;
                    case MemberExpression member when member.Expression is ConstantExpression:
                        model = GetModelFromMemberAccess(member);
                        break;
                    case not null:
                        // It would be great to cache this somehow, but it's unclear there's a reasonable way to do
                        // so, given that it embeds captured values such as "this". We could consider special-casing
                        // for "() => something.Member" and building a cache keyed by "something.GetType()" with values
                        // of type Func<object, object> so we can cheaply map from "something" to "something.Member".
                        var modelLambda = Expression.Lambda(typeof(Func<object?>), memberExpression.Expression);
                        var modelLambdaCompiled = (Func<object?>)modelLambda.Compile();
                        var result = modelLambdaCompiled() ??
                            throw new ArgumentException("The provided expression must evaluate to a non-null value.");

                        model = result;
                        break;
                    default:
                        throw new ArgumentException($"The provided expression contains a {accessorBody.GetType().Name} which is not supported. {nameof(FieldIdentifier)} only supports simple member accessors (fields, properties) of an object.");
                }
                break;
            case MethodCallExpression methodCallExpression when ExpressionFormatter.IsSingleArgumentIndexer(accessorBody):
                fieldName = ExpressionFormatter.FormatIndexArgument(methodCallExpression.Arguments[0]);
                model = GetModelFromIndexer(methodCallExpression.Object!);
                break;
            case BinaryExpression binaryExpression when binaryExpression.NodeType == ExpressionType.ArrayIndex:
                fieldName = ExpressionFormatter.FormatIndexArgument(binaryExpression.Right);
                model = GetModelFromIndexer(binaryExpression.Left);
                break;
            default:
                throw new ArgumentException($"The provided expression contains a {accessorBody.GetType().Name} which is not supported. {nameof(FieldIdentifier)} only supports simple member accessors (fields, properties) of an object.");
        }
    }

    internal static object GetModelFromMemberAccess(
        MemberExpression member,
        ConcurrentDictionary<(Type ModelType, MemberInfo Member), Func<object, object>>? cache = null)
    {
        cache ??= _fieldAccessors;
        Func<object, object>? accessor = null;
        object? value = null;
        switch (member.Expression)
        {
            case ConstantExpression model:
                value = model.Value ?? throw new ArgumentException("The provided expression must evaluate to a non-null value.");
                accessor = cache.GetOrAdd((value.GetType(), member.Member), CreateAccessor);
                break;
            default:
                break;
        }

        if (accessor == null)
        {
            throw new InvalidOperationException($"Unable to compile expression: {member}");
        }

        if (value == null)
        {
            throw new ArgumentException("The provided expression must evaluate to a non-null value.");
        }

        var result = accessor(value);
        return result;

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "Application code does not get trimmed. We expect the members in the expression to not be trimmed.")]
        static Func<object, object> CreateAccessor((Type model, MemberInfo member) arg)
        {
            var parameter = Expression.Parameter(typeof(object), "value");
            Expression expression = Expression.Convert(parameter, arg.model);

            expression = Expression.MakeMemberAccess(expression, arg.member);
            expression = Expression.Convert(expression, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(expression, parameter);

            var func = lambda.Compile();
            return func;
        }
    }

    private static object GetModelFromIndexer(Expression methodCallExpression)
    {
        object model;
        var methodCallObjectLambda = Expression.Lambda(typeof(Func<object?>), methodCallExpression!);
        var methodCallObjectLambdaCompiled = (Func<object?>)methodCallObjectLambda.Compile();
        var result = methodCallObjectLambdaCompiled();
        if (result is null)
        {
            throw new ArgumentException("The provided expression must evaluate to a non-null value.");
        }
        model = result;
        return model;
    }

    private static void ClearCache()
    {
        _fieldAccessors.Clear();
    }
}
