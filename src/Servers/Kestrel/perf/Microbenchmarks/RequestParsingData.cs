// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class RequestParsingData
{
    public const int InnerLoopCount = 512;

    public const int Pipelining = 16;

    private const string _plaintextTechEmpowerRequest =
        "GET /plaintext HTTP/1.1\r\n" +
        "Host: localhost\r\n" +
        "Accept: text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7\r\n" +
        "Connection: keep-alive\r\n" +
        "\r\n";

    private const string _jsonTechEmpowerRequest =
        "GET /json HTTP/1.1\r\n" +
        "Host: localhost\r\n" +
        "Accept: Accept:application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7\r\n" +
        "Connection: keep-alive\r\n" +
        "\r\n";

    // edge-casey - client's don't normally send this
    private const string _plaintextAbsoluteUriRequest =
        "GET http://localhost/plaintext HTTP/1.1\r\n" +
        "Host: localhost\r\n" +
        "Accept: text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7\r\n" +
        "Connection: keep-alive\r\n" +
        "\r\n";

    private const string _liveaspnetRequest =
        "GET / HTTP/1.1\r\n" +
        "Host: live.asp.net\r\n" +
        "Connection: keep-alive\r\n" +
        "Upgrade-Insecure-Requests: 1\r\n" +
        "User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36\r\n" +
        "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8\r\n" +
        "DNT: 1\r\n" +
        "Accept-Encoding: gzip, deflate, sdch, br\r\n" +
        "Accept-Language: en-US,en;q=0.8\r\n" +
        "Cookie: __unam=7a67379-1s65dc575c4-6d778abe-1; omniID=9519gfde_3347_4762_8762_df51458c8ec2\r\n" +
        "\r\n";

    private const string _unicodeRequest =
        "GET /questions/40148683/why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric HTTP/1.1\r\n" +
        "Accept: text/html, application/xhtml+xml, image/jxr, */*\r\n" +
        "Accept-Language: en-US,en-GB;q=0.7,en;q=0.3\r\n" +
        "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.14965\r\n" +
        "Accept-Encoding: gzip, deflate\r\n" +
        "Host: stackoverflow.com\r\n" +
        "Connection: Keep-Alive\r\n" +
        "Cache-Control: max-age=0\r\n" +
        "Upgrade-Insecure-Requests: 1\r\n" +
        "DNT: 1\r\n" +
        "Referer: http://stackoverflow.com/?tab=month\r\n" +
        "Pragma: no-cache\r\n" +
        "Cookie: prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric\r\n" +
        "\r\n";

    public static readonly byte[] PlaintextTechEmpowerPipelinedRequests = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat(_plaintextTechEmpowerRequest, Pipelining)));
    public static readonly byte[] PlaintextTechEmpowerRequest = Encoding.ASCII.GetBytes(_plaintextTechEmpowerRequest);

    public static readonly byte[] JsonTechEmpowerRequest = Encoding.ASCII.GetBytes(_jsonTechEmpowerRequest);

    public static readonly byte[] PlaintextAbsoluteUriRequest = Encoding.ASCII.GetBytes(_plaintextAbsoluteUriRequest);

    public static readonly byte[] LiveaspnetPipelinedRequests = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat(_liveaspnetRequest, Pipelining)));
    public static readonly byte[] LiveaspnetRequest = Encoding.ASCII.GetBytes(_liveaspnetRequest);

    public static readonly byte[] UnicodePipelinedRequests = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat(_unicodeRequest, Pipelining)));
    public static readonly byte[] UnicodeRequest = Encoding.ASCII.GetBytes(_unicodeRequest);
}
