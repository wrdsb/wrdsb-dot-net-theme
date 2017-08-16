using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Azure.KeyVault;
using System.Web.Configuration;
using DotNetThemeMVC.Controllers;

namespace DotNetThemeMVC
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // I put my GetToken method in a Utils class. Change for wherever you placed your method.
            VaultAccess.GetCert();
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(VaultAccess.GetAccessToken));

            var sec = kv.GetSecretAsync(WebConfigurationManager.AppSettings["SecretUri"]);

            //I put a variable in a Utils class to hold the secret for general  application use.
            VaultAccess.EncryptSecret = sec.Result.Value;
            System.Diagnostics.Debug.WriteLine(sec.Result.Value.ToString());
        }
    }
}
