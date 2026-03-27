# spec notes

- Supported modes:
  => Blazor SSR with enhanced forms
  => Blazor SSR with non-enhanced forms
  => MVC forms
- Opt-in/opt-out mechanism
  => globally enabled by AddRazorComponents
  => per-form opt-out using parameter on DataAnnoationsValidator
- JS lib insertion
  => always loaded for Blazor in blazor.web.js
  => MVC: we make the JS lib compatible with MVC, the MVC team will handle MVC distribution/testing
- Remote attributem support for async JS validators
  - What happens when there is an async JS validator with Blazor enhanced navigation?
    => We release with no support for async/remote validation in Blazor (we throw as early as possible)
- Localization
  => Yes, flow the same messages to server and client validation

- TODO: add spec for not changing behavior of interactive modes / interactive parts of the app
    => if possible, the data-val- attributes should be generated only for SSR/non-interactive parts
    => must have is that the JS code does not run and modify the DOM in interactive context
