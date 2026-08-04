customElements.define('passkey-signals', class extends HTMLElement {
    async connectedCallback() {
        const { rpId, userId, allAcceptedCredentialIds, name, displayName } = JSON.parse(this.getAttribute('options'));
        try {
            // Tells the authenticator which passkeys are still valid so that deleted ones are no
            // longer offered at sign-in, and keeps the displayed user details up to date.
            // Not all browsers support these, and they are best-effort, so failures are not surfaced.
            await window.PublicKeyCredential?.signalAllAcceptedCredentials?.({ rpId, userId, allAcceptedCredentialIds });
            await window.PublicKeyCredential?.signalCurrentUserDetails?.({ rpId, userId, name, displayName });
        } catch (error) {
            console.error(error);
        }
    }
});
