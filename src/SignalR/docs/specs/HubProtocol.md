# SignalR Hub Protocol

The SignalR Protocol is a protocol for two-way RPC over any Message-based transport. Either party in the connection may invoke procedures on the other party, and procedures can return zero or more results or an error.

## Terms

* Caller - The node that is issuing an `Invocation`, `StreamInvocation`, `CancelInvocation`, `Ping` messages and receiving `Completion`, `StreamItem` and `Ping` messages (a node can be both Caller and Callee for different invocations simultaneously)
* Callee - The node that is receiving an `Invocation`, `StreamInvocation`, `CancelInvocation`, `Ping` messages and issuing `Completion`, `StreamItem` and `Ping` messages (a node can be both Callee and Caller for different invocations simultaneously)
* Binder - The component on each node that handles mapping `Invocation` and `StreamInvocation` messages to method calls and return values to `Completion` and `StreamItem` messages

## Transport Requirements

The SignalR Protocol requires the following attributes from the underlying transport.

* Reliable, in-order, delivery of messages - Specifically, the SignalR protocol provides no facility for retransmission or reordering of messages. If that is important to an application scenario, the application must either use a transport that guarantees it (i.e. TCP) or provide their own system for managing message order.

## Overview

This document describes two encodings of the SignalR protocol: [JSON](http://www.json.org/) and [MessagePack](http://msgpack.org/). Only one format can be used for the duration of a connection, and the format must be agreed on by both sides after opening the connection and before sending any other messages. However, each format shares a similar overall structure.

In the SignalR protocol, the following types of messages can be sent:

| Message Name          | Sender         | Description                                                                                                                    |
| ------------------    | -------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `HandshakeRequest`    | Client         | Sent by the client to agree on the message format.                                                                            |
| `HandshakeResponse`   | Server         | Sent by the server as an acknowledgment of the previous `HandshakeRequest` message. Contains an error if the handshake failed. |
| `Close`               | Callee, Caller | Sent by the server when a connection is closed. Contains an error if the connection was closed because of an error.            |
| `Invocation`          | Caller         | Indicates a request to invoke a particular method (the Target) with provided Arguments on the remote endpoint.                 |
| `StreamInvocation`    | Caller         | Indicates a request to invoke a streaming method (the Target) with provided Arguments on the remote endpoint.                  |
| `StreamItem`          | Callee, Caller | Indicates individual items of streamed response data from a previous `StreamInvocation` message or streamed uploads from an invocation with streamIds.                               |
| `Completion`          | Callee, Caller | Indicates a previous `Invocation` or `StreamInvocation` has completed or a stream in an `Invocation` or `StreamInvocation` has completed. Contains an error if the invocation concluded with an error or the result of a non-streaming method invocation. The result will be absent for `void` methods. In case of streaming invocations no further `StreamItem` messages will be received. |
| `CancelInvocation`    | Caller         | Sent by the client to cancel a streaming invocation on the server.                                                             |
| `Ping`                | Caller, Callee | Sent by either party to check if the connection is active.                                                                     |

After opening a connection to the server the client must send a `HandshakeRequest` message to the server as its first message. The handshake message is **always** a JSON message and contains the name of the format (protocol) as well as the version of the protocol that will be used for the duration of the connection. The server will reply with a `HandshakeResponse`, also always JSON, containing an error if the server does not support the protocol. If the server does not support the protocol requested by the client or the first message received from the client is not a `HandshakeRequest` message the server must close the connection. Both the `HandshakeRequest` and `HandshakeResponse` messages must be terminated by the ASCII character `0x1E` (record separator).

The `HandshakeRequest` message contains the following properties:

* `protocol` - the name of the protocol to be used for messages exchanged between the server and the client
* `version` - the value must always be 1, for both MessagePack and Json protocols

Example:

```json
{
    "protocol": "messagepack",
    "version": 1
}
```

The `HandshakeResponse` message contains the following properties:

* `error` - the optional error message if the server does not support the requested protocol

Example:

```json
{
    "error": "Requested protocol 'messagepack' is not available."
}
```

## Communication between the Caller and the Callee

There are three kinds of interactions between the Caller and the Callee:

* Invocations - the Caller sends a message to the Callee and expects a message indicating that the invocation has been completed and optionally a result of the invocation
* Non-Blocking Invocations - the Caller sends a message to the Callee and does not expect any further messages for this invocation
* Streaming Invocations - the Caller sends a message to the Callee and expects one or more results returned by the Callee followed by a message indicating the end of invocation

## Invocations

In order to perform a single invocation, the Caller follows the following basic flow:

1. Allocate a unique (per connection) `Invocation ID` value (arbitrary string, chosen by the Caller) to represent the invocation
2. Send an `Invocation` or `StreamingInvocation` message containing the `Invocation ID`, the name of the `Target` being invoked, and the `Arguments` to provide to the method.
3. If the `Invocation` is marked as non-blocking (see "Non-Blocking Invocations" below), stop here and immediately yield back to the application.
4. Wait for a `StreamItem` or `Completion` message with a matching `Invocation ID`
5. If a `Completion` message arrives, go to 8
6. If the `StreamItem` message has a payload, dispatch the payload to the application (i.e. by yielding a result to an `IObservable`, or by collecting the result for dispatching in step 8)
7. Go to 4
8. Complete the invocation, dispatching the final payload item (if any) or the error (if any) to the application

The `Target` of an `Invocation` message must refer to a specific method, overloading is **not** permitted. In the .NET Binder, the `Target` value for a method is defined as the simple name of the Method (i.e. without qualifying type name, since a SignalR endpoint is specific to a single Hub class). `Target` is case-sensitive

**NOTE**: `Invocation ID`s are arbitrarily chosen by the Caller and the Callee is expected to use the same string in all response messages. Callees may establish reasonable limits on `Invocation ID` lengths and terminate the connection when an `Invocation ID` that is too long is received.

## Message Headers

All messages, except the `Ping` message, can carry additional headers. Headers are transmitted as a dictionary with string keys and string values. Clients and servers should disregard headers they do not understand. Since there are no headers defined in this spec, a client or server is never expected to interpret headers. However, clients and servers are expected to be able to process messages containing headers and disregard the headers.

## Non-Blocking Invocations

Invocations can be sent without an `Invocation ID` value. This indicates that the invocation is "non-blocking", and thus the caller does not expect a response. When a Callee receives an invocation without an `Invocation ID` value, it **must not** send any response to that invocation.

## Streaming

The SignalR protocol allows for multiple `StreamItem` messages to be transmitted in response to a `StreamingInvocation` message, and allows the receiver to dispatch these results as they arrive, to allow for streaming data from one endpoint to another.

On the Callee side, it is up to the Callee's Binder to determine if a method call will yield multiple results. For example, in .NET certain return types may indicate multiple results, while others may indicate a single result. Even then, applications may wish for multiple results to be buffered and returned in a single `Completion` frame. It is up to the Binder to decide how to map this. The Callee's Binder must encode each result in separate `StreamItem` messages, indicating the end of results by sending a `Completion` message.

On the Caller side, the user code which performs the invocation indicates how it would like to receive the results and it is up the Caller's Binder to handle the result. If the Caller expects only a single result, but multiple results are returned, or if the caller expects multiple results but only one result is returned, the Caller's Binder should yield an error. If the Caller wants to stop receiving `StreamItem` messages before the Callee sends a `Completion` message, the Caller can send a `CancelInvocation` message with the same `Invocation ID` used for the `StreamInvocation` message that started the stream. When the Callee receives a `CancelInvocation` message it will stop sending `StreamItem` messages and will send a `Completion` message. The Caller is free to ignore any `StreamItem` messages as well as the `Completion` message after sending `CancelInvocation`.

## Upload streaming

The Caller can send streaming data to the Callee, they can begin such a process by making an `Invocation` or `StreamInvocation` and adding a "StreamIds" property with an array of IDs that will represent the stream(s) associated with the invocation. The IDs must be unique from any other stream IDs used by the same Caller. The Caller then sends `StreamItem` messages with the "InvocationId" property set to the ID for the stream they are sending over. The Caller can end the stream by sending a `Completion` message with the ID of the stream they are completing. If the Callee sends a `Completion` the Caller should stop sending `StreamItem` and `Completion` messages, and the Callee is free to ignore any `StreamItem` and `Completion` messages that are sent after the invocation has completed.

## Completion and results

An Invocation is only considered completed when the `Completion` message is received. Receiving **any** message using the same `Invocation ID` after a `Completion` message has been received for that invocation is considered a protocol error and the recipient may immediately terminate the connection.

If a Callee is going to stream results, it **MUST** send each individual result in a separate `StreamItem` message, and complete the invocation with a `Completion`. If the Callee is going to return a single result, it **MUST** not send any `StreamItem` messages, and **MUST** send the single result in a `Completion` message. If the Callee receives an `Invocation` message for a method that would yield multiple results or the Callee receives a `StreamInvocation` message for a method that would return a single result it **MUST** complete the invocation with a `Completion` message containing an error.

## Errors

Errors are indicated by the presence of the `error` field in a `Completion` message. Errors always indicate the immediate end of the invocation. In the case of streamed responses, the arrival of a `Completion` message indicating an error should **not** stop the dispatching of previously-received results. The error is only yielded after the previously-received results have been dispatched.

If either endpoint commits a Protocol Error (see examples below), the other endpoint may immediately terminate the underlying connection.

* It is a protocol error for any message to be missing a required field, or to have an unrecognized field.
* It is a protocol error for a Caller to send a `StreamItem` or `Completion` message with an `Invocation ID` that has not been received in an `Invocation` message from the Callee
* It is a protocol error for a Caller to send a `StreamItem` or `Completion` message in response to a Non-Blocking Invocation (see "Non-Blocking Invocations" above)
* It is a protocol error for a Caller to send a `Completion` message with a result when a `StreamItem` message has previously been sent for the same `Invocation ID`.
* It is a protocol error for a Caller to send a `Completion` message carrying both a result and an error.
* It is a protocol error for an `Invocation` or `StreamInvocation` message to have an `Invocation ID` that has already been used by *that* endpoint. However, it is **not an error** for one endpoint to use an `Invocation ID` that was previously used by the other endpoint (allowing each endpoint to track it's own IDs).

## Ping (aka "Keep Alive")

The SignalR Hub protocol supports "Keep Alive" messages used to ensure that the underlying transport connection remains active. These messages help ensure:

1. Proxies don't close the underlying connection during idle times (when few messages are being sent)
2. If the underlying connection is dropped without being terminated gracefully, the application is informed as quickly as possible.

Keep alive behavior is achieved via the `Ping` message type. **Either endpoint** may send a `Ping` message at any time. The receiving endpoint may choose to ignore the message, it has no obligation to respond in anyway. Most implementations will want to reset a timeout used to determine if the other party is present.

Ping messages do not have any payload, they are completely empty messages (aside from the encoding necessary to identify the message as a `Ping` message).

The default ASP.NET Core implementation automatically pings both directions on active connections. These pings are at regular intervals, and allow detection of unexpected disconnects (for example, unplugging a server). If the client detects that the server has stopped pinging, the client will close the connection, and vice versa. If there's other traffic through the connection, keep-alive pings aren't needed. A `Ping` is only sent if the interval has elapsed without a message being sent.

## Example

Consider the following C# methods

```csharp
public int Add(int x, int y)
{
    return x + y;
}

public int SingleResultFailure(int x, int y)
{
    throw new Exception("It didn't work!");
}

public IEnumerable<int> Batched(int count)
{
    for (var i = 0; i < count; i++)
    {
        yield return i;
    }
}

public async IAsyncEnumerable<int> Stream(int count)
{
    for (var i = 0; i < count; i++)
    {
        await Task.Delay(10);
        yield return i;
    }
}

public async IAsyncEnumerable<int> StreamFailure(int count)
{
    for (var i = 0; i < count; i++)
    {
        await Task.Delay(10);
        yield return i;
    }
    throw new Exception("Ran out of data!");
}

private List<string> _callers = new List<string>();
public void NonBlocking(string caller)
{
    _callers.Add(caller);
}

public async Task<int> AddStream(IAsyncEnumerable<int> stream)
{
    int sum = 0;
    await foreach(var item in stream)
    {
        sum += item;
    }
    return sum;
}
```

In each of the below examples, lines starting `C->S` indicate messages sent from the Caller ("Client") to the Callee ("Server"), and lines starting `S->C` indicate messages sent from the Callee ("Server") back to the Caller ("Client"). Message syntax is just a pseudo-code and is not intended to match any particular encoding.

### Single Result (`Add` example above)

```
C->S: Invocation { Id = 42, Target = "Add", Arguments = [ 40, 2 ] }
S->C: Completion { Id = 42, Result = 42 }
```

**NOTE:** The following is **NOT** an acceptable encoding of this invocation:

```
C->S: Invocation { Id = 42, Target = "Add", Arguments = [ 40, 2 ] }
S->C: StreamItem { Id = 42, Item = 42 }
S->C: Completion { Id = 42 }
```

### Single Result with Error (`SingleResultFailure` example above)

```
C->S: Invocation { Id = 42, Target = "SingleResultFailure", Arguments = [ 40, 2 ] }
S->C: Completion { Id = 42, Error = "It didn't work!" }
```

### Batched Result (`Batched` example above)

```
C->S: Invocation { Id = 42, Target = "Batched", Arguments = [ 5 ] }
S->C: Completion { Id = 42, Result = [ 0, 1, 2, 3, 4 ] }
```

### Streamed Result (`Stream` example above)

```
C->S: StreamInvocation { Id = 42, Target = "Stream", Arguments = [ 5 ] }
S->C: StreamItem { Id = 42, Item = 0 }
S->C: StreamItem { Id = 42, Item = 1 }
S->C: StreamItem { Id = 42, Item = 2 }
S->C: StreamItem { Id = 42, Item = 3 }
S->C: StreamItem { Id = 42, Item = 4 }
S->C: Completion { Id = 42 }
```

**NOTE:** The following is **NOT** an acceptable encoding of this invocation:

```
C->S: StreamInvocation { Id = 42, Target = "Stream", Arguments = [ 5 ] }
S->C: StreamItem { Id = 42, Item = 0 }
S->C: StreamItem { Id = 42, Item = 1 }
S->C: StreamItem { Id = 42, Item = 2 }
S->C: StreamItem { Id = 42, Item = 3 }
S->C: Completion { Id = 42, Result = 4 }
```

This is invalid because the `Completion` message for streaming invocations must not contain any result.

### Streamed Result with Error (`StreamFailure` example above)

```
C->S: StreamInvocation { Id = 42, Target = "Stream", Arguments = [ 5 ] }
S->C: StreamItem { Id = 42, Item = 0 }
S->C: StreamItem { Id = 42, Item = 1 }
S->C: StreamItem { Id = 42, Item = 2 }
S->C: StreamItem { Id = 42, Item = 3 }
S->C: StreamItem { Id = 42, Item = 4 }
S->C: Completion { Id = 42, Error = "Ran out of data!" }
```

This should manifest to the Calling code as a sequence which emits `0`, `1`, `2`, `3`, `4`, but then fails with the error `Ran out of data!`.

### Streamed Result closed early (`Stream` example above)

```
C->S: StreamInvocation { Id = 42, Target = "Stream", Arguments = [ 5 ] }
S->C: StreamItem { Id = 42, Item = 0 }
S->C: StreamItem { Id = 42, Item = 1 }
C->S: CancelInvocation { Id = 42 }
S->C: StreamItem { Id = 42, Item = 2} // This can be ignored
S->C: Completion { Id = 42 } // This can be ignored
```

### Non-Blocking Call (`NonBlocking` example above)

```
C->S: Invocation { Target = "NonBlocking", Arguments = [ "foo" ] }
```

### Stream from Client to Server (`AddStream` example above)

```
C->S: Invocation { Id = 42, Target = "AddStream", Arguments = [ ], StreamIds = [ 1 ] }
C->S: StreamItem { Id = 1, Item = 1 }
C->S: StreamItem { Id = 1, Item = 2 }
C->S: StreamItem { Id = 1, Item = 3 }
C->S: Completion { Id = 1 }
S->C: Completion { Id = 42, Result = 6 }
```

### Ping

```
C->S: Ping
```

## JSON Encoding

In the JSON Encoding of the SignalR Protocol, each Message is represented as a single JSON object, which should be the only content of the underlying message from the Transport. All property names are case-sensitive. The underlying protocol is expected to handle encoding and decoding of the text, so the JSON string should be encoded in whatever form is expected by the underlying transport. For example, when using the ASP.NET Sockets transports, UTF-8 encoding is always used for text.

All JSON messages must be terminated by the ASCII character `0x1E` (record separator).

### Invocation Message Encoding

An `Invocation` message is a JSON object with the following properties:

* `type` - A `Number` with the literal value 1, indicating that this message is an Invocation.
* `invocationId` - An optional `String` encoding the `Invocation ID` for a message.
* `target` - A `String` encoding the `Target` name, as expected by the Callee's Binder
* `arguments` - An `Array` containing arguments to apply to the method referred to in Target. This is a sequence of JSON `Token`s, encoded as indicated below in the "JSON Payload Encoding" section
* `streamIds` - An optional `Array` of strings representing unique ids for streams coming from the Caller to the Callee and being consumed by the method referred to in Target.

Example:

```json
{
    "type": 1,
    "invocationId": "123",
    "target": "Send",
    "arguments": [
        42,
        "Test Message"
    ]
}
```
Example (Non-Blocking):

```json
{
    "type": 1,
    "target": "Send",
    "arguments": [
        42,
        "Test Message"
    ]
}
```

Example (Invocation with stream from Caller):

```json
{
    "type": 1,
    "invocationId": "123",
    "target": "Send",
    "arguments": [
        42
    ],
    "streamIds": [
        "1"
    ]
}
```

### StreamInvocation Message Encoding

A `StreamInvocation` message is a JSON object with the following properties:

* `type` - A `Number` with the literal value 4, indicating that this message is a StreamInvocation.
* `invocationId` - A `String` encoding the `Invocation ID` for a message.
* `target` - A `String` encoding the `Target` name, as expected by the Callee's Binder.
* `arguments` - An `Array` containing arguments to apply to the method referred to in Target. This is a sequence of JSON `Token`s, encoded as indicated below in the "JSON Payload Encoding" section.
* `streamIds` - An optional `Array` of strings representing unique ids for streams coming from the Caller to the Callee and being consumed by the method referred to in Target.

Example:

```json
{
    "type": 4,
    "invocationId": "123",
    "target": "Send",
    "arguments": [
        42,
        "Test Message"
    ]
}
```

### StreamItem Message Encoding

A `StreamItem` message is a JSON object with the following properties:

* `type` - A `Number` with the literal value 2, indicating that this message is a `StreamItem`.
* `invocationId` - A `String` encoding the `Invocation ID` for a message.
* `item` - A `Token` encoding the stream item (see "JSON Payload Encoding" for details).

Example

```json
{
    "type": 2,
    "invocationId": "123",
    "item": 42
}
```

### Completion Message Encoding

A `Completion` message is a JSON object with the following properties

* `type` - A `Number` with the literal value `3`, indicating that this message is a `Completion`.
* `invocationId` - A `String` encoding the `Invocation ID` for a message.
* `result` - A `Token` encoding the result value (see "JSON Payload Encoding" for details). This field is **ignored** if `error` is present.
* `error` - A `String` encoding the error message.

It is a protocol error to include both a `result` and an `error` property in the `Completion` message. A conforming endpoint may immediately terminate the connection upon receiving such a message.

Example - A `Completion` message with no result or error

```json
{
    "type": 3,
    "invocationId": "123"
}
```

Example - A `Completion` message with a result

```json
{
    "type": 3,
    "invocationId": "123",
    "result": 42
}
```

Example - A `Completion` message with an error

```json
{
    "type": 3,
    "invocationId": "123",
    "error": "It didn't work!"
}
```

Example - The following `Completion` message is a protocol error because it has both of `result` and `error`

```json
{
    "type": 3,
    "invocationId": "123",
    "result": 42,
    "error": "It didn't work!"
}
```

### CancelInvocation Message Encoding
A `CancelInvocation` message is a JSON object with the following properties

* `type` - A `Number` with the literal value `5`, indicating that this message is a `CancelInvocation`.
* `invocationId` - A `String` encoding the `Invocation ID` for a message.

Example
```json
{
    "type": 5,
    "invocationId": "123"
}
```

### Ping Message Encoding
A `Ping` message is a JSON object with the following properties:

* `type` - A `Number` with the literal value `6`, indicating that this message is a `Ping`.

Example
```json
{
    "type": 6
}
```

### Close Message Encoding
A `Close` message is a JSON object with the following properties

* `type` - A `Number` with the literal value `7`, indicating that this message is a `Close`.
* `error` - An optional `String` encoding the error message.
* `allowReconnect` - An optional `Boolean` indicating to clients with automatic reconnects enabled that they should attempt to reconnect after receiving the message.

Example - A `Close` message without an error
```json
{
    "type": 7
}
```

Example - A `Close` message with an error
```json
{
    "type": 7,
    "error": "Connection closed because of an error!"
}
```

Example - A `Close` message with an error that allows automatic client reconnects.
```json
{
    "type": 7,
    "error": "Connection closed because of an error!",
    "allowReconnect": true
}
```

### JSON Header Encoding

Message headers are encoded into a JSON object, with string values, that are stored in the `headers` property. For example:

```json
{
    "type": 1,
    "headers": {
        "Foo": "Bar"
    },
    "invocationId": "123",
    "target": "Send",
    "arguments": [
        42,
        "Test Message"
    ]
}
```


### JSON Payload Encoding

Items in the arguments array within the `Invocation` message type, as well as the `item` value of the `StreamItem` message and the `result` value of the `Completion` message, encode values which have meaning to each particular Binder. A general guideline for encoding/decoding these values is provided in the "Type Mapping" section at the end of this document, but Binders should provide configuration to applications to allow them to customize these mappings. These mappings need not be self-describing, because when decoding the value, the Binder is expected to know the destination type (by looking up the definition of the method indicated by the Target).

## MessagePack (MsgPack) encoding

In the MsgPack Encoding of the SignalR Protocol, each Message is represented as a single MsgPack array containing items that correspond to properties of the given hub protocol message. The array items may be primitive values, arrays (e.g. method arguments) or objects (e.g. argument value). The first item in the array is the message type.

MessagePack uses different formats to encode values. Refer to the [MsgPack format spec](https://github.com/msgpack/msgpack/blob/master/spec.md#formats) for format definitions.

### Invocation Message Encoding

`Invocation` messages have the following structure:

```
[1, Headers, InvocationId, NonBlocking, Target, [Arguments], [StreamIds]]
```

* `1` - Message Type - `1` indicates this is an `Invocation` message.
* `Headers` - A MsgPack Map containing the headers, with string keys and string values (see MessagePack Headers Encoding below)
* InvocationId - One of:
  * A `Nil`, indicating that there is no Invocation ID, OR
  * A `String` encoding the Invocation ID for the message.
* Target - A `String` encoding the Target name, as expected by the Callee's Binder.
* Arguments - An Array containing arguments to apply to the method referred to in Target.
* StreamIds - An `Array` of strings representing unique ids for streams coming from the Caller to the Callee and being consumed by the method referred to in Target.

#### Example:

The following payload

```
0x96 0x01 0x80 0xa3 0x78 0x79 0x7a 0xa6 0x6d 0x65 0x74 0x68 0x6f 0x64 0x91 0x2a 0x90
```

is decoded as follows:

* `0x96` - 6-element array
* `0x01` - `1` (Message Type - `Invocation` message)
* `0x80` - Map of length 0 (Headers)
* `0xa3` - string of length 3 (InvocationId)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`
* `0xa6` - string of length 6 (Target)
* `0x6d` - `m`
* `0x65` - `e`
* `0x74` - `t`
* `0x68` - `h`
* `0x6f` - `o`
* `0x64` - `d`
* `0x91` - 1-element array (Arguments)
* `0x2a` - `42` (Argument value)
* `0x90` - 0-element array (StreamIds)

#### Non-Blocking Example:

The following payload
```
0x96 0x01 0x80 0xc0 0xa6 0x6d 0x65 0x74 0x68 0x6f 0x64 0x91 0x2a 0x90
```

is decoded as follows:

* `0x96` - 6-element array
* `0x01` - `1` (Message Type - `Invocation` message)
* `0x80` - Map of length 0 (Headers)
* `0xc0` - `nil` (Invocation ID)
* `0xa6` - string of length 6 (Target)
* `0x6d` - `m`
* `0x65` - `e`
* `0x74` - `t`
* `0x68` - `h`
* `0x6f` - `o`
* `0x64` - `d`
* `0x91` - 1-element array (Arguments)
* `0x2a` - `42` (Argument value)
* `0x90` - 0-element array (StreamIds)

### StreamInvocation Message Encoding

`StreamInvocation` messages have the following structure:

```
[4, Headers, InvocationId, Target, [Arguments], [StreamIds]]
```

* `4` - Message Type - `4` indicates this is a `StreamInvocation` message.
* `Headers` - A MsgPack Map containing the headers, with string keys and string values (see MessagePack Headers Encoding below)
* InvocationId - A `String` encoding the Invocation ID for the message.
* Target - A `String` encoding the Target name, as expected by the Callee's Binder.
* Arguments - An Array containing arguments to apply to the method referred to in Target.
* StreamIds - An `Array` of strings representing unique ids for streams coming from the Caller to the Callee and being consumed by the method referred to in Target.

Example:

The following payload

```
0x96 0x04 0x80 0xa3 0x78 0x79 0x7a 0xa6 0x6d 0x65 0x74 0x68 0x6f 0x64 0x91 0x2a 0x90
```

is decoded as follows:

* `0x96` - 6-element array
* `0x04` - `4` (Message Type - `StreamInvocation` message)
* `0x80` - Map of length 0 (Headers)
* `0xa3` - string of length 3 (InvocationId)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`
* `0xa6` - string of length 6 (Target)
* `0x6d` - `m`
* `0x65` - `e`
* `0x74` - `t`
* `0x68` - `h`
* `0x6f` - `o`
* `0x64` - `d`
* `0x91` - 1-element array (Arguments)
* `0x2a` - `42` (Argument value)
* `0x90` - 0-element array (StreamIds)

### StreamItem Message Encoding

`StreamItem` messages have the following structure:

```
[2, Headers, InvocationId, Item]
```

* `2` - Message Type - `2` indicates this is a `StreamItem` message
* `Headers` - A MsgPack Map containing the headers, with string keys and string values (see MessagePack Headers Encoding below)
* InvocationId - A `String` encoding the Invocation ID for the message
* Item - the value of the stream item

Example:

The following payload:
```
0x94 0x02 0x80 0xa3 0x78 0x79 0x7a 0x2a
```

is decoded as follows:

* `0x94` - 4-element array
* `0x02` - `2` (Message Type - `StreamItem` message)
* `0x80` - Map of length 0 (Headers)
* `0xa3` - string of length 3 (InvocationId)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`
* `0x2a` - `42` (Item)

### Completion Message Encoding

`Completion` messages have the following structure

```
[3, Headers, InvocationId, ResultKind, Result?]
```

* `3` - Message Type - `3` indicates this is a `Completion` message
* `Headers` - A MsgPack Map containing the headers, with string keys and string values (see MessagePack Headers Encoding below)
* InvocationId - A `String` encoding the Invocation ID for the message
* ResultKind - A flag indicating the invocation result kind:
    * `1` - Error result - Result contains a `String` with the error message
    * `2` - Void result - Result is absent
    * `3` - Non-Void result - Result contains the value returned by the server
* Result - An optional item containing the result of invocation. Absent if the server did not return any value (void methods)

Examples:

#### Error Result:

The following payload:
```
0x95 0x03 0x80 0xa3 0x78 0x79 0x7a 0x01 0xa5 0x45 0x72 0x72 0x6f 0x72
```

is decoded as follows:

* `0x94` - 4-element array
* `0x03` - `3` (Message Type - `Result` message)
* `0x80` - Map of length 0 (Headers)
* `0xa3` - string of length 3 (InvocationId)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`
* `0x01` - `1` (ResultKind - Error result)
* `0xa5` - string of length 5
* `0x45` - `E`
* `0x72` - `r`
* `0x72` - `r`
* `0x6f` - `o`
* `0x72` - `r`

#### Void Result:

The following payload:
```
0x94 0x03 0x80 0xa3 0x78 0x79 0x7a 0x02
```

is decoded as follows:

* `0x94` - 4-element array
* `0x03` - `3` (Message Type - `Result` message)
* `0x80` - Map of length 0 (Headers)
* `0xa3` - string of length 3 (InvocationId)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`
* `0x02` - `2` (ResultKind - Void result)

#### Non-Void Result:

The following payload:
```
0x95 0x03 0x80 0xa3 0x78 0x79 0x7a 0x03 0x2a
```

is decoded as follows:

* `0x95` - 5-element array
* `0x03` - `3` (Message Type - `Result` message)
* `0x80` - Map of length 0 (Headers)
* `0xa3` - string of length 3 (InvocationId)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`
* `0x03` - `3` (ResultKind - Non-Void result)
* `0x2a` - `42` (Result)

### CancelInvocation Message Encoding

`CancelInvocation` messages have the following structure

```
[5, Headers, InvocationId]
```

* `5` - Message Type - `5` indicates this is a `CancelInvocation` message
* `Headers` - A MsgPack Map containing the headers, with string keys and string values (see MessagePack Headers Encoding below)
* InvocationId - A `String` encoding the Invocation ID for the message

Example:

The following payload:
```
0x93 0x05 0x80 0xa3 0x78 0x79 0x7a
```

is decoded as follows:

* `0x93` - 3-element array
* `0x05` - `5` (Message Type `CancelInvocation` message)
* `0x80` - Map of length 0 (Headers)
* `0xa3` - string of length 3 (InvocationId)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`

### Ping Message Encoding

`Ping` messages have the following structure

```
[6]
```

* `6` - Message Type - `6` indicates this is a `Ping` message.

Examples:

#### Ping message

The following payload:
```
0x91 0x06
```

is decoded as follows:

* `0x91` - 1-element array
* `0x06` - `6` (Message Type - `Ping` message)

### Close Message Encoding

`Close` messages have the following structure

```
[7, Error, AllowReconnect?]
```

* `7` - Message Type - `7` indicates this is a `Close` message.
* `Error` - Error - A `String` encoding the error for the message.
* `AllowReconnect` - An optional `Boolean` indicating to clients with automatic reconnects enabled that they should attempt to reconnect after receiving the message.

Examples:

#### Close message

The following payload:
```
0x92 0x07 0xa3 0x78 0x79 0x7a
```

is decoded as follows:

* `0x92` - 2-element array
* `0x07` - `7` (Message Type - `Close` message)
* `0xa3` - string of length 3 (Error)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`

#### Close message that allows automatic client reconnects

The following payload:
```
0x93 0x07 0xa3 0x78 0x79 0x7a 0xc3
```

is decoded as follows:

* `0x93` - 3-element array
* `0x07` - `7` (Message Type - `Close` message)
* `0xa3` - string of length 3 (Error)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`
* `0xc3` - `True` (AllowReconnect)

### MessagePack Headers Encoding

Headers are encoded in MessagePack messages as a Map that immediately follows the type value. The Map can be empty, in which case it is represented by the byte `0x80`. If there are items in the map,
both the keys and values must be String values.

Headers are not valid in a Ping message. The Ping message is **always exactly encoded** as `0x91 0x06`

Below shows an example encoding of a message containing headers:

```
0x96 0x01 0x82 0xa1 0x78 0xa1 0x79 0xa1 0x7a 0xa1 0x7a 0xa3 0x78 0x79 0x7a 0xa6 0x6d 0x65 0x74 0x68 0x6f 0x64 0x91 0x2a 0x90
```

and is decoded as follows:

* `0x96` - 6-element array
* `0x01` - `1` (Message Type - `Invocation` message)
* `0x82` - Map of length 2
* `0xa1` - string of length 1 (Key)
* `0x78` - `x`
* `0xa1` - string of length 1 (Value)
* `0x79` - `y`
* `0xa1` - string of length 1 (Key)
* `0x7a` - `z`
* `0xa1` - string of length 1 (Value)
* `0x7a` - `z`
* `0xa3` - string of length 3 (InvocationId)
* `0x78` - `x`
* `0x79` - `y`
* `0x7a` - `z`
* `0xa6` - string of length 6 (Target)
* `0x6d` - `m`
* `0x65` - `e`
* `0x74` - `t`
* `0x68` - `h`
* `0x6f` - `o`
* `0x64` - `d`
* `0x91` - 1-element array (Arguments)
* `0x2a` - `42` (Argument value)
* `0x90` - 0-element array (StreamIds)

and interpreted as an Invocation message with headers: `'x' = 'y'` and `'z' = 'z'`.

## Type Mappings

Below are some sample type mappings between JSON types and the .NET client. This is not an exhaustive or authoritative list, just informative guidance. Official clients will provide ways for users to override the default mapping behavior for a particular method, parameter, or parameter type

|                  .NET Type                      |          JSON Type           |   MsgPack format family   |
| ----------------------------------------------- | ---------------------------- |---------------------------|
| `System.Byte`, `System.UInt16`, `System.UInt32` | `Number`                     | `positive fixint`, `uint` |
| `System.SByte`, `System.Int16`, `System.Int32`  | `Number`                     | `fixint`, `int`           |
| `System.UInt64`                                 | `Number`                     | `positive fixint`, `uint` |
| `System.Int64`                                  | `Number`                     | `fixint`, `int`           |
| `System.Single`                                 | `Number`                     | `float`                   |
| `System.Double`                                 | `Number`                     | `float`                   |
| `System.Boolean`                                | `true` or `false`            | `true`, `false`           |
| `System.String`                                 | `String`                     | `fixstr`, `str`           |
| `System.Byte`[]                                 | `String` (Base64-encoded)    | `bin`                     |
| `IEnumerable<T>`                                | `Array`                      | `bin`                     |
| custom `enum`                                   | `Number`                     | `fixint`, `int`           |
| custom `struct` or `class`                      | `Object`                     | `fixmap`, `map`           |

MessagePack payloads are wrapped in an outer message framing described below.

#### Binary encoding

```
([Length][Body])([Length][Body])... continues until end of the connection ...
```

* `[Length]` - A 32-bit unsigned integer encoded as VarInt. Variable size - 1-5 bytes.
* `[Body]` - The body of the message, exactly `[Length]` bytes in length.


##### VarInt

VarInt encodes the most significant bit as a marker indicating whether the byte is the last byte of the VarInt or if it spans to the next byte. Bytes appear in the reverse order - i.e. the first byte contains the least significant bits of the value.

Examples:
 * VarInt: `0x35` (`%00110101`) - the most significant bit is 0 so the value is %x0110101 i.e. 0x35 (53)
 * VarInt: `0x80 0x25` (`%10000000 %00101001`) - the most significant bit of the first byte is 1 so the remaining bits (%x0000000) are the lowest bits of the value. The most significant bit of the second byte is 0 meaning this is last byte of the VarInt. The actual value bits (%x0101001) need to be prepended to the bits we already read so the values is %01010010000000 i.e. 0x1480 (5248)

The biggest supported payloads are 2GB in size so the biggest number we need to support is 0x7fffffff which when encoded as VarInt is 0xFF 0xFF 0xFF 0xFF 0x07 - hence the maximum size of the length prefix is 5 bytes.

For example, when sending the following frames (`\n` indicates the actual Line Feed character, not an escape sequence):

* "Hello\nWorld"
* `0x01 0x02`

The encoding will be as follows, as a list of binary digits in hex (text in parentheses `()` are comments). Whitespace and newlines are irrelevant and for illustration only.
```
0x0B                                                   (start of frame; VarInt value: 11)
0x68 0x65 0x6C 0x6C 0x6F 0x0A 0x77 0x6F 0x72 0x6C 0x64 (UTF-8 encoding of 'Hello\nWorld')
0x02                                                   (start of frame; VarInt value: 2)
0x01 0x02                                              (body)
```
