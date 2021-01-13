let hasFailed = false;

export async function showErrorNotification() {
    let errorUi = document.querySelector('#blazor-error-ui') as HTMLElement;
    if (errorUi) {
        errorUi.style.display = 'block';
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
