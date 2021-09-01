let hasFailed = false;

export async function showErrorNotification(customErrorMessage: string = '') {
    let errorUi = document.querySelector('#blazor-error-ui') as HTMLElement;
    if (errorUi) {
        errorUi.style.display = 'block';

        if (customErrorMessage && errorUi.firstChild) {
            errorUi.firstChild.textContent = `\n\t${customErrorMessage}\t\n`;
        }
    }

    if (!hasFailed) {
        hasFailed = true;
        const errorUiReloads = document.querySelectorAll<HTMLElement>('#blazor-error-ui .reload');
        errorUiReloads.forEach(reload => {
            reload.onclick = function (e) {
                location.reload();
                e.preventDefault();
            };
        });

        let errorUiDismiss = document.querySelectorAll<HTMLElement>('#blazor-error-ui .dismiss');
        errorUiDismiss.forEach(dismiss => {
            dismiss.onclick = function (e) {
                const errorUi = document.querySelector<HTMLElement>('#blazor-error-ui');
                if (errorUi) {
                    errorUi.style.display = 'none';
                }
                e.preventDefault();
            };
        });
    }
}
