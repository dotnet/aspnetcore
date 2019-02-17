// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { AbortError, HttpError, TimeoutError } from "./Errors";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";

export class FetchHttpClient extends HttpClient {
  private readonly logger: ILogger;

  public constructor(logger: ILogger) {
    super();
    this.logger = logger;
  }

  /** @inheritDoc */
  public send(request: HttpRequest): Promise<HttpResponse> {
    // Check that abort was not signaled before calling send
    if (request.abortSignal && request.abortSignal.aborted) {
      return Promise.reject(new AbortError());
    }

    if (!request.method) {
      return Promise.reject(new Error("No method defined."));
    }
    if (!request.url) {
      return Promise.reject(new Error("No url defined."));
    }

    return new Promise<HttpResponse>((resolve, reject) => {
      // https://developers.google.com/web/updates/2017/09/abortable-fetch
      const controller = new AbortController();
      const signal = controller.signal;

      const requestInfo: RequestInit = {
        body: request.content || "",
        credentials: "include",
        headers: {
          "Content-Type": "text/plain;charset=UTF-8",
          "X-Requested-With": "fetch",
          ...request.headers,
        },
        method: request.method!,
        signal,
      };

      if (request.abortSignal) {
        request.abortSignal.onabort = () => {
          controller.abort();
          reject(new AbortError());
        };
      }

      timeout(request.timeout || 10000, fetch(request.url!, requestInfo))
        .then((response: Response) => {
          if (!response.ok) {
            throw new Error(`${response.status}: ${response.statusText}.`);
          }
          return response;
        })
        .then((response) => {
          if (request.abortSignal) {
            request.abortSignal.onabort = null;
          }

          let content;
          switch (request.responseType) {
            case "arraybuffer":
              content = response.arrayBuffer();
              break;
            case "blob":
              content = response.blob();
              break;
            case "document":
              content = response.json();
              break;
            case "json":
              content = response.json();
              break;
            case "text":
              content = response.text();
              break;
            default:
              content = response.text();
              break;
          }

          content.then((payload) => {
            resolve(
              new HttpResponse(
                response.status,
                response.statusText,
                payload,
              ),
            );
          }).catch((error) => {
            reject(new HttpError(response.statusText, response.status));
          });
        })
        .catch((error) => {
          if (error.message === "timemout") {
            this.logger.log(LogLevel.Warning, `Timeout from HTTP request.`);
            reject(new TimeoutError());
          }
          this.logger.log(
            LogLevel.Warning,
            `Error from HTTP request. ${error.message}.`,
          );
          const [statusText, status] = error.message.split(":");
          reject(new HttpError(statusText, status));
        });
    });
  }
}

function timeout(ms: number, promise: Promise<any>) {
  return new Promise<any>((resolve, reject) => {
    setTimeout(() => {
      reject(new Error("timeout"));
    }, ms);
    promise.then(resolve, reject);
  });
}
