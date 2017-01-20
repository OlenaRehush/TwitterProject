using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Twitter.DAL.Models;
using Twitter.Models;

namespace Twitter.Services.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string PhotoURL { get; set; }
        public int CountOfFollowers { get; set; }
        public int CountOfMessages { get; set; }
        public string UserName { get; set; }
        public string Description { get; set; }
        public string HomeTown { get; set; }
        public ICollection<Tweet> Messages { get; set; }
        public ICollection<User> Followers { get; set; }
        public ICollection<User> Subscriptions { get; set; }
        public ICollection<Tweet> SubscribersTweets { get; set; }
        public bool HasRegistered { get; set; }
        public string LoginProvider { get; set; }
        public bool IsSubscribed { get; set; }
    }
}