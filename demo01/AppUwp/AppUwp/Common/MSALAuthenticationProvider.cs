using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AppUwp.Common
{
    public class MsalAuthenticationProvider : IAuthenticationProvider
    {
        private async Task<AuthenticationResult> Authenticate()
        {
            string[] scopes = new string[] { "Mail.Read", "User.Read", "User.ReadBasic.All" };

            PublicClientApplication pca = new PublicClientApplication("3bdad76a-f08c-4e59-a452-311577cf9bad");

            try
            {
                return await pca.AcquireTokenSilentAsync(scopes, pca.Users.FirstOrDefault());
            }
            catch (MsalException msalException)
            {

                if (msalException.ErrorCode == "failed_to_acquire_token_silently" || msalException.ErrorCode == "user_null")
                    return await pca.AcquireTokenAsync(scopes);
            }

            return null;

        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var ar = await Authenticate();

            if (ar == null)
                throw new Exception("Can't authenticate user");

            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", ar.AccessToken);

        }
    }
}
