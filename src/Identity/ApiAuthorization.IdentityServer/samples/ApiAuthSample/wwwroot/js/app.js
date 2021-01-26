var ids = {
    login: 'login',
    logout: 'logout',
    callApi: 'call-api',
    loginResult: 'login-result',
    apiResults: 'api-result'
};

let mgr = undefined;

function invokeLogin() {
    // Redirects to the Authorization Server for sign in.
    return mgr.signinRedirect();
}

function invokeLogout() {
    // Redirects to the Authorization Server for sign out.
    return mgr.signoutRedirect();
}

async function handleAuthorizationServerCallback() {
    try {
        let user = await mgr.signinRedirectCallback();
        updateUserUI(user);
    } catch (error) {
        updateUserUI(undefined, error);
    }
}

async function callApi() {
    try {
        let user = await mgr.getUser();
        let response = await fetch(
            window.location.origin + '/api/values',
            {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${user.access_token}`
                }
            });

        if (response.ok) {
            return await response.json();
        } else {
            let text = await response.text();
            return text;
        }
    } catch (e) {
        return e.message;
    }
}

// Code to update the UI

if (window.location.hash || window.location.search) {
    initializeApplication()
        .then(() => {
            handleAuthorizationServerCallback();
            window.location.hash = '';
        });
}

document.onreadystatechange = function () {
    if (document.readyState === 'complete') {
        let login = document.getElementById(ids.login);
        let logout = document.getElementById(ids.logout);
        let callApi = document.getElementById(ids.callApi);

        login.addEventListener('click', invokeLogin);
        logout.addEventListener('click', invokeLogout);
        callApi.addEventListener('click', invokeCallApi);
    }
};

async function initializeApplication() {
    const response = await fetch('_configuration/ApiAuthSampleSPA');
    const configuration = await response.json();
    mgr = new Oidc.UserManager(configuration);

    enableLoginButton();

    function enableLoginButton() {
        const login = document.querySelector('#login');
        login.disabled = false;
    }
}


function updateUserUI(user, error) {
    let loginResults = document.getElementById(ids.loginResult);
    let heading = document.createElement('h2');
    heading.innerText = 'Login result';
    if (user) {
        loginResults.appendChild(heading);
        loginResults.insertAdjacentText('beforeend', `Hello ${user.profile.name}`);
        updateButtons(true, false, false);
    } else {
        loginResults.innerText = error.message;
    }
}

function updateButtons(login, callApi, logout) {
    let loginB = document.getElementById(ids.login);
    let logoutB = document.getElementById(ids.logout);
    let callApiB = document.getElementById(ids.callApi);

    loginB.disabled = login;
    logoutB.disabled = logout;
    callApiB.disabled = callApi;
}

async function invokeCallApi() {
    let result = await callApi();
    let results = document.getElementById(ids.apiResults);
    if (Array.isArray(result)) {
        let list = document.createElement('ul');
        let listElements = result.map(e => createListElement(e));
        for (let element of listElements) {
            list.appendChild(element);
        }
        let heading = document.createElement('h2');
        heading.innerText = 'API call results';
        results.appendChild(heading);
        results.appendChild(list);
    } else {
        results.innerText = result;
    }

    function createListElement(element) {
        let node = document.createElement('li');
        node.innerText = element;
        return node;
    }
}
