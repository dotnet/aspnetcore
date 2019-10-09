let hasFailed = false;

export async function showErrorNotification() {
    let errorUi = document.querySelector('#error-ui') as HTMLElement;
    if (errorUi) {
        errorUi.style.display = 'block';
    }

    if (!hasFailed) {
        hasFailed = true;
        const errorUiReloads = document.querySelectorAll<HTMLElement>('#error-ui .reload');
        errorUiReloads.forEach(reload => {
            reload.onclick = function (e) {
                location.reload();
                e.preventDefault();
            };
        });

        let errorUiDismiss = document.querySelectorAll<HTMLElement>('#error-ui .dismiss');
        errorUiDismiss.forEach(dismiss => {
            dismiss.onclick = function (e) {
                const errorUi = document.querySelector<HTMLElement>('#error-ui');
                if (errorUi) {
                    errorUi.style.display = 'none';
                }
                e.preventDefault();
            };
        });
    }
}
