// This web component is used from the RenderAttributesBeforeConnectedCallback test case

window.customElements.define('custom-web-component-data-from-attribute', class extends HTMLElement {
    connectedCallback() {
        let myattribute = this.getAttribute('myattribute') || 'failed';

        this.innerHTML = myattribute;
    }
});
