// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms;

internal sealed class ExpressionFormatter
{
    internal const int StackallocBufferSize = 128;
    internal const int EstimatedStringLengthPadding = 8;

    private delegate void CapturedValueFormatter(object closure, ref ReverseStringBuilder builder);

    private readonly ConcurrentDictionary<MemberInfo, CapturedValueFormatter> _capturedValueFormatterCache = new();

    public string FormatLambda(LambdaExpression expression)
    {
        var builder = new ReverseStringBuilder(stackalloc char[StackallocBufferSize]);
        var node = expression.Body;
        var wasLastExpressionMemberAccess = false;
        var shouldNotCache = false;

        while (node is not null)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)node;

                    if (!IsSingleArgumentIndexer(methodCallExpression))
                    {
                        throw new InvalidOperationException("Method calls cannot be formatted.");
                    }

                    if (wasLastExpressionMemberAccess)
                    {
                        wasLastExpressionMemberAccess = false;
                        builder.InsertFront(".");
                    }

                    builder.InsertFront("]");
                    shouldNotCache |= FormatIndexArgument(methodCallExpression.Arguments.Single(), ref builder);
                    builder.InsertFront("[");
                    node = methodCallExpression.Object;
                    break;

                case ExpressionType.ArrayIndex:
                    var binaryExpression = (BinaryExpression)node;

                    if (wasLastExpressionMemberAccess)
                    {
                        wasLastExpressionMemberAccess = false;
                        builder.InsertFront(".");
                    }

                    builder.InsertFront("]");
                    shouldNotCache |= FormatIndexArgument(binaryExpression.Right, ref builder);
                    builder.InsertFront("[");
                    node = binaryExpression.Left;
                    break;

                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)node;
                    var nextNode = memberExpression.Expression;

                    // FIXME: This doesn't work in all cases. Need a better
                    // way of detecting when we've reached the model object.
                    if (nextNode?.Type.Name.StartsWith('<') ?? false)
                    {
                        // The next node has a compiler-generated closure type,
                        // which means the current member access is on the captured model.
                        // We don't want to include the model variable name in the generated
                        // string, so we exit.
                        node = null;
                        break;
                    }

                    if (wasLastExpressionMemberAccess)
                    {
                        builder.InsertFront(".");
                    }
                    wasLastExpressionMemberAccess = true;

                    var name = memberExpression.Member.Name;
                    builder.InsertFront(name);

                    node = nextNode;
                    break;

                default:
                    // Unsupported expression type.
                    node = null;
                    break;
            }
        }

        var result = builder.ToString();

        // TODO: Top-level caching.

        builder.Dispose();

        return result;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The relevant members should be preserved since they were referenced in a Linq expression")]
    private static bool IsSingleArgumentIndexer(Expression expression)
    {
        if (expression is not MethodCallExpression methodExpression || methodExpression.Arguments.Count != 1)
        {
            return false;
        }

        var declaringType = methodExpression.Method.DeclaringType;
        if (declaringType is null)
        {
            return false;
        }

        // Check whether GetDefaultMembers() (if present in CoreCLR) would return a member of this type. Compiler
        // names the indexer property, if any, in a generated [DefaultMember] attribute for the containing type.
        var defaultMember = declaringType.GetCustomAttribute<DefaultMemberAttribute>(inherit: true);
        if (defaultMember is null)
        {
            return false;
        }

        // Find default property (the indexer) and confirm its getter is the method in this expression.
        var runtimeProperties = declaringType.GetRuntimeProperties();
        if (runtimeProperties is null)
        {
            return false;
        }

        foreach (var property in runtimeProperties)
        {
            if (string.Equals(defaultMember.MemberName, property.Name, StringComparison.Ordinal) &&
                property.GetMethod == methodExpression.Method)
            {
                return true;
            }
        }

        return false;
    }

    private bool FormatIndexArgument(
        Expression indexExpression,
        ref ReverseStringBuilder builder)
    {
        switch (indexExpression)
        {
            case MemberExpression memberExpression when memberExpression.Expression is ConstantExpression constantExpression:
                FormatCapturedValue(memberExpression, constantExpression, ref builder);
                return true;
            case ConstantExpression constantExpression:
                FormatConstantValue(constantExpression, ref builder);
                return false;
            default:
                throw new InvalidOperationException($"Unable to evaluate index expressions of type '{indexExpression.GetType().Name}'.");
        }
    }

    private void FormatCapturedValue(MemberExpression memberExpression, ConstantExpression constantExpression, ref ReverseStringBuilder builder)
    {
        var member = memberExpression.Member;
        if (!_capturedValueFormatterCache.TryGetValue(member, out var format))
        {
            format = CreateCapturedValueFormatter(memberExpression);
            _capturedValueFormatterCache[member] = format;
        }

        format(constantExpression.Value!, ref builder);
    }

    private static CapturedValueFormatter CreateCapturedValueFormatter(MemberExpression memberExpression)
    {
        var memberType = memberExpression.Type;

        if (memberType == typeof(int))
        {
            var func = CompileMemberEvaluator<int>(memberExpression);
            return (object closure, ref ReverseStringBuilder builder) => builder.InsertFront(func.Invoke(closure));
        }
        else if (memberType == typeof(string))
        {
            var func = CompileMemberEvaluator<string>(memberExpression);
            return (object closure, ref ReverseStringBuilder builder) => builder.InsertFront(func.Invoke(closure));
        }
        else if (typeof(ISpanFormattable).IsAssignableFrom(memberType))
        {
            var func = CompileMemberEvaluator<ISpanFormattable>(memberExpression);
            return (object closure, ref ReverseStringBuilder builder) => builder.InsertFront(func.Invoke(closure));
        }
        else if (typeof(IFormattable).IsAssignableFrom(memberType))
        {
            var func = CompileMemberEvaluator<IFormattable>(memberExpression);
            return (object closure, ref ReverseStringBuilder builder) => builder.InsertFront(func.Invoke(closure));
        }
        else
        {
            throw new InvalidOperationException($"Cannot format an index argument of type '{memberType}'.");
        }

        static Func<object, TResult> CompileMemberEvaluator<TResult>(MemberExpression memberExpression)
        {
            var parameterExpression = Expression.Parameter(typeof(object));
            var convertExpression = Expression.Convert(parameterExpression, memberExpression.Member.DeclaringType!);
            var replacedMemberExpression = memberExpression.Update(convertExpression);
            var replacedExpression = Expression.Lambda<Func<object, TResult>>(replacedMemberExpression, parameterExpression);
            return replacedExpression.Compile();
        }
    }

    private static void FormatConstantValue(ConstantExpression constantExpression, ref ReverseStringBuilder builder)
    {
        switch (constantExpression.Value)
        {
            case string s:
                builder.InsertFront(s);
                break;
            case ISpanFormattable spanFormattable:
                // This is better than the formattable case because we don't allocate an extra string.
                builder.InsertFront(spanFormattable);
                break;
            case IFormattable formattable:
                builder.InsertFront(formattable);
                break;
            case null:
                builder.InsertFront("null");
                break;
            case var x:
                throw new InvalidOperationException($"Unable to format constant values of type '{x.GetType()}'.");
        }
    }
}
