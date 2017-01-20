using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Twitter.DAL.Models;
using Twitter.DAL.Repository;
using Twitter.Models;
using Twitter.Services.Models;

namespace Twitter.Controllers
{
    public class MessageController:ApiController
    {
        private IRepository<Message> _messageRepository;
        private IRepository<ApplicationUser> _userRepository;
        private ApplicationUser _user;

        private ApplicationUser AppUser
        {
            get
            {
                var tuser= _user == null ? _userRepository.Get(c => c.UserName == User.Identity.Name) : _user;
                return tuser;
            }
        }

        public MessageController(IRepository<Message> messageRepository, IRepository<ApplicationUser> userRepository)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        public MessageController() { }

        [HttpPut]
       // [Authorize(Roles ="User")]
        [Route("api/Messages/AddMessage")]
        public IHttpActionResult CreateNewMessage(Message item)
        {
            IHttpActionResult result = Ok();

            if (item != null)
            {
                Message message = new Message()
                {
                    Text = item.Text,
                    User=AppUser,
                    DataCreated=DateTime.Now
                };

                _messageRepository.Create(message);
                _messageRepository.Save();
            }
            else
            {
                result = BadRequest("Переданої моделі не існує");
            }
            
            return result;
        }

        [Route("api/messages/allmessages")]
        [ActionName("GetAll")]
        public IEnumerable<Message> GetAllMessages()
        {
            return _messageRepository.GetAll();
        }

        [HttpGet]
        [Route("api/messages/getmessage")]
        [ActionName("GetById")]
        public Message GetMessageById(int id)
        {
            return _messageRepository.GetById(id);
        }

        [HttpDelete]
        [Route("api/message/{is}")]
        [AcceptVerbs("POST")]
        public void DeleteMessageById(int id)
        {
            _messageRepository.DeleteById(id);
        }

        [HttpGet]
        [Route("api/messages/updates")]
        public void UpdateMessage([FromBody] Message item)
        {
            if (item != null)
            {
                _messageRepository.Update(item);
                _messageRepository.Save();
            }
        }
    }
}