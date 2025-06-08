// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export interface ReconnectDisplay {
  show(options: ReconnectDisplayUpdateOptions): void;
  update(options: ReconnectDisplayUpdateOptions): void;
  hide(): void;
  failed(): void;
  rejected(): void;
}

export type ReconnectDisplayUpdateOptions = ReconnectOptions | PauseOptions;

export type PauseOptions = {
  type: 'pause',
  remote: boolean
};

export type ReconnectOptions = {
  type: 'reconnect',
  currentAttempt: number,
  secondsToNextAttempt: number
};
