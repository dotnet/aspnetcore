async function createCredential(optionsJSON) {
    // See: https://www.w3.org/TR/webauthn-2/#sctn-registering-a-new-credential

    // 1. Let options be a new PublicKeyCredentialCreationOptions structure configured to
    //    the Relying Party’s needs for the ceremony.
    // See: https://www.w3.org/TR/webauthn-3/#dom-publickeycredential-parsecreationoptionsfromjson
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJSON);

    // 2. Call navigator.credentials.create() and pass options as the publicKey option.
    //    Let credential be the result of the successfully resolved promise.
    //    If the promise is rejected, abort the ceremony with a user-visible error,
    //    or otherwise guide the user experience as might be determinable from the
    //    context available in the rejected promise.
    const credential = await navigator.credentials.create({ publicKey: options });

    // 3. Let response be credential.response. If response is not an instance of
    //    AuthenticatorAttestationResponse, abort the ceremony with a user-visible error.
    if (!(credential?.response instanceof AuthenticatorAttestationResponse)) {
        throw new Error('The authenticator failed to provide a valid credential.');
    }

    // Continue the ceremony on the server.
    // See: https://www.w3.org/TR/webauthn-3/#dom-publickeycredential-tojson
    return JSON.stringify(credential);
}

async function getCredential(optionsJSON) {
    // See: https://www.w3.org/TR/webauthn-2/#sctn-verifying-assertion

    // 1. Let options be a new PublicKeyCredentialRequestOptions structure configured to
    //    the Relying Party’s needs for the ceremony.
    // See: https://www.w3.org/TR/webauthn-3/#dom-publickeycredential-parserequestoptionsfromjson
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJSON);

    // 2. Call navigator.credentials.get() and pass options as the publicKey option.
    //    Let credential be the result of the successfully resolved promise.
    //    If the promise is rejected, abort the ceremony with a user-visible error,
    //    or otherwise guide the user experience as might be determinable from the
    //    context available in the rejected promise.
    const credential = await navigator.credentials.get({ publicKey: options });

    // 3. Let response be credential.response. If response is not an instance of
    //    AuthenticatorAssertionResponse, abort the ceremony with a user - visible error.
    if (!(credential?.response instanceof AuthenticatorResponse)) {
        throw new Error('The authenticator failed to provide a valid credential.');
    }

    // Continue the ceremony on the server.
    // See: https://www.w3.org/TR/webauthn-3/#dom-publickeycredential-tojson
    return JSON.stringify(credential);
}

async function submitResponse(action) {
    const optionsScript = document.getElementById('passkey-options');
    const form = document.getElementById('passkey-response-form');
    const responseInput = document.getElementById('passkey-response');
    const errorInput = document.getElementById('passkey-error');

    try {
        const optionsJson = optionsScript.innerHTML;
        const options = JSON.parse(optionsJson);

        if (action === 'create') {
            responseInput.value = await createCredential(options);
        } else if (action === 'get') {
            responseInput.value = await getCredential(options);
        } else {
            throw new Error(`Unknown passkey action '${action}'.`);
        }
    } catch (error) {
        errorInput.value = error.message;
    }

    form.submit();
}

const action = document.currentScript.getAttribute('data-action');
submitResponse(action);
