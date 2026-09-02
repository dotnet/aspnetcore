const browserSupportsPasskeys =
    typeof navigator.credentials !== 'undefined' &&
    typeof window.PublicKeyCredential !== 'undefined' &&
    typeof window.PublicKeyCredential.parseCreationOptionsFromJSON === 'function' &&
    typeof window.PublicKeyCredential.parseRequestOptionsFromJSON === 'function';

async function fetchWithErrorHandling(url, options = {}) {
    const response = await fetch(url, {
        credentials: 'include',
        ...options
    });
    if (!response.ok) {
        const text = await response.text();
        console.error(text);
        const contentType = response.headers.get('content-type') ?? '';
        throw new Error(contentType.startsWith('text/plain') && text
            ? text
            : `The server responded with status ${response.status}.`);
    }
    return response;
}

async function createCredential(signal) {
    const optionsResponse = await fetchWithErrorHandling('/Account/Manage/PasskeyCreationOptions', {
        method: 'POST',
        signal,
    });
    const optionsJson = await optionsResponse.json();
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson);
    return await navigator.credentials.create({ publicKey: options, signal });
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
    const query = new URLSearchParams({ username: email });
    const optionsResponse = await fetchWithErrorHandling(`/Account/PasskeyRequestOptions?${query}`, {
        method: 'POST',
        signal,
    });
    const optionsJson = await optionsResponse.json();
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson);
    return await navigator.credentials.get({ publicKey: options, mediation, signal });
}

async function registerCredential(email, signal) {
    const query = new URLSearchParams({ username: email });
    const optionsResponse = await fetchWithErrorHandling(`/Account/PasskeyRegistrationOptions?${query}`, {
        method: 'POST',
        signal,
    });
    const optionsJson = await optionsResponse.json();
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson);
    return await navigator.credentials.create({ publicKey: options, signal });
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

    getEmail() {
        const email = new FormData(this.internals.form).get(this.attrs.emailName);
        if (typeof email !== 'string') {
            throw new Error('The email address is missing.');
        }

        return email;
    }

    async obtainCredential(useConditionalMediation, signal) {
        if (!browserSupportsPasskeys) {
            throw new Error('Some passkey features are missing. Please update your browser.');
        }

        if (this.attrs.operation === 'Create') {
            return await createCredential(signal);
        } else if (this.attrs.operation === 'Reauthenticate') {
            return await reauthenticateCredential(signal);
        } else if (this.attrs.operation === 'Request') {
            const mediation = useConditionalMediation ? 'conditional' : undefined;
            return await requestCredential(this.getEmail(), mediation, signal);
        } else if (this.attrs.operation === 'Register') {
            return await registerCredential(this.getEmail(), signal);
        } else {
            throw new Error(`Unknown passkey operation '${this.attrs.operation}'.`);
        }
    }

    async obtainAndSubmitCredential(useConditionalMediation = false) {
        this.abortController?.abort();
        this.abortController = new AbortController();
        const signal = this.abortController.signal;
        const formData = new FormData();
        try {
            const credential = await this.obtainCredential(useConditionalMediation, signal);
            const credentialJson = JSON.stringify(credential);
            formData.append(`${this.attrs.name}.CredentialJson`, credentialJson);
        } catch (error) {
            if (error.name === 'AbortError') {
                // The user explicitly canceled the operation - return without error.
                return;
            }
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
        this.internals.setFormValue(formData);
        this.internals.form.submit();
    }

    async tryAutofillPasskey() {
        if (browserSupportsPasskeys && this.attrs.operation === 'Request' && await PublicKeyCredential.isConditionalMediationAvailable?.()) {
            await this.obtainAndSubmitCredential(/* useConditionalMediation */ true);
        }
    }
});
