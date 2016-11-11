using Microsoft.Owin;
using Owin;
using Microsoft.Owin.Security.DataProtection;

[assembly: OwinStartupAttribute(typeof(DotNetThemeMVC.Startup))]
namespace DotNetThemeMVC
{
    public partial class Startup
    {
        internal static IDataProtectionProvider DataProtectionProvider { get; private set; }

        public void Configuration(IAppBuilder app)
        {
            DataProtectionProvider = app.GetDataProtectionProvider();
            ConfigureAuth(app);
            
        }
    }
}
