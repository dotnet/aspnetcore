// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HubConnection } from "../src/HubConnection";
import { IConnection } from "../src/IConnection";
import { IHubProtocol, MessageType } from "../src/IHubProtocol";
import { ILogger } from "../src/ILogger";
import { JsonHubProtocol } from "../src/JsonHubProtocol";
import { NullLogger } from "../src/Loggers";
import { Subject } from "../src/Subject";
import { VerifyLogger } from "./Common";
import { TestConnection } from "./TestConnection";
import { delayUntil, registerUnhandledRejectionHandler } from "./Utils";

registerUnhandledRejectionHandler();

function createHubConnection(connection: IConnection, logger?: ILogger | null, protocol?: IHubProtocol | null) {
    return HubConnection.create(connection, logger || NullLogger.instance, protocol || new JsonHubProtocol());
}

// These tests check that the message size doesn't change without us being aware of it and making a conscious decision to increase the size

describe("Message size", () => {
    it("send invocation", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection, logger);
            try {
                // We don't actually care to wait for the send.
                // tslint:disable-next-line:no-floating-promises
                hubConnection.send("target", 1)
                    .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

                // Verify the message is sent
                expect(connection.sentData.length).toBe(1);
                expect(connection.parsedSentData[0].type).toEqual(MessageType.Invocation);
                expect((connection.sentData[0] as string).length).toEqual(44);
            } finally {
                // Close the connection
                await hubConnection.stop();
            }
        });
    });

    it("invoke invocation", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection, logger);
            try {
                // We don't actually care to wait for the invoke.
                // tslint:disable-next-line:no-floating-promises
                hubConnection.invoke("target", 1)
                    .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

                // Verify the message is sent
                expect(connection.sentData.length).toBe(1);
                expect(connection.parsedSentData[0].type).toEqual(MessageType.Invocation);
                expect((connection.sentData[0] as string).length).toEqual(63);
            } finally {
                // Close the connection
                await hubConnection.stop();
            }
        });
    });

    it("stream invocation", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection, logger);
            try {
                hubConnection.stream("target", 1);

                // Verify the message is sent
                expect(connection.sentData.length).toBe(1);
                expect(connection.parsedSentData[0].type).toEqual(MessageType.StreamInvocation);
                expect((connection.sentData[0] as string).length).toEqual(63);
            } finally {
                // Close the connection
                await hubConnection.stop();
            }
        });
    });

    it("upload invocation", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection, logger);
            try {
                // We don't actually care to wait for the invoke.
                // tslint:disable-next-line:no-floating-promises
                hubConnection.invoke("target", 1, new Subject())
                    .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

                // Verify the message is sent
                expect(connection.sentData.length).toBe(1);
                expect(connection.parsedSentData[0].type).toEqual(MessageType.Invocation);
                expect((connection.sentData[0] as string).length).toEqual(81);
            } finally {
                // Close the connection
                await hubConnection.stop();
            }
        });
    });

    it("upload stream invocation", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection, logger);
            try {
                hubConnection.stream("target", 1, new Subject());

                // Verify the message is sent
                expect(connection.sentData.length).toBe(1);
                expect(connection.parsedSentData[0].type).toEqual(MessageType.StreamInvocation);
                expect((connection.sentData[0] as string).length).toEqual(81);
            } finally {
                // Close the connection
                await hubConnection.stop();
            }
        });
    });

    it("completion message", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection, logger);
            try {
                const subject = new Subject();
                hubConnection.stream("target", 1, subject);
                subject.complete();

                await delayUntil(1000, () => connection.sentData.length === 2);

                // Verify the message is sent
                expect(connection.sentData.length).toBe(2);
                expect(connection.parsedSentData[1].type).toEqual(MessageType.Completion);
                expect((connection.sentData[1] as string).length).toEqual(29);
            } finally {
                // Close the connection
                await hubConnection.stop();
            }
        });
    });

    it("cancel message", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection, logger);
            try {
                hubConnection.stream("target", 1).subscribe({
                    complete: () => {},
                    error: () => {},
                    next: () => {},
                }).dispose();

                await delayUntil(1000, () => connection.sentData.length === 2);

                // Verify the message is sent
                expect(connection.sentData.length).toBe(2);
                expect(connection.parsedSentData[1].type).toEqual(MessageType.CancelInvocation);
                expect((connection.sentData[1] as string).length).toEqual(29);
            } finally {
                // Close the connection
                await hubConnection.stop();
            }
        });
    });
});
