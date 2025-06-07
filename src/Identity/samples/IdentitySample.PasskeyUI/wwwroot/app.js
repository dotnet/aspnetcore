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
    addRouteScript('/', () => {
        const form = document.getElementById('auth-form');
        const usernameInput = document.getElementById('input-username');
        const credentialInput = document.getElementById('input-credential');
        const actionInput = document.getElementById('input-action');
        const registerInput = document.getElementById('input-register');
        const authenticateInput = document.getElementById('input-authenticate');
        const statusMessage = document.getElementById('status-message');

        async function submitCredential(action, credentialCallback) {
            statusMessage.textContent = 'Submitting...';
            try {
                var credential = await credentialCallback();
                var credentialJson = JSON.stringify(credential);
                credentialInput.value = credentialJson;
                actionInput.value = action;
                form.submit();
            } catch (error) {
                statusMessage.textContent = 'Error: ' + error.message;
                throw error;
            }
        }

        registerInput.addEventListener('click', async (e) => {
            e.preventDefault();

            await submitCredential('register', async () => {
                const username = usernameInput.value;
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
                const credential = await navigator.credentials.create({ publicKey: options });
                return credential;
            });
        });

        authenticateInput.addEventListener('click', async (e) => {
            e.preventDefault();

            await submitCredential('authenticate', async () => {
                // The username is optional for authentication, so we don't validate it here.
                const username = usernameInput.value;

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
                const credential = await navigator.credentials.get({ publicKey: options });
                return credential;
            });
        });
    });

    enableRouteScripts();
})();
