// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            IHtmlFormElement form,
            IEnumerable<KeyValuePair<string,string>> formValues)
        {
            foreach (var kvp in formValues)
            {
                var element = Assert.IsAssignableFrom<IHtmlInputElement>(form[kvp.Key]);
                element.Value = kvp.Value;
            }

            var submitElement = Assert.Single(form.QuerySelectorAll("[type=submit]"));
            var submitButton = Assert.IsAssignableFrom<IHtmlElement>(submitElement);

            var submit = form.GetSubmission(submitButton);
            var submision = new HttpRequestMessage(new HttpMethod(submit.Method.ToString()), submit.Target)
            {
                Content = new StreamContent(submit.Body)
            };

            foreach (var header in submit.Headers)
            {
                submision.Headers.TryAddWithoutValidation(header.Key, header.Value);
                submision.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return client.SendAsync(submision);
        }
    }
}
