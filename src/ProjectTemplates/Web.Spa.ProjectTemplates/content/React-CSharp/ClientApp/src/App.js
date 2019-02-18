import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { FetchData } from './components/FetchData';
import { Counter } from './components/Counter';
import { Login } from './components/api-authorization/Login'
import { Logout } from './components/api-authorization/Logout'
import AuthorizeRoute from './components/api-authorization/AuthorizeRoute';

export default class App extends Component {
  static displayName = App.name;

  render () {
    return (
      <Layout>
        <Route exact path='/' component={Home} />
        <Route path='/counter' component={Counter} />
        <Route path='/fetch-data' component={FetchData} />
        <AuthorizeRoute path='/fetch-data' component={FetchData} />
        <Route path='/authentication/login' render={() => loginAction('login')} />
        <Route path='/authentication/login-callback' render={() => loginAction('login-callback')} />
        <Route path='/authentication/profile' render={() => loginAction('profile')} />
        <Route path='/authentication/register' render={() => loginAction('register')} />
        <Route path='/authentication/logout' render={() => logoutAction('logout')} />
        <Route path='/authentication/logout-callback' render={() => logoutAction('logout-callback')} />
      </Layout>
    );
  }
}

function loginAction(name){
    return (<Login action={name}></Login>);
}

function logoutAction(name) {
    return (<Logout action={name}></Logout>);
}
