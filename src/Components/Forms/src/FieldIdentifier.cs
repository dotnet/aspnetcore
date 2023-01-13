// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
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
        fieldName = memberExpression.Member.Name;

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
}
