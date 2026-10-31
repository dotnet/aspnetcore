// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

public class TestHubMessageEqualityComparer : IEqualityComparer<HubMessage>
{
    public static readonly TestHubMessageEqualityComparer Instance = new TestHubMessageEqualityComparer();

    private TestHubMessageEqualityComparer() { }

    public bool Equals(HubMessage x, HubMessage y)
    {
        // Types should be equal
        if (!Equals(x.GetType(), y.GetType()))
        {
            return false;
        }

        switch (x)
        {
            case InvocationMessage invocationMessage:
                return InvocationMessagesEqual(invocationMessage, (InvocationMessage)y);
            case StreamItemMessage streamItemMessage:
                return StreamItemMessagesEqual(streamItemMessage, (StreamItemMessage)y);
            case CompletionMessage completionMessage:
                return CompletionMessagesEqual(completionMessage, (CompletionMessage)y);
            case StreamInvocationMessage streamInvocationMessage:
                return StreamInvocationMessagesEqual(streamInvocationMessage, (StreamInvocationMessage)y);
            case CancelInvocationMessage cancelItemMessage:
                return string.Equals(cancelItemMessage.InvocationId, ((CancelInvocationMessage)y).InvocationId, StringComparison.Ordinal);
            case PingMessage _:
                // If the types are equal (above), then we're done.
                return true;
            case CloseMessage closeMessage:
                return string.Equals(closeMessage.Error, ((CloseMessage)y).Error);
            case AckMessage ackMessage:
                return ackMessage.SequenceId == ((AckMessage)y).SequenceId;
            case SequenceMessage sequenceMessage:
                return sequenceMessage.SequenceId == ((SequenceMessage)y).SequenceId;
            default:
                throw new InvalidOperationException($"Unknown message type: {x.GetType().FullName}");
        }
    }

    private bool CompletionMessagesEqual(CompletionMessage x, CompletionMessage y)
    {
        return SequenceEqual(x.Headers, y.Headers)
            && string.Equals(x.InvocationId, y.InvocationId, StringComparison.Ordinal)
            && string.Equals(x.Error, y.Error, StringComparison.Ordinal)
            && x.HasResult == y.HasResult
            && (Equals(x.Result, y.Result) || SequenceEqual(x.Result, y.Result));
    }

    private bool StreamItemMessagesEqual(StreamItemMessage x, StreamItemMessage y)
    {
        return SequenceEqual(x.Headers, y.Headers)
            && string.Equals(x.InvocationId, y.InvocationId, StringComparison.Ordinal)
            && (Equals(x.Item, y.Item) || SequenceEqual(x.Item, y.Item));
    }

    private bool InvocationMessagesEqual(InvocationMessage x, InvocationMessage y)
    {
        return SequenceEqual(x.Headers, y.Headers)
            && string.Equals(x.InvocationId, y.InvocationId, StringComparison.Ordinal)
            && string.Equals(x.Target, y.Target, StringComparison.Ordinal)
            && ArgumentListsEqual(x.Arguments, y.Arguments)
            && StringArrayEqual(x.StreamIds, y.StreamIds);
    }

    private bool StreamInvocationMessagesEqual(StreamInvocationMessage x, StreamInvocationMessage y)
    {
        return SequenceEqual(x.Headers, y.Headers)
            && string.Equals(x.InvocationId, y.InvocationId, StringComparison.Ordinal)
            && string.Equals(x.Target, y.Target, StringComparison.Ordinal)
            && ArgumentListsEqual(x.Arguments, y.Arguments)
            && StringArrayEqual(x.StreamIds, y.StreamIds);
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

    private bool StringArrayEqual(string[] left, string[] right)
    {
        if (left == null && right == null)
        {
            return true;
        }

        if (left == null || right == null)
        {
            return false;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!string.Equals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(HubMessage obj)
    {
        // We never use these in a hash-table
        return 0;
    }
}
