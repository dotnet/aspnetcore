import React from 'react'
import { Component } from 'react'
import { Route, Redirect } from 'react-router-dom'

import authService from './AuthorizeService'

export default class AuthorizeRoute extends Component {
    constructor(props) {
        super(props);

        this.state = {
            ready: false,
            authenticated: false
        };
    }

    componentDidMount() {
        authService.isAuthenticated()
            .then(authenticated => {
                this.setState({ ready: true, authenticated })
            });
    }

    render() {
        if (!this.state.ready) {
            return <div></div>;
        } else {
            let { component: Component, ...rest } = this.props;
            return <Route {...rest}
                render={(props) => {
                    if (this.state.authenticated) {
                        return <Component {...props} />
                    } else {
                        return <Redirect to={`/authentication/login?returnUrl=${encodeURI(window.location.href)}`} />
                    }
                }} />
        }
    }
}