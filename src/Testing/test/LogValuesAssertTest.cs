// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing.Tests;

public class LogValuesAssertTest
{
    public static TheoryData<
        IEnumerable<KeyValuePair<string, object>>,
        IEnumerable<KeyValuePair<string, object>>> ExpectedValues_SubsetOf_ActualValuesData
    {
        get
        {
            return new TheoryData<
                IEnumerable<KeyValuePair<string, object>>,
                IEnumerable<KeyValuePair<string, object>>>()
                {
                    {
                        new KeyValuePair<string,object>[] { },
                        new KeyValuePair<string,object>[] { }
                    },
                    {
                        // subset
                        new KeyValuePair<string,object>[] { },
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                            new KeyValuePair<string, object>("RouteKey", "id")
                        }
                    },
                    {
                        // subset
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                            new KeyValuePair<string, object>("RouteKey", "id")
                        },
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                            new KeyValuePair<string, object>("RouteKey", "id"),
                            new KeyValuePair<string, object>("RouteConstraint", "Something")
                        }
                    },
                    {
                        // equal number of values
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                            new KeyValuePair<string, object>("RouteKey", "id")
                        },
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                            new KeyValuePair<string, object>("RouteKey", "id"),
                        }
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(ExpectedValues_SubsetOf_ActualValuesData))]
    public void Asserts_Success_ExpectedValues_SubsetOf_ActualValues(
        IEnumerable<KeyValuePair<string, object>> expectedValues,
        IEnumerable<KeyValuePair<string, object>> actualValues)
    {
        // Act && Assert
        LogValuesAssert.Contains(expectedValues, actualValues);
    }

    public static TheoryData<
        IEnumerable<KeyValuePair<string, object>>,
        IEnumerable<KeyValuePair<string, object>>> ExpectedValues_MoreThan_ActualValuesData
    {
        get
        {
            return new TheoryData<
                IEnumerable<KeyValuePair<string, object>>,
                IEnumerable<KeyValuePair<string, object>>>()
                {
                    {
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                            new KeyValuePair<string, object>("RouteKey", "id")
                        },
                        new KeyValuePair<string,object>[] { }
                    },
                    {
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                            new KeyValuePair<string, object>("RouteKey", "id"),
                            new KeyValuePair<string, object>("RouteConstraint", "Something")
                        },
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                            new KeyValuePair<string, object>("RouteKey", "id")
                        }
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(ExpectedValues_MoreThan_ActualValuesData))]
    public void Asserts_Failure_ExpectedValues_MoreThan_ActualValues(
        IEnumerable<KeyValuePair<string, object>> expectedValues,
        IEnumerable<KeyValuePair<string, object>> actualValues)
    {
        // Act && Assert
        var equalException = Assert.Throws<EqualException>(
            () => LogValuesAssert.Contains(expectedValues, actualValues));

        Assert.Equal(GetString(expectedValues), equalException.Expected);
        Assert.Equal(GetString(actualValues), equalException.Actual);
    }

    [Fact]
    public void Asserts_Success_IgnoringOrderOfItems()
    {
        // Arrange
        var expectedLogValues = new[]
        {
                new KeyValuePair<string, object>("RouteConstraint", "Something"),
                new KeyValuePair<string, object>("RouteValue", "Failure"),
                new KeyValuePair<string, object>("RouteKey", "id")
            };
        var actualLogValues = new[]
        {
                new KeyValuePair<string, object>("RouteKey", "id"),
                new KeyValuePair<string, object>("RouteConstraint", "Something"),
                new KeyValuePair<string, object>("RouteValue", "Failure"),
            };

        // Act && Assert
        LogValuesAssert.Contains(expectedLogValues, actualLogValues);
    }

    [Fact]
    public void Asserts_Success_OnSpecifiedKeyAndValue()
    {
        // Arrange
        var actualLogValues = new[]
        {
                new KeyValuePair<string, object>("RouteConstraint", "Something"),
                new KeyValuePair<string, object>("RouteKey", "id"),
                new KeyValuePair<string, object>("RouteValue", "Failure"),
            };

        // Act && Assert
        LogValuesAssert.Contains("RouteKey", "id", actualLogValues);
    }

    public static TheoryData<
        IEnumerable<KeyValuePair<string, object>>,
        IEnumerable<KeyValuePair<string, object>>> CaseSensitivityComparisionData
    {
        get
        {
            return new TheoryData<
                IEnumerable<KeyValuePair<string, object>>,
                IEnumerable<KeyValuePair<string, object>>>()
                {
                    {
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteKey", "id"),
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                        },
                        new[]
                        {
                            new KeyValuePair<string, object>("ROUTEKEY", "id"),
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                        }
                    },
                    {
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteKey", "id"),
                            new KeyValuePair<string, object>("RouteValue", "Failure"),
                        },
                        new[]
                        {
                            new KeyValuePair<string, object>("RouteKey", "id"),
                            new KeyValuePair<string, object>("RouteValue", "FAILURE"),
                        }
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(CaseSensitivityComparisionData))]
    public void DefaultComparer_Performs_CaseSensitiveComparision(
        IEnumerable<KeyValuePair<string, object>> expectedValues,
        IEnumerable<KeyValuePair<string, object>> actualValues)
    {
        // Act && Assert
        var equalException = Assert.Throws<EqualException>(
            () => LogValuesAssert.Contains(expectedValues, actualValues));

        Assert.Equal(GetString(expectedValues), equalException.Expected);
        Assert.Equal(GetString(actualValues), equalException.Actual);
    }

    private string GetString(IEnumerable<KeyValuePair<string, object>> logValues)
    {
        return logValues == null ?
            "Null" :
            string.Join(",", logValues.Select(kvp => $"[{kvp.Key} {kvp.Value}]"));
    }
}
