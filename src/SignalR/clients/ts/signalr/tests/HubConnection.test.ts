// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AbortError } from "../src/Errors";
import { HubConnection, HubConnectionState } from "../src/HubConnection";
import { IConnection } from "../src/IConnection";
import { HubMessage, IHubProtocol, MessageType } from "../src/IHubProtocol";
import { ILogger, LogLevel } from "../src/ILogger";
import { TransferFormat } from "../src/ITransport";
import { JsonHubProtocol } from "../src/JsonHubProtocol";
import { NullLogger } from "../src/Loggers";
import { IStreamSubscriber } from "../src/Stream";
import { Subject } from "../src/Subject";
import { TextMessageFormat } from "../src/TextMessageFormat";

import { VerifyLogger } from "./Common";
import { TestConnection } from "./TestConnection";
import { delayUntil, PromiseSource, registerUnhandledRejectionHandler } from "./Utils";

function createHubConnection(connection: IConnection, logger?: ILogger | null, protocol?: IHubProtocol | null) {
    return HubConnection.create(connection, logger || NullLogger.instance, protocol || new JsonHubProtocol());
}

registerUnhandledRejectionHandler();

describe("HubConnection", () => {
    describe("start", () => {
        it("sends handshake message", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();
                    expect(connection.sentData.length).toBe(2);
                    expect(JSON.parse(connection.sentData[0])).toEqual({
                        protocol: "json",
                        version: 1,
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can change url", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();
                    await hubConnection.stop();
                    hubConnection.baseUrl = "http://newurl.com";
                    expect(hubConnection.baseUrl).toBe("http://newurl.com");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can change url in onclose", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    expect(hubConnection.baseUrl).toBe("http://example.com");
                    hubConnection.onclose(() => {
                        hubConnection.baseUrl = "http://newurl.com";
                    });

                    await hubConnection.stop();
                    expect(hubConnection.baseUrl).toBe("http://newurl.com");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("changing url while active throws", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    expect(() => {
                        hubConnection.baseUrl = "http://newurl.com";
                    }).toThrow("The HubConnection must be in the Disconnected or Reconnecting state to change the url.");

                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("state connected", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
                try {
                    await hubConnection.start();
                    expect(hubConnection.state).toBe(HubConnectionState.Connected);
                } finally {
                    await hubConnection.stop();
                }
            });
        });
    });

    describe("ping", () => {
        it("sends pings when receiving pings", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);

                hubConnection.keepAliveIntervalInMilliseconds = 5;

                try {
                    await hubConnection.start();

                    const pingInterval = setInterval(async () => {
                        await connection.receive({ type: MessageType.Ping });
                    }, 5);

                    await delayUntil(500);

                    clearInterval(pingInterval);

                    const numPings = connection.sentData.filter((s) => JSON.parse(s).type === MessageType.Ping).length;
                    expect(numPings).toBeGreaterThanOrEqual(2);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("does not send pings for connection with inherentKeepAlive", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection(true, true);
                const hubConnection = createHubConnection(connection, logger);

                hubConnection.keepAliveIntervalInMilliseconds = 5;

                try {
                    await hubConnection.start();
                    await delayUntil(500);

                    const numPings = connection.sentData.filter((s) => JSON.parse(s).type === MessageType.Ping).length;
                    expect(numPings).toEqual(0);
                } finally {
                    await hubConnection.stop();
                }
            });
        });
    });

    describe("stop", () => {
        it("state disconnected", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
                try {
                    await hubConnection.start();
                    expect(hubConnection.state).toBe(HubConnectionState.Connected);
                } finally {
                    await hubConnection.stop();
                    expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
                }
            });
        });

        it("sends close message", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    await hubConnection.stop();

                    // Handshake, Ping, Close
                    expect(connection.sentData.length).toBe(3);
                    expect(JSON.parse(connection.sentData[2])).toEqual({
                        type: MessageType.Close,
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });
    });

    describe("send", () => {
        it("sends a non blocking invocation", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);
                try {
                    // We don't actually care to wait for the send.
                    hubConnection.send("testMethod", "arg", 42)
                        .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

                    // Verify the message is sent
                    expect(connection.sentData.length).toBe(1);
                    expect(JSON.parse(connection.sentData[0])).toEqual({
                        arguments: [
                            "arg",
                            42,
                        ],
                        target: "testMethod",
                        type: MessageType.Invocation,
                    });
                } finally {
                    // Close the connection
                    await hubConnection.stop();
                }
            });
        });

        it("works if argument is null", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);
                try {
                    // We don't actually care to wait for the send.
                    hubConnection.send("testMethod", "arg", null)
                        .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

                    // Verify the message is sent
                    expect(connection.sentData.length).toBe(1);
                    expect(JSON.parse(connection.sentData[0])).toEqual({
                        arguments: [
                            "arg",
                            null,
                        ],
                        target: "testMethod",
                        type: MessageType.Invocation,
                    });
                } finally {
                    // Close the connection
                    await hubConnection.stop();
                }
            });
        });
    });

    describe("invoke", () => {
        it("sends an invocation", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);
                try {
                    // We don't actually care to wait for the send.
                    hubConnection.invoke("testMethod", "arg", 42)
                        .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

                    // Verify the message is sent
                    expect(connection.sentData.length).toBe(1);
                    expect(JSON.parse(connection.sentData[0])).toEqual({
                        arguments: [
                            "arg",
                            42,
                        ],
                        invocationId: connection.lastInvocationId,
                        target: "testMethod",
                        type: MessageType.Invocation,
                    });

                } finally {
                    // Close the connection
                    await hubConnection.stop();
                }
            });
        });

        it("can process handshake from text", async () => {
            await VerifyLogger.run(async (logger) => {
                let protocolCalled = false;

                const mockProtocol = new TestProtocol(TransferFormat.Text);
                mockProtocol.onreceive = (d) => {
                    protocolCalled = true;
                };

                const connection = new TestConnection(false);
                const hubConnection = createHubConnection(connection, logger, mockProtocol);
                try {
                    let startCompleted = false;
                    const startPromise = hubConnection.start().then(() => startCompleted = true);
                    const data = "{}" + TextMessageFormat.RecordSeparator;
                    expect(startCompleted).toBe(false);

                    connection.receiveText(data);
                    await startPromise;

                    // message only contained handshake response
                    expect(protocolCalled).toEqual(false);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can process handshake from binary", async () => {
            await VerifyLogger.run(async (logger) => {
                let protocolCalled = false;

                const mockProtocol = new TestProtocol(TransferFormat.Binary);
                mockProtocol.onreceive = (d) => {
                    protocolCalled = true;
                };

                const connection = new TestConnection(false);
                const hubConnection = createHubConnection(connection, logger, mockProtocol);
                try {
                    let startCompleted = false;
                    const startPromise = hubConnection.start().then(() => startCompleted = true);
                    expect(startCompleted).toBe(false);

                    // handshake response + message separator
                    const data = [0x7b, 0x7d, 0x1e];

                    connection.receiveBinary(new Uint8Array(data).buffer);
                    await startPromise;

                    // message only contained handshake response
                    expect(protocolCalled).toEqual(false);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can process handshake and additional messages from binary", async () => {
            await VerifyLogger.run(async (logger) => {
                let receivedProcotolData: ArrayBuffer | undefined;

                const mockProtocol = new TestProtocol(TransferFormat.Binary);
                mockProtocol.onreceive = (d) => receivedProcotolData = d as ArrayBuffer;

                const connection = new TestConnection(false);
                const hubConnection = createHubConnection(connection, logger, mockProtocol);
                try {
                    let startCompleted = false;
                    const startPromise = hubConnection.start().then(() => startCompleted = true);
                    expect(startCompleted).toBe(false);

                    // handshake response + message separator + message pack message
                    const data = [
                        0x7b, 0x7d, 0x1e, 0x65, 0x95, 0x03, 0x80, 0xa1, 0x30, 0x01, 0xd9, 0x5d, 0x54, 0x68, 0x65, 0x20, 0x63, 0x6c,
                        0x69, 0x65, 0x6e, 0x74, 0x20, 0x61, 0x74, 0x74, 0x65, 0x6d, 0x70, 0x74, 0x65, 0x64, 0x20, 0x74, 0x6f, 0x20,
                        0x69, 0x6e, 0x76, 0x6f, 0x6b, 0x65, 0x20, 0x74, 0x68, 0x65, 0x20, 0x73, 0x74, 0x72, 0x65, 0x61, 0x6d, 0x69,
                        0x6e, 0x67, 0x20, 0x27, 0x45, 0x6d, 0x70, 0x74, 0x79, 0x53, 0x74, 0x72, 0x65, 0x61, 0x6d, 0x27, 0x20, 0x6d,
                        0x65, 0x74, 0x68, 0x6f, 0x64, 0x20, 0x69, 0x6e, 0x20, 0x61, 0x20, 0x6e, 0x6f, 0x6e, 0x2d, 0x73, 0x74, 0x72,
                        0x65, 0x61, 0x6d, 0x69, 0x6e, 0x67, 0x20, 0x66, 0x61, 0x73, 0x68, 0x69, 0x6f, 0x6e, 0x2e,
                    ];

                    connection.receiveBinary(new Uint8Array(data).buffer);
                    await startPromise;

                    // left over data is the message pack message
                    expect(receivedProcotolData!.byteLength).toEqual(102);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can process handshake and additional messages from text", async () => {
            await VerifyLogger.run(async (logger) => {
                let receivedProcotolData: string | undefined;

                const mockProtocol = new TestProtocol(TransferFormat.Text);
                mockProtocol.onreceive = (d) => receivedProcotolData = d as string;

                const connection = new TestConnection(false);
                const hubConnection = createHubConnection(connection, logger, mockProtocol);
                try {
                    let startCompleted = false;
                    const startPromise = hubConnection.start().then(() => startCompleted = true);
                    expect(startCompleted).toBe(false);

                    const data = "{}" + TextMessageFormat.RecordSeparator + "{\"type\":6}" + TextMessageFormat.RecordSeparator;

                    connection.receiveText(data);
                    await startPromise;

                    expect(receivedProcotolData).toEqual("{\"type\":6}" + TextMessageFormat.RecordSeparator);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("start completes if connection closes and handshake not received yet", async () => {
            await VerifyLogger.run(async (logger) => {
                const mockProtocol = new TestProtocol(TransferFormat.Text);

                const connection = new TestConnection(false);
                const hubConnection = createHubConnection(connection, logger, mockProtocol);
                try {
                    let startCompleted = false;
                    const startPromise = hubConnection.start().then(() => startCompleted = true);
                    expect(startCompleted).toBe(false);

                    await connection.stop();
                    try {
                        await startPromise;
                    } catch (e) {
                        expect(e).toBeInstanceOf(AbortError);
                        expect((e as AbortError).message).toBe("The underlying connection was closed before the hub handshake could complete.");
                    }
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("rejects the promise when an error is received", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const invokePromise = hubConnection.invoke("testMethod", "arg", 42);

                    connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, error: "foo" });

                    await expect(invokePromise).rejects.toThrow("foo");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("resolves the promise when a result is received", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const invokePromise = hubConnection.invoke("testMethod", "arg", 42);

                    connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, result: "foo" });

                    expect(await invokePromise).toBe("foo");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("is able to send stream items to server with invoke", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const subject = new Subject();
                    const invokePromise = hubConnection.invoke("testMethod", "arg", subject);

                    expect(JSON.parse(connection.sentData[2])).toEqual({
                        arguments: ["arg"],
                        invocationId: "1",
                        streamIds: ["0"],
                        target: "testMethod",
                        type: MessageType.Invocation,
                    });

                    subject.next("item numero uno");
                    await new Promise<void>((resolve) => {
                        setTimeout(resolve, 50);
                    });
                    expect(JSON.parse(connection.sentData[3])).toEqual({
                        invocationId: "0",
                        item: "item numero uno",
                        type: MessageType.StreamItem,
                    });

                    connection.receive({ type: MessageType.Completion, invocationId: "1", result: "foo" });

                    expect(await invokePromise).toBe("foo");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("is able to send stream items to server with send", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const subject = new Subject();
                    await hubConnection.send("testMethod", "arg", subject);

                    expect(JSON.parse(connection.sentData[2])).toEqual({
                        arguments: ["arg"],
                        streamIds: ["0"],
                        target: "testMethod",
                        type: MessageType.Invocation,
                    });

                    subject.next("item numero uno");
                    await new Promise<void>((resolve) => {
                        setTimeout(resolve, 50);
                    });
                    expect(JSON.parse(connection.sentData[3])).toEqual({
                        invocationId: "0",
                        item: "item numero uno",
                        type: MessageType.StreamItem,
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("is able to send stream items to server with stream", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    let streamItem = "";
                    let streamError: any = null;
                    const subject = new Subject();
                    hubConnection.stream("testMethod", "arg", subject).subscribe({
                        complete: () => {
                        },
                        error: (e) => {
                            streamError = e;
                        },
                        next: (item) => {
                            streamItem = item;
                        },
                    });

                    expect(JSON.parse(connection.sentData[2])).toEqual({
                        arguments: ["arg"],
                        invocationId: "1",
                        streamIds: ["0"],
                        target: "testMethod",
                        type: MessageType.StreamInvocation,
                    });

                    subject.next("item numero uno");
                    await new Promise<void>((resolve) => {
                        setTimeout(resolve, 50);
                    });
                    expect(JSON.parse(connection.sentData[3])).toEqual({
                        invocationId: "0",
                        item: "item numero uno",
                        type: MessageType.StreamItem,
                    });

                    connection.receive({ type: MessageType.StreamItem, invocationId: "1", item: "foo" });
                    expect(streamItem).toEqual("foo");

                    expect(streamError).toBe(null);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("completes pending invocations when stopped", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);

                await hubConnection.start();

                const invokePromise = hubConnection.invoke("testMethod");
                await hubConnection.stop();

                await expect(invokePromise).rejects.toThrow("Invocation canceled due to the underlying connection being closed.");
            });
        });

        it("completes pending invocations when connection is lost", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const invokePromise = hubConnection.invoke("testMethod");
                    // Typically this would be called by the transport
                    connection.onclose!(new Error("Connection lost"));

                    await expect(invokePromise).rejects.toThrow("Connection lost");
                } finally {
                    await hubConnection.stop();
                }
            });
        });
    });

    describe("on", () => {
        it("invocations ignored in callbacks not registered", async () => {
            await VerifyLogger.run(async (logger) => {
                const warnings: string[] = [];
                const wrappingLogger = {
                    log: (logLevel: LogLevel, message: string) => {
                        if (logLevel === LogLevel.Warning) {
                            warnings.push(message);
                        }
                        logger.log(logLevel, message);
                    },
                } as ILogger;
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, wrappingLogger);
                try {
                    await hubConnection.start();

                    connection.receive({
                        arguments: ["test"],
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    expect(warnings).toEqual(["No client method with the name 'message' found."]);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("invocations ignored in callbacks that have registered then unregistered", async () => {
            await VerifyLogger.run(async (logger) => {
                const warnings: string[] = [];
                const wrappingLogger = {
                    log: (logLevel: LogLevel, message: string) => {
                        if (logLevel === LogLevel.Warning) {
                            warnings.push(message);
                        }
                        logger.log(logLevel, message);
                    },
                } as ILogger;
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, wrappingLogger);
                try {
                    await hubConnection.start();

                    const handler = () => { };
                    hubConnection.on("message", handler);
                    hubConnection.off("message", handler);

                    connection.receive({
                        arguments: ["test"],
                        invocationId: "0",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    expect(warnings).toEqual(["No client method with the name 'message' found.",
                        "No result given for 'message' method and invocation ID '0'."]);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("all handlers can be unregistered with just the method name", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    let count = 0;
                    const p = new PromiseSource<void>();
                    const handler = () => { count++; };
                    const secondHandler = () => {
                        count++;
                        p.resolve();
                    };
                    hubConnection.on("inc", handler);
                    hubConnection.on("inc", secondHandler);

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: "inc",
                        type: MessageType.Invocation,
                    });

                    await p;
                    hubConnection.off("inc");

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: "inc",
                        type: MessageType.Invocation,
                    });

                    expect(count).toBe(2);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("a single handler can be unregistered with the method name and handler", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    let count = 0;
                    let p = new PromiseSource<void>();
                    const handler = () => { count++; };
                    const secondHandler = () => {
                        count++;
                        p.resolve();
                    };
                    hubConnection.on("inc", handler);
                    hubConnection.on("inc", secondHandler);

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: "inc",
                        type: MessageType.Invocation,
                    });

                    await p;
                    p = new PromiseSource<void>();
                    hubConnection.off("inc", handler);

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: "inc",
                        type: MessageType.Invocation,
                    });

                    await p;

                    expect(count).toBe(3);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can't register the same handler multiple times", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    let count = 0;
                    const handler = () => { count++; };
                    hubConnection.on("inc", handler);
                    hubConnection.on("inc", handler);

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: "inc",
                        type: MessageType.Invocation,
                    });

                    expect(count).toBe(1);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("callback invoked when server invokes a method on the client", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    let value = "";
                    const p = new PromiseSource<void>();
                    hubConnection.on("message", (v) => {
                        value = v;
                        p.resolve();
                    });

                    connection.receive({
                        arguments: ["test"],
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    await p;
                    expect(value).toBe("test");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("callback invoked when server invokes a method on the client and then handles rejected promise on send", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                let promiseRejected = false;
                try {
                    await hubConnection.start();
                    const p = new PromiseSource<void>();
                    hubConnection.on("message", async () => {
                        // Force sending of response to error
                        connection.send = () => {
                            promiseRejected = true;
                            return Promise.reject(new Error("Send error"));
                        }
                        p.resolve();
                    });
                    connection.receive({
                        arguments: ["test"],
                        nonblocking: true,
                        target: "message",
                        invocationId: "0",
                        type: MessageType.Invocation,
                    });

                    await p;
                    expect(promiseRejected).toBe(true);
                } finally {
                    await hubConnection.stop();
                }
            }, new RegExp("Invoke client method threw error: Error: Send error"));
        });

        it("stop on handshake error", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection(false);
                const hubConnection = createHubConnection(connection, logger);
                try {
                    let closeError: Error | undefined;
                    hubConnection.onclose((e) => closeError = e);

                    let startCompleted = false;
                    const startPromise = hubConnection.start().then(() => startCompleted = true);
                    expect(startCompleted).toBe(false);
                    try {
                        connection.receiveHandshakeResponse("Error!");
                    } catch {
                    }
                    await expect(startPromise)
                        .rejects
                        .toThrow("Server returned handshake error: Error!");

                    expect(closeError).toEqual(undefined);
                } finally {
                    await hubConnection.stop();
                }
            },
            "Server returned handshake error: Error!");
        });

        it("connection stopped before handshake completes",async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection(false);
                const hubConnection = createHubConnection(connection, logger);

                let startCompleted = false;
                const startPromise = hubConnection.start().then(() => startCompleted = true);
                expect(startCompleted).toBe(false);

                await hubConnection.stop()

                try {
                    await startPromise
                } catch (e) {
                    expect(e).toBeInstanceOf(AbortError);
                    expect((e as AbortError).message).toBe("The connection was stopped before the hub handshake could complete.");
                }
            }, "The connection was stopped before the hub handshake could complete.");
        });

        it("stop on close message", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    const p = new PromiseSource<void>();
                    let closeError: Error | undefined;
                    hubConnection.onclose((e) => {
                        p.resolve();
                        closeError = e;
                    });

                    await hubConnection.start();

                    connection.receive({
                        type: MessageType.Close,
                    });

                    await p;
                    expect(closeError).toBeUndefined();
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("stop on error close message", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    let isClosed = false;
                    let closeError: Error | undefined;
                    hubConnection.onclose((e) => {
                        isClosed = true;
                        closeError = e;
                    });

                    await hubConnection.start();

                    // allowReconnect Should have no effect since auto reconnect is disabled by default.
                    connection.receive({
                        allowReconnect: true,
                        error: "Error!",
                        type: MessageType.Close,
                    });

                    expect(isClosed).toEqual(true);
                    expect(closeError!.message).toEqual("Server returned an error on close: Error!");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can have multiple callbacks", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    let numInvocations1 = 0;
                    let numInvocations2 = 0;
                    const p = new PromiseSource<void>();
                    hubConnection.on("message", () => numInvocations1++);
                    hubConnection.on("message", () => {
                        numInvocations2++;
                        p.resolve();
                    });

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    await p;
                    expect(numInvocations1).toBe(1);
                    expect(numInvocations2).toBe(1);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can unsubscribe from on", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    let numInvocations = 0;
                    const callback = () => numInvocations++;
                    hubConnection.on("message", callback);

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    hubConnection.off("message", callback);

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    expect(numInvocations).toBe(1);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("unsubscribing dynamically doesn't affect the current invocation loop", async () => {
            await VerifyLogger.run(async (logger) => {
                const eventToTrack = "eventName";

                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    let numInvocations1 = 0;
                    let numInvocations2 = 0;
                    const callback1 = () => {
                        hubConnection.off(eventToTrack, callback1);
                        numInvocations1++;
                    }
                    let p = new PromiseSource<void>();
                    const callback2 = () => {
                        numInvocations2++;
                        p.resolve();
                    };

                    hubConnection.on(eventToTrack, callback1);
                    hubConnection.on(eventToTrack, callback2);

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: eventToTrack,
                        type: MessageType.Invocation,
                    });

                    await p;
                    p = new PromiseSource<void>();
                    expect(numInvocations1).toBe(1);
                    expect(numInvocations2).toBe(1);

                    connection.receive({
                        arguments: [],
                        nonblocking: true,
                        target: eventToTrack,
                        type: MessageType.Invocation,
                    });

                    await p;
                    expect(numInvocations1).toBe(1);
                    expect(numInvocations2).toBe(2);
                }
                finally {
                    await hubConnection.stop();
                }
            });
        });

        it("unsubscribing from non-existing callbacks no-ops", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    hubConnection.off("_", () => { });
                    hubConnection.on("message", (t) => { });
                    hubConnection.on("message", () => { });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("using null/undefined for methodName or method no-ops", async () => {
            await VerifyLogger.run(async (logger) => {
                const warnings: string[] = [];
                const wrappingLogger = {
                    log(logLevel: LogLevel, message: string) {
                        if (logLevel === LogLevel.Warning) {
                            warnings.push(message);
                        }
                        logger.log(logLevel, message);
                    },
                } as ILogger;

                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, wrappingLogger);
                try {
                    await hubConnection.start();

                    hubConnection.on(null!, undefined!);
                    hubConnection.on(undefined!, null!);
                    hubConnection.on("message", null!);
                    hubConnection.on("message", undefined!);
                    hubConnection.on(null!, () => { });
                    hubConnection.on(undefined!, () => { });

                    // invoke a method to make sure we are not trying to use null/undefined
                    connection.receive({
                        arguments: [],
                        invocationId: "0",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    expect(warnings).toEqual(["No client method with the name 'message' found.",
                        "No result given for 'message' method and invocation ID '0'."]);

                    hubConnection.off(null!, undefined!);
                    hubConnection.off(undefined!, null!);
                    hubConnection.off("message", null!);
                    hubConnection.off("message", undefined!);
                    hubConnection.off(null!, () => { });
                    hubConnection.off(undefined!, () => { });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can return result from callback", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.on("message", () => 10);

                    connection.receive({
                        arguments: [],
                        invocationId: "1",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(3);
                    expect(connection.parsedSentData[2].type).toEqual(3);
                    expect(connection.parsedSentData[2].result).toEqual(10);
                    expect(connection.parsedSentData[2].invocationId).toEqual("1");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can return null result from callback", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.on("message", () => null);

                    connection.receive({
                        arguments: [],
                        invocationId: "1",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(3);
                    expect(connection.parsedSentData[2].type).toEqual(3);
                    expect(connection.parsedSentData[2].result).toBeNull();
                    expect(connection.parsedSentData[2].invocationId).toEqual("1");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can return task result from callback", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const p = new PromiseSource<number>();
                    hubConnection.on("message", () => p);

                    connection.receive({
                        arguments: [],
                        invocationId: "1",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });
                    p.resolve(13);

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(3);
                    expect(connection.parsedSentData[2].type).toEqual(3);
                    expect(connection.parsedSentData[2].result).toEqual(13);
                    expect(connection.parsedSentData[2].invocationId).toEqual("1");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can throw from callback when expecting result", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.on("message", () => { throw new Error("from callback"); });

                    connection.receive({
                        arguments: [],
                        invocationId: "1",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(3);
                    expect(connection.parsedSentData[2].type).toEqual(3);
                    expect(connection.parsedSentData[2].error).toEqual("Error: from callback");
                    expect(connection.parsedSentData[2].invocationId).toEqual("1");
                } finally {
                    await hubConnection.stop();
                }
            }, "A callback for the method 'message' threw error 'Error: from callback'.");
        });

        it("multiple results sends error", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.on("message", () => 3);
                    hubConnection.on("message", () => 4);

                    connection.receive({
                        arguments: [],
                        invocationId: "1",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(3);
                    expect(connection.parsedSentData[2].type).toEqual(3);
                    expect(connection.parsedSentData[2].error).toEqual('Client provided multiple results.');
                    expect(connection.parsedSentData[2].invocationId).toEqual("1");
                } finally {
                    await hubConnection.stop();
                }
            }, "Multiple results provided for 'message'. Sending error to server.");
        });

        it("multiple result handlers error from last one sent", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.on("message", () => 3);
                    hubConnection.on("message", () => { throw new Error("from callback"); });

                    connection.receive({
                        arguments: [],
                        invocationId: "1",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(3);
                    expect(connection.parsedSentData[2].type).toEqual(3);
                    expect(connection.parsedSentData[2].error).toEqual("Error: from callback");
                    expect(connection.parsedSentData[2].result).toBeUndefined();
                    expect(connection.parsedSentData[2].invocationId).toEqual("1");
                } finally {
                    await hubConnection.stop();
                }
            }, "A callback for the method 'message' threw error 'Error: from callback'.");
        });

        it("multiple result handlers ignore error if last one has result", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.on("message", () => { throw new Error("from callback"); });
                    hubConnection.on("message", () => 3);

                    connection.receive({
                        arguments: [],
                        invocationId: "1",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(3);
                    expect(connection.parsedSentData[2].type).toEqual(3);
                    expect(connection.parsedSentData[2].result).toEqual(3);
                    expect(connection.parsedSentData[2].error).toBeUndefined();
                    expect(connection.parsedSentData[2].invocationId).toEqual("1");
                } finally {
                    await hubConnection.stop();
                }
            }, "A callback for the method 'message' threw error 'Error: from callback'.");
        });

        it("sends completion error if return result expected but not returned", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.on("message", () => {});

                    connection.receive({
                        arguments: [],
                        invocationId: "1",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(3);
                    expect(connection.parsedSentData[2].type).toEqual(3);
                    expect(connection.parsedSentData[2].error).toEqual("Client didn't provide a result.");
                    expect(connection.parsedSentData[2].invocationId).toEqual("1");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("sends completion error if return result expected but no handlers", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    connection.receive({
                        arguments: [],
                        invocationId: "1",
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(3);
                    expect(connection.parsedSentData[2].type).toEqual(3);
                    expect(connection.parsedSentData[2].error).toEqual("Client didn't provide a result.");
                    expect(connection.parsedSentData[2].invocationId).toEqual("1");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("logs error if return result not expected", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.on("message", () => 13);

                    connection.receive({
                        arguments: [],
                        invocationId: undefined,
                        nonblocking: true,
                        target: "message",
                        type: MessageType.Invocation,
                    });

                    // nothing to wait on and the code is all synchronous, but because of how JS and async works we need to trigger
                    // async here to guarantee the sent message is written
                    await delayUntil(1);

                    expect(connection.parsedSentData.length).toEqual(2);
                } finally {
                    await hubConnection.stop();
                }
            }, "Result given for 'message' method but server is not expecting a result.");
        });
    });

    describe("stream", () => {
        it("sends an invocation", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.stream("testStream", "arg", 42);

                    // Verify the message is sent (+ handshake + ping)
                    expect(connection.sentData.length).toBe(3);
                    expect(JSON.parse(connection.sentData[2])).toEqual({
                        arguments: [
                            "arg",
                            42,
                        ],
                        invocationId: connection.lastInvocationId,
                        target: "testStream",
                        type: MessageType.StreamInvocation,
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("completes with an error when an error is yielded", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const observer = new TestObserver();
                    hubConnection.stream<any>("testMethod", "arg", 42)
                        .subscribe(observer);

                    connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, error: "foo" });

                    await expect(observer.completed).rejects.toThrow("Error: foo");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("completes the observer when a completion is received", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const observer = new TestObserver();
                    hubConnection.stream<any>("testMethod", "arg", 42)
                        .subscribe(observer);

                    connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId });

                    expect(await observer.completed).toEqual([]);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("completes pending streams when stopped", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const observer = new TestObserver();
                    hubConnection.stream<any>("testMethod")
                        .subscribe(observer);
                    await hubConnection.stop();

                    await expect(observer.completed).rejects.toThrow("Error: Invocation canceled due to the underlying connection being closed.");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("completes pending streams when connection is lost", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const observer = new TestObserver();
                    hubConnection.stream<any>("testMethod")
                        .subscribe(observer);

                    // Typically this would be called by the transport
                    connection.onclose!(new Error("Connection lost"));

                    await expect(observer.completed).rejects.toThrow("Error: Connection lost");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("yields items as they arrive", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const observer = new TestObserver();
                    hubConnection.stream<any>("testMethod")
                        .subscribe(observer);

                    connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 1 });
                    expect(observer.itemsReceived).toEqual([1]);

                    connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 2 });
                    expect(observer.itemsReceived).toEqual([1, 2]);

                    connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 3 });
                    expect(observer.itemsReceived).toEqual([1, 2, 3]);

                    connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId });
                    expect(await observer.completed).toEqual([1, 2, 3]);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("does not require error function registered", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();
                    hubConnection.stream("testMethod").subscribe(NullSubscriber.instance);

                    // Typically this would be called by the transport
                    // triggers observer.error()
                    connection.onclose!(new Error("Connection lost"));
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("does not require complete function registered", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();
                    hubConnection.stream("testMethod").subscribe(NullSubscriber.instance);

                    // Send completion to trigger observer.complete()
                    // Expectation is connection.receive will not throw
                    connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("can be canceled", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    const observer = new TestObserver();
                    const subscription = hubConnection.stream("testMethod")
                        .subscribe(observer);

                    connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 1 });
                    expect(observer.itemsReceived).toEqual([1]);

                    subscription.dispose();

                    connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 2 });
                    // Observer should no longer receive messages
                    expect(observer.itemsReceived).toEqual([1]);

                    // Close message sent asynchronously so we need to wait
                    await delayUntil(1000, () => connection.sentData.length === 4);
                    // Verify the cancel is sent (+ handshake)
                    expect(connection.sentData.length).toBe(4);
                    expect(JSON.parse(connection.sentData[3])).toEqual({
                        invocationId: connection.lastInvocationId,
                        type: MessageType.CancelInvocation,
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("ignores error from 'next' and 'complete' function", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.stream("testMethod")
                        .subscribe({
                            next: (_item) => {
                                throw new Error("from next");
                            },
                            error: (_e) => {},
                            complete: () => {
                                throw new Error("from complete");
                            }
                        });

                    connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 1 });

                    connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId });
                } finally {
                    await hubConnection.stop();
                }
            }, /Stream callback threw error: Error: from complete/,
            /Stream callback threw error: Error: from next/);
        });

        it("ignores error from 'error' function", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    hubConnection.stream("testMethod")
                        .subscribe({
                            next: (_item) => {},
                            error: (_e) => {
                                throw new Error("from error");
                            },
                            complete: () => {}
                        });

                    connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 1 });
                } finally {
                    await hubConnection.stop();
                }
            }, /Stream 'error' callback called with 'Error: Invocation canceled due to the underlying connection being closed.' threw error: Error: from error/);
        });
    });

    describe("onClose", () => {
        it("can have multiple callbacks", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                await hubConnection.start();

                try {
                    let invocations = 0;
                    hubConnection.onclose((e) => invocations++);
                    hubConnection.onclose((e) => invocations++);
                    // Typically this would be called by the transport
                    connection.onclose!();
                    expect(invocations).toBe(2);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("callbacks receive error", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                await hubConnection.start();

                try {
                    let error: Error | undefined;
                    hubConnection.onclose((e) => error = e);

                    // Typically this would be called by the transport
                    connection.onclose!(new Error("Test error."));
                    expect(error!.message).toBe("Test error.");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("ignores null callbacks", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    hubConnection.onclose(null!);
                    hubConnection.onclose(undefined!);
                    // Typically this would be called by the transport
                    (hubConnection as any).connectionState = HubConnectionState.Connected;
                    connection.onclose!();
                    // expect no errors
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("state disconnected", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                await hubConnection.start();

                try {
                    let state: HubConnectionState | undefined;
                    hubConnection.onclose((e) => state = hubConnection.state);
                    // Typically this would be called by the transport
                    connection.onclose!();

                    expect(state).toBe(HubConnectionState.Disconnected);
                } finally {
                    await hubConnection.stop();
                }
            });
        });
    });

    describe("keepAlive", () => {
        it("can receive ping messages", async () => {
            await VerifyLogger.run(async (logger) => {
                // Receive the ping mid-invocation so we can see that the rest of the flow works fine

                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();
                    const invokePromise = hubConnection.invoke("testMethod", "arg", 42);

                    connection.receive({ type: MessageType.Ping });
                    connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, result: "foo" });

                    expect(await invokePromise).toBe("foo");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("does not terminate if messages are received", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    const timeoutInMilliseconds = 400;
                    hubConnection.serverTimeoutInMilliseconds = timeoutInMilliseconds;

                    const p = new PromiseSource<Error | undefined>();
                    hubConnection.onclose((e) => p.resolve(e));

                    await hubConnection.start();

                    const pingInterval = setInterval(async () => {
                        await connection.receive({ type: MessageType.Ping });
                    }, 10);

                    await delayUntil(timeoutInMilliseconds * 2);

                    await connection.stop();
                    clearInterval(pingInterval);

                    const error = await p.promise;

                    expect(error).toBeUndefined();
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("terminates if no messages received within timeout interval", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                const hubConnection = createHubConnection(connection, logger);
                try {
                    hubConnection.serverTimeoutInMilliseconds = 100;

                    const p = new PromiseSource<Error | undefined>();
                    hubConnection.onclose((e) => p.resolve(e));

                    await hubConnection.start();

                    const error = await p.promise;

                    expect(error).toEqual(new Error("Server timeout elapsed without receiving a message from the server."));
                } finally {
                    await hubConnection.stop();
                }
            });
        });
    });

    describe("stateful reconnect", () => {
        it("sends sequence message on reconnect", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    // HubConnection should set these
                    expect(connection.features.disconnected).toBeDefined();
                    expect(connection.features.resend).toBeDefined();

                    // Pretend TestConnection disconnected
                    connection.features.disconnected();
                    await connection.features.resend();

                    expect(connection.sentData.length).toBe(3);
                    expect(JSON.parse(connection.sentData[2])).toEqual({
                        type: MessageType.Sequence,
                        sequenceId: 1,
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("resends sent messages on reconnect", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    // HubConnection should set these
                    expect(connection.features.disconnected).toBeDefined();
                    expect(connection.features.resend).toBeDefined();

                    await hubConnection.send("test", 13);
                    await hubConnection.send("test", 12);
                    await hubConnection.send("test", 11);

                    // Pretend TestConnection disconnected
                    connection.features.disconnected();
                    await connection.features.resend();

                    expect(connection.sentData.length).toBe(9);
                    expect(JSON.parse(connection.sentData[5])).toEqual({
                        type: MessageType.Sequence,
                        sequenceId: 1,
                    });
                    expect(JSON.parse(connection.sentData[6])).toEqual({
                        type: MessageType.Invocation,
                        target: "test",
                        arguments: [13]
                    });
                    expect(JSON.parse(connection.sentData[7])).toEqual({
                        type: MessageType.Invocation,
                        target: "test",
                        arguments: [12]
                    });
                    expect(JSON.parse(connection.sentData[8])).toEqual({
                        type: MessageType.Invocation,
                        target: "test",
                        arguments: [11]
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("resends sent messages while disconnected on reconnect", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    // HubConnection should set these
                    expect(connection.features.disconnected).toBeDefined();
                    expect(connection.features.resend).toBeDefined();

                    await hubConnection.send("test", 13);

                    // Pretend TestConnection disconnected
                    connection.features.disconnected();

                    // Send while disconnected, waits until resend completes
                    let sendTask = hubConnection.send("test", 22);
                    let sendDone = false;
                    sendTask = sendTask.finally(() => sendDone = true);

                    expect(sendDone).toBeFalsy();

                    await connection.features.resend();

                    await sendTask;
                    expect(sendDone).toBeTruthy();

                    expect(connection.sentData.length).toBe(6);
                    expect(JSON.parse(connection.sentData[3])).toEqual({
                        type: MessageType.Sequence,
                        sequenceId: 1,
                    });
                    expect(JSON.parse(connection.sentData[4])).toEqual({
                        type: MessageType.Invocation,
                        target: "test",
                        arguments: [13]
                    });
                    expect(JSON.parse(connection.sentData[5])).toEqual({
                        type: MessageType.Invocation,
                        target: "test",
                        arguments: [22]
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("receiving ack removes buffered messages", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    // HubConnection should set these
                    expect(connection.features.disconnected).toBeDefined();
                    expect(connection.features.resend).toBeDefined();

                    await hubConnection.send("test", 13);
                    await hubConnection.send("test", 14);
                    await hubConnection.send("test", 15);

                    connection.receive({ type: MessageType.Ack, sequenceId: 2 });

                    // Pretend TestConnection disconnected
                    connection.features.disconnected();

                    await connection.features.resend();

                    expect(connection.sentData.length).toBe(7);
                    expect(JSON.parse(connection.sentData[5])).toEqual({
                        type: MessageType.Sequence,
                        sequenceId: 3,
                    });
                    expect(JSON.parse(connection.sentData[6])).toEqual({
                        type: MessageType.Invocation,
                        target: "test",
                        arguments: [15]
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("sends ack after receiving message", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });

                    await delayUntil(2000, () => connection.sentData.length === 3);

                    expect(JSON.parse(connection.sentData[2])).toEqual({
                        type: MessageType.Ack,
                        sequenceId: 1,
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("sends ack after receiving many messages", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });

                    await delayUntil(2000, () => connection.sentData.length === 3);

                    expect(JSON.parse(connection.sentData[2])).toEqual({
                        type: MessageType.Ack,
                        sequenceId: 4,
                    });
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("messages ignored after reconnect if already received", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    let methodCalled = 0;
                    hubConnection.on("t", () => {
                        methodCalled++;
                    });
                    await hubConnection.start();

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });

                    expect(methodCalled).toBe(2);

                    // HubConnection should set these
                    expect(connection.features.disconnected).toBeDefined();
                    expect(connection.features.resend).toBeDefined();

                    // Pretend TestConnection disconnected
                    connection.features.disconnected();

                    await connection.features.resend();

                    connection.receive({ type: MessageType.Sequence, sequenceId: 1 });

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });

                    expect(methodCalled).toBe(2);

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    expect(methodCalled).toBe(3);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("messages ignored after reconnect if sequence message not received", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    let methodCalled = 0;
                    hubConnection.on("t", () => {
                        methodCalled++;
                    });
                    await hubConnection.start();

                    // HubConnection should set these
                    expect(connection.features.disconnected).toBeDefined();
                    expect(connection.features.resend).toBeDefined();

                    // Pretend TestConnection disconnected
                    connection.features.disconnected();

                    await connection.features.resend();

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });

                    expect(methodCalled).toBe(0);

                    connection.receive({ type: MessageType.Sequence, sequenceId: 1 });
                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });

                    expect(methodCalled).toBe(1);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("sequence message updates what messages are ignored", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    let methodCalled = 0;
                    hubConnection.on("t", () => {
                        methodCalled++;
                    });
                    await hubConnection.start();

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });

                    expect(methodCalled).toBe(2);

                    // HubConnection should set these
                    expect(connection.features.disconnected).toBeDefined();
                    expect(connection.features.resend).toBeDefined();

                    // Pretend TestConnection disconnected
                    connection.features.disconnected();

                    await connection.features.resend();

                    connection.receive({ type: MessageType.Sequence, sequenceId: 2 });

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    expect(methodCalled).toBe(2);

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });

                    expect(methodCalled).toBe(3);
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("sequence message with ID too high closes connection", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    let methodCalled = 0;
                    hubConnection.on("t", () => {
                        methodCalled++;
                    });
                    const closeError = new PromiseSource<Error | undefined>();
                    hubConnection.onclose((e) => {
                        closeError.reject(e);
                    });
                    await hubConnection.start();

                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });
                    connection.receive({ type: MessageType.Invocation, target: "t", arguments: [] });

                    expect(methodCalled).toBe(2);

                    // HubConnection should set these
                    expect(connection.features.disconnected).toBeDefined();
                    expect(connection.features.resend).toBeDefined();

                    // Pretend TestConnection disconnected
                    connection.features.disconnected();

                    await connection.features.resend();

                    connection.receive({ type: MessageType.Sequence, sequenceId: 4 });

                    await expect(closeError).rejects.toThrow("Sequence ID greater than amount of messages we've received.");
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("buffer full blocks sending, unblocks with ack", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    const closeError = new PromiseSource<Error | undefined>();
                    hubConnection.onclose((e) => {
                        closeError.resolve(e);
                    });

                    await hubConnection.start();

                    // send large message to fill buffer, will be waiting until an ack occurs
                    let sendTask = hubConnection.send("t", 'x'.repeat(100_000));
                    let sendDone = false;
                    sendTask = sendTask.finally(() => sendDone = true);

                    await delayUntil(1);

                    expect(sendDone).toBeFalsy();

                    connection.receive({ type: MessageType.Ack, sequenceId: 1 });

                    await sendTask;
                    expect(sendDone).toBeTruthy();
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("buffer full blocks sending, unblocks with close message", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    const closeError = new PromiseSource<Error | undefined>();
                    hubConnection.onclose((e) => {
                        closeError.resolve(e);
                    });

                    await hubConnection.start();

                    // send large message to fill buffer, will be waiting until an ack occurs
                    let sendTask = hubConnection.send("t", 'x'.repeat(100_000));
                    let sendDone = false;
                    sendTask = sendTask.finally(() => sendDone = true);

                    await delayUntil(1);

                    expect(sendDone).toBeFalsy();

                    connection.receive({ type: MessageType.Close, error: "test" });

                    await expect(sendTask).rejects.toThrow("Server returned an error on close: test");
                    expect(sendDone).toBeTruthy();

                    expect(await closeError).toEqual(new Error("Server returned an error on close: test"));
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("buffer full blocks sending, unblocks when calling stop", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    await hubConnection.start();

                    // send large message to fill buffer, will be waiting until an ack occurs
                    let sendTask = hubConnection.send("t", 'x'.repeat(100_000));
                    let sendDone = false;
                    sendTask = sendTask.finally(() => sendDone = true);

                    await delayUntil(1);

                    expect(sendDone).toBeFalsy();

                    await hubConnection.stop();

                    await expect(sendTask).rejects.toThrow("Connection closed.");

                    expect(sendDone).toBeTruthy();
                } finally {
                    await hubConnection.stop();
                }
            });
        });

        it("buffer full blocks sending, other sends also block promise but still send over connection", async () => {
            await VerifyLogger.run(async (logger) => {
                const connection = new TestConnection();
                // tell HubConnection we "negotiated" reconnect
                connection.features.reconnect = true;

                const hubConnection = createHubConnection(connection, logger);
                try {
                    const closeError = new PromiseSource<Error | undefined>();
                    hubConnection.onclose((e) => {
                        closeError.resolve(e);
                    });

                    await hubConnection.start();

                    // send large message to fill buffer, will be waiting until an ack occurs
                    let sendTask = hubConnection.send("t", 'x'.repeat(100_000));
                    let sendDone = false;
                    sendTask = sendTask.finally(() => sendDone = true);

                    await delayUntil(1);

                    expect(sendDone).toBeFalsy();

                    // send large message to fill buffer, will be waiting until an ack occurs
                    const sendTask2 = hubConnection.send("t", 'x');
                    let sendDone2 = false;
                    sendTask2.finally(() => sendDone2 = true);

                    await delayUntil(1);

                    expect(sendDone2).toBeFalsy();

                    expect(connection.sentData.length).toBe(4);
                    expect(JSON.parse(connection.sentData[3])).toEqual({
                        type: MessageType.Invocation,
                        arguments: ['x'],
                        target: 't'
                    });

                    connection.receive({ type: MessageType.Ack, sequenceId: 1 });

                    await sendTask;
                    expect(sendDone).toBeTruthy();

                    // Second send is also unblocked because it is under the buffer limit once the large message is acked
                    await sendTask2;
                    expect(sendDone2).toBeTruthy();
                } finally {
                    await hubConnection.stop();
                }
            });
        });
    });
});

class TestProtocol implements IHubProtocol {
    public readonly name: string = "TestProtocol";
    public readonly version: number = 1;

    public readonly transferFormat: TransferFormat;

    public onreceive: ((data: string | ArrayBuffer) => void) | null;

    constructor(transferFormat: TransferFormat) {
        this.transferFormat = transferFormat;
        this.onreceive = null;
    }

    public parseMessages(input: any): HubMessage[] {
        if (this.onreceive) {
            this.onreceive(input);
        }

        return [];
    }

    public writeMessage(message: HubMessage): any {
        if (message.type === 6 || message.type === 7) {
            return `{"type": ${message.type}}` + TextMessageFormat.RecordSeparator;
        } else {
            throw new Error(`update TestProtocol to write message type ${message.type}`);
        }
    }
}

class TestObserver implements IStreamSubscriber<any> {
    public readonly closed: boolean = false;
    public itemsReceived: any[];
    private _itemsSource: PromiseSource<any[]>;

    get completed(): Promise<any[]> {
        return this._itemsSource.promise;
    }

    constructor() {
        this.itemsReceived = [];
        this._itemsSource = new PromiseSource<any[]>();
    }

    public next(value: any) {
        this.itemsReceived.push(value);
    }

    public error(err: any) {
        this._itemsSource.reject(new Error(err));
    }

    public complete() {
        this._itemsSource.resolve(this.itemsReceived);
    }
}

class NullSubscriber<T> implements IStreamSubscriber<T> {
    public static instance: NullSubscriber<any> = new NullSubscriber();

    private constructor() {
    }

    public next(value: T): void {
    }
    public error(err: any): void {
    }
    public complete(): void {
    }
}
