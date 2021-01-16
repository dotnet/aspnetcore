import React, { useEffect, useState } from 'react'
import authService from './AuthorizeService';
import { AuthenticationResultStatus } from './AuthorizeService';
import { LoginActions, QueryParameterNames, ApplicationPaths } from './ApiAuthorizationConstants';

// The main responsibility of this component is to handle the user's login process.
// This is the starting point for the login process. Any component that needs to authenticate
// a user can simply perform a redirect to this component with a returnUrl query parameter and
// let the component perform the login and return back to the return url.
const Login = (props) => {
  const [message, setMessage] = useState(undefined);

  const getReturnUrl = (state) => {
    const params = new URLSearchParams(window.location.search);
    const fromQuery = params.get(QueryParameterNames.ReturnUrl);
    if (fromQuery && !fromQuery.startsWith(`${window.location.origin}/`)) {
      // This is an extra check to prevent open redirects.
      throw new Error("Invalid return url. The return url needs to have the same origin as the current page.")
    }
    return (state && state.returnUrl) || fromQuery || `${window.location.origin}/`;
  };

  const navigateToReturnUrl = (returnUrl) => {
    // It's important that we do a replace here so that we remove the callback uri with the
    // fragment containing the tokens from the browser history.
    window.location.replace(returnUrl);
  };

  const redirectToApiAuthorizationPath = (apiAuthorizationPath) => {
    const redirectUrl = `${window.location.origin}/${apiAuthorizationPath}`;
    // It's important that we do a replace here so that when the user hits the back arrow on the
    // browser they get sent back to where it was on the app instead of to an endpoint on this
    // component.
    window.location.replace(redirectUrl);
  };

  useEffect(() => {
    const login = async (returnUrl) => {
      const state = { returnUrl };
      const result = await authService.signIn(state);
      switch (result.status) {
        case AuthenticationResultStatus.Redirect:
          break;
        case AuthenticationResultStatus.Success:
          await navigateToReturnUrl(returnUrl);
          break;
        case AuthenticationResultStatus.Fail:
          setMessage(result.message);
          break;
        default:
          throw new Error(`Invalid status result ${result.status}.`);
      }
    };

    const processLoginCallback = async () => {
      const url = window.location.href;
      const result = await authService.completeSignIn(url);
      switch (result.status) {
        case AuthenticationResultStatus.Redirect:
          // There should not be any redirects as the only time completeSignIn finishes
          // is when we are doing a redirect sign in flow.
          throw new Error('Should not redirect.');
        case AuthenticationResultStatus.Success:
          await navigateToReturnUrl(getReturnUrl(result.state));
          break;
        case AuthenticationResultStatus.Fail:
          setMessage(result.message);
          break;
        default:
          throw new Error(`Invalid authentication result status '${result.status}'.`);
      }
    };

    const redirectToProfile = () => {
      redirectToApiAuthorizationPath(ApplicationPaths.IdentityManagePath);
    };

    const redirectToRegister = () => {
      redirectToApiAuthorizationPath(`${ApplicationPaths.IdentityRegisterPath}?${QueryParameterNames.ReturnUrl}=${encodeURI(ApplicationPaths.Login)}`);
    };

    const action = props.action;
    switch (action) {
      case LoginActions.Login:
        login(getReturnUrl());
        break;
      case LoginActions.LoginCallback:
        processLoginCallback();
        break;
      case LoginActions.LoginFailed:
        const params = new URLSearchParams(window.location.search);
        const error = params.get(QueryParameterNames.Message);
        setMessage(error);
        break;
      case LoginActions.Profile:
        redirectToProfile();
        break;
      case LoginActions.Register:
        redirectToRegister();
        break;
      default:
        throw new Error(`Invalid action '${action}'`);
    }
  }, [props.action]);

  const action = props.action;
  if (!!message) {
    return <div>{message}</div>
  } else {
    switch (action) {
      case LoginActions.Login:
        return (<div>Processing login</div>);
      case LoginActions.LoginCallback:
        return (<div>Processing login callback</div>);
      case LoginActions.Profile:
      case LoginActions.Register:
        return (<div></div>);
      default:
        throw new Error(`Invalid action '${action}'`);
    }
  }
};

export default Login;
