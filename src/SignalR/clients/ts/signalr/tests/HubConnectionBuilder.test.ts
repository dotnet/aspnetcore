// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpConnection } from "signalr/src/HttpConnection";
import { DefaultReconnectPolicy } from "../src/DefaultReconnectPolicy";
import { HttpRequest, HttpResponse } from "../src/HttpClient";
import { HubConnection, HubConnectionState } from "../src/HubConnection";
import { HubConnectionBuilder } from "../src/HubConnectionBuilder";
import { IHttpConnectionOptions } from "../src/IHttpConnectionOptions";
import { HubMessage, IHubProtocol } from "../src/IHubProtocol";
import { ILogger, LogLevel } from "../src/ILogger";
import { HttpTransportType, TransferFormat } from "../src/ITransport";
import { NullLogger } from "../src/Loggers";
import { ConsoleLogger } from "../src/Utils";

import { VerifyLogger } from "./Common";
import { TestHttpClient } from "./TestHttpClient";
import { PromiseSource, registerUnhandledRejectionHandler } from "./Utils";

const longPollingNegotiateResponse = {
    availableTransports: [
        { transport: "LongPolling", transferFormats: ["Text", "Binary"] },
    ],
    connectionId: "abc123",
    connectionToken: "123abc",
    negotiateVersion: 1,
};

const commonHttpOptions: IHttpConnectionOptions = {
    logMessageContent: true,
};

// We use a different mapping table here to help catch any unintentional breaking changes.
const ExpectedLogLevelMappings = {
    trace: LogLevel.Trace,
    debug: LogLevel.Debug,
    info: LogLevel.Information,
    information: LogLevel.Information,
    warn: LogLevel.Warning,
    warning: LogLevel.Warning,
    error: LogLevel.Error,
    critical: LogLevel.Critical,
    none: LogLevel.None,
};

class CapturingConsole {
    public messages: any[] = [];

    public error(message: any) {
        this.messages.push(CapturingConsole._stripPrefix(message));
    }

    public warn(message: any) {
        this.messages.push(CapturingConsole._stripPrefix(message));
    }

    public info(message: any) {
        this.messages.push(CapturingConsole._stripPrefix(message));
    }

    public log(message: any) {
        this.messages.push(CapturingConsole._stripPrefix(message));
    }

    private static _stripPrefix(input: any): any {
        if (typeof input === "string") {
            input = input.replace(/\[.*\]\s+/, "");
        }
        return input;
    }
}

registerUnhandledRejectionHandler();

describe("HubConnectionBuilder", () => {
    for (const val of [undefined, null, ""]) {
        it(`withUrl throws if url is ${String(val)}`, () => {
            const builder = new HubConnectionBuilder();
            expect(() => builder.withUrl(val!)).toThrow(/The 'url' argument (is required|should not be empty)./);
        });
    }

    eachMissingValue((val, name) => {
        it(`withHubProtocol throws if protocol is ${name}`, () => {
            const builder = new HubConnectionBuilder();
            expect(() => builder.withHubProtocol(val!)).toThrow("The 'protocol' argument is required.");
        });
    });

    it("builds HubConnection with HttpConnection using provided URL", async () => {
        await VerifyLogger.run(async (logger) => {
            const pollSent = new PromiseSource<HttpRequest>();
            const pollCompleted = new PromiseSource<HttpResponse>();
            const testClient = createTestClient(pollSent, pollCompleted.promise)
                .on("POST", "http://example.com?id=123abc", (req) => {
                    // Respond from the poll with the handshake response
                    pollCompleted.resolve(new HttpResponse(204, "No Content", "{}"));
                    return new HttpResponse(202);
                });
            const connection = createConnectionBuilder()
                .withUrl("http://example.com", {
                    ...commonHttpOptions,
                    httpClient: testClient,
                    logger,
                })
                .build();

            await expect(connection.start()).rejects.toThrow("The underlying connection was closed before the hub handshake could complete.");
            expect(connection.state).toBe(HubConnectionState.Disconnected);

            expect((await pollSent.promise).url).toMatch(/http:\/\/example.com\?id=123abc.*/);
        });
    });

    it("can configure transport type", async () => {
        const protocol = new TestProtocol();

        const builder = createConnectionBuilder()
            .withUrl("http://example.com", HttpTransportType.WebSockets)
            .withHubProtocol(protocol);
        expect(builder.httpConnectionOptions!.transport).toBe(HttpTransportType.WebSockets);
    });

    it("can configure hub protocol", async () => {
        await VerifyLogger.run(async (logger) => {
            const protocol = new TestProtocol();

            const pollSent = new PromiseSource<HttpRequest>();
            const pollCompleted = new PromiseSource<HttpResponse>();
            let negotiateRequest!: HttpRequest;
            const testClient = createTestClient(pollSent, pollCompleted.promise)
                .on("POST", "http://example.com?id=123abc", (req) => {
                    // Respond from the poll with the handshake response
                    negotiateRequest = req;
                    pollCompleted.resolve(new HttpResponse(204, "No Content", "{}"));
                    return new HttpResponse(202);
                });

            const connection = createConnectionBuilder()
                .withUrl("http://example.com", {
                    ...commonHttpOptions,
                    httpClient: testClient,
                    logger,
                })
                .withHubProtocol(protocol)
                .build();

            await expect(connection.start()).rejects.toThrow("The underlying connection was closed before the hub handshake could complete.");
            expect(connection.state).toBe(HubConnectionState.Disconnected);

            expect(negotiateRequest.content).toBe(`{"protocol":"${protocol.name}","version":1}\x1E`);
        });
    });

    describe("configureLogging", () => {
        function testLogLevels(logger: ILogger, minLevel: LogLevel) {
            const capturingConsole = new CapturingConsole();
            (logger as ConsoleLogger).out = capturingConsole;

            for (let level = LogLevel.Trace; level < LogLevel.None; level++) {
                const message = `Message at LogLevel.${LogLevel[level]}`;
                const expectedMessage = `${LogLevel[level]}: Message at LogLevel.${LogLevel[level]}`;
                logger.log(level, message);

                if (level >= minLevel) {
                    expect(capturingConsole.messages).toContain(expectedMessage);
                } else {
                    expect(capturingConsole.messages).not.toContain(expectedMessage);
                }
            }
        }

        eachMissingValue((val, name) => {
            it(`throws if logger is ${name}`, () => {
                const builder = new HubConnectionBuilder();
                expect(() => builder.configureLogging(val!)).toThrow("The 'logging' argument is required.");
            });
        });

        [
            LogLevel.None,
            LogLevel.Critical,
            LogLevel.Error,
            LogLevel.Warning,
            LogLevel.Information,
            LogLevel.Debug,
            LogLevel.Trace,
        ].forEach((minLevel) => {
            const levelName = LogLevel[minLevel];
            it(`accepts LogLevel.${levelName}`, async () => {
                const builder = new HubConnectionBuilder()
                    .configureLogging(minLevel);

                expect(builder.logger).toBeDefined();
                expect(builder.logger).toBeInstanceOf(ConsoleLogger);

                testLogLevels(builder.logger!, minLevel);
            });
        });

        const levelNames = Object.keys(ExpectedLogLevelMappings) as (keyof typeof ExpectedLogLevelMappings)[];
        for (const str of levelNames) {
            const mapped = ExpectedLogLevelMappings[str];
            const mappedName = LogLevel[mapped];
            it(`accepts "${str}" as an alias for LogLevel.${mappedName}`, async () => {
                const builder = new HubConnectionBuilder()
                    .configureLogging(str);

                expect(builder.logger).toBeDefined();
                expect(builder.logger).toBeInstanceOf(ConsoleLogger);

                testLogLevels(builder.logger!, mapped);
            });
        }

        it("allows logger to be replaced", async () => {
            let loggedMessages = 0;
            const logger = {
                log() {
                    loggedMessages += 1;
                },
            };
            const pollSent = new PromiseSource<HttpRequest>();
            const pollCompleted = new PromiseSource<HttpResponse>();
            const testClient = createTestClient(pollSent, pollCompleted.promise)
                .on("POST", "http://example.com?id=123abc", (req) => {
                    // Respond from the poll with the handshake response
                    pollCompleted.resolve(new HttpResponse(204, "No Content", "{}"));
                    return new HttpResponse(202);
                });
            const connection = createConnectionBuilder(logger)
                .withUrl("http://example.com", {
                    ...commonHttpOptions,
                    httpClient: testClient,
                })
                .build();

            try {
                await connection.start();
            } catch {
                // Ignore failures
            }

            expect(loggedMessages).toBeGreaterThan(0);
        });

        it("configures logger for both HttpConnection and HubConnection", async () => {
            const pollSent = new PromiseSource<HttpRequest>();
            const pollCompleted = new PromiseSource<HttpResponse>();
            const testClient = createTestClient(pollSent, pollCompleted.promise)
                .on("POST", "http://example.com?id=123abc", (req) => {
                    // Respond from the poll with the handshake response
                    pollCompleted.resolve(new HttpResponse(204, "No Content", "{}"));
                    return new HttpResponse(202);
                });
            const logger = new CaptureLogger();
            const connection = createConnectionBuilder(logger)
                .withUrl("http://example.com", {
                    ...commonHttpOptions,
                    httpClient: testClient,
                })
                .build();

            try {
                await connection.start();
            } catch {
                // Ignore failures
            }

            // A HubConnection message
            expect(logger.messages).toContain("Starting HubConnection.");

            // An HttpConnection message
            expect(logger.messages).toContain("Starting connection with transfer format 'Text'.");
        });

        it("does not replace HttpConnectionOptions logger if provided", async () => {
            const pollSent = new PromiseSource<HttpRequest>();
            const pollCompleted = new PromiseSource<HttpResponse>();
            const testClient = createTestClient(pollSent, pollCompleted.promise)
                .on("POST", "http://example.com?id=123abc", (req) => {
                    // Respond from the poll with the handshake response
                    pollCompleted.resolve(new HttpResponse(204, "No Content", "{}"));
                    return new HttpResponse(202);
                });
            const hubConnectionLogger = new CaptureLogger();
            const httpConnectionLogger = new CaptureLogger();
            const connection = createConnectionBuilder(hubConnectionLogger)
                .withUrl("http://example.com", {
                    httpClient: testClient,
                    logger: httpConnectionLogger,
                })
                .build();

            try {
                await connection.start();
            } catch {
                // Ignore failures
            }

            // A HubConnection message
            expect(hubConnectionLogger.messages).toContain("Starting HubConnection.");
            expect(httpConnectionLogger.messages).not.toContain("Starting HubConnection.");

            // An HttpConnection message
            expect(httpConnectionLogger.messages).toContain("Starting connection with transfer format 'Text'.");
            expect(hubConnectionLogger.messages).not.toContain("Starting connection with transfer format 'Text'.");
        });
    });

    it("reconnectPolicy undefined by default", () => {
        const builder = new HubConnectionBuilder().withUrl("http://example.com");
        expect(builder.reconnectPolicy).toBeUndefined();
    });

    it("withAutomaticReconnect throws if reconnectPolicy is already set", () => {
        const builder = new HubConnectionBuilder().withAutomaticReconnect();
        expect(() => builder.withAutomaticReconnect()).toThrow("A reconnectPolicy has already been set.");
    });

    it("withAutomaticReconnect uses default retryDelays when called with no arguments", () => {
        // From DefaultReconnectPolicy.ts
        const DEFAULT_RETRY_DELAYS_IN_MILLISECONDS = [0, 2000, 10000, 30000, null];
        const builder = new HubConnectionBuilder()
            .withAutomaticReconnect();

        let retryCount = 0;
        for (const delay of DEFAULT_RETRY_DELAYS_IN_MILLISECONDS) {
            const retryContext = {
                previousRetryCount: retryCount++,
                elapsedMilliseconds: 0,
                retryReason: new Error(),
            };

            expect(builder.reconnectPolicy!.nextRetryDelayInMilliseconds(retryContext)).toBe(delay);
        }
    });

    it("withAutomaticReconnect uses custom retryDelays when provided", () => {
        const customRetryDelays = [3, 1, 4, 1, 5, 9];
        const builder = new HubConnectionBuilder()
            .withAutomaticReconnect(customRetryDelays);

        let retryCount = 0;
        for (const delay of customRetryDelays) {
            const retryContext = {
                previousRetryCount: retryCount++,
                elapsedMilliseconds: 0,
                retryReason: new Error(),
            };

            expect(builder.reconnectPolicy!.nextRetryDelayInMilliseconds(retryContext)).toBe(delay);
        }

        const retryContextFinal = {
            previousRetryCount: retryCount++,
            elapsedMilliseconds: 0,
            retryReason: new Error(),
        };

        expect(builder.reconnectPolicy!.nextRetryDelayInMilliseconds(retryContextFinal)).toBe(null);
    });

    it("withAutomaticReconnect uses a custom IRetryPolicy when provided", () => {
        const customRetryDelays = [127, 0, 0, 1];
        const builder = new HubConnectionBuilder()
            .withAutomaticReconnect(new DefaultReconnectPolicy(customRetryDelays));

        let retryCount = 0;
        for (const delay of customRetryDelays) {
            const retryContext = {
                previousRetryCount: retryCount++,
                elapsedMilliseconds: 0,
                retryReason: new Error(),
            };

            expect(builder.reconnectPolicy!.nextRetryDelayInMilliseconds(retryContext)).toBe(delay);
        }

        const retryContextFinal = {
            previousRetryCount: retryCount++,
            elapsedMilliseconds: 0,
            retryReason: new Error(),
        };

        expect(builder.reconnectPolicy!.nextRetryDelayInMilliseconds(retryContextFinal)).toBe(null);
    });

    it("can configure serverTimeoutInMilliseconds for HubConnection", async () => {
        const milliseconds = 60000;

        const connection = createConnectionBuilder()
            .withUrl("http://example.com")
            .withServerTimeout(milliseconds)
            .build();

        expect(connection.serverTimeoutInMilliseconds).toBe(milliseconds);
    });

    it("can configure keepAliveIntervalInMilliseconds for HubConnection", async () => {
        const milliseconds = 60000;

        const connection = createConnectionBuilder()
            .withUrl("http://example.com")
            .withKeepAliveInterval(milliseconds)
            .build();

        expect(connection.keepAliveIntervalInMilliseconds).toBe(milliseconds);
    });

    it("can configure Stateful Reconnect buffer limit on HubConnection", () => {
        const connection = createConnectionBuilder()
            .withUrl("http://example.com")
            .withStatefulReconnect({ bufferSize: 103 })
            .build();

        expect((connection as any)._statefulReconnectBufferSize).toBe(103);
    });

    it("enables Stateful Reconnect on HttpConnection", () => {
        const connection = createConnectionBuilder()
            .withUrl("http://example.com")
            .withStatefulReconnect()
            .build();

        expect((connection as any).connection._options._useStatefulReconnect).toBe(true);
    });
});

class CaptureLogger implements ILogger {
    public readonly messages: string[] = [];

    public log(logLevel: LogLevel, message: string): void {
        this.messages.push(message);
    }
}

class TestProtocol implements IHubProtocol {
    public name: string = "test";
    public version: number = 1;
    public transferFormat: TransferFormat = TransferFormat.Text;
    public parseMessages(input: string | ArrayBuffer, logger: ILogger): HubMessage[] {
        throw new Error("Method not implemented.");
    }
    public writeMessage(message: HubMessage): string | ArrayBuffer {
        // builds ping message in the `hubConnection` constructor
        return "";
    }
}

function createConnectionBuilder(logger?: ILogger): HubConnectionBuilder {
    // We don't want to spam test output with logs. This can be changed as needed
    return new HubConnectionBuilder()
        .configureLogging(logger || NullLogger.instance);
}

function createTestClient(pollSent: PromiseSource<HttpRequest>, pollCompleted: Promise<HttpResponse>, negotiateResponse?: any): TestHttpClient {
    let firstRequest = true;
    return new TestHttpClient()
        .on("POST", "http://example.com/negotiate?negotiateVersion=1", () => negotiateResponse || longPollingNegotiateResponse)
        .on("GET", /http:\/\/example.com\?id=123abc&_=.*/, (req) => {
            if (firstRequest) {
                firstRequest = false;
                return new HttpResponse(200);
            } else {
                pollSent.resolve(req);
                return pollCompleted;
            }
        });
}

function eachMissingValue(callback: (val: undefined | null, name: string) => void) {
    callback(null, "null");
    callback(undefined, "undefined");
}
