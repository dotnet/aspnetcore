import React, { useEffect, useRef, useState } from 'react'
import { Route, Redirect } from 'react-router-dom'
import { ApplicationPaths, QueryParameterNames } from './ApiAuthorizationConstants'
import authService from './AuthorizeService'

const AuthorizeRoute = (props) => {
  const [authenticated, setAuthenticated] = useState(false);
  const [ready, setReady] = useState(false);

  const _subscription = useRef();

  const populateAuthenticationState = async () => {
    const authenticated = await authService.isAuthenticated();
    setAuthenticated(authenticated);
    setReady(true);
  };

  const authenticationChanged = async () => {
    setAuthenticated(false);
    setReady(false);
    await populateAuthenticationState();
  };

  useEffect(() => {
    _subscription.current = authService.subscribe(() => authenticationChanged());
    populateAuthenticationState();

    return () => {
      authService.unsubscribe(_subscription.current);
    }
  });

  var link = document.createElement("a");
  link.href = props.path;
  const returnUrl = `${link.protocol}//${link.host}${link.pathname}${link.search}${link.hash}`;
  const redirectUrl = `${ApplicationPaths.Login}?${QueryParameterNames.ReturnUrl}=${encodeURIComponent(returnUrl)}`
  if (!ready) {
    return <div></div>;
  } else {
    const { component: Component, ...rest } = props;
    return <Route {...rest}
      render={(props) => {
        if (authenticated) {
          return <Component {...props} />
        } else {
          return <Redirect to={redirectUrl} />
        }
      }} />
  }
};

export default AuthorizeRoute;
