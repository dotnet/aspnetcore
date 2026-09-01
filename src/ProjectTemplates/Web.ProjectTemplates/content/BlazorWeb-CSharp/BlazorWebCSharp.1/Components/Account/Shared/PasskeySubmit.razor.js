const browserSupportsPasskeys =
    typeof navigator.credentials !== 'undefined' &&
    typeof window.PublicKeyCredential !== 'undefined' &&
    typeof window.PublicKeyCredential.parseCreationOptionsFromJSON === 'function' &&
    typeof window.PublicKeyCredential.parseRequestOptionsFromJSON === 'function';

// A conditional creation needs no user interaction, so it should be quick. Giving up early
// costs at most a passkey the user can still add manually, but waiting stalls the sign-in.
const upgradeTimeoutMs = 5000;

async function fetchWithErrorHandling(url, options = {}) {
    const response = await fetch(url, {
        credentials: 'include',
        ...options
    });
    if (!response.ok) {
        const text = await response.text();
        console.error(text);
        throw new Error(`The server responded with status ${response.status}.`);
    }
    return response;
}

async function createCredential(optionsJson, mediation, signal) {
    let options;
    if (!optionsJson) {
        const optionsResponse = await fetchWithErrorHandling('/Account/Manage/PasskeyCreationOptions', {
            method: 'POST',
            signal,
        });
        options = await optionsResponse.json();
    } else {
        options = JSON.parse(optionsJson);
    }
    options = PublicKeyCredential.parseCreationOptionsFromJSON(options);
    return await navigator.credentials.create({ publicKey: options, mediation, signal });
}

async function browserSupportsConditionalCreate() {
    try {
        const capabilities = await PublicKeyCredential.getClientCapabilities?.();
        return capabilities?.conditionalCreate === true;
    } catch {
        return false;
    }
}

async function reauthenticateCredential(signal) {
    const optionsResponse = await fetchWithErrorHandling('/Account/Manage/PasskeyReauthenticationOptions', {
        method: 'POST',
        signal,
    });
    const optionsJson = await optionsResponse.json();
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson);
    return await navigator.credentials.get({ publicKey: options, signal });
}

async function requestCredential(email, mediation, signal) {
    const optionsResponse = await fetchWithErrorHandling(`/Account/PasskeyRequestOptions?username=${email}`, {
        method: 'POST',
        signal,
    });
    const optionsJson = await optionsResponse.json();
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson);
    return await navigator.credentials.get({ publicKey: options, mediation, signal });
}

customElements.define('passkey-submit', class extends HTMLElement {
    static formAssociated = true;
    static observedAttributes = ['unknown-credential-signal-options'];

    async connectedCallback() {
        this.internals = this.attachInternals();
        this.attrs = {
            operation: this.getAttribute('operation'),
            name: this.getAttribute('name'),
            emailName: this.getAttribute('email-name'),
            creationOptions: this.getAttribute('creation-options'),
        };

        this.internals.form.addEventListener('submit', (event) => {
            if (event.submitter?.name === '__passkeySubmit') {
                event.preventDefault();
                this.obtainAndSubmitCredential();
            }
        });

        try {
            await this.signalUnknownCredential();
        } finally {
            this.tryAutofillPasskey();
            this.tryUpgradeToPasskey();
        }
    }

    attributeChangedCallback() {
        // Enhanced navigation updates the attribute in place without reconnecting the element,
        // so connectedCallback does not run again. It also runs before connectedCallback when the
        // element is upgraded, which is why the initial signal is left to connectedCallback.
        if (this.isConnected && this.internals) {
            this.signalUnknownCredential();
        }
    }

    async signalUnknownCredential() {
        const options = this.getAttribute('unknown-credential-signal-options');
        if (!options) {
            return;
        }
        try {
            // Not all browsers support this, and it is best-effort, so failures are not surfaced.
            await window.PublicKeyCredential?.signalUnknownCredential?.(JSON.parse(options));
        } catch (error) {
            console.error(error);
        }
    }

    disconnectedCallback() {
        this.abortController?.abort();
    }

    async obtainCredential(useConditionalMediation, signal) {
        if (!browserSupportsPasskeys) {
            throw new Error('Some passkey features are missing. Please update your browser.');
        }

        if (this.attrs.operation === 'Create') {
            return await createCredential(/* optionsJson */ undefined, /* mediation */ undefined, signal);
        } else if (this.attrs.operation === 'Upgrade') {
            if (!this.attrs.creationOptions) {
                throw new Error('Passkey creation options were not provided.');
            }
            return await createCredential(this.attrs.creationOptions, 'conditional', signal);
        } else if (this.attrs.operation === 'Reauthenticate') {
            return await reauthenticateCredential(signal);
        } else if (this.attrs.operation === 'Request') {
            const email = new FormData(this.internals.form).get(this.attrs.emailName);
            const mediation = useConditionalMediation ? 'conditional' : undefined;
            return await requestCredential(email, mediation, signal);
        } else {
            throw new Error(`Unknown passkey operation '${this.attrs.operation}'.`);
        }
    }

    async obtainAndSubmitCredential(useConditionalMediation = false) {
        const isUpgrade = this.attrs.operation === 'Upgrade';
        this.abortController?.abort();
        this.abortController = new AbortController();
        // An upgrade happens while the user waits to be signed in, so it must not hang.
        const signal = isUpgrade
            ? AbortSignal.any([this.abortController.signal, AbortSignal.timeout(upgradeTimeoutMs)])
            : this.abortController.signal;
        const formData = new FormData();
        try {
            const credential = await this.obtainCredential(useConditionalMediation, signal);
            const credentialJson = JSON.stringify(credential);
            formData.append(`${this.attrs.name}.CredentialJson`, credentialJson);
        } catch (error) {
            if (isUpgrade) {
                // Upgrading is a best-effort operation that the user did not ask for, so failures
                // are logged but never shown, and the form is still submitted to continue sign-in.
                console.debug(error);
            } else if (error.name === 'AbortError') {
                // The user explicitly canceled the operation - return without error.
                return;
            } else {
                console.error(error);
                if (useConditionalMediation) {
                    // An error occurred during conditional mediation, which is not user-initiated.
                    // We log the error in the console but do not relay it to the user.
                    return;
                }
                const errorMessage = error.name === 'NotAllowedError'
                    ? 'No passkey was provided by the authenticator.'
                    : error.message;
                formData.append(`${this.attrs.name}.Error`, errorMessage);
            }
        }
        this.internals.setFormValue(formData);
        this.internals.form.submit();
    }

    async tryAutofillPasskey() {
        if (browserSupportsPasskeys && this.attrs.operation === 'Request' && await PublicKeyCredential.isConditionalMediationAvailable?.()) {
            await this.obtainAndSubmitCredential(/* useConditionalMediation */ true);
        }
    }

    async tryUpgradeToPasskey() {
        if (this.attrs.operation !== 'Upgrade') {
            return;
        }

        // The form is only a fallback for browsers without scripting, which can't upgrade anyway.
        this.internals.form.hidden = true;

        if (!browserSupportsPasskeys || !await browserSupportsConditionalCreate()) {
            this.internals.form.submit();
            return;
        }

        await this.obtainAndSubmitCredential();
    }
});
