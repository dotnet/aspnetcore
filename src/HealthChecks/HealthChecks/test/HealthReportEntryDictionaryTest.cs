// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

#nullable enable

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public class HealthReportEntryDictionaryTest
{
    [Fact]
    public void Constructor_GenerateDictionaryWithDuplicates()
    {
        // Arrange
        // Act
        var result = new HealthReportEntryDictionary(new List<KeyValuePair<string, HealthReportEntry>>()
        {
            new KeyValuePair<string, HealthReportEntry>("Foo", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Foo", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Bar", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quack", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
        });

        // Assert
        Assert.Collection(
            result.OrderBy(kvp => kvp.Key),
            actual =>
            {
                Assert.Equal("Bar", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Equal(actual.Value.Duration, TimeSpan.MinValue);
            },
            actual =>
            {
                Assert.Equal("Foo", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Equal(actual.Value.Duration, TimeSpan.MinValue);
            },
            actual =>
            {
                Assert.Equal("Foo", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Equal(actual.Value.Duration, TimeSpan.MinValue);
            },
            actual =>
            {
                Assert.Equal("Quack", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Equal(actual.Value.Duration, TimeSpan.MinValue);
            },
            actual =>
            {
                Assert.Equal("Quick", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Equal(actual.Value.Duration, TimeSpan.MinValue);
            },
            actual =>
            {
                Assert.Equal("Quick", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Equal(actual.Value.Duration, TimeSpan.MinValue);
            },
            actual =>
            {
                Assert.Equal("Quick", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Equal(actual.Value.Duration, TimeSpan.MinValue);
            });
    }

    [Fact]
    public void Indexer()
    {
        // Arrange
        var first = new KeyValuePair<string, HealthReportEntry>("Foo", new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.MinValue, null, null));
        var healthReportEntryDictionary = new HealthReportEntryDictionary(new List<KeyValuePair<string, HealthReportEntry>>()
        {
            first,
            new KeyValuePair<string, HealthReportEntry>("Foo", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Bar", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quack", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
        });

        // Act
        var result = healthReportEntryDictionary["Foo"];

        // Assert
        Assert.Equal(result, first.Value);
    }

    [Fact]
    public void ContainsKey()
    {
        // Arrange
        var healthReportEntryDictionary = new HealthReportEntryDictionary(new List<KeyValuePair<string, HealthReportEntry>>()
        {
            new KeyValuePair<string, HealthReportEntry>("Foo", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Foo", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Bar", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quack", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
        });

        // Act
        var result = healthReportEntryDictionary.ContainsKey("Foo");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryGetValue()
    {
        // Arrange
        var first = new KeyValuePair<string, HealthReportEntry>("Foo", new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.MinValue, null, null));
        var healthReportEntryDictionary = new HealthReportEntryDictionary(new List<KeyValuePair<string, HealthReportEntry>>()
        {
            first,
            new KeyValuePair<string, HealthReportEntry>("Foo", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Bar", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quick", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
            new KeyValuePair<string, HealthReportEntry>("Quack", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null)),
        });

        // Act
        var result = healthReportEntryDictionary.TryGetValue("Foo", out var resultOut);

        // Assert
        Assert.True(result);
        Assert.Equal(resultOut, first.Value);
    }

}
