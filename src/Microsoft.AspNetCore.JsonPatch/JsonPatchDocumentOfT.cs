// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Converters;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch
{
    // Implementation details: the purpose of this type of patch document is to ensure we can do type-checking
    // when producing a JsonPatchDocument.  However, we cannot send this "typed" over the wire, as that would require
    // including type data in the JsonPatchDocument serialized as JSON (to allow for correct deserialization) - that's
    // not according to RFC 6902, and would thus break cross-platform compatibility.
    [JsonConverter(typeof(TypedJsonPatchDocumentConverter))]
    public class JsonPatchDocument<TModel> : IJsonPatchDocument where TModel : class
    {
        public List<Operation<TModel>> Operations { get; private set; }

        [JsonIgnore]
        public IContractResolver ContractResolver { get; set; }

        public JsonPatchDocument()
        {
            Operations = new List<Operation<TModel>>();
            ContractResolver = new DefaultContractResolver();
        }

        // Create from list of operations
        public JsonPatchDocument(List<Operation<TModel>> operations, IContractResolver contractResolver)
        {
            if (operations == null)
            {
                throw new ArgumentNullException(nameof(operations));
            }

            if (contractResolver == null)
            {
                throw new ArgumentNullException(nameof(contractResolver));
            }

            Operations = operations;
            ContractResolver = contractResolver;
        }

        /// <summary>
        /// Add operation.  Will result in, for example,
        /// { "op": "add", "path": "/a/b/c", "value": [ "foo", "bar" ] }
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Add<TProp>(Expression<Func<TModel, TProp>> path, TProp value)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "add",
                GetPath(path),
                from: null,
                value: value));

            return this;
        }

        /// <summary>
        /// Add value to list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <param name="position">position</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Add<TProp>(
            Expression<Func<TModel, IList<TProp>>> path,
            TProp value,
            int position)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "add",
                GetPath(path) + "/" + position,
                from: null,
                value: value));

            return this;
        }

        /// <summary>
        /// At value at end of list
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Add<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "add",
                GetPath(path) + "/-",
                from: null,
                value: value));

            return this;
        }

        /// <summary>
        /// Remove value at target location.  Will result in, for example,
        /// { "op": "remove", "path": "/a/b/c" }
        /// </summary>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Remove<TProp>(Expression<Func<TModel, TProp>> path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>("remove", GetPath(path), from: null));

            return this;
        }

        /// <summary>
        /// Remove value from list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="position">position</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Remove<TProp>(Expression<Func<TModel, IList<TProp>>> path, int position)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "remove",
                GetPath(path) + "/" + position,
                from: null));

            return this;
        }

        /// <summary>
        /// Remove value from end of list
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Remove<TProp>(Expression<Func<TModel, IList<TProp>>> path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "remove",
                GetPath(path) + "/-",
                from: null));

            return this;
        }

        /// <summary>
        /// Replace value.  Will result in, for example,
        /// { "op": "replace", "path": "/a/b/c", "value": 42 }
        /// </summary>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Replace<TProp>(Expression<Func<TModel, TProp>> path, TProp value)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "replace",
                GetPath(path),
                from: null,
                value: value));

            return this;
        }

        /// <summary>
        /// Replace value in a list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <param name="position">position</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Replace<TProp>(Expression<Func<TModel, IList<TProp>>> path,
            TProp value, int position)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "replace",
                GetPath(path) + "/" + position,
                from: null,
                value: value));

            return this;
        }

        /// <summary>
        /// Replace value at end of a list
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Replace<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "replace",
                GetPath(path) + "/-",
                from: null,
                value: value));

            return this;
        }

        /// <summary>
        /// Removes value at specified location and add it to the target location.  Will result in, for example:
        /// { "op": "move", "from": "/a/b/c", "path": "/a/b/d" }
        /// </summary>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Move<TProp>(
            Expression<Func<TModel, TProp>> from,
            Expression<Func<TModel, TProp>> path)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "move",
                GetPath(path),
                GetPath(from)));

            return this;
        }

        /// <summary>
        /// Move from a position in a list to a new location
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position</param>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Move<TProp>(
            Expression<Func<TModel, IList<TProp>>> from,
            int positionFrom,
            Expression<Func<TModel, TProp>> path)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "move",
                GetPath(path),
                GetPath(from) + "/" + positionFrom));

            return this;
        }

        /// <summary>
        /// Move from a property to a location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <param name="positionTo">position</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Move<TProp>(
            Expression<Func<TModel, TProp>> from,
            Expression<Func<TModel, IList<TProp>>> path,
            int positionTo)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "move",
                GetPath(path) + "/" + positionTo,
                GetPath(from)));

            return this;
        }

        /// <summary>
        /// Move from a position in a list to another location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position (source)</param>
        /// <param name="path">target location</param>
        /// <param name="positionTo">position (target)</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Move<TProp>(
            Expression<Func<TModel, IList<TProp>>> from,
            int positionFrom,
            Expression<Func<TModel, IList<TProp>>> path,
            int positionTo)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "move",
                GetPath(path) + "/" + positionTo,
                GetPath(from) + "/" + positionFrom));

            return this;
        }

        /// <summary>
        /// Move from a position in a list to the end of another list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position</param>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Move<TProp>(
            Expression<Func<TModel, IList<TProp>>> from,
            int positionFrom,
            Expression<Func<TModel, IList<TProp>>> path)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "move",
                GetPath(path) + "/-",
                GetPath(from) + "/" + positionFrom));

            return this;
        }

        /// <summary>
        /// Move to the end of a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Move<TProp>(
           Expression<Func<TModel, TProp>> from,
           Expression<Func<TModel, IList<TProp>>> path)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "move",
                GetPath(path) + "/-",
                GetPath(from)));

            return this;
        }

        /// <summary>
        /// Copy the value at specified location to the target location.  Willr esult in, for example:
        /// { "op": "copy", "from": "/a/b/c", "path": "/a/b/e" }
        /// </summary>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Copy<TProp>(
           Expression<Func<TModel, TProp>> from,
           Expression<Func<TModel, TProp>> path)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "copy",
                GetPath(path)
              , GetPath(from)));

            return this;
        }

        /// <summary>
        /// Copy from a position in a list to a new location
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position</param>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Copy<TProp>(
           Expression<Func<TModel, IList<TProp>>> from,
            int positionFrom,
           Expression<Func<TModel, TProp>> path)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "copy",
                GetPath(path),
                GetPath(from) + "/" + positionFrom));

            return this;
        }

        /// <summary>
        /// Copy from a property to a location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <param name="positionTo">position</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Copy<TProp>(
            Expression<Func<TModel, TProp>> from,
            Expression<Func<TModel, IList<TProp>>> path,
            int positionTo)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "copy",
                GetPath(path) + "/" + positionTo,
                GetPath(from)));

            return this;
        }

        /// <summary>
        /// Copy from a position in a list to a new location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position (source)</param>
        /// <param name="path">target location</param>
        /// <param name="positionTo">position (target)</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Copy<TProp>(
            Expression<Func<TModel, IList<TProp>>> from,
            int positionFrom,
            Expression<Func<TModel, IList<TProp>>> path,
            int positionTo)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "copy",
                GetPath(path) + "/" + positionTo,
                GetPath(from) + "/" + positionFrom));

            return this;
        }

        /// <summary>
        /// Copy from a position in a list to the end of another list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position</param>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Copy<TProp>(
            Expression<Func<TModel, IList<TProp>>> from,
            int positionFrom,
            Expression<Func<TModel, IList<TProp>>> path)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "copy",
                GetPath(path) + "/-",
                GetPath(from) + "/" + positionFrom));

            return this;
        }

        /// <summary>
        /// Copy to the end of a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<TModel> Copy<TProp>(
            Expression<Func<TModel, TProp>> from,
            Expression<Func<TModel, IList<TProp>>> path)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Operations.Add(new Operation<TModel>(
                "copy",
                GetPath(path) + "/-",
                GetPath(from)));

            return this;
        }

        /// <summary>
        /// Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        public void ApplyTo(TModel objectToApplyTo)
        {
            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            ApplyTo(objectToApplyTo, new ObjectAdapter(ContractResolver, logErrorAction: null));
        }

        /// <summary>
        /// Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        /// <param name="logErrorAction">Action to log errors</param>
        public void ApplyTo(TModel objectToApplyTo, Action<JsonPatchError> logErrorAction)
        {
            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            var adapter = new ObjectAdapter(ContractResolver, logErrorAction);
            foreach (var op in Operations)
            {
                try
                {
                    op.Apply(objectToApplyTo, adapter);
                }
                catch (JsonPatchException jsonPatchException)
                {
                    var errorReporter = logErrorAction ?? ErrorReporter.Default;
                    errorReporter(new JsonPatchError(objectToApplyTo, op, jsonPatchException.Message));

                    // As per JSON Patch spec if an operation results in error, further operations should not be executed.
                    break;
                }
            }
        }

        /// <summary>
        /// Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        /// <param name="adapter">IObjectAdapter instance to use when applying</param>
        public void ApplyTo(TModel objectToApplyTo, IObjectAdapter adapter)
        {
            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            // apply each operation in order
            foreach (var op in Operations)
            {
                op.Apply(objectToApplyTo, adapter);
            }
        }

        IList<Operation> IJsonPatchDocument.GetOperations()
        {
            var allOps = new List<Operation>();

            if (Operations != null)
            {
                foreach (var op in Operations)
                {
                    var untypedOp = new Operation();

                    untypedOp.op = op.op;
                    untypedOp.value = op.value;
                    untypedOp.path = op.path;
                    untypedOp.from = op.from;

                    allOps.Add(untypedOp);
                }
            }

            return allOps;
        }

        private string GetPath<TProp>(Expression<Func<TModel, TProp>> expr)
        {
            return "/" + GetPath(expr.Body, true).ToLowerInvariant();
        }

        private string GetPath(Expression expr, bool firstTime)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.ArrayIndex:
                    var binaryExpression = (BinaryExpression)expr;

                    if (ContinueWithSubPath(binaryExpression.Left.NodeType, false))
                    {
                        var leftFromBinaryExpression = GetPath(binaryExpression.Left, false);
                        return leftFromBinaryExpression + "/" + binaryExpression.Right.ToString();
                    }
                    else
                    {
                        return binaryExpression.Right.ToString();
                    }
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)expr;

                    if (ContinueWithSubPath(methodCallExpression.Object.NodeType, false))
                    {
                        var leftFromMemberCallExpression = GetPath(methodCallExpression.Object, false);
                        return leftFromMemberCallExpression + "/" +
                            GetIndexerInvocation(methodCallExpression.Arguments[0]);
                    }
                    else
                    {
                        return GetIndexerInvocation(methodCallExpression.Arguments[0]);
                    }
                case ExpressionType.Convert:
                    return GetPath(((UnaryExpression)expr).Operand, false);
                case ExpressionType.MemberAccess:
                    var memberExpression = expr as MemberExpression;

                    if (ContinueWithSubPath(memberExpression.Expression.NodeType, false))
                    {
                        var left = GetPath(memberExpression.Expression, false);
                        // Get property name, respecting JsonProperty attribute
                        return left + "/" + GetPropertyNameFromMemberExpression(memberExpression);
                    }
                    else
                    {
                        // Get property name, respecting JsonProperty attribute
                        return GetPropertyNameFromMemberExpression(memberExpression);
                    }
                case ExpressionType.Parameter:
                    // Fits "x => x" (the whole document which is "" as JSON pointer)
                    return firstTime ? string.Empty : null;
                default:
                    return string.Empty;
            }
        }

        private string GetPropertyNameFromMemberExpression(MemberExpression memberExpression)
        {
            var jsonObjectContract = ContractResolver.ResolveContract(memberExpression.Expression.Type) as JsonObjectContract;
            if (jsonObjectContract != null)
            {
                return jsonObjectContract.Properties
                    .First(jsonProperty => jsonProperty.UnderlyingName == memberExpression.Member.Name)
                    .PropertyName;
            }

            return null;
        }

        private static bool ContinueWithSubPath(ExpressionType expressionType, bool firstTime)
        {
            if (firstTime)
            {
                return (expressionType == ExpressionType.ArrayIndex
                       || expressionType == ExpressionType.Call
                       || expressionType == ExpressionType.Convert
                       || expressionType == ExpressionType.MemberAccess
                       || expressionType == ExpressionType.Parameter);
            }
            else
            {
                return (expressionType == ExpressionType.ArrayIndex
                    || expressionType == ExpressionType.Call
                    || expressionType == ExpressionType.Convert
                    || expressionType == ExpressionType.MemberAccess);
            }
        }

        private static string GetIndexerInvocation(Expression expression)
        {
            var converted = Expression.Convert(expression, typeof(object));
            var fakeParameter = Expression.Parameter(typeof(object), null);
            var lambda = Expression.Lambda<Func<object, object>>(converted, fakeParameter);
            Func<object, object> func;

            func = lambda.Compile();

            return Convert.ToString(func(null), CultureInfo.InvariantCulture);
        }
    }
}