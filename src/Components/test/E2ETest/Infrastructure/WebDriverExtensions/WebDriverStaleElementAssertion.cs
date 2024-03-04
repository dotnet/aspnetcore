// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure;

internal static class WebDriverStaleElementAssertion
{
    public static void AssertThrowsDueToElementRemovedFromPage<T>(Func<T> callback)
        => AssertThrowsDueToElementRemovedFromPage(() => callback());

    public static void AssertThrowsDueToElementRemovedFromPage(Action callback)
    {
        var ex = Assert.Throws<Exception>(callback);
        if (!ExceptionMeansElementWasRemoved(ex))
        {
            throw ex;
        }
    }

    public static bool ExceptionMeansElementWasRemoved(Exception ex)
    {
        if (ex is StaleElementReferenceException)
        {
            // This is the normal exception that occurs if you accessed something
            // that was already removed from the page
            return true;
        }
        else if (ex is WebDriverException)
        {
            // Sometimes we get this exception instead if the element is stale
            // It may depend on timing
            if (ex.Message.Contains("Node with given id does not belong to the document", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
