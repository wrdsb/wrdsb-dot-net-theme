using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DotNetThemeMVC.Startup))]
namespace DotNetThemeMVC
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
