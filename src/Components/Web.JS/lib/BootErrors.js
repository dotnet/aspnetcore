// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
let hasFailed = false;
export function showErrorNotification() {
    const errorUi = document.querySelector('#blazor-error-ui');
    if (errorUi) {
        errorUi.style.display = 'block';
    }
    if (!hasFailed) {
        hasFailed = true;
        const errorUiReloads = document.querySelectorAll('#blazor-error-ui .reload');
        errorUiReloads.forEach(reload => {
            reload.onclick = function (e) {
                location.reload();
                e.preventDefault();
            };
        });
        const errorUiDismiss = document.querySelectorAll('#blazor-error-ui .dismiss');
        errorUiDismiss.forEach(dismiss => {
            dismiss.onclick = function (e) {
                const errorUi = document.querySelector('#blazor-error-ui');
                if (errorUi) {
                    errorUi.style.display = 'none';
                }
                e.preventDefault();
            };
        });
    }
}
//# sourceMappingURL=BootErrors.js.map