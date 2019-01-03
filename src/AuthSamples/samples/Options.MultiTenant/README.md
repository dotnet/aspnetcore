AuthSamples.Options.MultiTenant

Sample demonstrating dynamic authentication schemes and options with multiple tenants each having their own schemes and credentials:

1. Run the app, the Home page will show all the authentication schemes.
2. You can add new schemes via the form at the bottom, and remove any via the Remove button.
3. You can also update any of the scheme options message via the add/update form.
4. Tenant id is specified via query string ?tenant=id, each tenant should have its own set of schemes/creditials.

The dynamic scheme code very similar to the dynamic scheme sample, the relevant multitenant code is:
- TenantResolver: resolves the tenant from the request.
- TenantSchemeResolver: maintains the set of authentication schemes per tenant.
- TenantOptionsMonitor: resolves the appropriate set of authentication options for the tenant.
- TenantOptionsCache: allows the app to invalidate/update the options for a particular scheme.