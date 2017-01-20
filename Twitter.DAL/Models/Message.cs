using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twitter.Models;

namespace Twitter.DAL.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public virtual ApplicationUser User { get; set; }
        public DateTime DataCreated { get; set; }
    }
}
