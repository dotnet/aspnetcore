import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { FetchData } from './components/FetchData';
import { Counter } from './components/Counter';
////#if (IndividualLocalAuth)
import { Login } from './components/api-authorization/Login'
import { Logout } from './components/api-authorization/Logout'
import AuthorizeRoute from './components/api-authorization/AuthorizeRoute';
import { ApplicationPaths, LoginActions, LogoutActions } from './components/api-authorization/ApiAuthorizationConstants';
////#endif

export default class App extends Component {
  static displayName = App.name;

  render () {
    return (
      <Layout>
        <Route exact path='/' component={Home} />
        <Route path='/counter' component={Counter} />
////#if (!IndividualLocalAuth)
        <Route path='/fetch-data' component={FetchData} />
////#endif
////#if (IndividualLocalAuth)
        <AuthorizeRoute path='/fetch-data' component={FetchData} />
        <Route path={ApplicationPaths.Login} render={() => loginAction(LoginActions.Login)} />
        <Route path={ApplicationPaths.LoginFailed} render={() => loginAction(LoginActions.LoginFailed)} />
        <Route path={ApplicationPaths.LoginCallback} render={() => loginAction(LoginActions.LoginCallback)} />
        <Route path={ApplicationPaths.Profile} render={() => loginAction(LoginActions.Profile)} />
        <Route path={ApplicationPaths.Register} render={() => loginAction(LoginActions.Register)} />
        <Route path={ApplicationPaths.LogOut} render={() => logoutAction(LogoutActions.Logout)} />
        <Route path={ApplicationPaths.LogOutCallback} render={() => logoutAction(LogoutActions.LogoutCallback)} />
        <Route path={ApplicationPaths.LoggedOut} render={() => logoutAction(LogoutActions.LoggedOut)} />
////#endif
      </Layout>
    );
  }
}
////#if (IndividualLocalAuth)

function loginAction(name){
    return (<Login action={name}></Login>);
}

function logoutAction(name) {
    return (<Logout action={name}></Logout>);
}
////#endif
