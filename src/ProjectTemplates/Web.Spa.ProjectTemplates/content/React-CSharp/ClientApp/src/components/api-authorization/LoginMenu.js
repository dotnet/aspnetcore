import React, { Component, Fragment } from 'react';
import { NavItem, NavLink } from 'reactstrap';
import { Link } from 'react-router-dom';
import authService from './AuthorizeService';

export class LoginMenu extends Component {
    constructor(props) {
        super(props);

        this.state = {
            isAuthenticated: false,
            userName: null
        };
    }

    componentDidMount() {
        this._subscription = authService.subscribe(() => this.populateState());
        this.populateState();
    }

    componentWillUnmount() {
        authService.unsubscribe(this._subscription);
    }

    async populateState() {
        const [isAuthenticated, user] = await Promise.all([authService.isAuthenticated(), authService.getUser()])
        this.setState({
            isAuthenticated,
            userName: user && user.name
        });
    }

    render() {
        if (!this.state.isAuthenticated) {
            return this.anonymousView();
        } else {
            return this.authenticatedView();
        }
    }

    authenticatedView() {
        return (<Fragment>
            <NavItem>
                <NavLink tag={Link} className="text-dark" to="/authentication/profile">Hello {this.state.userName}</NavLink>
            </NavItem>
            <NavItem>
                <NavLink tag={Link} className="text-dark" to="/authentication/logout">Logout</NavLink>
            </NavItem>
        </Fragment>);

    }

    anonymousView() {
        return (<Fragment>
            <NavItem>
                <NavLink tag={Link} className="text-dark" to="/authentication/register">Register</NavLink>
            </NavItem>
            <NavItem>
                <NavLink tag={Link} className="text-dark" to="/authentication/login">Login</NavLink>
            </NavItem>
        </Fragment>);
    }
}
