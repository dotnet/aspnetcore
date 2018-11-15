using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MusicStore.Mocks.OpenIdConnect
{
    internal class OpenIdConnectBackChannelHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage();

            var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "ForTesting", "Mocks", "OpenIdConnect"));

            if (request.RequestUri.AbsoluteUri == "https://login.windows.net/[tenantName].onmicrosoft.com/.well-known/openid-configuration")
            {
                response.Content = new StringContent(File.ReadAllText(Path.Combine(basePath, "openid-configuration.json")));
            }
            else if (request.RequestUri.AbsoluteUri == "https://login.windows.net/common/discovery/keys")
            {
                response.Content = new StringContent(File.ReadAllText(Path.Combine(basePath, "keys.json")));
            }
            else if (request.RequestUri.AbsoluteUri == "https://login.windows.net/4afbc689-805b-48cf-a24c-d4aa3248a248/oauth2/token")
            {
                response.Content = new StringContent("{\"id_token\": \"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6ImtyaU1QZG1Cdng2OHNrVDgtbVBBQjNCc2VlQSJ9.eyJhdWQiOiJjOTk0OTdhYS0zZWUyLTQ3MDctYjhhOC1jMzNmNTEzMjNmZWYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC80YWZiYzY4OS04MDViLTQ4Y2YtYTI0Yy1kNGFhMzI0OGEyNDgvIiwiaWF0IjoxNDIyMzk1NzYzLCJuYmYiOjE0MjIzOTU3NjMsImV4cCI6MTQyMjM5OTY2MywidmVyIjoiMS4wIiwidGlkIjoiNGFmYmM2ODktODA1Yi00OGNmLWEyNGMtZDRhYTMyNDhhMjQ4IiwiYW1yIjpbInB3ZCJdLCJvaWQiOiJmODc2YWJlYi1kNmI1LTQ0ZTQtOTcxNi02MjY2YWMwMTgxYTgiLCJ1cG4iOiJ1c2VyM0BwcmFidXJhamdtYWlsLm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6IlBVZGhjbFA1UGdJalNVOVAxUy1IZWxEYVNGU2YtbVhWMVk2MC1LMnZXcXciLCJnaXZlbl9uYW1lIjoiVXNlcjMiLCJmYW1pbHlfbmFtZSI6IlVzZXIzIiwibmFtZSI6IlVzZXIzIiwidW5pcXVlX25hbWUiOiJ1c2VyM0BwcmFidXJhamdtYWlsLm9ubWljcm9zb2Z0LmNvbSIsIm5vbmNlIjoiNjM1NTc5OTI4NjM5NTE3NzE1Lk9UUmpPVFZrTTJFdE1EUm1ZUzAwWkRFM0xUaGhaR1V0WldabVpHTTRPRGt6Wkdaa01EUmxORGhrTjJNdE9XSXdNQzAwWm1Wa0xXSTVNVEl0TVRVd1ltUTRNemRtT1dJMCIsImNfaGFzaCI6IkZHdDN3Y1FBRGUwUFkxUXg3TzFyNmciLCJwd2RfZXhwIjoiNjY5MzI4MCIsInB3ZF91cmwiOiJodHRwczovL3BvcnRhbC5taWNyb3NvZnRvbmxpbmUuY29tL0NoYW5nZVBhc3N3b3JkLmFzcHgifQ.coAdCkdMgnslMHagdU8IBgH7Z0dilRdMfKytyqPJuTr6sbmbhrAoAj-KeGwbKgzrd-BeDk_rW47dntWuuAqGrAOGzxXvS2dcSWgoEKoXuDccIL5b4rIomRpfJpaeE-YwiU3usyRvoQCpHmtOa0g7xVilIj3_1-9ylMgRDY5qcrtQ_hEZlGuYyiCPR0dw8WmNU7r6PKObG-o3Yk_RbEBHjnaWxKoJwrVUEZUQOJDAvlr6ZYEmGTlD_BM0Rc_0fJZPU7A3uN9PHLw1atm-chN06IDXf23R33JI_xFuEZnj9HZQ_eIzNCl7GFmUryK3FFgYJpIbsI0BIFuksSikXz33IA\", \"access_token\": \"access\"}");
            }

            return Task.FromResult(response);
        }
    }
}
