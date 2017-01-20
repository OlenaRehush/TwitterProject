using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twitter.DAL.Models;
using Twitter.Models;

namespace Twitter.DAL
{
    public class TwitterContext:IdentityDbContext<ApplicationUser, CustomRole, int, CustomUserLogin, CustomUserRole, CustomUserClaim>,ITwitterContext
    {
        private static TwitterContext _twitterContext=null;

        public TwitterContext() : base("DefaultConnection")
        {
            Database.SetInitializer<TwitterContext>(new CreateDatabaseIfNotExists<TwitterContext>());
        }

        public DbSet<Message> Messages { get; set; }

        public static TwitterContext Create()
        {
            if (_twitterContext == null)
            {
                _twitterContext = new TwitterContext();
            }
            return _twitterContext;
        }

        

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>().HasKey(x => x.Id);
            modelBuilder.Entity<CustomUserRole>().HasKey(x => new { x.UserId, x.RoleId });
            modelBuilder.Entity<CustomRole>().HasKey(x => x.Id);
            modelBuilder.Entity<CustomUserClaim>().HasKey(x => x.UserId);
            modelBuilder.Entity<CustomUserLogin>().HasKey(x => x.UserId);
            modelBuilder.Entity<ApplicationUser>().HasKey(x => x.Id);

            modelBuilder.Entity<Message>()
                .Property(x => x.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany<Message>(x => x.Messages)
                .WithRequired(x => x.User);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(x => x.Followers)
                .WithMany(c=>c.Subscribers).Map(x =>
                {
                    x.MapLeftKey("UserId");
                    x.MapRightKey("FollowerId");
                    x.ToTable("UsersFollowers");
                }

            );
        }
    }
}
