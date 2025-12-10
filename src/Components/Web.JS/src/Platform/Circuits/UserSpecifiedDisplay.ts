// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ReconnectDisplay, ReconnectDisplayUpdateOptions } from './ReconnectDisplay';
import { ReconnectStateChangedEvent } from './ReconnectStateChangedEvent';

export class UserSpecifiedDisplay implements ReconnectDisplay {
  static readonly ShowClassName = 'components-reconnect-show';

  static readonly HideClassName = 'components-reconnect-hide';

  static readonly RetryingClassName = 'components-reconnect-retrying';

  static readonly FailedClassName = 'components-reconnect-failed';

  static readonly PausedClassName = 'components-reconnect-paused';

  static readonly ResumeFailedClassName = 'components-reconnect-resume-failed';

  static readonly RejectedClassName = 'components-reconnect-rejected';

  static readonly MaxRetriesId = 'components-reconnect-max-retries';

  static readonly CurrentAttemptId = 'components-reconnect-current-attempt';

  static readonly SecondsToNextAttemptId = 'components-seconds-to-next-attempt';

  static readonly ReconnectStateChangedEventName = 'components-reconnect-state-changed';

  private reconnect = false;

  constructor(private dialog: HTMLElement, private readonly document: Document, maxRetries?: number) {
    this.document = document;

    if (maxRetries !== undefined) {
      const maxRetriesElement = this.document.getElementById(UserSpecifiedDisplay.MaxRetriesId);

      if (maxRetriesElement) {
        maxRetriesElement.innerText = maxRetries.toString();
      }
    }
  }

  show(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.ShowClassName);
    this.dispatchReconnectStateChangedEvent({ state: 'show' });
  }

  update(options: ReconnectDisplayUpdateOptions): void {
    this.reconnect = options.type === 'reconnect';
    if (options.type === 'reconnect') {
      const { currentAttempt, secondsToNextAttempt } = options;
      const currentAttemptElement = this.document.getElementById(UserSpecifiedDisplay.CurrentAttemptId);

      if (currentAttemptElement) {
        currentAttemptElement.innerText = currentAttempt.toString();
      }

      const secondsToNextAttemptElement = this.document.getElementById(UserSpecifiedDisplay.SecondsToNextAttemptId);

      if (secondsToNextAttemptElement) {
        secondsToNextAttemptElement.innerText = secondsToNextAttempt.toString();
      }

      if (currentAttempt > 1 && secondsToNextAttempt > 0) {
        this.dialog.classList.add(UserSpecifiedDisplay.RetryingClassName);
      }

      this.dispatchReconnectStateChangedEvent({ state: 'retrying', currentAttempt, secondsToNextAttempt });
    }
    if (options.type === 'pause') {
      const remote = options.remote;
      this.dialog.classList.remove(UserSpecifiedDisplay.ShowClassName, UserSpecifiedDisplay.RetryingClassName);
      this.dialog.classList.add(UserSpecifiedDisplay.PausedClassName);
      this.dispatchReconnectStateChangedEvent({ state: 'paused', remote: remote });
    }
  }

  hide(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.HideClassName);
    this.dispatchReconnectStateChangedEvent({ state: 'hide' });
  }

  failed(): void {
    this.removeClasses();
    if (this.reconnect) {
      this.dialog.classList.add(UserSpecifiedDisplay.FailedClassName);
      this.dispatchReconnectStateChangedEvent({ state: 'failed' });
    } else {
      this.dialog.classList.add(UserSpecifiedDisplay.ResumeFailedClassName);
      this.dispatchReconnectStateChangedEvent({ state: 'resume-failed' });
    }
  }

  rejected(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.RejectedClassName);
    this.dispatchReconnectStateChangedEvent({ state: 'rejected' });
  }

  private removeClasses() {
    this.dialog.classList.remove(
      UserSpecifiedDisplay.ShowClassName,
      UserSpecifiedDisplay.HideClassName,
      UserSpecifiedDisplay.RetryingClassName,
      UserSpecifiedDisplay.FailedClassName,
      UserSpecifiedDisplay.RejectedClassName,
      UserSpecifiedDisplay.PausedClassName,
      UserSpecifiedDisplay.ResumeFailedClassName
    );
  }

  private dispatchReconnectStateChangedEvent(eventData: ReconnectStateChangedEvent) {
    const event = new CustomEvent(UserSpecifiedDisplay.ReconnectStateChangedEventName, { detail: eventData });
    this.dialog.dispatchEvent(event);
  }
}
