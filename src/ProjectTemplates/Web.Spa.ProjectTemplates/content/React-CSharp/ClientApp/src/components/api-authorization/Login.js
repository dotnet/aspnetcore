import React from 'react'
import { Component } from 'react';
import authService from './AuthorizeService';
import { AuthenticationResultStatus } from './AuthorizeService';

export class Login extends Component {
    constructor(props) {
        super(props);

        this.state = {
            loginErrors: []
        };

        const action = this.props.action;
        switch (action) {
            case 'login':
                this.login(this.getReturnUrl());
                break;
            case 'login-callback':
                this.processLoginCallback();
                break;
            case 'profile':
                this.redirectToProfile();
                break;
            case 'register':
                this.redirectToRegister();
                break;
            default:
                throw new Error(`Invalid action '${action}'`);
        }
    }

    getReturnUrl = (state) => {
        let params = new URLSearchParams(window.location.search);
        let fromQuery = params.get('returnUrl');
        return (state && state.returnUrl) || fromQuery || `${window.location.protocol}//${window.location.host}/`;
    }

    render() {
        const action = this.props.action;
        if (this.state.loginErrors.length > 0) {
            let errors = [];
            let i = 0;
            for (let error of this.state.loginErrors) {
                errors.push(<p key={i++}>{error}</p>);
            }
            return <div>{errors}</div>
        } else {
            switch (action) {
                case 'login':
                    return (<div>Processing login</div>);
                case 'login-callback':
                    return (<div>Processing logout</div>);
                default:
                    throw new Error(`Invalid action '${action}'`);
            }
        }
    }

    login = async (returnUrl) => {
        const state = { returnUrl };
        const result = await authService.authenticate(state);
        switch (result.status) {
            case AuthenticationResultStatus.Redirect:
                window.location.replace(result.redirectUrl);
                break;
            case AuthenticationResultStatus.Success:
                await this.navigateToReturnUrl(returnUrl);
                break;
            case AuthenticationResultStatus.Fail:
                this.state.loginErrors.push(result.message);
                break;
            default:
                throw new Error(`Invalid status result ${result.status}.`);
        }
    }

    navigateToReturnUrl = (returnUrl) =>
        window.location.replace(returnUrl);

    processLoginCallback = async () => {
        const url = window.location.href;
        const result = await authService.completeAuthentication(url);
        switch (result.status) {
            case AuthenticationResultStatus.Redirect:
                // There should not be any redirects as the only time completeAuthentication finishes
                // is when we are doing a redirect sign in flow.
                throw new Error('Should not redirect.');
            case AuthenticationResultStatus.Success:
                await this.navigateToReturnUrl(this.getReturnUrl(result.state));
                break;
            case AuthenticationResultStatus.Fail:
                this.state.loginErrors.push(result.message);
                break;
            default:
                throw new Error(`Invalid authentication result status '${result.status}'.`);
        }
    };

    redirectToRegister() {
        this.redirectToApiAuthorizationPath('/Identity/Account/Register');
    }

    redirectToProfile() {
        this.redirectToApiAuthorizationPath('/Identity/Account/Manage');
    }

    redirectToApiAuthorizationPath(apiAuthorizationPath) {
        const redirectUrl = `${this.getBaseUrl()}${apiAuthorizationPath}`;
        window.location.replace(redirectUrl);
    }

    getBaseUrl() {
        const url = window.location;
        return `${url.protocol}//${url.host}`;
    }
}