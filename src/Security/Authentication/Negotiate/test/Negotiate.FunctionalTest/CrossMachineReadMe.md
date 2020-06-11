Cross Machine Tests

Kerberos can only be tested in a multi-machine environment. On localhost it always falls back to NTLM which has different requirements.  Multi-machine is also necessary for interop testing across OSs. Kerberos also requires domain controller SPN configuration so we can't test it on arbitrary test boxes.

Test structure:
- A remote test server with various endpoints with different authentication restrictions.
- A remote test client with endpoints that execute specific scenarios. The input for these endpoints is theory data. The output is either 200Ok, or a failure code and description.
- The CrossMachineTest class that drives the tests. It invokes the client app with the theory data and confirms the results.

We use these three components because it allows us to run the tests from a dev machine or CI agent that is not part of the dedicated test domain/environment.

(Static) Environment Setup:
- Warning, this environment can take a day to set up. That's why we want a static test environment that we can re-use.
- Create a Windows server running DNS and Active Directory. Promote it to a domain controller.
  - Create an SPN on this machine for Windows -> Windows testing. `setspn -S "http/chrross-dc.crkerberos.com" -U administrator`
  - Future: Can we replace the domain controller with an AAD instance? We'd still want a second windows machine for Windows -> Windows testing, but AAD might be easier to configure.
    - https://docs.microsoft.com/azure/active-directory-domain-services/active-directory-ds-getting-started
    - https://docs.microsoft.com/azure/active-directory-domain-services/active-directory-ds-join-ubuntu-linux-vm
    - https://docs.microsoft.com/azure/active-directory-domain-services/active-directory-ds-enable-kcd
- Create another Windows machine and join it to the test domain.
- Create a Linux machine and joining it to the domain. Ubuntu 18.04 has been used in the past.
  - https://www.safesquid.com/content-filtering/integrating-linux-host-windows-ad-kerberos-sso-authentication
  - Include an HTTP SPN

Test deployment variations, prioritized:
- Windows -> Linux
- Windows -> Windows
- Localhost Windows -> Windows
Future:
- Note the Linux HttpClient doesn't support default credentials, you have to update Negotiate.Client to provide explicit credentials.
- Linux -> Windows
- Linux -> Linux
- Localhost Linux -> Linux

Test run setup:
- Publish Negotiate.Client as a standalone application targeting the OS you want to run it on. Copy it to that machine and run it.
  - Make sure it's running on a public IP and that the port is not blocked by the firewall.
  - Note we primarily care about having the server on the latest runtime. Publishing the client the same way is convenient but not required. We do want to update it periodically for interop testing.
  - HTTPS is optional for this client.
- Publish Negotiate.Server as a standalone application targeting the OS you want to run it on. Copy it to that machine and run it.
  - Make sure it's running on a public IP and that the port is not blocked by the firewall.
  - Note the server app starts two server instances, one with connection persistence enabled and the other with it disabled.
  - HTTPS is needed on the server for some HTTP/2 downgrade tests. These tests can be ignored if HTTPS is not conveniently available.
  - Future: Automate remote publishing
- In CrossMachineTests:
  - Set ClientAddress, ServerPersistAddress, and ServerNonPersistAddress. (Future: Pull from environment variables?)
  - UnSkip the test cases. (Future: Make these conditional on the test environment being available, environment variables?)
- Run tests!
