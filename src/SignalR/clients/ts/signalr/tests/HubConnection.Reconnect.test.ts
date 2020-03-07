// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DefaultReconnectPolicy } from "../src/DefaultReconnectPolicy";
import { HubConnection, HubConnectionState } from "../src/HubConnection";
import { MessageType } from "../src/IHubProtocol";
import { RetryContext } from "../src/IRetryPolicy";
import { JsonHubProtocol } from "../src/JsonHubProtocol";

import { VerifyLogger } from "./Common";
import { TestConnection } from "./TestConnection";
import { PromiseSource } from "./Utils";

describe("auto reconnect", () => {
    it("is not enabled by default", async () => {
        await VerifyLogger.run(async (logger) => {
            const closePromise = new PromiseSource();
            let onreconnectingCalled = false;

            const connection = new TestConnection();
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol());

            hubConnection.onclose(() => {
                closePromise.resolve();
            });

            hubConnection.onreconnecting(() => {
                onreconnectingCalled = true;
            });

            await hubConnection.start();

            // Typically this would be called by the transport
            connection.onclose!(new Error("Connection lost"));

            await closePromise;

            expect(onreconnectingCalled).toBe(false);
        });
    });

    it("can be opted into", async () => {
        await VerifyLogger.run(async (logger) => {
            const reconnectedPromise = new PromiseSource();

            let nextRetryDelayCalledPromise = new PromiseSource();
            let continueRetryingPromise = new PromiseSource();

            let lastRetryCount = -1;
            let lastElapsedMs = -1;
            let retryReason = null;
            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            const connection = new TestConnection();
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), {
                    nextRetryDelayInMilliseconds(retryContext: RetryContext) {
                        lastRetryCount = retryContext.previousRetryCount;
                        lastElapsedMs = retryContext.elapsedMilliseconds;
                        retryReason = retryContext.retryReason;
                        nextRetryDelayCalledPromise.resolve();
                        return 0;
                    },
                });

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
                reconnectedPromise.resolve();
            });

            hubConnection.onclose(() => {
                closeCount++;
            });

            await hubConnection.start();

            connection.start = () => {
                const promise = continueRetryingPromise;
                continueRetryingPromise = new PromiseSource();
                return promise;
            };

            const oncloseError = new Error("Connection lost");
            const continueRetryingError = new Error("Reconnect attempt failed");

            // Typically this would be called by the transport
            connection.onclose!(oncloseError);

            await nextRetryDelayCalledPromise;
            nextRetryDelayCalledPromise = new PromiseSource();

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(lastRetryCount).toBe(0);
            expect(lastElapsedMs).toBe(0);
            expect(retryReason).toBe(oncloseError);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            // Make sure the the Promise is "handled" immediately upon rejection or else this test fails.
            continueRetryingPromise.catch(() => { });
            continueRetryingPromise.reject(continueRetryingError);
            await nextRetryDelayCalledPromise;

            expect(lastRetryCount).toBe(1);
            expect(lastElapsedMs).toBeGreaterThanOrEqual(0);
            expect(retryReason).toBe(continueRetryingError);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            continueRetryingPromise.resolve();
            await reconnectedPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Connected);
            expect(lastRetryCount).toBe(1);
            expect(lastElapsedMs).toBeGreaterThanOrEqual(0);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(1);
            expect(closeCount).toBe(0);

            await hubConnection.stop();

            expect(lastRetryCount).toBe(1);
            expect(lastElapsedMs).toBeGreaterThanOrEqual(0);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(1);
            expect(closeCount).toBe(1);
        });
    });

    it("stops if the reconnect policy returns null", async () => {
        await VerifyLogger.run(async (logger) => {
            const closePromise = new PromiseSource();

            let nextRetryDelayCalledPromise = new PromiseSource();

            let lastRetryCount = -1;
            let lastElapsedMs = -1;
            let retryReason = null;
            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            const connection = new TestConnection();
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), {
                    nextRetryDelayInMilliseconds(retryContext: RetryContext) {
                        lastRetryCount = retryContext.previousRetryCount;
                        lastElapsedMs = retryContext.elapsedMilliseconds;
                        retryReason = retryContext.retryReason;
                        nextRetryDelayCalledPromise.resolve();

                        return retryContext.previousRetryCount === 0 ? 0 : null;
                    },
                });

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
            });

            hubConnection.onclose(() => {
                closeCount++;
                closePromise.resolve();
            });

            await hubConnection.start();

            const oncloseError = new Error("Connection lost");
            const startError = new Error("Reconnect attempt failed");

            connection.start = () => {
                throw startError;
            };

            // Typically this would be called by the transport
            connection.onclose!(oncloseError);

            await nextRetryDelayCalledPromise;
            nextRetryDelayCalledPromise = new PromiseSource();

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(lastRetryCount).toBe(0);
            expect(lastElapsedMs).toBe(0);
            expect(retryReason).toBe(oncloseError);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            await nextRetryDelayCalledPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
            expect(lastRetryCount).toBe(1);
            expect(lastElapsedMs).toBeGreaterThanOrEqual(0);
            expect(retryReason).toBe(startError);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(1);
        });
    });

    it("can reconnect multiple times", async () => {
        await VerifyLogger.run(async (logger) => {
            let reconnectedPromise = new PromiseSource();
            let nextRetryDelayCalledPromise = new PromiseSource();

            let lastRetryCount = -1;
            let lastElapsedMs = -1;
            let retryReason = null;
            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            const connection = new TestConnection();
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), {
                    nextRetryDelayInMilliseconds(retryContext: RetryContext) {
                        lastRetryCount = retryContext.previousRetryCount;
                        lastElapsedMs = retryContext.elapsedMilliseconds;
                        retryReason = retryContext.retryReason;
                        nextRetryDelayCalledPromise.resolve();
                        return 0;
                    },
                });

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
                reconnectedPromise.resolve();
            });

            hubConnection.onclose(() => {
                closeCount++;
            });

            await hubConnection.start();

            const oncloseError = new Error("Connection lost 1");
            const oncloseError2 = new Error("Connection lost 2");

            // Typically this would be called by the transport
            connection.onclose!(oncloseError);

            await nextRetryDelayCalledPromise;
            nextRetryDelayCalledPromise = new PromiseSource();

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(lastRetryCount).toBe(0);
            expect(lastElapsedMs).toBe(0);
            expect(retryReason).toBe(oncloseError);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            await reconnectedPromise;
            reconnectedPromise = new PromiseSource();

            expect(hubConnection.state).toBe(HubConnectionState.Connected);
            expect(lastRetryCount).toBe(0);
            expect(lastElapsedMs).toBe(0);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(1);
            expect(closeCount).toBe(0);

            connection.onclose!(oncloseError2);

            await nextRetryDelayCalledPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(lastRetryCount).toBe(0);
            expect(lastElapsedMs).toBe(0);
            expect(retryReason).toBe(oncloseError2);
            expect(onreconnectingCount).toBe(2);
            expect(onreconnectedCount).toBe(1);
            expect(closeCount).toBe(0);

            await reconnectedPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Connected);
            expect(lastRetryCount).toBe(0);
            expect(lastElapsedMs).toBe(0);
            expect(onreconnectingCount).toBe(2);
            expect(onreconnectedCount).toBe(2);
            expect(closeCount).toBe(0);

            await hubConnection.stop();

            expect(lastRetryCount).toBe(0);
            expect(lastElapsedMs).toBe(0);
            expect(onreconnectingCount).toBe(2);
            expect(onreconnectedCount).toBe(2);
            expect(closeCount).toBe(1);
        });
    });

    it("does not transition into the reconnecting state if the first retry delay is null", async () => {
        await VerifyLogger.run(async (logger) => {
            const closePromise = new PromiseSource();

            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            const connection = new TestConnection();
            // Note the [] parameter to the DefaultReconnectPolicy.
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), new DefaultReconnectPolicy([]));

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
            });

            hubConnection.onclose(() => {
                closeCount++;
                closePromise.resolve();
            });

            await hubConnection.start();

            // Typically this would be called by the transport
            connection.onclose!(new Error("Connection lost"));

            await closePromise;

            expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
            expect(onreconnectingCount).toBe(0);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(1);
        });
    });

    it("does not transition into the reconnecting state if the connection is lost during initial handshake", async () => {
        await VerifyLogger.run(async (logger) => {
            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            // Disable autoHandshake in TestConnection
            const connection = new TestConnection(false);
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), new DefaultReconnectPolicy());

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
            });

            hubConnection.onclose(() => {
                closeCount++;
            });

            const startPromise = hubConnection.start();

            expect(hubConnection.state).toBe(HubConnectionState.Connecting);

            // Typically this would be called by the transport
            connection.onclose!(new Error("Connection lost"));

            await expect(startPromise).rejects.toThrow("Connection lost");

            expect(onreconnectingCount).toBe(0);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);
        });
    });

    it("continues reconnecting state if the connection is lost during a reconnecting handshake", async () => {
        await VerifyLogger.run(async (logger) => {
            const reconnectedPromise = new PromiseSource();
            let nextRetryDelayCalledPromise = new PromiseSource();

            let lastRetryCount = 0;
            let retryReason = null;
            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            // Disable autoHandshake in TestConnection
            const connection = new TestConnection(false);
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), {
                    nextRetryDelayInMilliseconds(retryContext: RetryContext) {
                        lastRetryCount = retryContext.previousRetryCount;
                        retryReason = retryContext.retryReason;
                        nextRetryDelayCalledPromise.resolve();
                        return 0;
                    },
                });

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
                reconnectedPromise.resolve();
            });

            hubConnection.onclose(() => {
                closeCount++;
            });

            const startPromise = hubConnection.start();
            // Manually complete handshake.
            connection.receive({});
            await startPromise;

            let replacedStartCalledPromise = new PromiseSource();
            connection.start = () => {
                replacedStartCalledPromise.resolve();
                return Promise.resolve();
            };

            const oncloseError = new Error("Connection lost 1");
            const oncloseError2 = new Error("Connection lost 2");

            // Typically this would be called by the transport
            connection.onclose!(oncloseError);

            await nextRetryDelayCalledPromise;
            nextRetryDelayCalledPromise = new PromiseSource();

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(lastRetryCount).toBe(0);
            expect(retryReason).toBe(oncloseError);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            await replacedStartCalledPromise;
            replacedStartCalledPromise = new PromiseSource();

            // Fail underlying connection during reconnect during handshake
            connection.onclose!(oncloseError2);

            await nextRetryDelayCalledPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(lastRetryCount).toBe(1);
            expect(retryReason).toBe(oncloseError2);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            await replacedStartCalledPromise;

            // Manually complete handshake.
            connection.receive({});

            await reconnectedPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Connected);
            expect(lastRetryCount).toBe(1);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(1);
            expect(closeCount).toBe(0);

            await hubConnection.stop();

            expect(lastRetryCount).toBe(1);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(1);
            expect(closeCount).toBe(1);
        });
    });

    it("continues reconnecting state if invalid handshake response received", async () => {
        await VerifyLogger.run(async (logger) => {
            const reconnectedPromise = new PromiseSource();
            let nextRetryDelayCalledPromise = new PromiseSource();

            let lastRetryCount = 0;
            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            // Disable autoHandshake in TestConnection
            const connection = new TestConnection(false);
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), {
                    nextRetryDelayInMilliseconds(retryContext: RetryContext) {
                        lastRetryCount = retryContext.previousRetryCount;
                        nextRetryDelayCalledPromise.resolve();
                        return 0;
                    },
                });

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
                reconnectedPromise.resolve();
            });

            hubConnection.onclose(() => {
                closeCount++;
            });

            const startPromise = hubConnection.start();
            // Manually complete handshake.
            connection.receive({});
            await startPromise;

            let replacedStartCalledPromise = new PromiseSource();
            connection.start = () => {
                replacedStartCalledPromise.resolve();
                return Promise.resolve();
            };

            // Typically this would be called by the transport
            connection.onclose!(new Error("Connection lost"));

            await nextRetryDelayCalledPromise;
            nextRetryDelayCalledPromise = new PromiseSource();

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(lastRetryCount).toBe(0);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            await replacedStartCalledPromise;
            replacedStartCalledPromise = new PromiseSource();

            // Manually fail handshake
            expect(() => connection.receive({ error: "invalid" })).toThrow("invalid");

            await nextRetryDelayCalledPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(lastRetryCount).toBe(1);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            await replacedStartCalledPromise;

            // Manually complete handshake.
            connection.receive({});

            await reconnectedPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Connected);
            expect(lastRetryCount).toBe(1);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(1);
            expect(closeCount).toBe(0);

            await hubConnection.stop();

            expect(lastRetryCount).toBe(1);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(1);
            expect(closeCount).toBe(1);
        },
        "Server returned handshake error: invalid");
    });

    it("can be stopped while restarting the underlying connection", async () => {
        await VerifyLogger.run(async (logger) => {
            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            const connection = new TestConnection();
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), new DefaultReconnectPolicy([0]));

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
            });

            hubConnection.onclose(() => {
                closeCount++;
            });

            await hubConnection.start();

            const stopCalledPromise = new PromiseSource();
            let stopPromise: Promise<void>;

            connection.start = () => {
                stopCalledPromise.resolve();
                stopPromise = hubConnection.stop();
                return Promise.resolve();
            };

            // Typically this would be called by the transport
            connection.onclose!(new Error("Connection lost"));

            await stopCalledPromise;
            await stopPromise!;

            expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(1);
        });
    });

    it("can be stopped during a restart handshake", async () => {
        await VerifyLogger.run(async (logger) => {
            const closedPromise = new PromiseSource();
            const nextRetryDelayCalledPromise = new PromiseSource();
            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            // Disable autoHandshake in TestConnection
            const connection = new TestConnection(false);
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), {
                    nextRetryDelayInMilliseconds() {
                        nextRetryDelayCalledPromise.resolve();
                        return 0;
                    },
                });

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
                closedPromise.resolve();
            });

            hubConnection.onclose(() => {
                closeCount++;
            });

            const startPromise = hubConnection.start();
            // Manually complete handshake.
            connection.receive({});
            await startPromise;

            const replacedSendCalledPromise = new PromiseSource();
            connection.send = () => {
                replacedSendCalledPromise.resolve();
                return Promise.resolve();
            };

            // Typically this would be called by the transport
            connection.onclose!(new Error("Connection lost"));

            await nextRetryDelayCalledPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            // Wait for the handshake to actually started. Right now, we're awaiting the 0ms delay.
            await replacedSendCalledPromise;

            await hubConnection.stop();

            expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(1);
        });
    });

    it("can be stopped during a reconnect delay", async () => {
        await VerifyLogger.run(async (logger) => {
            const closedPromise = new PromiseSource();
            const nextRetryDelayCalledPromise = new PromiseSource();
            let onreconnectingCount = 0;
            let onreconnectedCount = 0;
            let closeCount = 0;

            const connection = new TestConnection();
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), {
                    nextRetryDelayInMilliseconds() {
                        nextRetryDelayCalledPromise.resolve();
                        // 60s is hopefully longer than this test could ever take.
                        return 60 * 1000;
                    },
                });

            hubConnection.onreconnecting(() => {
                onreconnectingCount++;
            });

            hubConnection.onreconnected(() => {
                onreconnectedCount++;
                closedPromise.resolve();
            });

            hubConnection.onclose(() => {
                closeCount++;
            });

            await hubConnection.start();

            // Typically this would be called by the transport
            connection.onclose!(new Error("Connection lost"));

            await nextRetryDelayCalledPromise;

            expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(0);

            await hubConnection.stop();

            expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
            expect(onreconnectingCount).toBe(1);
            expect(onreconnectedCount).toBe(0);
            expect(closeCount).toBe(1);
        });
    });

    it("reconnect on close message if allowReconnect is true and auto reconnect is enabled", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new TestConnection();
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), new DefaultReconnectPolicy());
            try {
                let isReconnecting = false;
                let reconnectingError: Error | undefined;

                hubConnection.onreconnecting((e) => {
                    isReconnecting = true;
                    reconnectingError = e;
                });

                await hubConnection.start();

                connection.receive({
                    allowReconnect: true,
                    error: "Error!",
                    type: MessageType.Close,
                });

                expect(isReconnecting).toEqual(true);
                expect(reconnectingError!.message).toEqual("Server returned an error on close: Error!");
            } finally {
                await hubConnection.stop();
            }
        });
    });

    it("stop on close message if allowReconnect is missing and auto reconnect is enabled", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new TestConnection();
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol(), new DefaultReconnectPolicy());
            try {
                let isClosed = false;
                let closeError: Error | undefined;
                hubConnection.onclose((e) => {
                    isClosed = true;
                    closeError = e;
                });

                await hubConnection.start();

                connection.receive({
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
});
