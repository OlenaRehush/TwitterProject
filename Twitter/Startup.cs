using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;
using Owin;
using Twitter.DAL;
using Newtonsoft.Json;
using System.Web.Http;

[assembly: OwinStartup(typeof(Twitter.Startup))]

namespace Twitter
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            UnityConfig.RegisterComponents();
            ConfigureAuth(app);
            TwitterContext db = new TwitterContext();
            db.Database.CreateIfNotExists();
        }
    }
}
