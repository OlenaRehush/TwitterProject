using Microsoft.Practices.Unity;
using System.Web.Http;
using Twitter.DAL;
using Twitter.DAL.Models;
using Twitter.DAL.Repository;
using Twitter.Models;
using Unity.WebApi;

namespace Twitter
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
			var container = new UnityContainer();

            // register all your components with the container here
            // it is NOT necessary to register your controllers


            container.RegisterInstance<TwitterContext>(TwitterContext.Create(),new PerResolveLifetimeManager());
            container.RegisterType<IRepository<ApplicationUser>, Repository<ApplicationUser>>();
            container.RegisterType<IRepository<Message>, Repository<Message>>();
            container.RegisterType<IRepository<CustomUserLogin>, Repository<CustomUserLogin>>();
            container.RegisterType<IRepository<CustomUserClaim>, Repository<CustomUserClaim>>();
            container.RegisterType<IRepository<CustomUserRole>, Repository<CustomUserRole>>();

            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}