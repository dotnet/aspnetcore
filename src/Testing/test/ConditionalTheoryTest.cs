// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.InternalTesting;

[TestCaseOrderer("Microsoft.AspNetCore.InternalTesting.AlphabeticalOrderer", "Microsoft.AspNetCore.InternalTesting.Tests")]
public class ConditionalTheoryTest : IClassFixture<ConditionalTheoryTest.ConditionalTheoryAsserter>
{
    public ConditionalTheoryTest(ConditionalTheoryAsserter asserter)
    {
        Asserter = asserter;
    }

    public ConditionalTheoryAsserter Asserter { get; }

    [ConditionalTheory(Skip = "Test is always skipped.")]
    [InlineData(0)]
    public void ConditionalTheorySkip(int arg)
    {
        Assert.True(false, "This test should always be skipped.");
    }

    private static int _conditionalTheoryRuns = 0;

    [ConditionalTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2, Skip = "Skip these data")]
    public void ConditionalTheoryRunOncePerDataLine(int arg)
    {
        _conditionalTheoryRuns++;
        Assert.True(_conditionalTheoryRuns <= 2, $"Theory should run 2 times, but ran {_conditionalTheoryRuns} times.");
    }

    [ConditionalTheory, Trait("Color", "Blue")]
    [InlineData(1)]
    public void ConditionalTheoriesShouldPreserveTraits(int arg)
    {
        Assert.True(true);
    }

    [ConditionalTheory(Skip = "Skip this")]
    [MemberData(nameof(GetInts))]
    public void ConditionalTheoriesWithSkippedMemberData(int arg)
    {
        Assert.True(false, "This should never run");
    }

    private static int _conditionalMemberDataRuns = 0;

    [ConditionalTheory]
    [InlineData(4)]
    [MemberData(nameof(GetInts))]
    public void ConditionalTheoriesWithMemberData(int arg)
    {
        _conditionalMemberDataRuns++;
        Assert.True(_conditionalTheoryRuns <= 3, $"Theory should run 2 times, but ran {_conditionalMemberDataRuns} times.");
    }

    public static TheoryData<int> GetInts
        => new TheoryData<int> { 0, 1 };

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Windows)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    [OSSkipCondition(OperatingSystems.Linux)]
    [MemberData(nameof(GetActionTestData))]
    public void ConditionalTheoryWithFuncs(Func<int, int> func)
    {
        Assert.True(false, "This should never run");
    }

    [Fact]
    public void TestAlwaysRun()
    {
        // This is required to ensure that this type at least gets initialized.
        Assert.True(true);
    }

#if NETCOREAPP
    [ConditionalTheory]
    [FrameworkSkipCondition(RuntimeFrameworks.CLR)]
    [MemberData(nameof(GetInts))]
    public void ThisTestMustRunOnCoreCLR(int value)
    {
        Asserter.TestRan = true;
    }
#elif NET472
    [ConditionalTheory]
    [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
    [MemberData(nameof(GetInts))]
    public void ThisTestMustRunOnCLR(int value)
    {
        Asserter.TestRan = true;
    }
#else
#error Target frameworks need to be updated.
#endif

    // Test is named this way to be the lowest test in the alphabet, it relies on test ordering
    [Fact]
    public void ZzzzzzzEnsureThisIsTheLastTest()
    {
        Assert.True(Asserter.TestRan);
    }

    public static TheoryData<Func<int, int>> GetActionTestData
        => new TheoryData<Func<int, int>>
        {
            (i) => i * 1
        };

    public class ConditionalTheoryAsserter : IDisposable
    {
        public bool TestRan { get; set; }

        public void Dispose()
        {
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(SkippableData))]
    public void WithSkipableData(Skippable skippable)
    {
        Assert.Null(skippable.Skip);
        Assert.Equal(1, skippable.Data);
    }

    public static TheoryData<Skippable> SkippableData => new TheoryData<Skippable>
    {
        new Skippable() { Data = 1 },
        new Skippable() { Data = 2, Skip = "This row should be skipped." }
    };

    public class Skippable : IXunitSerializable
    {
        public Skippable() { }
        public int Data { get; set; }
        public string Skip { get; set; }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Data), Data, typeof(int));
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Data = info.GetValue<int>(nameof(Data));
        }

        public override string ToString()
        {
            return Data.ToString(CultureInfo.InvariantCulture);
        }
    }
}
