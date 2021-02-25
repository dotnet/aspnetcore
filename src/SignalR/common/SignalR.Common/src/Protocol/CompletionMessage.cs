// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// Represents an invocation that has completed. If there is an error then the invocation didn't complete successfully.
    /// </summary>
    public class CompletionMessage : HubInvocationMessage
    {
        /// <summary>
        /// Optional error message if the invocation wasn't completed successfully. This must be null if there is a result.
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// Optional result from the invocation. This must be null if there is an error.
        /// This can also be null if there wasn't a result from the method invocation.
        /// </summary>
        public object? Result { get; }

        /// <summary>
        /// Specifies whether the completion contains a result.
        /// </summary>
        public bool HasResult { get; }

        /// <summary>
        /// Constructs a <see cref="CompletionMessage"/>.
        /// </summary>
        /// <param name="invocationId">The ID of the invocation that has completed.</param>
        /// <param name="error">An optional error if the invocation failed.</param>
        /// <param name="result">An optional result if the invocation returns a result.</param>
        /// <param name="hasResult">Specifies whether the completion contains a result.</param>
        public CompletionMessage(string invocationId, string? error, object? result, bool hasResult)
            : base(invocationId)
        {
            if (error != null && result != null)
            {
                throw new ArgumentException($"Expected either '{nameof(error)}' or '{nameof(result)}' to be provided, but not both");
            }

            Error = error;
            Result = result;
            HasResult = hasResult;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var errorStr = Error == null ? "<<null>>" : $"\"{Error}\"";
            var resultField = HasResult ? $", {nameof(Result)}: {Result ?? "<<null>>"}" : string.Empty;
            return $"Completion {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Error)}: {errorStr}{resultField} }}";
        }

        // Static factory methods. Don't want to use constructor overloading because it will break down
        // if you need to send a payload statically-typed as a string. And because a static factory is clearer here
        /// <summary>
        /// Constructs a <see cref="CompletionMessage"/> with an error.
        /// </summary>
        /// <param name="invocationId">The ID of the invocation that is being completed.</param>
        /// <param name="error">The error that occurred during the invocation.</param>
        /// <returns>The constructed <see cref="CompletionMessage"/>.</returns>
        public static CompletionMessage WithError(string invocationId, string? error)
            => new CompletionMessage(invocationId, error, result: null, hasResult: false);

        /// <summary>
        /// Constructs a <see cref="CompletionMessage"/> with a result.
        /// </summary>
        /// <param name="invocationId">The ID of the invocation that is being completed.</param>
        /// <param name="payload">The result from the invocation.</param>
        /// <returns>The constructed <see cref="CompletionMessage"/>.</returns>
        public static CompletionMessage WithResult(string invocationId, object? payload)
            => new CompletionMessage(invocationId, error: null, result: payload, hasResult: true);

        /// <summary>
        /// Constructs a <see cref="CompletionMessage"/> without an error or result.
        /// This means the invocation was successful but there is no return value.
        /// </summary>
        /// <param name="invocationId">The ID of the invocation that is being completed.</param>
        /// <returns>The constructed <see cref="CompletionMessage"/>.</returns>
        public static CompletionMessage Empty(string invocationId)
            => new CompletionMessage(invocationId, error: null, result: null, hasResult: false);
    }
}
