// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export interface ReconnectDisplay {
  show(): void;
  update(currentAttempt: number, secondsToNextAttempt: number): void;
  hide(): void;
  failed(): void;
  rejected(): void;
}
