// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms;

internal static class ExpressionFormatter
{
    internal const int StackAllocBufferSize = 128;

    private delegate void CapturedValueFormatter(object closure, ref ReverseStringBuilder builder);

    private static readonly ConcurrentDictionary<MemberInfo, CapturedValueFormatter> s_capturedValueFormatterCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, MethodInfoData> s_methodInfoDataCache = new();

    public static void ClearCache()
    {
        s_capturedValueFormatterCache.Clear();
        s_methodInfoDataCache.Clear();
    }

    public static string FormatLambda(LambdaExpression expression, Predicate<Type>? canConvertDirectly = null)
    {
        var builder = new ReverseStringBuilder(stackalloc char[StackAllocBufferSize]);
        var node = expression.Body;
        var wasLastExpressionMemberAccess = false;

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
                    FormatIndexArgument(methodCallExpression.Arguments[0], ref builder);
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
                    FormatIndexArgument(binaryExpression.Right, ref builder);
                    builder.InsertFront("[");
                    node = binaryExpression.Left;
                    break;

                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)node;
                    var nextNode = memberExpression.Expression;

                    if (nextNode?.NodeType == ExpressionType.Constant)
                    {
                        // Special case primitive values that are bound directly from the form.
                        // By convention, the name for the field will be "value".
                        if (canConvertDirectly?.Invoke(memberExpression.Type) == true &&
                            memberExpression.Member.IsDefined(typeof(SupplyParameterFromFormAttribute), inherit: false))
                        {
                            builder.InsertFront("value");
                        }
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

        builder.Dispose();

        return result;
    }

    private static bool IsSingleArgumentIndexer(Expression expression)
    {
        if (expression is not MethodCallExpression methodExpression || methodExpression.Arguments.Count != 1)
        {
            return false;
        }

        var methodInfoData = GetOrCreateMethodInfoData(methodExpression.Method);
        return methodInfoData.IsSingleArgumentIndexer;
    }

    private static MethodInfoData GetOrCreateMethodInfoData(MethodInfo methodInfo)
    {
        if (!s_methodInfoDataCache.TryGetValue(methodInfo, out var methodInfoData))
        {
            methodInfoData = GetMethodInfoData(methodInfo);
            s_methodInfoDataCache[methodInfo] = methodInfoData;
        }

        return methodInfoData;

        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The relevant members should be preserved since they were referenced in a LINQ expression")]
        static MethodInfoData GetMethodInfoData(MethodInfo methodInfo)
        {
            var declaringType = methodInfo.DeclaringType;
            if (declaringType is null)
            {
                return new(IsSingleArgumentIndexer: false);
            }

            // Check whether GetDefaultMembers() (if present in CoreCLR) would return a member of this type. Compiler
            // names the indexer property, if any, in a generated [DefaultMember] attribute for the containing type.
            var defaultMember = declaringType.GetCustomAttribute<DefaultMemberAttribute>(inherit: true);
            if (defaultMember is null)
            {
                return new(IsSingleArgumentIndexer: false);
            }

            // Find default property (the indexer) and confirm its getter is the method in this expression.
            var runtimeProperties = declaringType.GetRuntimeProperties();
            if (runtimeProperties is null)
            {
                return new(IsSingleArgumentIndexer: false);
            }

            foreach (var property in runtimeProperties)
            {
                if (string.Equals(defaultMember.MemberName, property.Name, StringComparison.Ordinal) &&
                    property.GetMethod == methodInfo)
                {
                    return new(IsSingleArgumentIndexer: true);
                }
            }

            return new(IsSingleArgumentIndexer: false);
        }
    }

    private static void FormatIndexArgument(
        Expression indexExpression,
        ref ReverseStringBuilder builder)
    {
        switch (indexExpression)
        {
            case MemberExpression memberExpression when memberExpression.Expression is ConstantExpression constantExpression:
                FormatCapturedValue(memberExpression, constantExpression, ref builder);
                break;
            case ConstantExpression constantExpression:
                FormatConstantValue(constantExpression, ref builder);
                break;
            default:
                throw new InvalidOperationException($"Unable to evaluate index expressions of type '{indexExpression.GetType().Name}'.");
        }
    }

    private static void FormatCapturedValue(MemberExpression memberExpression, ConstantExpression constantExpression, ref ReverseStringBuilder builder)
    {
        var member = memberExpression.Member;
        if (!s_capturedValueFormatterCache.TryGetValue(member, out var format))
        {
            format = CreateCapturedValueFormatter(memberExpression);
            s_capturedValueFormatterCache[member] = format;
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

    private record struct MethodInfoData(bool IsSingleArgumentIndexer);
}
