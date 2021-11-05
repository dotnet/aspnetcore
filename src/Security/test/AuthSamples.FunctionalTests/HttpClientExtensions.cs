// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Xunit;

namespace AuthSamples.FunctionalTests;

public static class HttpClientExtensions
{
    // Copied from Identity functionals
    public static Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        IHtmlFormElement form,
        IEnumerable<KeyValuePair<string, string>> formValues)
    {
        var submitElement = Assert.Single(form.QuerySelectorAll("[type=submit]"));
        var submitButton = Assert.IsAssignableFrom<IHtmlElement>(submitElement);

        return client.SendAsync(form, submitButton, formValues);
    }

    // Copied from Identity functionals
    public static Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        IHtmlFormElement form,
        IHtmlElement submitButton,
        IEnumerable<KeyValuePair<string, string>> formValues)
    {
        foreach (var kvp in formValues)
        {
            var element = Assert.IsAssignableFrom<IHtmlInputElement>(form[kvp.Key]);
            element.Value = kvp.Value;
        }

        var submit = form.GetSubmission(submitButton);
        var target = (Uri)submit.Target;
        if (submitButton.HasAttribute("formaction"))
        {
            var formaction = submitButton.GetAttribute("formaction");
            target = new Uri(formaction, UriKind.Relative);
        }
        var submision = new HttpRequestMessage(new HttpMethod(submit.Method.ToString()), target)
        {
            Content = new StreamContent(submit.Body)
        };

        foreach (var header in submit.Headers)
        {
            if (!submision.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                submision.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return client.SendAsync(submision);
    }
}
