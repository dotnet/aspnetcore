// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

type CallbackRegistration = {
  dispose(): void;
};

export class CallbackCollection {
  private readonly _callbacks = new Set<() => void>();

  private _isInvocationPending = false;

  public registerCallback(callback: () => void): CallbackRegistration {
    this._callbacks.add(callback);
    return {
      dispose: () => {
        this._callbacks.delete(callback);
      },
    };
  }

  public enqueueCallbackInvocation(): void {
    if (this._isInvocationPending) {
      return;
    }

    this._isInvocationPending = true;
    setTimeout(() => {
      this._isInvocationPending = false;
      for (const callback of this._callbacks) {
        callback();
      }
    }, 0);
  }
}
