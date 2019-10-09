// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Version token that will be replaced by the prepack command
/** The version of the SignalR client. */
export const VERSION: string = "0.0.0-DEV_BUILD";

// Everything that users need to access must be exported here. Including interfaces.
export { AbortSignal } from "./AbortController";
export { AbortError, HttpError, TimeoutError } from "./Errors";
export { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
export { DefaultHttpClient } from "./DefaultHttpClient";
export { IHttpConnectionOptions } from "./IHttpConnectionOptions";
export { HubConnection, HubConnectionState } from "./HubConnection";
export { HubConnectionBuilder } from "./HubConnectionBuilder";
export { MessageType, MessageHeaders, HubMessage, HubMessageBase, HubInvocationMessage, InvocationMessage, StreamInvocationMessage, StreamItemMessage, CompletionMessage,
    PingMessage, CloseMessage, CancelInvocationMessage, IHubProtocol } from "./IHubProtocol";
export { ILogger, LogLevel } from "./ILogger";
export { HttpTransportType, TransferFormat, ITransport } from "./ITransport";
export { IStreamSubscriber, IStreamResult, ISubscription } from "./Stream";
export { NullLogger } from "./Loggers";
export { JsonHubProtocol } from "./JsonHubProtocol";
export { Subject } from "./Subject";
export { IRetryPolicy, RetryContext } from "./IRetryPolicy";
