// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for licenseProjectTemplates.Helpers information.

using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

namespace ProjectTemplates.Helpers
{
    public static class AspNetProcessExtensions
    {
        public static void VisitInBrowser(this AspNetProcess process, IWebDriver driver)
        {
            process.Output.WriteLine($"Opening browser at {process.ListeningUri}...");
            driver.Navigate().GoToUrl(process.ListeningUri);

            if (driver is EdgeDriver)
            {
                // Workaround for untrusted ASP.NET Core development certificates.
                // The edge driver doesn't supported skipping the SSL warning page.

                if (driver.Title.Contains("Certificate error", StringComparison.OrdinalIgnoreCase))
                {
                    process.Output.WriteLine("Page contains certificate error. Attempting to get around this...");
                    driver.FindElement(By.Id("moreInformationDropdownSpan")).Click();
                    var continueLink = driver.FindElement(By.Id("invalidcert_continue"));
                    if (continueLink != null)
                    {
                        process.Output.WriteLine($"Clicking on link '{continueLink.Text}' to skip invalid certificate error page.");
                        continueLink.Click();
                        driver.Navigate().GoToUrl(process.ListeningUri);
                    }
                    else
                    {
                        process.Output.WriteLine("Could not find link to skip certificate error page.");
                    }
                }
            }
        }
    }
}
