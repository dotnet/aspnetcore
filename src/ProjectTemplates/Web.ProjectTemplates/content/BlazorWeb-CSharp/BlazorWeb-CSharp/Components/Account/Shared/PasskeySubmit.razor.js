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

async function createCredential() {
    const optionsResponse = await fetchWithErrorHandling('/Account/PasskeyCreationOptions', {
        method: 'POST',
    });
    const optionsJson = await optionsResponse.json();
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson);
    return await navigator.credentials.create({ publicKey: options });
}

async function requestCredential(email) {
    const optionsResponse = await fetchWithErrorHandling(`/Account/PasskeyRequestOptions?username=${email}`, {
        method: 'POST',
    });
    const optionsJson = await optionsResponse.json();
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson);
    return await navigator.credentials.get({ publicKey: options });
}

customElements.define('passkey-submit', class extends HTMLElement {
    static formAssociated = true;

    connectedCallback() {
        this.internals = this.attachInternals();
        this.isObtainingCredentials = false;
        this.attrs = {
            operation: this.getAttribute('operation'),
            name: this.getAttribute('name'),
            emailName: this.getAttribute('email-name'),
        };

        this.internals.form.addEventListener('submit', (event) => {
            if (event.submitter?.name === '__passkeySubmit') {
                event.preventDefault();
                this.obtainCredentialAndReSubmit();
            }
        });
    }

    async obtainCredentialAndReSubmit() {
        if (this.isObtainingCredentials) {
            return;
        }

        this.isObtainingCredentials = true;
        const formData = new FormData();
        try {
            let credential;
            if (this.attrs.operation === 'Create') {
                credential = await createCredential();
            } else if (this.attrs.operation === 'Request') {
                const email = new FormData(this.internals.form).get(this.attrs.emailName);
                credential = await requestCredential(email);
            } else {
                throw new Error(`Unknown passkey operation '${operation}'.`);
            }
            const credentialJson = JSON.stringify(credential);
            formData.append(`${this.attrs.name}.CredentialJson`, credentialJson);
        } catch (error) {
            formData.append(`${this.attrs.name}.Error`, error.message);
            console.error(error);
        } finally {
            this.isObtainingCredentials = false;
        }
        this.internals.setFormValue(formData);
        this.internals.form.submit();
    }
});
