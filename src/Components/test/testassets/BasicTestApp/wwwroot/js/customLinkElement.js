// This web component is used from the CanFollowLinkDefinedInOpenShadowRoot test case

window.customElements.define('custom-link-with-shadow-root', class extends HTMLElement {
    connectedCallback() {
        const shadowRoot = this.attachShadow({ mode: 'open' });
        const href = this.getAttribute('target-url');
        shadowRoot.innerHTML = `<a href='${href}'>Anchor tag within shadow root</a>`;
    }
});
