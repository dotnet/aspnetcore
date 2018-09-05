// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import com.microsoft.aspnet.signalr.Negotiate;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.junit.runners.Parameterized;

import java.util.Arrays;
import java.util.Collection;

import static org.junit.Assert.assertEquals;

@RunWith(Parameterized.class)
public class ResolveNegotiateUrlTest {
    private String url;
    private String resolvedUrl;

    public ResolveNegotiateUrlTest(String url, String resolvedUrl) {
        this.url = url;
        this.resolvedUrl = resolvedUrl;
    }

    @Parameterized.Parameters
    public static Collection protocols() {
        return Arrays.asList(new String[][]{
                {"http://example.com/hub/", "http://example.com/hub/negotiate"},
                {"http://example.com/hub", "http://example.com/hub/negotiate"},
                {"http://example.com/endpoint?q=my/Data", "http://example.com/endpoint/negotiate?q=my/Data"},
                {"http://example.com/endpoint/?q=my/Data", "http://example.com/endpoint/negotiate?q=my/Data"},
                {"http://example.com/endpoint/path/more?q=my/Data", "http://example.com/endpoint/path/more/negotiate?q=my/Data"},});
    }

    @Test
    public void checkNegotiateUrl() {
        String urlResult = Negotiate.resolveNegotiateUrl(this.url);
        assertEquals(this.resolvedUrl, urlResult);
    }
}