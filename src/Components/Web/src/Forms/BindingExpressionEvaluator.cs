// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Evaluates the member-access chain a <c>@bind</c> or <c>For</c> expression is built from without
/// compiling a delegate, so that form binding works when the runtime cannot generate code.
/// </summary>
/// <remarks>
/// Each hop is read through a <see cref="BindableTypeDescriptor"/> when the application described the
/// model graph, and through the <see cref="MemberInfo"/> the expression node already carries otherwise.
/// A node that cannot be evaluated any deeper is anchored at the edit context's model when its static
/// type is compatible. Only shapes the walk does not recognize fall back to
/// <see cref="FieldIdentifier.Create{TField}(Expression{Func{TField}})"/>, which behaves exactly as before.
/// </remarks>
internal static class BindingExpressionEvaluator
{
    private const string NonNullMessage = "The provided expression must evaluate to a non-null value.";

    public static FieldIdentifier CreateFieldIdentifier<TField>(
        Expression<Func<TField>> accessor,
        object? anchorModel,
        IBindableTypeResolver? resolver = null)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        return TryParse(accessor.Body, anchorModel, resolver, out var model, out var fieldName)
            ? new FieldIdentifier(model, fieldName)
            : FieldIdentifier.Create(accessor);
    }

    private static bool TryParse(
        Expression body,
        object? anchorModel,
        IBindableTypeResolver? resolver,
        out object model,
        out string fieldName)
    {
        model = null!;
        fieldName = null!;

        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary && unary.Type == typeof(object))
        {
            body = unary.Operand;
        }

        switch (body)
        {
            case MemberExpression member:
                fieldName = member.Member.Name;
                return TryEvaluate(member.Expression, anchorModel, resolver, out model);
            case MethodCallExpression call when ExpressionFormatter.IsSingleArgumentIndexer(call):
                fieldName = ExpressionFormatter.FormatIndexArgument(call.Arguments[0]);
                return TryEvaluate(call.Object, anchorModel, resolver, out model);
            case BinaryExpression { NodeType: ExpressionType.ArrayIndex } arrayIndex:
                fieldName = ExpressionFormatter.FormatIndexArgument(arrayIndex.Right);
                return TryEvaluate(arrayIndex.Left, anchorModel, resolver, out model);
            default:
                return false;
        }
    }

    private static bool TryEvaluate(
        Expression? node,
        object? anchorModel,
        IBindableTypeResolver? resolver,
        out object value)
    {
        value = null!;

        switch (node)
        {
            case null:
                return false;
            case ConstantExpression constant:
                value = constant.Value ?? throw new ArgumentException(NonNullMessage);
                return true;
            case MemberExpression member
                when TryEvaluate(member.Expression, anchorModel, resolver, out var memberTarget):
                value = ReadMember(memberTarget, member, resolver) ?? throw new ArgumentException(NonNullMessage);
                return true;
            case MethodCallExpression call
                when ExpressionFormatter.IsSingleArgumentIndexer(call)
                    && TryEvaluate(call.Object, anchorModel, resolver, out var indexerTarget)
                    && TryEvaluate(call.Arguments[0], anchorModel, resolver, out var index):
                value = ReadIndexer(indexerTarget, index, call, resolver) ?? throw new ArgumentException(NonNullMessage);
                return true;
            case BinaryExpression { NodeType: ExpressionType.ArrayIndex } arrayIndex
                when TryEvaluate(arrayIndex.Left, anchorModel, resolver, out var array)
                    && TryEvaluate(arrayIndex.Right, anchorModel, resolver, out var arrayIndexValue):
                value = ((Array)array).GetValue((int)arrayIndexValue) ?? throw new ArgumentException(NonNullMessage);
                return true;
        }

        if (anchorModel is not null && node.Type.IsInstanceOfType(anchorModel))
        {
            value = anchorModel;
            return true;
        }

        return false;
    }

    private static object? ReadMember(object target, MemberExpression member, IBindableTypeResolver? resolver)
    {
        if (resolver is not null &&
            resolver.TryGetBindableTypeDescriptor(member.Expression!.Type, out var descriptor))
        {
            foreach (var described in descriptor.Members)
            {
                if (string.Equals(described.Name, member.Member.Name, StringComparison.Ordinal))
                {
                    return described.GetValue(target);
                }
            }
        }

        return member.Member switch
        {
            PropertyInfo property => property.GetValue(target),
            FieldInfo field => field.GetValue(target),
            _ => throw new ArgumentException(
                $"The provided expression contains a {member.Member.GetType().Name} which is not supported. " +
                $"{nameof(FieldIdentifier)} only supports simple member accessors (fields, properties) of an object."),
        };
    }

    private static object? ReadIndexer(
        object target,
        object index,
        MethodCallExpression call,
        IBindableTypeResolver? resolver)
    {
        if (resolver is not null &&
            resolver.TryGetBindableTypeDescriptor(call.Object!.Type, out var descriptor))
        {
            var indexType = call.Arguments[0].Type;
            foreach (var described in descriptor.Indexers)
            {
                if (described.IndexType == indexType)
                {
                    return described.GetValue(target, index);
                }
            }
        }

        return call.Method.Invoke(target, [index]);
    }
}
