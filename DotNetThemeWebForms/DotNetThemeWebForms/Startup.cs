using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DotNetTheme.Startup))]
namespace DotNetTheme
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
