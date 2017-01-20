using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Twitter.DAL.Models;
using Twitter.DAL.Repository;
using Twitter.Models;
using Twitter.Services.Models;

namespace Twitter.Controllers
{
    public class UserController : ApiController
    {
        private IRepository<Message> _messageRepository;
        private IRepository<Twitter.Models.ApplicationUser> _userRepository;

        public UserController(IRepository<Message> messageRepository, IRepository<Twitter.Models.ApplicationUser> userRepository)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        public UserController() { }

        [Route("api/user/addSubscriber")]
        [HttpPost]
        public IHttpActionResult AddSubscription([FromUri]int subscriberId)
        {
            IHttpActionResult result = Ok();
            ApplicationUser user = _userRepository.Get(c => c.Email == User.Identity.Name);

            var sub = _userRepository.GetById(subscriberId);
            if (sub != null)
            {
                user.Subscribers.Add(sub);
                _userRepository.Save();
            }
            return result;
        }

        [HttpPost]
        [Route("api/user/deleteSubscriber")]
        public IHttpActionResult DeleteSubscription([FromUri]int subscriberId)
        {
            IHttpActionResult result = Ok();
            ApplicationUser user = _userRepository.Get(c => c.Email == User.Identity.Name);

            var sub = _userRepository.GetById(subscriberId);
            if (sub != null)
            {
                var check = user.Subscribers.Where(x => x.Id == sub.Id) == null ? null : user.Subscribers.Where(x => x.Id == sub.Id).First();
                if (check != null)
                {
                    user.Subscribers.Remove(sub);
                    _userRepository.Save();
                }
            }
            return result;
        }

        [Route("api/users/allusers")]
        [ActionName("GetAll")]
        [AllowAnonymous]
        public IEnumerable<User> GetAllUsers()
        {
            var users = _userRepository.GetAll();
            var usersInfo = users.ToList().Select(x => new User()
            {
                Id=x.Id,
                FullName=x.FullName,
                UserName=x.UserName,
                Messages= x.Messages.Select(c => new Tweet { UserId = x.Id, DataCreated = c.DataCreated, TweetText = c.Text, UserName = x.FullName }).ToList(),
                Followers=x.Followers.Select(c=>new User() { FullName=c.FullName, Id=c.Id, PhotoURL=c.PhotoURL}).ToList(),
                Subscriptions = x.Subscribers.Select(c => new User { FullName = c.FullName, Id = c.Id, PhotoURL = c.PhotoURL }).ToList(),
                Description =x.Description,
                CountOfFollowers=x.Followers.Count,
                CountOfMessages=x.Messages.Count,
                HomeTown=x.Hometown,
                PhotoURL=x.PhotoURL
            }).ToList();

            return usersInfo;
        }

        [HttpGet]
        [Route("api/users/getuser")]
        [ActionName("GetById")]
        public User GetUserById(int id)
        {
            var user = _userRepository.GetById(id);
            if (user != null)
            {
                var userInfo = new User()
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName,
                    Messages = user.Messages.Select(x => new Tweet { UserId = user.Id, DataCreated = x.DataCreated, TweetText = x.Text, UserName = user.FullName }).ToList(),
                    Followers = user.Followers.Select(c => new User() { FullName = c.FullName, Id = c.Id, PhotoURL = c.PhotoURL }).ToList(),
                    Subscriptions = user.Subscribers.Select(x => new User { FullName = x.FullName, Id = x.Id, PhotoURL = x.PhotoURL }).ToList(),
                    Description = user.Description,
                    CountOfFollowers = user.Followers.Count,
                    CountOfMessages = user.Messages.Count,
                    HomeTown = user.Hometown,
                    PhotoURL = user.PhotoURL,
                    IsSubscribed=user.Followers.Where(x=>x.UserName==User.Identity.Name).Count()>0
                };

                return userInfo;
            }
            return null;
        }

        public void UpdateUser([FromBody] Twitter.Models.ApplicationUser item)
        {
            if (item != null)
            {
                _userRepository.Update(item);
                _userRepository.Save();
            }
        }
    }
}
