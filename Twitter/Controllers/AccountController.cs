using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using System.Net.Http;
using System.Net;
using System.Globalization;
using Twitter;
using Twitter.DAL.Models;
using Twitter.Models;
using Twitter.DAL.Repository;
using Twitter.Results;
using Twitter.Providers;
using Twitter.Services.Models;

namespace Twitter.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;

        private IRepository<Message> _messageRepository;
        private IRepository<ApplicationUser> _userRepository;
        private IRepository<CustomUserLogin> _customUserLoginRepository;
        private IRepository<CustomUserClaim> _customUserClaimRepository;
        private IRepository<CustomUserRole> _customUserRoleRepository;

        private readonly ISecureDataFormat<AuthenticationTicket> _accessTokenFormat;

        public AccountController() { }
        
        public AccountController(
            IRepository<Message> _messageRepository,
            IRepository<CustomUserLogin> _customUserLoginRepository,
            IRepository<CustomUserClaim> _customUserClaimRepository,
            IRepository<CustomUserRole> _customUserRoleRepository,
            IRepository<ApplicationUser> _userRepository
            )
        {
            this._customUserLoginRepository = _customUserLoginRepository;
            this._customUserClaimRepository = _customUserClaimRepository;
            this._customUserRoleRepository = _customUserRoleRepository;
            this._userRepository = _userRepository;
            this._messageRepository = _messageRepository;
        }

        public AccountController(ApplicationUserManager userManager, ISecureDataFormat<AuthenticationTicket> accessTokenFormat)
        {
            UserManager = userManager;
            _accessTokenFormat = accessTokenFormat;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // Get api/Account/IsAuthorizen 
        [HttpGet]
        [Route("IsAuthorizen")]
        [AllowAnonymous]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        public bool IsAuthorizen ()
        {
            bool isAuthorizen = User.Identity.IsAuthenticated;

            return isAuthorizen;
        } 
        // GET api/Account/UserRoles
        [HttpGet]
        [Route("UserRoles")]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [AllowAnonymous]
        public async Task<IList<string>> GetUserRoleAsync()
        {
            IList<string> roles = new List<string>();
            if (User.Identity.IsAuthenticated)
            {
                var user = await UserManager.FindByEmailAsync(User.Identity.GetUserName());
                roles = await UserManager.GetRolesAsync(user.Id);
            }

            return roles;
        }

        [HttpGet]
        [Route("UserRolesNoAuthorizen")]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Authorize]
        public async Task<IList<string>> GetUserRoleAuthorizenAsync()
        {
            var user = await UserManager.FindByEmailAsync(User.Identity.GetUserName());
            var userRoles = await UserManager.GetRolesAsync(user.Id);
            return userRoles;
        }

       // [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        public User GetUserInfo()
        {
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            ApplicationUser user = _userRepository.Get( c => c.Email == User.Identity.Name);

            if (user == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            var tweets = user.Subscribers.Join(_messageRepository.GetAll(), u => u.Id, m => m.User.Id, (u, m) => { return new Tweet()
            {
                UserId=u.Id,
                TweetText=m.Text,
                DataCreated=m.DataCreated,
                UserName=u.FullName
            }; });
        //         Get(x=>user.Subscribers.Contains(x.User))

            User userInfo = new User
            {
                Id = user.Id,
                FullName = user.FullName,
                Description = user.Description,
                PhotoURL = user.PhotoURL,
                HasRegistered = externalLogin == null,
                LoginProvider = externalLogin != null ? externalLogin.LoginProvider : null,
                Messages = user.Messages.Select(x=>new Tweet { UserId=user.Id, DataCreated=x.DataCreated, TweetText=x.Text, UserName=user.FullName}).ToList(),
                HomeTown = user.Hometown,
                Followers = user.Followers.Select(x => new User { FullName = x.FullName, Id = x.Id, PhotoURL = x.PhotoURL }).ToList(),
                Subscriptions = user.Subscribers.Select(x => new User { FullName = x.FullName, Id = x.Id, PhotoURL = x.PhotoURL }).ToList(),
                SubscribersTweets=tweets.ToList(),
                UserName = user.UserName,
                CountOfFollowers = user.Followers.Count,
                CountOfMessages=user.Messages.Count
            };

            return userInfo;
        }

        // POST api/Account/Logout
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfoAsync(string returnUrl, bool generateState = false)
        {
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId<int>()); 

            if (user == null)
            {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (CustomUserLogin linkedAccount in user.Logins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/AddExternalLogin
        #region Forgot Password
        // POST: /Account/ForgotPassword
        [HttpPost]
        [Route("ForgotPassword")]
        [AllowAnonymous]
        // Except Confirm Email
        public async Task<IHttpActionResult> ForgotPasswordAsync(ForgotPasswordViewModel model)
        {
            IHttpActionResult result = Ok();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await UserManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return BadRequest("Користувач з таким адресом електронної пошти не зареєстрований!");
            }

            try
            {
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);

                string angularController = "Reset_Password";
                string resetPwdMethod = "ResetPassword";

                string urlStart = String.Format("http://" + Url.Request.Headers.Host + "/" + angularController);
                

                string callbackUrl = Url.Link(resetPwdMethod, new
                {
                    Id = user.Id,
                    email = user.Email,
                    code = code,
                 
                });

                callbackUrl = urlStart + callbackUrl.Remove(0, callbackUrl.IndexOf(resetPwdMethod) + resetPwdMethod.Length);

                string mail = 
                    "<h2>Соціальна карта відповідального міста</h2> <br/>" +
                    "<h3>Відновлення пароля</h3> <br/>" +
                    "<p>Доброго дня!</p>" +
                    "<p>Ви отримали лист з запитом на відновлення паролю, будь ласка встановіть новий пароль натиснувши на посилання нижче:</p>" +
                    "<a href=\"" +
                    callbackUrl +
                    "\">Відновити пароль</a>" +
                    "<p>Якщо у вас виникли якісь потреби чи пропозиції, будь ласка, пишіть нам на нашу електронну пошту charity@gmail.com</p> " +
                    "<br/><p>Дякую!</p><p>З повагою адміністрація Соціальної карти відповідального міста!</p> ";

                await UserManager.SendEmailAsync(user.Id, "Відновлення паролю", mail);

            }
            catch (Exception ex)
            {
                result = InternalServerError(ex);
            }

            return result;
        }

        #endregion

        #region Reset Password

        // POST: api/Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [Route("ResetPassword", Name = "ResetPassword")]
        public async Task<IHttpActionResult> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToRoute("Default", new { controller = "User" });
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return Ok();
            }
            return BadRequest("Зміна паролю неможлива, виконайте запит відновлення паролю ще раз");
        }

        #endregion

        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLoginAsync(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = _accessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("The external login is already associated with an account.");
            }

            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId<int>(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLoginAsync(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId<int>());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId<int>(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLoginAsync(string provider, string error = null)
        {
            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

                ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                   OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else
            {
                IEnumerable<Claim> claims = externalLogin.GetClaims();
                ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                Authentication.SignIn(identity);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        [Authorize]
        [Route("Update")]
        [HttpPut]
        public async Task<IHttpActionResult> UpdateAsync(UpdateBindingModel model)
        {
            ApplicationUser user = await UserManager.FindByEmailAsync(User.Identity.GetUserName());
            if (user == null)
            {
                return BadRequest("Користувач не зареєстрований");
            }
            user.FullName = model.FullName;
            user.Birthday = DateTime.ParseExact(model.Birthday + " 00:00:00", "dd/MM/yyyy HH:mm:ss", new CultureInfo("uk-UA"));
            user.PhoneNumber = model.PhoneNumber;
            user.PhotoURL = model.PhotoURL;
            user.Description = model.Description;
            user.Address = model.Address;
            user.WebSite = model.WebSite;

            _userRepository.Update(user);
            _userRepository.Save();

            return Ok();
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> RegisterAsync([FromBody]RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                IHttpActionResult result = Ok();

                var errorKeys = new List<string>();
                foreach (var key in ModelState.Keys)
                    errorKeys.Add(key);

                switch (errorKeys[0])
                {
                    case "model.Password":
                        {
                            result = BadRequest("Пароль повинен містити від 8 до 16 символів");
                            break;
                        }
                    case "model.ConfirmPassword" : 
                        {
                            result = BadRequest("Паролі не співпадають");
                            break;
                        }
                    case "model.Email":
                        {
                            result = BadRequest("Невірний формат електронної пошти");
                            break;
                        }
                    case "model.FullName":
                        {
                            result = BadRequest("Повне ім'я обов'язкове");
                            break;
                        }
                }

                return result;
            }

            model.RoleName = "User";

            var user = new ApplicationUser()
            {
                UserName = model.Email,
                FullName = model.FullName,
                Email = model.Email,
                Birthday = String.IsNullOrEmpty(model.Birthday) == true ? DateTime.Now : DateTime.Parse(model.Birthday),
                Gender = model.Gender,
                PhoneNumber = model.PhoneNumber,
                PhotoURL = model.PhotoURL,
                Description = model.Description,
                Address = model.Address,
                Rating = 0,
                EmailConfirmed=true
                
            };
                       
            ApplicationUser userInDB = await UserManager.FindAsync(user.UserName, model.Password);
            if (userInDB != null)
            {
                return BadRequest("Такий користувач вже зареєстрований");
            }

            IdentityResult resultUser = await UserManager.CreateAsync(user, model.Password);
            if (!resultUser.Succeeded)
            {
                return GetErrorResult(resultUser);
            }
            IdentityResult resultRole = await UserManager.AddToRoleAsync(user.Id, model.RoleName);
            if (!resultRole.Succeeded)
            {
                await UserManager.DeleteAsync(user);
                return GetErrorResult(resultRole);
            }


            string code = await this.UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
            try
            {
                string callbackUrl = Url.Link("DefaultApi", new { controller = "Account/ConfirmEmail", userId = user.Id, code = code });


                string mail =
                   "<h2>Соціальна карта відповідального міста</h2> <br/>" +
                   "<h3>Підтвердження реєстрації</h3> <br/>" +
                   "<p>Доброго дня!</p>" +
                   "<p>Ви отримали лист з запитом на підтвердження реєстрації, будь ласка перейдіть по посиланню нижче:</p>" +
                   "<a href=\"" +
                   callbackUrl +
                   "\">Підтвердити реєстрацію</a>" +
                   "<p>Якщо у вас виникли якісь потреби чи пропозиції, будь ласка, пишіть нам на нашу електронну пошту charity@gmail.com</p> " +
                   "<br/><p>Дякую!</p><p>З повагою адміністрація Соціальної карти відповідального міста!</p> ";


                await this.UserManager.SendEmailAsync(user.Id, "Підтвердження реєстрації", mail);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("ReconfirmEmail")]
        public async Task<IHttpActionResult> ReconfirmEmailAsync(string email)
        {
            ApplicationUser userInDB = await UserManager.FindByEmailAsync(email);
            if (userInDB == null)
            {
                return BadRequest("Такий користувач вже зареєстрований");
            }

            if (userInDB.EmailConfirmed == false)
            {
                string code = await this.UserManager.GenerateEmailConfirmationTokenAsync(userInDB.Id);
                try
                {
                    string callbackUrl = Url.Link("DefaultApi", new { controller = "Account/ConfirmEmail", userId = userInDB.Id, code = code });

                    string mail =
                       "<h2>Соціальна карта відповідального міста</h2> <br/>" +
                       "<h3>Підтвердження реєстрації</h3> <br/>" +
                       "<p>Доброго дня!</p>" +
                       "<p>Ви отримали лист з повторним запитом на підтвердження реєстрації, будь ласка перейдіть по посиланню нижче:</p>" +
                       "<a href=\"" +
                       callbackUrl +
                       "\">Підтвердити реєстрацію</a>" +
                       "<p>Якщо у вас виникли якісь потреби чи пропозиції, будь ласка, пишіть нам на нашу електронну пошту charity@gmail.com</p> " +
                       "<br/><p>Дякую!</p><p>З повагою адміністрація Соціальної карти відповідального міста!</p> ";
                    await this.UserManager.SendEmailAsync(userInDB.Id, "Підтвердження реєстрації", mail);
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }
            else
            {
                return BadRequest("Такий користувач вже підтвердив свою електронну пошту");
            }

            return Ok();
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [Route("ConfirmEmail")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> ConfirmEmailAsync(int userId, string code = "")
        {
            if (userId == default(int) || string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Ідентифікатор користувача або код, неправильні");
            }

            IdentityResult result = await this.UserManager.ConfirmEmailAsync(userId, code);

            if (result.Succeeded)
            {
                return Redirect("http://" + Url.Request.Headers.Host + "/login");
            }
            else
            {
                return GetErrorResult(result);
            }
        }

        // POST api/Account/RegisterExternal
        [AllowAnonymous]
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var info = await Authentication.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return InternalServerError();
            }

            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }
            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete]
        [Route("{id}")]
        [AcceptVerbs("GET", "POST")]
        public async Task<IHttpActionResult> DeleteAccountAsync(int id)
        {
            IHttpActionResult result = Ok();

            if (id <= 0)
            {
                var message = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Не правильний ідентифікатор користувача")
                };
                throw new HttpResponseException(message);
            }
            else
            {
                _messageRepository.Delete(x => x.User.Id == id);
                _customUserClaimRepository.Delete(x => x.UserId == id);
                _customUserLoginRepository.Delete(x => x.UserId == id);
                _customUserRoleRepository.Delete(x => x.UserId == id);
                ApplicationUser user = await UserManager.FindByIdAsync(id);
                await UserManager.DeleteAsync(user);
            }

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            // Закрив цей кусочок тому що акаунт не створювався через те що база даних передчасно Dispose

            //if (disposing && _userManager != null)
            //{
            //    _userManager.Dispose();
            //    _userManager = null;
            //}

           // base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }

}