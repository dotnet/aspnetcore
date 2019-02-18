import React from 'react'
import { Component } from 'react';
import authService from './AuthorizeService';
import { AuthenticationResultStatus } from './AuthorizeService';
import { LoginActions, QueryParameterNames, ApplicationPaths } from './ApiAuthorizationConstants';

export class Login extends Component {
    constructor(props) {
        super(props);

        this.state = {
            message: undefined
        };
    }

    componentDidMount() {
        const action = this.props.action;
        switch (action) {
            case LoginActions.Login:
                this.login(this.getReturnUrl());
                break;
            case LoginActions.LoginCallback:
                this.processLoginCallback();
                break;
            case LoginActions.Profile:
                this.redirectToProfile();
                break;
            case LoginActions.Register:
                this.redirectToRegister();
                break;
            case LoginActions.LoginFailed:
                const params = new URLSearchParams(window.location.search);
                const error = params.get(QueryParameterNames.Message);
                this.setState({ message: error });
                break;
            default:
                throw new Error(`Invalid action '${action}'`);
        }
    }

    render() {
        const action = this.props.action;
        const { message } = this.state;

        if (!!message) {
            return <div>{message}</div>
        } else {
            switch (action) {
                case LoginActions.Login:
                    return (<div>Processing login</div>);
                case LoginActions.LoginCallback:
                    return (<div>Processing login callback</div>);
                default:
                    throw new Error(`Invalid action '${action}'`);
            }
        }
    }

    async login(returnUrl) {
        const state = { returnUrl };
        const result = await authService.signIn(state);
        switch (result.status) {
            case AuthenticationResultStatus.Redirect:
                window.location.replace(result.redirectUrl);
                break;
            case AuthenticationResultStatus.Success:
                await this.navigateToReturnUrl(returnUrl);
                break;
            case AuthenticationResultStatus.Fail:
                this.setState({ message: result.message });
                break;
            default:
                throw new Error(`Invalid status result ${result.status}.`);
        }
    }

    async processLoginCallback() {
        const url = window.location.href;
        const result = await authService.completeSignIn(url);
        switch (result.status) {
            case AuthenticationResultStatus.Redirect:
                // There should not be any redirects as the only time completeAuthentication finishes
                // is when we are doing a redirect sign in flow.
                throw new Error('Should not redirect.');
            case AuthenticationResultStatus.Success:
                await this.navigateToReturnUrl(this.getReturnUrl(result.state));
                break;
            case AuthenticationResultStatus.Fail:
                this.setState({ message: result.message });
                break;
            default:
                throw new Error(`Invalid authentication result status '${result.status}'.`);
        }
    }

    getReturnUrl(state) {
        const params = new URLSearchParams(window.location.search);
        const fromQuery = params.get(QueryParameterNames.ReturnUrl);
        return (state && state.returnUrl) || fromQuery || `${window.location.protocol}//${window.location.host}/`;
    }

    redirectToRegister() {
        this.redirectToApiAuthorizationPath(ApplicationPaths.IdentityRegisterPath);
    }

    redirectToProfile() {
        this.redirectToApiAuthorizationPath(ApplicationPaths.IdentityManagePath);
    }

    redirectToApiAuthorizationPath(apiAuthorizationPath) {
        const redirectUrl = `${this.getBaseUrl()}${apiAuthorizationPath}`;
        window.location.replace(redirectUrl);
    }

    getBaseUrl() {
        const url = window.location;
        return `${url.protocol}//${url.host}`;
    }

    navigateToReturnUrl(returnUrl) {
        window.location.replace(returnUrl);
    }
}