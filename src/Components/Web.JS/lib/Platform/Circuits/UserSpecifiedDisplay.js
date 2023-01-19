// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
export class UserSpecifiedDisplay {
    constructor(dialog, maxRetries, document) {
        this.dialog = dialog;
        this.maxRetries = maxRetries;
        this.document = document;
        this.document = document;
        const maxRetriesElement = this.document.getElementById(UserSpecifiedDisplay.MaxRetriesId);
        if (maxRetriesElement) {
            maxRetriesElement.innerText = this.maxRetries.toString();
        }
    }
    show() {
        this.removeClasses();
        this.dialog.classList.add(UserSpecifiedDisplay.ShowClassName);
    }
    update(currentAttempt) {
        const currentAttemptElement = this.document.getElementById(UserSpecifiedDisplay.CurrentAttemptId);
        if (currentAttemptElement) {
            currentAttemptElement.innerText = currentAttempt.toString();
        }
    }
    hide() {
        this.removeClasses();
        this.dialog.classList.add(UserSpecifiedDisplay.HideClassName);
    }
    failed() {
        this.removeClasses();
        this.dialog.classList.add(UserSpecifiedDisplay.FailedClassName);
    }
    rejected() {
        this.removeClasses();
        this.dialog.classList.add(UserSpecifiedDisplay.RejectedClassName);
    }
    removeClasses() {
        this.dialog.classList.remove(UserSpecifiedDisplay.ShowClassName, UserSpecifiedDisplay.HideClassName, UserSpecifiedDisplay.FailedClassName, UserSpecifiedDisplay.RejectedClassName);
    }
}
UserSpecifiedDisplay.ShowClassName = 'components-reconnect-show';
UserSpecifiedDisplay.HideClassName = 'components-reconnect-hide';
UserSpecifiedDisplay.FailedClassName = 'components-reconnect-failed';
UserSpecifiedDisplay.RejectedClassName = 'components-reconnect-rejected';
UserSpecifiedDisplay.MaxRetriesId = 'components-reconnect-max-retries';
UserSpecifiedDisplay.CurrentAttemptId = 'components-reconnect-current-attempt';
//# sourceMappingURL=UserSpecifiedDisplay.js.map