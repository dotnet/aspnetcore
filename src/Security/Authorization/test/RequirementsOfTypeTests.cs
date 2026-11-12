// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.AspNetCore.Authorization.Test;

public class RequirementsOfTypeTests
{
    public static TheoryData<string> CollectionKinds => new() { "ReadOnlyCollection", "List", "Array", "Enumerable" };

    [Theory]
    [MemberData(nameof(CollectionKinds))]
    public void EmptyRequirements_YieldsNothing(string kind)
    {
        var requirements = CreateRequirements(kind);

        Assert.Empty(Enumerate<MatchingRequirement>(requirements));
    }

    [Theory]
    [MemberData(nameof(CollectionKinds))]
    public void SingleMatchingRequirement_YieldsIt(string kind)
    {
        var matching = new MatchingRequirement();
        var requirements = CreateRequirements(kind, matching);

        Assert.Same(matching, Assert.Single(Enumerate<MatchingRequirement>(requirements)));
    }

    [Theory]
    [MemberData(nameof(CollectionKinds))]
    public void SingleNonMatchingRequirement_YieldsNothing(string kind)
    {
        var requirements = CreateRequirements(kind, new OtherRequirement());

        Assert.Empty(Enumerate<MatchingRequirement>(requirements));
    }

    [Theory]
    [MemberData(nameof(CollectionKinds))]
    public void OnlyNonMatchingRequirements_YieldsNothing(string kind)
    {
        var requirements = CreateRequirements(kind, new OtherRequirement(), new OtherRequirement(), new OtherRequirement());

        Assert.Empty(Enumerate<MatchingRequirement>(requirements));
    }

    [Theory]
    [MemberData(nameof(CollectionKinds))]
    public void ManyMixedRequirements_YieldsMatchesInOrderLikeOfType(string kind)
    {
        var first = new MatchingRequirement();
        var second = new MatchingRequirement();
        var derived = new DerivedMatchingRequirement();
        var requirements = CreateRequirements(
            kind,
            new OtherRequirement(),
            first,
            null,
            new OtherRequirement(),
            second,
            first,
            derived,
            new OtherRequirement());

        var result = Enumerate<MatchingRequirement>(requirements);

        Assert.Equal(new MatchingRequirement[] { first, second, first, derived }, result);
        Assert.Equal(requirements.OfType<MatchingRequirement>(), result);
    }

    [Theory]
    [MemberData(nameof(CollectionKinds))]
    public void FiltersByInterfaceNotImplementingIAuthorizationRequirement(string kind)
    {
        var selfHandling = new SelfHandlingRequirement();
        var requirements = CreateRequirements(kind, new OtherRequirement(), selfHandling, new MatchingRequirement());

        Assert.Same(selfHandling, Assert.Single(Enumerate<IAuthorizationHandler>(requirements)));
    }

    [Fact]
    public void ReadOnlyCollection_DoesNotCallGetEnumerator()
    {
        var matching = new MatchingRequirement();
        var inner = new TrackingList(new IAuthorizationRequirement[] { new OtherRequirement(), matching });
        var requirements = new ReadOnlyCollection<IAuthorizationRequirement>(inner);

        var result = Enumerate<MatchingRequirement>(requirements);

        Assert.Same(matching, Assert.Single(result));
        Assert.Equal(0, inner.GetEnumeratorCalls);
    }

    [Fact]
    public void NonReadOnlyCollection_EnumeratesLazilyAndDisposesSourceEnumerator()
    {
        var matching = new MatchingRequirement();
        var requirements = new TrackingEnumerable(new IAuthorizationRequirement[] { matching, new OtherRequirement(), new MatchingRequirement() });

        foreach (var requirement in new RequirementsOfType<MatchingRequirement>(requirements))
        {
            Assert.Same(matching, requirement);
            break;
        }

        Assert.Equal(1, requirements.MoveNextCalls);
        Assert.True(requirements.Disposed);
    }

    private static List<T> Enumerate<T>(IEnumerable<IAuthorizationRequirement> requirements)
    {
        var result = new List<T>();
        foreach (var requirement in new RequirementsOfType<T>(requirements))
        {
            result.Add(requirement);
        }
        return result;
    }

    private static IEnumerable<IAuthorizationRequirement> CreateRequirements(string kind, params IAuthorizationRequirement[] requirements)
        => kind switch
        {
            "ReadOnlyCollection" => new List<IAuthorizationRequirement>(requirements).AsReadOnly(),
            "List" => new List<IAuthorizationRequirement>(requirements),
            "Array" => requirements,
            "Enumerable" => requirements.Select(r => r),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private class MatchingRequirement : IAuthorizationRequirement
    {
    }

    private sealed class DerivedMatchingRequirement : MatchingRequirement
    {
    }

    private sealed class OtherRequirement : IAuthorizationRequirement
    {
    }

    private sealed class SelfHandlingRequirement : IAuthorizationRequirement, IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context) => Task.CompletedTask;
    }

    private sealed class TrackingList(IList<IAuthorizationRequirement> inner) : Collection<IAuthorizationRequirement>(inner), IEnumerable<IAuthorizationRequirement>
    {
        public int GetEnumeratorCalls { get; private set; }

        IEnumerator<IAuthorizationRequirement> IEnumerable<IAuthorizationRequirement>.GetEnumerator()
        {
            GetEnumeratorCalls++;
            return base.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            GetEnumeratorCalls++;
            return base.GetEnumerator();
        }
    }

    private sealed class TrackingEnumerable(IAuthorizationRequirement[] items) : IEnumerable<IAuthorizationRequirement>
    {
        public int MoveNextCalls { get; private set; }

        public bool Disposed { get; private set; }

        public IEnumerator<IAuthorizationRequirement> GetEnumerator()
        {
            try
            {
                foreach (var item in items)
                {
                    MoveNextCalls++;
                    yield return item;
                }
            }
            finally
            {
                Disposed = true;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
