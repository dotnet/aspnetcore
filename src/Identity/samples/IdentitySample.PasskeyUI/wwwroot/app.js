(function () {
    // Following is a quick and dirty way to execute scripts based on the current route.
    const routeScripts = {};

    function addRouteScript(path, callback) {
        routeScripts[path] = callback;
    }

    function executeScript() {
        const routeScript = routeScripts[location.pathname];
        routeScript?.();
    }

    function enableRouteScripts() {
        Blazor.addEventListener('enhancednavigationend', executeScript);

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', executeScript);
        } else {
            executeScript();
        }
    }

    // Define home page JS functionality.
    addRouteScript('/', async () => {
        let abortController;
        const form = document.getElementById('auth-form');
        const statusMessage = document.getElementById('status-message');

        async function fetchNewCredential(username) {
            if (!username) {
                throw new Error('Please enter a username.');
            }

            const optionsResponse = await fetch('/attestation/options', {
                method: 'POST',
                body: JSON.stringify({
                    username,
                    authenticatorSelection: {
                        residentKey: 'preferred',
                    }
                    // TODO: Allow configuration of other options.
                }),
                headers: {
                    'Content-Type': 'application/json',
                },
                credentials: 'include',
            });
            const optionsJson = await optionsResponse.json();
            const options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson);
            abortController?.abort();
            abortController = new AbortController();
            return await navigator.credentials.create({
                publicKey: options,
                signal: abortController.signal,
            });
        }

        async function fetchExistingCredential(username, useConditionalMediation) {
            // The username is optional for authentication, so we don't validate it here.
            const optionsResponse = await fetch('/assertion/options', {
                method: 'POST',
                body: JSON.stringify({
                    username,
                    // TODO: Allow configuration of other options.
                }),
                headers: {
                    'Content-Type': 'application/json',
                },
                credentials: 'include',
            });
            const optionsJson = await optionsResponse.json();
            const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson);
            abortController?.abort();
            abortController = new AbortController();
            return await navigator.credentials.get({
                publicKey: options,
                mediation: useConditionalMediation ? 'conditional' : undefined,
                signal: abortController.signal,
            });
        }

        async function fetchAndSubmitCredential(action, useConditionalMediation = false) {
            try {
                const username = new FormData(form).get('username');
                let credential;
                if (action === 'register') {
                    credential = await fetchNewCredential(username);
                } else if (action === 'authenticate') {
                    credential = await fetchExistingCredential(username, useConditionalMediation);
                } else {
                    throw new Error('Unknown action: ' + action);
                }
                var credentialJson = JSON.stringify(credential);
                form.addEventListener('formdata', (e) => {
                    e.formData.append('action', action);
                    e.formData.append('credential', credentialJson);
                }, { once: true });
                form.submit();
            } catch (error) {
                // Ignore abort errors, they are expected when the user cancels the operation.
                if (error.name !== 'AbortError') {
                    statusMessage.textContent = 'Error: ' + error.message;
                    throw error;
                }
            }
        }

        form.addEventListener('submit', (e) => {
            if (e.submitter?.name == 'action') {
                e.preventDefault();
                fetchAndSubmitCredential(e.submitter.value);
            }
        });

        if (await PublicKeyCredential.isConditionalMediationAvailable()) {
            await fetchAndSubmitCredential('authenticate', /* useConditionalMediation */ true);
        }
    });

    enableRouteScripts();
})();
