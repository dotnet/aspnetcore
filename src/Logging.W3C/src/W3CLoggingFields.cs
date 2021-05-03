using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.W3C
{
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
        /// Flag for logging the status of the
        /// action, in HTTP or FTP terms.
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
        /// Flag for logging the browser used on the client.
        /// </summary>
        UserAgent = 0x4000,

        /// <summary>
        /// Flag for logging the content of the cookie
        /// sent by the client, if any.
        /// </summary>
        Cookie = 0x8000,

        /// <summary>
        /// Flag for logging the content of the cookie
        /// sent by the client, if any.
        /// </summary>
        Referrer = 0x10000,

        /// <summary>
        /// Flag for logging all default fields.
        /// Includes <see cref="Date"/>, <see cref="Time"/>, <see cref="ClientIpAddress"/>,
        /// <see cref="ServerName"/>, <see cref="ServerIpAddress"/>, <see cref="ServerPort"/>,
        /// <see cref="Method"/>, <see cref="UriStem"/>, <see cref="UriQuery"/>,
        /// <see cref="ProtocolStatus"/>, <see cref="TimeTaken"/>, <see cref="ProtocolVersion"/>,
        /// <see cref="Host"/>, <see cref="UserAgent"/>, and <see cref="Referrer"/>.
        /// </summary>
        Default = Date | Time | ClientIpAddress | ServerName | ServerIpAddress | ServerPort | Method |
            UriStem | UriQuery | ProtocolStatus | TimeTaken | ProtocolVersion | Host | UserAgent | Referrer,

        /// <summary>
        /// Flag for logging all optional fields.
        /// Includes <see cref="UserName"/> and <see cref="Cookie"/>.
        /// These fields contain information which could expose
        /// identifiable information about the client user.
        /// </summary>
        Optional = UserName | Cookie,

        /// <summary>
        /// Flag for logging all possible fields.
        /// /// Includes <see cref="Default"/> and <see cref="Optional"/>.
        /// </summary>
        All = Default | Optional
    }
}
