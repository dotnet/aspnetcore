import React from 'react'
import { Component } from 'react';
import authService from './AuthorizeService';
import { AuthenticationResultStatus } from './AuthorizeService';

export class Logout extends Component {
    constructor(props) {
        super(props);

        this.state = {
            logoutErrors: [],
            isReady: false,
            authenticated: false
        };

        const action = this.props.action;
        switch (action) {
            case 'logout':
                this.logout(this.getReturnUrl());
                break;
            case 'logout-callback':
                this.processLogoutCallback();
                break;
            default:
                throw new Error(`Invalid action '${action}'`);
        }
    }

    componentDidMount() {
        authService.isauthenticated()
            .then(authenticated =>
                this.setState({
                    isReady: true,
                    authenticated
                }));
    }

    getReturnUrl = (state) => {
        let params = new URLSearchParams(window.location.search);
        let fromQuery = params.get('returnUrl');
        return (state && state.returnUrl) || fromQuery || `${window.location.protocol}//${window.location.host}/`;
    }

    render() {
        if (this.state.isReady) {
            return <div></div>
        }
        if (this.state.logoutErrors.length > 0) {
            let errors = [];
            let i = 0;
            for (let error of this.state.logoutErrors) {
                errors.push(<p key={i++}>{error}</p>);
            }
            return <div>{errors}</div>
        } else {
            const action = this.props.action;
            switch (action) {
                case 'logout':
                    return (<div>Processing logout</div>);
                case 'logout-callback':
                    return (<div>Processing logout callback</div>);
                default:
                    throw new Error(`Invalid action '${action}'`);
            }
        }
    }

    logout = async (returnUrl) => {
        const state = { returnUrl };
        var isauthenticated = await authService.isAuthenticated();
        if (isauthenticated) {
            const result = await authService.signOut(state);
            switch (result.status) {
                case AuthenticationResultStatus.Redirect:
                    window.location.replace(result.redirectUrl);
                    break;
                case AuthenticationResultStatus.Success:
                    await this.navigateToReturnUrl(returnUrl);
                    break;
                case AuthenticationResultStatus.Fail:
                    this.setState({ logoutErrors: [...this.state.logoutErrors, result.message] });
                    break;
            }
        } else {
            this.setState({ logoutErrors: [...this.state.logoutErrors, "You successfully logged out!"] });
        }
    }

    navigateToReturnUrl = (returnUrl) =>
        window.location.replace(returnUrl);

    processLogoutCallback = async () => {
        const url = window.location.href;
        const result = await authService.completeSignOut(url);
        switch (result.status) {
            case AuthenticationResultStatus.Redirect:
                // There should not be any redirects as the only time completeAuthentication finishes
                // is when we are doing a redirect sign in flow.
                throw new Error('Should not redirect.');
            case AuthenticationResultStatus.Success:
                await this.navigateToReturnUrl(this.getReturnUrl(result.state));
                break;
            case AuthenticationResultStatus.Fail:
                this.setState({ logoutErrors: [...this.state.logoutErrors, result.message] });
                break;
        }
    };
}