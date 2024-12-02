// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ReconnectDisplay } from './ReconnectDisplay';
export class UserSpecifiedDisplay implements ReconnectDisplay {
  static readonly ShowClassName = 'components-reconnect-show';

  static readonly HideClassName = 'components-reconnect-hide';

  static readonly FailedClassName = 'components-reconnect-failed';

  static readonly RejectedClassName = 'components-reconnect-rejected';

  static readonly MaxRetriesId = 'components-reconnect-max-retries';

  static readonly CurrentAttemptId = 'components-reconnect-current-attempt';

  static readonly SecondsToNextAttemptId = 'components-seconds-to-next-attempt';

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
  }

  update(currentAttempt: number, secondsToNextAttempt: number): void {
    const currentAttemptElement = this.document.getElementById(UserSpecifiedDisplay.CurrentAttemptId);

    if (currentAttemptElement) {
      currentAttemptElement.innerText = currentAttempt.toString();
    }

    const secondsToNextAttemptElement = this.document.getElementById(UserSpecifiedDisplay.SecondsToNextAttemptId);

    if (secondsToNextAttemptElement) {
      secondsToNextAttemptElement.innerText = secondsToNextAttempt.toString();
    }
  }

  hide(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.HideClassName);
  }

  failed(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.FailedClassName);
  }

  rejected(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.RejectedClassName);
  }

  private removeClasses() {
    this.dialog.classList.remove(UserSpecifiedDisplay.ShowClassName, UserSpecifiedDisplay.HideClassName, UserSpecifiedDisplay.FailedClassName, UserSpecifiedDisplay.RejectedClassName);
  }
}
