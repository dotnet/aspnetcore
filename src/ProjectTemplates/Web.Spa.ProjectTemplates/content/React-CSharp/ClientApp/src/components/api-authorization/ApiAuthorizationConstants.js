export const ApplicationName = 'Company.WebApplication1';

export const QueryParameterNames = {
  ReturnUrl: "returnUrl",
  Message: 'message'
};

export const LogoutActions = {
  LogoutCallback: 'logout-callback',
  Logout: 'logout',
  LoggedOut: 'logged-out'
};

export const LoginActions = {
  Login: 'login',
  LoginCallback: 'login-callback',
  LoginFailed: 'login-failed',
  Profile: 'profile',
  Register: 'register'
};

export const ApplicationPaths = {
  DefaultLoginRedirectPath: '/',
  ApiAuthorizationClientConfigurationUrl: `/_configuration/${ApplicationName}`,
  Login: `/authentication/${LoginActions.Login}`,
  LoginFailed: `/authentication/${LoginActions.LoginFailed}`,
  LoginCallback: `/authentication/${LoginActions.LoginCallback}`,
  Register: `/authentication/${LoginActions.Register}`,
  Profile: `/authentication/${LoginActions.Profile}`,
  LogOut: `/authentication/${LogoutActions.Logout}`,
  LoggedOut: `/authentication/${LogoutActions.LoggedOut}`,
  LogOutCallback: `/authentication/${LogoutActions.LogoutCallback}`,
  IdentityRegisterPath: '/Identity/Account/Register',
  IdentityManagePath: '/Identity/Account/Manage'
};
