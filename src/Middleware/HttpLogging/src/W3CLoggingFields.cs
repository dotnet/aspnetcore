// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Flags used to control which parts of the
/// request and response are logged in W3C format.
/// </summary>
[Flags]
public enum W3CLoggingFields : long
{
    /// <summary>
    /// No logging.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// Flag for logging the date
    /// that the activity occurred.
    /// </summary>
    Date = 0x1,

    /// <summary>
    /// Flag for logging the time
    /// that the activity occurred.
    /// </summary>
    Time = 0x2,

    /// <summary>
    /// Flag for logging the IP address
    /// of the client that accessed the server.
    /// </summary>
    ClientIpAddress = 0x4,

    /// <summary>
    /// Flag for logging the name of the
    /// authenticated user that accessed the server.
    /// UserName contents can contain private information
    /// which may have regulatory concerns under GDPR
    /// and other laws. UserName should not be logged
    /// unless logs are secure and access controlled
    /// and the privacy impact assessed.
    /// </summary>
    UserName = 0x8,

    /// <summary>
    /// Flag for logging the name of the
    /// server on which the log entry was generated.
    /// </summary>
    ServerName = 0x10,

    /// <summary>
    /// Flag for logging the IP address of the
    /// server on which the log entry was generated.
    /// </summary>
    ServerIpAddress = 0x20,

    /// <summary>
    /// Flag for logging the port number
    /// the client is connected to.
    /// </summary>
    ServerPort = 0x40,

    /// <summary>
    /// Flag for logging the action
    /// the client was trying to perform.
    /// </summary>
    Method = 0x80,

    /// <summary>
    /// Flag for logging the resource accessed.
    /// </summary>
    UriStem = 0x100,

    /// <summary>
    /// Flag for logging the query, if any,
    /// the client was trying to perform.
    /// </summary>
    UriQuery = 0x200,

    /// <summary>
    /// Flag for logging the HTTP response status code.
    /// </summary>
    ProtocolStatus = 0x400,

    /// <summary>
    /// Flag for logging the duration of time,
    /// in milliseconds, that the action consumed.
    /// </summary>
    TimeTaken = 0x800,

    /// <summary>
    /// Flag for logging the protocol (HTTP, FTP) version
    /// used by the client. For HTTP this will be either
    /// HTTP 1.0 or HTTP 1.1.
    /// </summary>
    ProtocolVersion = 0x1000,

    /// <summary>
    /// Flag for logging the content of the host header.
    /// </summary>
    Host = 0x2000,

    /// <summary>
    /// Flag for logging the requesting user agent.
    /// </summary>
    UserAgent = 0x4000,

    /// <summary>
    /// Flag for logging the content of the cookie
    /// sent by the client, if any.
    /// Cookie contents can contain authentication tokens,
    /// or private information which may have regulatory concerns
    /// under GDPR and other laws. Cookies should not be logged
    /// unless logs are secure and access controlled
    /// and the privacy impact assessed.
    /// </summary>
    Cookie = 0x8000,

    /// <summary>
    /// Flag for logging the previous site visited by the user,
    /// which provided a link to the current site, if any.
    /// </summary>
    Referer = 0x10000,

    /// <summary>
    /// Flag for logging properties that are part of the <see cref="ConnectionInfo"/>
    /// Includes <see cref="ClientIpAddress"/>, <see cref="ServerIpAddress"/> and <see cref="ServerPort"/>.
    /// </summary>
    ConnectionInfoFields = ClientIpAddress | ServerIpAddress | ServerPort,

    /// <summary>
    /// Flag for logging properties that are part of the <see cref="HttpRequest.Headers"/>
    /// Includes <see cref="Host"/>, <see cref="Referer"/>, and <see cref="UserAgent"/>.
    /// </summary>
    RequestHeaders = Host | Referer | UserAgent,

    /// <summary>
    /// Flag for logging properties that are part of the <see cref="HttpRequest"/>
    /// Includes <see cref="UriStem"/>, <see cref="UriQuery"/>, <see cref="ProtocolVersion"/>,
    /// <see cref="Method"/>, <see cref="Host"/>, <see cref="Referer"/>,
    /// and <see cref="UserAgent"/>.
    /// </summary>
    Request = UriStem | UriQuery | ProtocolVersion | Method | RequestHeaders,

    /// <summary>
    /// Flag for logging all possible fields.
    /// Includes <see cref="Date"/>, <see cref="Time"/>, <see cref="ClientIpAddress"/>,
    /// <see cref="ServerName"/>, <see cref="ServerIpAddress"/>, <see cref="ServerPort"/>,
    /// <see cref="Method"/>, <see cref="UriStem"/>, <see cref="UriQuery"/>,
    /// <see cref="ProtocolStatus"/>, <see cref="TimeTaken"/>, <see cref="ProtocolVersion"/>,
    /// <see cref="Host"/>, <see cref="UserAgent"/>, <see cref="Referer"/>,
    /// <see cref="UserName"/>, and <see cref="Cookie"/>.
    /// </summary>
    All = Date | Time | ServerName | Method | UriStem | UriQuery | ProtocolStatus |
        TimeTaken | ProtocolVersion | Host | UserAgent | Referer | ConnectionInfoFields |
        UserName | Cookie
}
