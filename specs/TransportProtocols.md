# Transport Protocols

This document describes the protocols used by the three ASP.NET Endpoint Transports: WebSockets, Server-Sent Events, Long Polling and HTTP Post

## Transport Requirements

A transport is required to have the following attributes:

1. Duplex - Able to send messages from Server to Client and from Client to Server
1. Frame-based - Messages have fixed length (as opposed to streaming, where data is just pushed throughout the connection)
1. Frame Metadata - Able to encode one piece of Frame Metadata
    * `Type` - Either `Binary` or `Text`.
1. Able to handle partial messages from the Server application (i.e. where `EndOfMessage` is `false`). The transport can opt to implement this by buffering, since only WebSockets natively supports this functionality.
1. Binary-safe - Able to transmit arbitrary binary data, regardless of content
1. Text-safe - Able to transmit arbitrary text data, preserving the content. Line-endings must be preserved **but may be converted to a different format**. For example `\r\n` may be converted to `\n`. This is due to quirks in some transports (Server Sent Events). If the exact line-ending needs to be preserved, the data should be sent as a `Binary` message.

Multi-frame messages (where a message is split into multiple frames) may not overlap and all frames **must** have the same `Type` value (`Text` or `Binary`). It is an error to change the `Type` flag mid-message and the endpoint may terminate the connection if that occurs.

The only transport which fully implements the duplex requirement is WebSockets, the others are "half-transports" which implement one end of the duplex connection. They are used in combination to achieve a duplex connection.

Throughout this document, the term `[endpoint-base]` is used to refer to the route assigned to a particular end point. The term `[connection-id]` is used to refer to the connection ID provided by the `[endpoint-base]/negotiate` end point.

**NOTE on errors:** In all error cases, by default, the detailed exception message is **never** provided; a short description string may be provided. However, an application developer may elect to allow detailed exception messages to be emitted, which should only be used in the `Development` environment. Unexpected errors are communicated by HTTP `500 Server Error` status codes or WebSockets `1008 Policy Violation` close frames; in these cases the connection should be considered to be terminated.

## WebSockets (Full Duplex)

The WebSockets transport is unique in that it is full duplex, and a persistent connection that can be established in a single operation. As a result, the client is not required to use the `[endpoint-base]/negotiate` endpoint to establish a connection in advance. It also includes all the necessary metadata in it's own frame metadata.

The WebSocket transport is activated by making a WebSocket connection to `[endpoint-base]/ws`. The **optional** `connectionId` query string value is used to identify the connection to attach to. If there is no `connectionId` query string value, a new connection is established. If the parameter is specified but there is no connection with the specified ID value, a `404 Not Found` response is returned. Upon receiving this request, the connection is established and the server responds with a WebSocket upgrade (`101 Switching Protocols`) immediately ready for frames to be sent/received. The WebSocket OpCode field is used to indicate the type of the frame (Text or Binary) and the WebSocket "FIN" flag is used to indicate the end of a message.

Establishing a second WebSocket connection when there is already a WebSocket connection associated with the Endpoints connection is not permitted and will fail with a `409 Conflict` status code.

Errors while establishing the connection are handled by returning a `500 Server Error` status code as the response to the upgrade request. This includes errors initializing EndPoint types. Unhandled application errors trigger a WebSocket `Close` frame with reason code that matches the error as per the spec (for errors like messages being too large, or invalid UTF-8). For other unexpected errors during the connection, the `1008 Policy Violation` status code is used. 

## HTTP Post (Client-to-Server only)

HTTP Post is a half-transport, it is only able to send messages from the Client to the Server, as such it is always used with one of the other half-transports which can send from Server to Client (Server Sent Events and Long Polling).

This transport requires that a connection be established using the `[endpoint-base]/negotiate` end point.

The HTTP POST request is made to the URL `[endpoint-base]/send`. The **mandatory** `connectionId` query string value is used to identify the connection to send to. If there is no `connectionId` query string value, a `400 Bad Request` response is returned. The content consists of frames in the same format as the Long Polling transport. It is up to the client which of the Text or Binary protocol they use, and the server is able to detect which they use either via the `Content-Type` (see the Long Polling transport section) or via the first byte (`T` for the Text-based protocol, `B` for the binary protocol). Upon receipt of the **entire** request, the server will process and deliver all the messages, responding with `202 Accepted` if all the messages are successfully processed. If a client makes another request to `/send` while an existing one is outstanding, the new request is immediately terminated by the server with the `409 Conflict` status code.

If the client transmits a `Close` or `Error` frame, the server will ignore and discard any frames following that frame and immediately return `202 Accepted`. Any further attempts to send to that connection will receive a `404 Not Found` response, as the connection will have been terminated.

If a client receives either a `202 Accepted` or `409 Conflict` request, the connection remains open. Any other response indicates that the connection has been terminated due to an error.

This transport will always produce single-frame messages, since the Long-Polling protocol below does not support encoding an end-of-message flag.

If the relevant connection has been terminated, a `404 Not Found` status code is returned. If there is an error instantiating an EndPoint or dispatching the message, a `500 Server Error` status code is returned.

## Server-Sent Events (Server-to-Client only)

Server-Sent Events (SSE) is a protocol specified by WHATWG at [https://html.spec.whatwg.org/multipage/comms.html#server-sent-events](https://html.spec.whatwg.org/multipage/comms.html#server-sent-events). It is capable of sending data from server to client only, so it must be paired with the HTTP Post transport. It also requires a connection already be established using the `[endpoint-base]/negotiate` endpoint.


The protocol is similar to Long Polling in that the client opens a request to an endpoint and leaves it open. The server transmits frames as "events" using the SSE protocol. The protocol encodes a single event as a sequence of key-value pair lines, separated by `:` and using any of `\r\n`, `\n` or `\r` as line-terminators, followed by a final blank line. Keys can be duplicated and their values are concatenated with `\n`. So the following represents two events:

```
foo: bar
baz: boz
baz: biz
quz: qoz
baz: flarg

foo: boz

```

In the first event, the value of `baz` would be `boz\nbiz\nflarg`, due to the concatenation behavior above. Full details can be found in the spec linked above.

In this transport, the client establishes an SSE connection to `[endpoint-base]/[connection-id]/sse`, and the server responds with an HTTP response with a `Content-Type` of `text/event-stream`. The **mandatory** `connectionId` query string value is used to identify the connection to send to. If there is no `connectionId` query string value, a `400 Bad Request` response is returned, if there is no connection with the specified ID, a `404 Not Found` response is returned. Each SSE event represents a single frame from client to server. The transport uses unnamed events, which means only the `data` field is available. Thus we use the first line of the `data` field for frame metadata. The frame body starts on the **second** line of the `data` field value. The first line has the following format (Identifiers in square brackets `[]` indicate fields defined below):

```
[Type]
```

* `[Type]` is a single UTF-8 character representing the type of the frame; `T` indicates Text, `B` indicates Binary, `E` indicates an error, `C` indicates that the server is terminating its end of the connection.

If the `[Type]` field is `T`, the remaining lines of the `data` field contain the value, in UTF-8 text. If the `[Type]` field is `B`, the remaining lines of the `data` field contain Base64-encoded binary data. Any `\n` characters in Binary frames are removed before Base64-decoding. However, servers should avoid line breaks in the Base64-encoded data. If the `[Type]` field is `E`, the remaining lines of the `data` field may contain a textual description of the error. Also, the connection will be terminated immediately following an `E` frame. If the `[Type]` field is `C`, there is no remaining content in the `data` field.

If the SSE response ends without a `C` or `E` frame, the client should immediately attempt to reestablish the connection. However, the protocol provides **no** guarantees that any messages that have not been seen will be retransmitted.

TBD: Keep Alive - Should it be done at this level?

For example, when sending the following frames (`\n` indicates the actual Line Feed character, not an escape sequence):

* Type=`Text`, "Hello\nWorld"
* Type=`Binary`, `0x01 0x02`
* Type=`Close`

The encoding will be as follows

```
data: T
data: Hello
data: World

data: B
data: AQI=

data: C

[Connection Terminated at this point]
```

This transport will buffer incomplete frames sent by the server until the full message is available and then send the message in a single frame.

## Long Polling (Server-to-Client only)

Long Polling is a server-to-client half-transport, so it is always paired with HTTP Post. It requires a connection already be established using the `[endpoint-base]/negotiate` endpoint.

Long Polling requires that the client poll the server for new messages. Unlike traditional polling, if there are no messages available, the server will simply block the request waiting for messages to be dispatched. At some point, the server, client or an upstream proxy will likely terminate the connection, at which point the client should immediately re-send the request. Long Polling is the only transport that allows a "reconnection" where a new request can be received while the server believes an existing request is in process. This can happen because of a time out. When this happens, the existing request is immediately terminated with an empty result (even if messages are available) and the new request replaces it as the active poll request.

Since there is such a long round-trip-time for messages, given that the client must issue a request before the server can transmit a message back, Long Polling responses contain batches of multiple messages. Also, in order to support browsers which do not support XHR2, which provides the ability to read binary data, there are two different modes for the polling transport.

A Poll is established by sending an HTTP GET request to `[endpoint-base]/poll` with the following query string parameters

* `connectionId` (Required) - The Connection ID of the destination connection.
* `supportsBinary` (Optional: default `false`) - A boolean indicating if the client supports raw binary data in responses

When messages are available, the server responds with a body in one of the two formats below (depending upon the value of `supportsBinary`). The response may be chunked, as per the chunked encoding part of the HTTP spec.

If the `connectionId` parameter is missing, a `400 Bad Request` response is returned. If there is no connection with the ID specified in `connectionId`, a `404 Not Found` response is returned.

### Text-based encoding (`supportsBinary` = `false` or not present)

The body will be formatted as below and encoded in UTF-8. The `Content-Type` response header is set to `application/vnd.microsoft.aspnetcore.endpoint-messages.v1+text`. Identifiers in square brackets `[]` indicate fields defined below, and parenthesis `()` indicate grouping.

```
T([Length]:[Type]:[Body];)([Length]:[Type]:[Body];)... continues until end of the response body ...
```

* `[Length]` - Length of the `[Body]` field in bytes, specified as UTF-8 digits (`0`-`9`, terminated by `:`). If the body is a binary frame, this length indicates the number of Base64-encoded characters, not the number of bytes in the final decoded message!
* `[Type]` - A single-byte UTF-8 character indicating the type of the frame, see the list of frame Types below
* `[Body]` - The body of the message, the content of which depends upon the value of `[Type]`

The following values are valid for `[Type]`:

* `T` - Indicates a text frame, the `[Body]` contains UTF-8 encoded text data.
* `B` - Indicates a text frame, the `[Body]` contains Base64 encoded binary data.
* `E` - Indicates an error frame, the `[Body]` contains an optional UTF-8 encoded error description. The connection is immediately closed after processing this frame.
* `C` - Indicates a close frame, there is no `[Body]`. The connection is immediately closed after processing this frame.

Note: If there is no `[Body]` for a frame, there does still need to be a `:` and `;` delimiting the body. So, for example, the following is an encoding of a single text frame `A` followed by a single close frame: `T1:T:A;0:C:;`

For example, when sending the following frames (`\n` indicates the actual Line Feed character, not an escape sequence):

* Type=`Text`, "Hello\nWorld"
* Type=`Binary`, `0x01 0x02`
* Type=`Close`

The encoding will be as follows

```
T11:T:Hello
World;4:B:AQI=;0:C:;
```

Note that the final frame still ends with the `;` terminator, and that since the body may contain `;`, newlines, etc., the length is specified in order to know exactly where the body ends.

This transport will buffer incomplete frames sent by the server until the full message is available and then send the message in a single frame.

### Binary encoding (`supportsBinary` = `true`)

In JavaScript/Browser clients, this encoding requires XHR2 (or similar HTTP request functionality which allows binary data) and TypedArray support.

The body is encoded as follows. The `Content-Type` response header is set to `application/vnd.microsoft.aspnetcore.endpoint-messages.v1+binary`. Identifiers in square brackets `[]` indicate fields defined below, and parenthesis `()` indicate grouping. Other symbols indicate ASCII-encoded text in the stream

```
B([Length][Type][Body])([Length][Type][Body])... continues until end of the response body ...
```

* `[Length]` - A 64-bit integer in Network Byte Order (Big-endian) representing the length of the body in bytes
* `[Type]` - An 8-bit integer indicating the type of the message.
    * `0x00` => `Text` - `[Body]` is UTF-8 encoded text data
    * `0x01` => `Binary` - `[Body]` is raw binary data
    * `0x02` => `Error` - `[Body]` is UTF-8 encoded error message
    * `0x03` => `Close` - `[Body]` is empty
    * All other values are reserved and must **not** be used. An endpoint may reject a frame using any other value and terminate the connection.
* `[Body]` - The body of the message, exactly `[Length]` bytes in length. `Text` and `Error` frames are always encoded in UTF-8.

For example, when sending the following frames (`\n` indicates the actual Line Feed character, not an escape sequence):

* Type=`Text`, "Hello\nWorld"
* Type=`Binary`, `0x01 0x02`
* Type=`Close`

The encoding will be as follows, as a list of binary digits in hex (text in parentheses `()` are comments). Whitespace and newlines are irrelevant and for illustration only.
```
0x66                                                   (ASCII 'B')
0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x0B                (start of frame; 64-bit integer value: 11)
0x80                                                   (Type = Text)
0x68 0x65 0x6C 0x6C 0x6F 0x0A 0x77 0x6F 0x72 0x6C 0x64 (UTF-8 encoding of 'Hello\nWorld')
0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x02                (start of frame; 64-bit integer value: 2)
0x01                                                   (Type = Binary)
0x01 0x02                                              (body)
0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00                (start of frame; 64-bit integer value: 0)
0x03                                                   (Type = Close)
```

This transport will buffer incomplete frames sent by the server until the full message is available and then send the message in a single frame.
