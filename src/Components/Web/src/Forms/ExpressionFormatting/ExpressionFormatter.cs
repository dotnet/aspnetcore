// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms;

internal sealed class ExpressionFormatter
{
    internal const int StackallocBufferSize = 128;
    internal const int EstimatedStringLengthPadding = 8;

    // Return value of 'true' means non-constant result, 'false' means constant.
    private delegate bool IndexArgumentFormatter(Expression expression, ref ReverseStringBuilder builder);

    private readonly ConcurrentDictionary<Expression, FormattedExpressionInfo> _topLevelExpressionInfoCache = new();
    private readonly ConcurrentDictionary<Expression, IndexArgumentFormatter> _indexArgumentFormatterCache = new();

    public string FormatLambda(LambdaExpression lambdaExpression)
    {
        if (_topLevelExpressionInfoCache.TryGetValue(lambdaExpression, out var expressionInfo))
        {
            if (expressionInfo.CachedResult is { } cachedResult)
            {
                // We've seen this expression before and know it yields a constant result,
                // so we'll return the cached result.
                return cachedResult;
            }

            var estimatedLength = expressionInfo.EstimatedLength;
            Debug.Assert(estimatedLength >= 0);

            // We've seen the expression before, but it may not format the same way every time.
            // We use the previous formatted string length as an estimate for the new formatted string.
            // Some extra padding gets added to avoid allocating an extra buffer if we exceed
            // the estimated amount.
            return FormatLambdaCore(lambdaExpression, new(estimatedLength + EstimatedStringLengthPadding));
        }
        else
        {
            // Either we haven't seen the expression before, or its formatted representation fit
            // within the stack-allocated buffer last time. In either case, we provide a stack-allocated
            // buffer to hold the initial contents of the formatted string. Additional buffers will be
            // allocated by the ReverseStringBuilder if we exceed this initial buffer.
            Span<char> initialBuffer = stackalloc char[StackallocBufferSize];
            return FormatLambdaCore(lambdaExpression, new(initialBuffer));
        }
    }

    private string FormatLambdaCore(LambdaExpression lambdaExpression, ReverseStringBuilder builder)
    {
        var node = lambdaExpression.Body;
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

                    // FIXME: Better way of detecting this?
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
                    // TODO: Should we throw here?
                    node = null;
                    break;
            }
        }

        var result = builder.ToString();

        if (shouldNotCache)
        {
            if (result.Length < StackallocBufferSize - EstimatedStringLengthPadding)
            {
                // We can be fairly certain that the formatted string will fit
                // within a stack-allocated buffer next time, so let's not estimate
                // a size to use for a heap-allocated buffer.
            }
            else
            {
                // We can't cache the string, but we can make a good guess on how big the
                // heap-allocated buffer should be next time.
                _topLevelExpressionInfoCache[lambdaExpression] = new()
                {
                    EstimatedLength = result.Length,
                };
            }
        }
        else
        {
            // Cache the result to return next time.
            _topLevelExpressionInfoCache[lambdaExpression] = new()
            {
                CachedResult = result,
            };
        }

        builder.Dispose();

        return result;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The relevant members should be preserved since they were referenced in a Linq expression")]
    private static bool IsSingleArgumentIndexer(Expression expression)
    {
        // TODO: This was copied from MVC. Investigate if we need to change anything.

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
            if ((string.Equals(defaultMember.MemberName, property.Name, StringComparison.Ordinal) &&
                property.GetMethod == methodExpression.Method))
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
        if (!_indexArgumentFormatterCache.TryGetValue(indexExpression, out var format))
        {
            format = MakeFormatter(indexExpression);
            _indexArgumentFormatterCache[indexExpression] = format;
        }

        return format(indexExpression, ref builder);
    }

    private static IndexArgumentFormatter MakeFormatter(Expression indexExpression)
    {
        switch (indexExpression.NodeType)
        {
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)indexExpression;
                return CreateCapturedVariableFormatter(memberExpression);
            case ExpressionType.Constant:
                var constantExpression = (ConstantExpression)indexExpression;
                return CreateConstantIndexFormatter(constantExpression);
            default:
                throw new InvalidOperationException($"Unable to evaluate index expressions of type '{indexExpression.GetType().Name}'.");
        }
    }

    private static IndexArgumentFormatter CreateCapturedVariableFormatter(MemberExpression memberExpression)
    {
        var memberType = memberExpression.Type;

        if (memberType == typeof(int))
        {
            // Use the "INumber<TSelf>" string builder overload when possible because it doesn't allocate
            // an extra string.
            // TODO: Consider handling ISpanFormattable types more generally. That way we can write to
            // rented arrays rather than allocate new strings.
            var func = CompileMemberEvaluator<int>(memberExpression);

            return (Expression _, ref ReverseStringBuilder builder) =>
            {
                var value = func.Invoke();
                builder.InsertFront(value);
                return true;
            };
        }

        if (memberType == typeof(string))
        {
            var func = CompileMemberEvaluator<string>(memberExpression);

            return (Expression _, ref ReverseStringBuilder builder) =>
            {
                var value = func.Invoke();
                builder.InsertFront(value);
                return true;
            };
        }

        if (typeof(IFormattable).IsAssignableFrom(memberType))
        {
            var func = CompileMemberEvaluator<IFormattable>(memberExpression);

            return (Expression _, ref ReverseStringBuilder builder) =>
            {
                var value = func.Invoke();
                builder.InsertFront(value.ToString(null, CultureInfo.InvariantCulture));
                return true;
            };
        }

        throw new InvalidOperationException($"Cannot format an index argument of type '{memberType}'.");

        static Func<TResult> CompileMemberEvaluator<TResult>(MemberExpression memberExpression)
        {
            var convertExpression = Expression.Convert(memberExpression, typeof(TResult));
            var lambdaExpression = Expression.Lambda<Func<TResult>>(convertExpression);
            return lambdaExpression.Compile();
        }
    }

    private static IndexArgumentFormatter CreateConstantIndexFormatter(ConstantExpression constantExpression)
    {
        // As much as possible, we return static delegates to avoid creating closures.
        var constantValue = constantExpression.Value;

        if (constantValue is null)
        {
            // TODO: Should we literally write out "null" in the generated string?
            return static (Expression _, ref ReverseStringBuilder _) => false;
        }

        var constantType = constantValue.GetType();

        if (constantType == typeof(int))
        {
            return static (Expression expression, ref ReverseStringBuilder builder) =>
            {
                var value = (int)((ConstantExpression)expression).Value!;
                builder.InsertFront(value);
                return false;
            };
        }

        if (constantType == typeof(string))
        {
            return static (Expression expression, ref ReverseStringBuilder builder) =>
            {
                var value = (string)((ConstantExpression)expression).Value!;
                builder.InsertFront(value);
                return false;
            };
        }

        if (constantValue is IFormattable formattable)
        {
            // In this case, we prefer to allocate a string and create a closure once
            // instead of avoiding the closure but allocating a string on every call.
            var formattedValue = formattable.ToString(null, CultureInfo.InvariantCulture);
            return (Expression _, ref ReverseStringBuilder builder) =>
            {
                builder.InsertFront(formattedValue);
                return false;
            };
        }

        throw new InvalidOperationException($"Cannot format an index argument of type '{constantType}'.");
    }

    private readonly record struct FormattedExpressionInfo(string? CachedResult, int EstimatedLength);
}
