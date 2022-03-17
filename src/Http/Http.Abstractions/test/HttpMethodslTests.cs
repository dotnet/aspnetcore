// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Http.Abstractions;

public class HttpMethodslTests
{
    [Fact]
    public void CanonicalizedValue_Success()
    {
        var testCases = new List<(string[] methods, string expectedMethod)>
            {
                (new string[] { "GET", "Get", "get" }, HttpMethods.Get),
                (new string[] { "POST", "Post", "post" }, HttpMethods.Post),
                (new string[] { "PUT", "Put", "put" }, HttpMethods.Put),
                (new string[] { "DELETE", "Delete", "delete" }, HttpMethods.Delete),
                (new string[] { "HEAD", "Head", "head" }, HttpMethods.Head),
                (new string[] { "CONNECT", "Connect", "connect" }, HttpMethods.Connect),
                (new string[] { "OPTIONS", "Options", "options" }, HttpMethods.Options),
                (new string[] { "PATCH", "Patch", "patch" }, HttpMethods.Patch),
                (new string[] { "TRACE", "Trace", "trace" }, HttpMethods.Trace)
            };

        for (int i = 0; i < testCases.Count; i++)
        {
            var testCase = testCases[i];
            for (int j = 0; j < testCase.methods.Length; j++)
            {
                CanonicalizedValueTest(testCase.methods[j], testCase.expectedMethod);
            }
        }
    }

    private void CanonicalizedValueTest(string method, string expectedMethod)
    {
        string inputMethod = CreateStringAtRuntime(method);
        var canonicalizedValue = HttpMethods.GetCanonicalizedValue(inputMethod);

        Assert.Same(expectedMethod, canonicalizedValue);
    }

    private string CreateStringAtRuntime(string input)
    {
        return new StringBuilder(input).ToString();
    }
}
