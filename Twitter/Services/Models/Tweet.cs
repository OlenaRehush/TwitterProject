using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Twitter.Services.Models
{
    public class Tweet
    {
        public int UserId { get; set; }
        public string TweetText { get; set; }
        public string UserName { get; set; }
        public DateTime DataCreated { get; set; }
    }
}