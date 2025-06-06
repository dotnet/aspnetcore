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
    const optionsResponse = await fetchWithErrorHandling(`/Account/PasskeyRequestOptions?email=${email}`, {
        method: 'POST',
    });
    const optionsJson = await optionsResponse.json();
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson);
    return await navigator.credentials.get({ publicKey: options });
}

customElements.define('passkey-submit', class extends HTMLElement {
    connectedCallback() {
        this.form = this.closest('form');
        this.attrs = {
            operation: this.getAttribute('operation'),
            credentialJsonName: this.getAttribute('credential-json-name'),
            errorName: this.getAttribute('error-name'),
            emailName: this.getAttribute('email-name'),
        };

        this.form.addEventListener('submit', (event) => {
            if (event.submitter?.name === '__passkey') {
                event.preventDefault();
                this.obtainCredentialAndReSubmit();
            }
        });
    }

    addFormValue(name, value) {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        this.form.appendChild(input);
    }

    async obtainCredentialAndReSubmit() {
        try {
            let credential;
            if (this.attrs.operation === 'create') {
                credential = await createCredential();
            } else if (this.attrs.operation === 'request') {
                const email = new FormData(this.form).get(this.attrs.emailName);
                credential = await requestCredential(email);
            } else {
                throw new Error(`Unknown passkey operation '${operation}'`);
            }
            const credentialJson = JSON.stringify(credential);
            this.addFormValue(this.attrs.credentialJsonName, credentialJson);
        } catch (error) {
            this.addFormValue(this.attrs.errorName, error.message);
            console.error(error);
        }
        this.form.submit();
    }
});
