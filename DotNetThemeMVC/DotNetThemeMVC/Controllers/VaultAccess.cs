using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace DotNetThemeMVC.Controllers
{
    public class VaultAccess
    {
        //this is an optional property to hold the secret after it is retrieved
        public static string EncryptSecret { get; set; }
        public static ClientAssertionCertificate AssertionCert { get; set; }

        //the method that will be provided to the KeyVaultClient
        public static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(WebConfigurationManager.AppSettings["ClientId"],
                        WebConfigurationManager.AppSettings["ClientSecret"]);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }

        public static void GetCert()
        {
            var clientAssertionCertPfx = CertificateHelper.FindCertificateByThumbprint(WebConfigurationManager.AppSettings["thumbprint"]);
            AssertionCert = new ClientAssertionCertificate(WebConfigurationManager.AppSettings["ClientId"], clientAssertionCertPfx);
        }
    }
}