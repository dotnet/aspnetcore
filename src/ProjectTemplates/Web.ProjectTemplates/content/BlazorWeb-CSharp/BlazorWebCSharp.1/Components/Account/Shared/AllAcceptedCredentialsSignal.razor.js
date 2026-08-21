customElements.define('all-accepted-credentials-signal', class extends HTMLElement {
    static observedAttributes = ['options'];

    connectedCallback() {
        this.signal();
    }

    attributeChangedCallback() {
        // Enhanced navigation updates the attribute in place without reconnecting the element,
        // so connectedCallback does not run again.
        if (this.isConnected) {
            this.signal();
        }
    }

    async signal() {
        const options = this.getAttribute('options');
        if (!options || options === this.signaledOptions) {
            return;
        }
        this.signaledOptions = options;
        try {
            // Tells the authenticator which passkeys are still valid so that deleted ones are no
            // longer offered at sign-in.
            // Not all browsers support this, and it is best-effort, so failures are not surfaced.
            await window.PublicKeyCredential?.signalAllAcceptedCredentials?.(JSON.parse(options));
        } catch (error) {
            if (this.signaledOptions === options) {
                this.signaledOptions = undefined;
            }
            console.error(error);
        }
    }
});
