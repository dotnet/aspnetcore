# Transport Protocols

This document describes the protocols used by the three ASP.NET Endpoint Transports: WebSockets, Server-Sent Events and Long Polling

## Transport Requirements

A transport is required to have the following attributes:

1. Duplex - Able to send messages from Server to Client and from Client to Server
1. Binary-safe - Able to transmit arbitrary binary data, regardless of content
1. Text-safe - Able to transmit arbitrary text data, preserving the content. Line-endings must be preserved **but may be converted to a different format**. For example `\r\n` may be converted to `\n`. This is due to quirks in some transports (Server Sent Events). If the exact line-ending needs to be preserved, the data should be sent as a `Binary` message.

The only transport which fully implements the duplex requirement is WebSockets, the others are "half-transports" which implement one end of the duplex connection. They are used in combination to achieve a duplex connection.

Throughout this document, the term `[endpoint-base]` is used to refer to the route assigned to a particular end point. The terms `connection-id` and `connectionToken` are used to refer to the connection ID and connection token provided by the `POST [endpoint-base]/negotiate` request.

**NOTE on errors:** In all error cases, by default, the detailed exception message is **never** provided; a short description string may be provided. However, an application developer may elect to allow detailed exception messages to be emitted, which should only be used in the `Development` environment. Unexpected errors are communicated by HTTP `500 Server Error` status codes or WebSockets non-`1000 Normal Closure` close frames; in these cases the connection should be considered to be terminated.

## `POST [endpoint-base]/negotiate` request

The `POST [endpoint-base]/negotiate` request is used to establish a connection between the client and the server.

*negotiateVersion:*

In the POST request the client sends a query string parameter with the key "negotiateVersion" and the value as the negotiate protocol version it would like to use. If the query string is omitted, the server treats the version as zero. The server will include a "negotiateVersion" property in the json response that says which version it will be using. The version is chosen as described below:
* If the servers minimum supported protocol version is greater than the version requested by the client it will send an error response and close the connection
* If the server supports the request version it will respond with the requested version
* If the requested version is greater than the servers largest supported version the server will respond with its largest supported version
The client may close the connection if the "negotiateVersion" in the response is not acceptable.

*useAck:*

In the POST request the client may include a query string parameter with the key "useAck" and the value of "true". If this is included the server will decide if it supports/allows the [ack protocol](#ack-protocol) described below, and return "useAck": "true" as a json property in the negotiate response if it will use the ack protocol. If true, the client must use the ack protocol when sending/receiving otherwise the connection will be terminated. Similarly, the server must use the ack protocol when sending/receiving. If false, the client must not use the ack protocol and will be terminated if it does. If the "useAck" property is missing from the negotiate response this also implies false, so the ack protocol should not be used.

-----------

The content type of the response is `application/json` and is a JSON payload containing properties to assist the client in establishing a persistent connection. Extra JSON properties that the client does not know about should be ignored. This allows for future additions without breaking older clients.

### Version 1

When the server and client agree on version 1 the server response will include a "connectionToken" property in addition to the "connectionId" property. The value of the "connectionToken" property will be used in the "id" query string for the HTTP requests described below, this value should be kept secret.

A successful negotiate response will look similar to the following payload:
  ```json
  {
    "connectionToken":"05265228-1e2c-46c5-82a1-6a5bcc3f0143",
    "connectionId":"807809a5-31bf-470d-9e23-afaee35d8a0d",
    "negotiateVersion":1,
    "availableTransports":[
      {
        "transport": "WebSockets",
        "transferFormats": [ "Text", "Binary" ]
      },
      {
        "transport": "ServerSentEvents",
        "transferFormats": [ "Text" ]
      },
      {
        "transport": "LongPolling",
        "transferFormats": [ "Text", "Binary" ]
      }
    ]
  }
  ```

  The payload returned from this endpoint provides the following data:

  * The `connectionToken` which is **required** by the Long Polling and Server-Sent Events transports (in order to correlate sends and receives).
  * The `connectionId` which is the id by which other clients can refer to it.
  * The `negotiateVersion` which is the negotiation protocol version being used between the server and client.
  * The `availableTransports` list which describes the transports the server supports. For each transport, the name of the transport (`transport`) is listed, as is a list of "transfer formats" supported by the transport (`transferFormats`)

### Version 0

When the server and client agree on version 0 the server response will include a "connectionId" property that is used in the "id" query string for the HTTP requests described below.

A successful negotiate response will look similar to the following payload:
  ```json
  {
    "connectionId":"807809a5-31bf-470d-9e23-afaee35d8a0d",
    "negotiateVersion":0,
    "availableTransports":[
      {
        "transport": "WebSockets",
        "transferFormats": [ "Text", "Binary" ]
      },
      {
        "transport": "ServerSentEvents",
        "transferFormats": [ "Text" ]
      },
      {
        "transport": "LongPolling",
        "transferFormats": [ "Text", "Binary" ]
      }
    ]
  }
  ```

  The payload returned from this endpoint provides the following data:

  * The `connectionId` which is **required** by the Long Polling and Server-Sent Events transports (in order to correlate sends and receives).
  * The `negotiateVersion` which is the negotiation protocol version being used between the server and client.
  * The `availableTransports` list which describes the transports the server supports. For each transport, the name of the transport (`transport`) is listed, as is a list of "transfer formats" supported by the transport (`transferFormats`)

### All versions

There are two other possible negotiation responses:

1. A redirect response which tells the client which URL and optionally access token to use as a result.

  ```json
  {
    "url": "https://myapp.com/chat",
    "accessToken": "accessToken"
  }
  ```

  The payload returned from this endpoint provides the following data:

  * The `url` which is the URL the client should connect to.
  * The `accessToken` which is an optional bearer token for accessing the specified url.


1. A response that contains an `error` which should stop the connection attempt.

  ```json
  {
    "error": "This connection is not allowed."
  }
  ```

  The payload returned from this endpoint provides the following data:

  * The `error` that gives details about why the negotiate failed.

## Transfer Formats

ASP.NET Endpoints support two different transfer formats: `Text` and `Binary`. `Text` refers to UTF-8 text, and `Binary` refers to any arbitrary binary data. The transfer format serves two purposes. First, in the WebSockets transport, it is used to determine if `Text` or `Binary` WebSocket frames should be used to carry data. This is useful in debugging as most browser Dev Tools only show the content of `Text` frames. When using a text-based protocol like JSON, it is preferable for the WebSockets transport to use `Text` frames. How a client/server indicate the transfer format currently being used is implementation-defined.

Some transports are limited to supporting only `Text` data (specifically, Server-Sent Events). These transports cannot carry arbitrary binary data (without additional encoding, such as Base-64) due to limitations in their protocol. The transfer formats supported by each transport are described as part of the `POST [endpoint-base]/negotiate` response to allow clients to ignore transports that cannot support arbitrary binary data when they have a need to send/receive that data. How the client indicates the transfer format it wishes to use is also implementation-defined.

## WebSockets (Full Duplex)

The WebSockets transport is unique in that it is full duplex, and a persistent connection that can be established in a single operation. As a result, the client is not required to use the `POST [endpoint-base]/negotiate` request to establish a connection in advance. It also includes all the necessary metadata in it's own frame metadata.

The WebSocket transport is activated by making a WebSocket connection to `[endpoint-base]`. The **optional** `id` query string value is used to identify the connection to attach to. If there is no `id` query string value, a new connection is established. If the parameter is specified but there is no connection with the specified ID value, a `404 Not Found` response is returned. Upon receiving this request, the connection is established and the server responds with a WebSocket upgrade (`101 Switching Protocols`) immediately ready for frames to be sent/received. The WebSocket OpCode field is used to indicate the type of the frame (Text or Binary).

Establishing a second WebSocket connection when there is already a WebSocket connection associated with the Endpoints connection is not permitted and will fail with a `409 Conflict` status code.

Errors while establishing the connection are handled by returning a `500 Server Error` status code as the response to the upgrade request. This includes errors initializing EndPoint types. Unhandled application errors trigger a WebSocket `Close` frame with reason code that matches the error as per the spec (for errors like messages being too large, or invalid UTF-8). For other unexpected errors during the connection, a  non-`1000 Normal Closure` status code is used.

## HTTP Post (Client-to-Server only)

HTTP Post is a half-transport, it is only able to send messages from the Client to the Server, as such it is **always** used with one of the other half-transports which can send from Server to Client (Server Sent Events and Long Polling).

This transport requires that a connection be established using the `POST [endpoint-base]/negotiate` request.

The HTTP POST request is made to the URL `[endpoint-base]`. The **mandatory** `id` query string value is used to identify the connection to send to. If there is no `id` query string value, a `400 Bad Request` response is returned. Upon receipt of the **entire** payload, the server will process the payload and responds with `200 OK` if the payload was successfully processed. If a client makes another request to `/` while an existing request is outstanding, the new request is immediately terminated by the server with the `409 Conflict` status code.

If a client receives a `409 Conflict` request, the connection remains open. Any other response indicates that the connection has been terminated due to an error.

If the relevant connection has been terminated, a `404 Not Found` status code is returned. If there is an error instantiating an EndPoint or dispatching the message, a `500 Server Error` status code is returned.

## Server-Sent Events (Server-to-Client only)

Server-Sent Events (SSE) is a protocol specified by WHATWG at [https://html.spec.whatwg.org/multipage/comms.html#server-sent-events](https://html.spec.whatwg.org/multipage/comms.html#server-sent-events). It is capable of sending data from server to client only, so it must be paired with the HTTP Post transport. It also requires a connection already be established using the `POST [endpoint-base]/negotiate` request.

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

In this transport, the client establishes an SSE connection to `[endpoint-base]` with an `Accept` header of `text/event-stream`, and the server responds with an HTTP response with a `Content-Type` of `text/event-stream`. The **mandatory** `id` query string value is used to identify the connection to send to. If there is no `id` query string value, a `400 Bad Request` response is returned, if there is no connection with the specified ID, a `404 Not Found` response is returned. Each SSE event represents a single frame from client to server. The transport uses unnamed events, which means only the `data` field is available. Thus we use the first line of the `data` field for frame metadata.

The Server-Sent Events transport only supports text data, because it is a text-based protocol. As a result, it is reported by the server as supporting only the `Text` transfer format. If a client wishes to send arbitrary binary data, it should skip the Server-Sent Events transport when selecting an appropriate transport.

When the client has finished with the connection, it can terminate the event stream connection (send a TCP reset). The server will clean up the necessary resources.

## Long Polling (Server-to-Client only)

Long Polling is a server-to-client half-transport, so it is always paired with HTTP Post. It requires a connection already be established using the `POST [endpoint-base]/negotiate` request.

Long Polling requires that the client poll the server for new messages. Unlike traditional polling, if there is no data available, the server will simply wait for messages to be dispatched. At some point, the server, client or an upstream proxy will likely terminate the connection, at which point the client should immediately re-send the request. Long Polling is the only transport that allows a "reconnection" where a new request can be received while the server believes an existing request is in process. This can happen because of a time out. When this happens, the existing request is immediately terminated with status code `204 No Content`. Any messages which have already been written to the existing request will be flushed and considered sent. In the case of a server side timeout with no data, a `200 OK` with a 0 `Content-Length` will be sent and the client should poll again for more data.

A Poll is established by sending an HTTP GET request to `[endpoint-base]` with the following query string parameters

#### Version 1
* `id` (Required) - The Connection Token of the destination connection.

#### Version 0
* `id` (Required) - The Connection ID of the destination connection.

When data is available, the server responds with a body in one of the two formats below (depending upon the value of the `Accept` header). The response may be chunked, as per the chunked encoding part of the HTTP spec.

If the `id` parameter is missing, a `400 Bad Request` response is returned. If there is no connection with the ID specified in `id`, a `404 Not Found` response is returned.

When the client has finished with the connection, it can issue a `DELETE` request to `[endpoint-base]` (with the `id` in the query string) to gracefully terminate the connection. The server will complete the latest poll with `204` to indicate that it has shut down.

## Ack Protocol

The ack protocol primarily consists of writing and reading framing around the data being sent and received.
All sends need to start with a 24 byte frame. The frame consists of 2 64-bit little-endian values, both base-64 encoded (preserving padding) for a total of 12 bytes. The first base-64 value when decoded is the length of the payload being sent (minus the framing) as an int64 value. The second base-64 value when decoded is the ack ID as an int64 of how many bytes have been received from the other side so far.

The second part of the protocol is for when the transport ungracefully reconnects and uses the Ack IDs to get any data that might have been missed during the disconnect window. This will be described after showing the framing.

### Framing

Consider the following example:

0x41 0x67 0x41 0x41 0x41 0x41 0x41 0x41 0x41 0x41 0x41 0x3d 0x48 0x51 0x41 0x41 0x41 0x41 0x41 0x41 0x41 0x41 0x41 0x3d 0x48 0x69

This is a 26 byte message, the first 24 bytes are the framing, which we'll split into two 12 byte sections and the 2 remaining bytes
(hex) 41 67 41 41 41 41 41 41 41 41 41 3d - Base64 represention as bytes
AgAAAAAAAAA= - Base64 representation in ASCII
2 0 0 0 0 0 0 0 0 0 0 0 - Base64 decoded, int64 value of 2, representing a 2 length payload after the framing

(hex) 48 51 41 41 41 41 41 41 41 41 41 3d - Base64 represention as bytes
HQAAAAAAAAA= - Base64 representation in ASCII
29 0 0 0 0 0 0 0 0 0 0 0 - Base64 decoded, int64 value of 29, representing an ack id of 29 bytes received from the endpoint so far

0x48 0x69
Hi

From now on we'll use `[ <Payload Length>, <Ack ID> ]` annotation to represent the framing, with an implicit payload attached to it.

To explain the Ack IDs we'll use the following example which is sending between a client and server, C and S respectively:

```
C->S: [ 5, 0 ]
S->C: [ 10, 29 ]
S->C: [ 13, 29 ]
C->S: [ 22, 71 ]
S->C: [ 1, 75 ]
```

The first send will send an Ack ID of 0 because the client hasn't received any data yet, so there is nothing to ack. When the server sends after it's received a message from the client it will send an Ack ID of the payload length (5) + the frame length (24), so 29. In this example we also send another message which won't have an updated Ack ID, because nothing new was received, so we send the previous value. The client in its next send adds all the received messages together to get the Ack ID to send to the server, 24 + 10 from the first message received, 24 + 13 from the second message received, for a total of 71. And then finally, the server adds its previously sent Ack ID of 29 with the message(s) received since its last send (24 + 22), for a total of 75 for the Ack ID it sends to the client.

### Reconnect

The second part of the protocol is what makes use of the Ack IDs.

If a transport ungracefully disconnects the client can attempt to reconnect using the same `id` it was using before. The server is free to reject any reconnect attempts, but generally should allow a few seconds grace period.

On a successful reconnect the client must send an Ack ID with a 0 length payload to the server indicating the last message it received before disconnecting. The client then waits for a message from the server that will contain the last Ack ID the server received before the disconnection, as well as a 0 length payload. This message **does not** increment the Ack ID tracking. The Ack ID received from the server will be used to send any missed messages from the client to the server. The normal send/receive loops can now start and if there is any unacked data on the client side the send loop should immediately send the missed data (framing and all).

On a successful reconnect the server must wait for the client to send the last Ack ID it received before disconnecting. This message **does not** increment the Ack ID tracking. The Ack ID received from the client will be used to send any missed messages from the server to the client. The server will then send the last Ack ID it received before the disconnect occurred as well as a 0 length payload. The normal send/receive loops can now start and if there is any unacked data on the server side the send loop should immediately send the missed data (framing and all).

The following example will send a few messages between client and server before having an ungraceful disconnect to show the reconnect flow:

```
C->S: [ 10, 0 ]
S->C: [ 1, 34 ]
C->S: [ 11, 25 ]
// Ungraceful disconnect
C->S: [ 0, 25 ]
S->C: [ 0, 34 ]
// normal send/receive loops for both sides are now started
C->S: [ 11, 25 ] // resend 11 byte payload that server didn't get before disconnect occurred
```

Another example that is the same as the last example except that the server did receive the clients last send before the disconnect:

```
C->S: [ 10, 0 ]
S->C: [ 1, 34 ]
C->S: [ 11, 25 ]
// Ungraceful disconnect
C->S: [ 0, 25 ]
S->C: [ 0, 69 ]
// normal send/receive loops for both sides are now started
// 11 bytes from C->S not resent because server did get it before the disconnect, as can be seen by the new Ack ID
```