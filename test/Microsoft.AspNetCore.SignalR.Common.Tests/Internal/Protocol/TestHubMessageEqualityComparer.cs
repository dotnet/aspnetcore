// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public class TestHubMessageEqualityComparer : IEqualityComparer<HubMessage>
    {
        public static readonly TestHubMessageEqualityComparer Instance = new TestHubMessageEqualityComparer();

        private TestHubMessageEqualityComparer() { }

        public bool Equals(HubMessage x, HubMessage y)
        {
            if (!string.Equals(x.InvocationId, y.InvocationId, StringComparison.Ordinal))
            {
                return false;
            }

            return InvocationMessagesEqual(x, y) || StreamItemMessagesEqual(x, y) || CompletionMessagesEqual(x, y)
                || StreamCompletionMessagesEqual(x, y) || CancelInvocationMessagesEqual(x, y);
        }

        private bool CompletionMessagesEqual(HubMessage x, HubMessage y)
        {
            return x is CompletionMessage left && y is CompletionMessage right &&
                string.Equals(left.Error, right.Error, StringComparison.Ordinal) &&
                left.HasResult == right.HasResult &&
                (Equals(left.Result, right.Result) || SequenceEqual(left.Result, right.Result));
        }

        private bool StreamCompletionMessagesEqual(HubMessage x, HubMessage y)
        {
            return x is StreamCompletionMessage left && y is StreamCompletionMessage right &&
                string.Equals(left.Error, right.Error, StringComparison.Ordinal);
        }

        private bool StreamItemMessagesEqual(HubMessage x, HubMessage y)
        {
            return x is StreamItemMessage left && y is StreamItemMessage right &&
                (Equals(left.Item, right.Item) || SequenceEqual(left.Item, right.Item));
        }

        private bool InvocationMessagesEqual(HubMessage x, HubMessage y)
        {
            return x is InvocationMessage left && y is InvocationMessage right &&
                string.Equals(left.Target, right.Target, StringComparison.Ordinal) &&
                ArgumentListsEqual(left.Arguments, right.Arguments) &&
                left.NonBlocking == right.NonBlocking;
        }

        private bool CancelInvocationMessagesEqual(HubMessage x, HubMessage y)
        {
            return x is CancelInvocationMessage && y is CancelInvocationMessage;
        }

        private bool ArgumentListsEqual(object[] left, object[] right)
        {
            if (left == right)
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (!(Equals(left[i], right[i]) || SequenceEqual(left[i], right[i])))
                {
                    return false;
                }
            }
            return true;
        }

        private bool SequenceEqual(object left, object right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            var leftEnumerable = left as IEnumerable;
            var rightEnumerable = right as IEnumerable;
            if (leftEnumerable == null || rightEnumerable == null)
            {
                return false;
            }

            var leftEnumerator = leftEnumerable.GetEnumerator();
            var rightEnumerator = rightEnumerable.GetEnumerator();
            var leftMoved = leftEnumerator.MoveNext();
            var rightMoved = rightEnumerator.MoveNext();
            for (; leftMoved && rightMoved; leftMoved = leftEnumerator.MoveNext(), rightMoved = rightEnumerator.MoveNext())
            {
                if (!Equals(leftEnumerator.Current, rightEnumerator.Current))
                {
                    return false;
                }
            }

            return !leftMoved && !rightMoved;
        }

        public int GetHashCode(HubMessage obj)
        {
            // We never use these in a hash-table
            return 0;
        }
    }
}
