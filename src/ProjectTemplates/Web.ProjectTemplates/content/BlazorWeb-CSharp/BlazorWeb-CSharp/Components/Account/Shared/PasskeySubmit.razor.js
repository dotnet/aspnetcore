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

async function createCredential(signal) {
    const optionsResponse = await fetchWithErrorHandling('/Account/PasskeyCreationOptions', {
        method: 'POST',
        signal,
    });
    const optionsJson = await optionsResponse.json();
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson);
    return await navigator.credentials.create({ publicKey: options, signal });
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

    connectedCallback() {
        this.internals = this.attachInternals();
        this.attrs = {
            operation: this.getAttribute('operation'),
            name: this.getAttribute('name'),
            emailName: this.getAttribute('email-name'),
        };

        this.internals.form.addEventListener('submit', (event) => {
            if (event.submitter?.name === '__passkeySubmit') {
                event.preventDefault();
                this.obtainCredentialAndSubmit();
            }
        });

        this.tryAutofillPasskey();
    }

    disconnectedCallback() {
        this.abortController?.abort();
    }

    async obtainCredentialAndSubmit(useConditionalMediation = false) {
        this.abortController?.abort();
        this.abortController = new AbortController();
        const signal = this.abortController.signal;
        const formData = new FormData();
        try {
            let credential;
            if (this.attrs.operation === 'Create') {
                credential = await createCredential(signal);
            } else if (this.attrs.operation === 'Request') {
                const email = new FormData(this.internals.form).get(this.attrs.emailName);
                const mediation = useConditionalMediation ? 'conditional' : undefined;
                credential = await requestCredential(email, mediation, signal);
            } else {
                throw new Error(`Unknown passkey operation '${operation}'.`);
            }
            const credentialJson = JSON.stringify(credential);
            formData.append(`${this.attrs.name}.CredentialJson`, credentialJson);
        } catch (error) {
            if (error.name === 'AbortError') {
                // Canceled by user action, do not submit the form
                return;
            }
            formData.append(`${this.attrs.name}.Error`, error.message);
            console.error(error);
        }
        this.internals.setFormValue(formData);
        this.internals.form.submit();
    }

    async tryAutofillPasskey() {
        if (this.attrs.operation === 'Request' && await PublicKeyCredential.isConditionalMediationAvailable()) {
            await this.obtainCredentialAndSubmit(/* useConditionalMediation */ true);
        }
    }
});
