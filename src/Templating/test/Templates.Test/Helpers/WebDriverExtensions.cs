using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace Templates.Test.Helpers
{
    public static class WebDriverExtensions
    {
        public static string GetText(this ISearchContext driver, string cssSelector)
        {
            return driver.FindElement(By.CssSelector(cssSelector)).Text;
        }

        public static void Click(this IWebDriver driver, By by)
        {
            Click(driver, null, by);
        }

        public static void Click(this IWebDriver driver, ISearchContext searchContext, By by)
        {
            // This elaborate way of clicking is a workaround for https://developer.microsoft.com/en-us/microsoft-edge/platform/issues/5238133/
            new Actions(driver)
                .MoveToElement((searchContext ?? driver).FindElement(by))
                .Click()
                .Perform();
        }

        public static void Click(this IWebDriver driver, string cssSelector)
        {
            Click(driver, null, cssSelector);
        }

        public static void Click(this IWebDriver driver, ISearchContext searchContext, string cssSelector)
        {
            Click(driver, searchContext, By.CssSelector(cssSelector));
        }

        public static IWebElement FindElement(this ISearchContext searchContext, string cssSelector)
        {
            return searchContext.FindElement(By.CssSelector(cssSelector));
        }

        public static IWebElement Parent(this IWebElement webElement)
        {
            return webElement.FindElement(By.XPath(".."));
        }

        public static IWebElement FindElement(this IWebDriver driver, string cssSelector, int timeoutSeconds)
        {
            return FindElement(driver, null, cssSelector, timeoutSeconds);
        }

        public static IWebElement FindElement(this IWebDriver driver, ISearchContext searchContext, string cssSelector, int timeoutSeconds)
        {
            return FindElement(driver, searchContext, By.CssSelector(cssSelector), timeoutSeconds);
        }

        public static IWebElement FindElement(this IWebDriver driver, By by, int timeoutSeconds)
        {
            return FindElement(driver, null, by, timeoutSeconds);
        }

        public static IWebElement FindElement(this IWebDriver driver, ISearchContext searchContext, By by, int timeoutSeconds)
        {
            return new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds))
                .Until(drv => searchContext.FindElement(by));
        }

        public static void WaitForUrl(this IWebDriver browser, string expectedUrl)
        {
            new WebDriverWait(browser, TimeSpan.FromSeconds(WebDriverFactory.DefaultMaxWaitTimeInSeconds))
                .Until(driver => driver.Url.Contains(expectedUrl, StringComparison.OrdinalIgnoreCase));
        }

        public static void WaitForElement(this IWebDriver browser, string expectedElementCss)
        {
            new WebDriverWait(browser, TimeSpan.FromSeconds(WebDriverFactory.DefaultMaxWaitTimeInSeconds))
                .Until(driver => driver.FindElements(By.CssSelector(expectedElementCss)).Count > 0);
        }

        public static void WaitForText(this IWebDriver browser, string cssSelector, string expectedText)
        {
            new WebDriverWait(browser, TimeSpan.FromSeconds(WebDriverFactory.DefaultMaxWaitTimeInSeconds))
                .Until(driver => {
                    try
                    {
                        var matchingElement = driver.FindElements(By.CssSelector(cssSelector)).FirstOrDefault();
                        return matchingElement?.Text == expectedText;
                    }
                    catch (Exception) // We can get a "stale element" exception if the DOM mutates while we're holding a reference to its element
                    {
                        return false;
                    }
                });
        }
    }
}
