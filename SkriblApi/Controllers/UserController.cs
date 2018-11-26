using DAL;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using Nexmo.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using BasketApi.CustomAuthorization;
using BasketApi.Models;
using BasketApi.ViewModels;
using System.IO;
using System.Configuration;
using System.Data.Entity;
using System.Net.Mail;
using static BasketApi.Global;

namespace BasketApi.Controllers
{
    [RoutePrefix("api/User")]
    public class UserController : ApiController
    {
        private ApplicationUserManager _userManager;

        [Route("all")]
        public IHttpActionResult Getall()
        {
            try
            {
                //var nexmoVerifyResponse = NumberVerify.Verify(new NumberVerify.VerifyRequest { brand = "INGIC", number = "+923325345126" });
                //var nexmoCheckResponse = NumberVerify.Check(new NumberVerify.CheckRequest { request_id = nexmoVerifyResponse.request_id, code = "6310"});
                return Ok("Hello");
            }
            catch (Exception ex)
            {
                return Ok("Hello");
            }
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Route("Login")]
        [HttpPost]
        public async Task<IHttpActionResult> Login(LoginBindingModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                using (SkriblContext ctx = new SkriblContext())
                {
                    DAL.User userModel;

                    userModel = ctx.Users.FirstOrDefault(x => x.Email == model.Email && x.Password == model.Password);

                    if (userModel != null)
                    {
                        await userModel.GenerateToken(Request);

                        return Ok(new CustomResponse<User> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = userModel });
                    }


                    return Content(HttpStatusCode.OK, new CustomResponse<Error>
                    {
                        Message = "Forbidden",
                        StatusCode = (int)HttpStatusCode.Forbidden,
                        Result = new Error { ErrorMessage = "Invalid email or password." }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        /// <summary>
        /// Login for web admin panel
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Route("WebPanelLogin")]
        [HttpPost]
        public async Task<IHttpActionResult> WebPanelLogin(WebLoginBindingModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                using (SkriblContext ctx = new SkriblContext())
                {
                    DAL.Admin adminModel;

                    adminModel = ctx.Admins.FirstOrDefault(x => x.Email == model.Email && x.Password == model.Password && x.IsDeleted == false);

                    if (adminModel != null)
                    {
                        await adminModel.GenerateToken(Request);
                        CustomResponse<Admin> response = new CustomResponse<Admin> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = adminModel };
                        return Ok(response);
                    }
                    else
                        return Content(HttpStatusCode.OK, new CustomResponse<Error>
                        {
                            Message = "Forbidden",
                            StatusCode = (int)HttpStatusCode.Forbidden,
                            Result = new Error { ErrorMessage = "Invalid Email or Password" }
                        });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        private int SavePicture(HttpRequestMessage request, out string PicturePath)
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                PicturePath = String.Empty;

                if (httpRequest.Files.Count > 1)
                    return 3;

                foreach (string file in httpRequest.Files)
                {
                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created);

                    var postedFile = httpRequest.Files[file];
                    if (postedFile != null && postedFile.ContentLength > 0)
                    {
                        int MaxContentLength = 1024 * 1024 * 1; //Size = 1 MB  

                        IList<string> AllowedFileExtensions = new List<string> { ".jpg", ".gif", ".png" };
                        var ext = Path.GetExtension(postedFile.FileName);
                        var extension = ext.ToLower();
                        if (!AllowedFileExtensions.Contains(extension))
                        {
                            var message = string.Format("Please Upload image of type .jpg,.gif,.png.");
                            return 1;
                        }
                        else if (postedFile.ContentLength > MaxContentLength)
                        {

                            var message = string.Format("Please Upload a file upto 1 mb.");
                            return 2;
                        }
                        else
                        {
                            int count = 1;
                            string fileNameOnly = Path.GetFileNameWithoutExtension(postedFile.FileName);
                            string newFullPath = HttpContext.Current.Server.MapPath("~/App_Data/" + postedFile.FileName);

                            while (File.Exists(newFullPath))
                            {
                                string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                                newFullPath = HttpContext.Current.Server.MapPath("~/App_Data/" + tempFileName + extension);
                            }

                            postedFile.SaveAs(newFullPath);
                            PicturePath = newFullPath;
                        }
                    }
                }
                return 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Logout
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            HttpContext.Current.GetOwinContext().Authentication.SignOut(OAuthDefaults.AuthenticationType);
            return Ok();
        }

        [Authorize]
        [Route("MarkVerified")]
        [HttpPost]
        public IHttpActionResult MarkUserAccountAsVerified(UserModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                using (SkriblContext ctx = new SkriblContext())
                {
                    var userModel = ctx.Users.FirstOrDefault(x => x.Email == model.Email);
                    if (userModel == null)
                        return BadRequest("User account doesn't exist");

                    userModel.Status = (int)Global.StatusCode.Verified;
                    ctx.SaveChanges();
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }


        /// <summary>
        /// Verify code sent to user. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [Route("VerifySmsCode")]
        public IHttpActionResult VerifySmsCode(PhoneVerificationModel model)
        {
            try
            {
                var userEmail = User.Identity.Name;

                if (string.IsNullOrEmpty(userEmail))
                    throw new Exception("User Email is empty in user.identity.name.");
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var nexmoCheckResponse = NumberVerify.Check(new NumberVerify.CheckRequest { request_id = model.request_id, code = model.Code });

                if (nexmoCheckResponse.status == "0")
                {
                    //using (BasketContext ctx = new BasketContext())
                    //{
                    //    ctx.Users.FirstOrDefault(x => x.Email == userEmail).Status = (int)Global.StatusCode.Verified;
                    //    ctx.SaveChanges();
                    //}
                    return Content(HttpStatusCode.OK, new MessageViewModel { Details = "Account Verified Successfully." });
                }
                else
                    return Content(HttpStatusCode.OK, new CustomResponse<NumberVerify.CheckResponse> { Message = "InternalServerError", StatusCode = (int)HttpStatusCode.InternalServerError, Result = nexmoCheckResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }


        [Route("Register")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Register()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                string newFullPath = string.Empty;
                string fileNameOnly = string.Empty;

                RegisterCustomerBindingModel model = new RegisterCustomerBindingModel();

                model.FirstName = httpRequest.Params["FirstName"];
                model.LastName = httpRequest.Params["LastName"];
                model.Email = httpRequest.Params["Email"];
                model.ConfirmPassword = httpRequest.Params["ConfirmPassword"];
                model.Password = httpRequest.Params["Password"];
                model.Nationality= httpRequest.Params["Nationality"];


                Validate(model);

                #region Validations
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (!Request.Content.IsMimeMultipartContent())
                {
                    return Content(HttpStatusCode.OK, new CustomResponse<Error>
                    {
                        Message = "UnsupportedMediaType",
                        StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                        Result = new Error { ErrorMessage = "Multipart data is not included in request." }
                    });
                }
                else if (httpRequest.Files.Count > 1)
                {
                    return Content(HttpStatusCode.OK, new CustomResponse<Error>
                    {
                        Message = "UnsupportedMediaType",
                        StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                        Result = new Error { ErrorMessage = "Multiple images are not supported, please upload one image." }
                    });
                }
                #endregion

                using (SkriblContext ctx = new SkriblContext())
                {
                    if (ctx.Users.Any(x => x.Email == model.Email))
                    {
                        return Content(HttpStatusCode.OK, new CustomResponse<Error>
                        {
                            Message = "Conflict",
                            StatusCode = (int)HttpStatusCode.Conflict,
                            Result = new Error { ErrorMessage = "User with email already exists." }
                        });
                    }


                    HttpPostedFile postedFile = null;
                    string fileExtension = string.Empty;

                    #region ImageSaving
                    if (httpRequest.Files.Count > 0)
                    {
                        postedFile = httpRequest.Files[0];
                        if (postedFile != null && postedFile.ContentLength > 0)
                        {
                            //int MaxContentLength = 1024 * 1024 * 10; //Size = 1 MB  

                            IList<string> AllowedFileExtensions = new List<string> { ".jpg", ".gif", ".png" };
                            //var ext = postedFile.FileName.Substring(postedFile.FileName.LastIndexOf('.'));
                            var ext = Path.GetExtension(postedFile.FileName);
                            fileExtension = ext.ToLower();
                            if (!AllowedFileExtensions.Contains(fileExtension))
                            {
                                return Content(HttpStatusCode.OK, new CustomResponse<Error>
                                {
                                    Message = "UnsupportedMediaType",
                                    StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                                    Result = new Error { ErrorMessage = "Please Upload image of type .jpg,.gif,.png." }
                                });
                            }
                            else if (postedFile.ContentLength > Global.MaximumImageSize)
                            {
                                return Content(HttpStatusCode.OK, new CustomResponse<Error>
                                {
                                    Message = "UnsupportedMediaType",
                                    StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                                    Result = new Error { ErrorMessage = "Please Upload a file upto " + Global.ImageSize + "." }
                                });
                            }
                            else
                            {
                            }
                        }
                    }

                    #endregion

                    User userModel;

                    userModel = new User
                    {
                        Email = model.Email,
                        Password = model.Password,
                        FirstName= model.FirstName,
                        LastName= model.LastName,
                        Status = (int)Global.StatusCode.NotVerified,
                        SignInType = 0,
                        Nationality = model.Nationality,
                        IsNotificationsOn = true,
                        IsDeleted=false
                    };

                    ctx.Users.Add(userModel);
                    ctx.SaveChanges();
                    if (httpRequest.Files.Count > 0)
                    {
                        newFullPath = HttpContext.Current.Server.MapPath("~/" + ConfigurationManager.AppSettings["UserImageFolderPath"] + userModel.Id + fileExtension);
                        postedFile.SaveAs(newFullPath);
                        userModel.ProfilePictureUrl = ConfigurationManager.AppSettings["UserImageFolderPath"] + userModel.Id + fileExtension;
                        ctx.SaveChanges();
                    }

                    await userModel.GenerateToken(Request);
                    CustomResponse<User> response = new CustomResponse<User> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = userModel };
                    return Ok(response);

                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        [Route("UploadUserImage")]
        [Authorize]
        public async Task<IHttpActionResult> UploadUserImage()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            try
            {
                var httpRequest = HttpContext.Current.Request;
                string newFullPath = string.Empty;
                string fileNameOnly = string.Empty;

                #region Validations
                var userEmail = User.Identity.Name;
                if (string.IsNullOrEmpty(userEmail))
                {
                    throw new Exception("User Email is empty in user.identity.name.");
                }
                else if (!Request.Content.IsMimeMultipartContent())
                {
                    return Content(HttpStatusCode.OK, new CustomResponse<Error>
                    {
                        Message = "UnsupportedMediaType",
                        StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                        Result = new Error { ErrorMessage = "Multipart data is not included in request." }
                    });
                }
                else if (httpRequest.Files.Count == 0)
                {
                    return Content(HttpStatusCode.OK, new CustomResponse<Error>
                    {
                        Message = "NotFound",
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Result = new Error { ErrorMessage = "Image not found, please upload an image." }
                    });
                }
                else if (httpRequest.Files.Count > 1)
                {
                    return Content(HttpStatusCode.OK, new CustomResponse<Error>
                    {
                        Message = "UnsupportedMediaType",
                        StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                        Result = new Error { ErrorMessage = "Multiple images are not allowed. Please upload 1 image." }
                    });
                }
                #endregion

                var postedFile = httpRequest.Files[0];

                if (postedFile != null && postedFile.ContentLength > 0)
                {

                    int MaxContentLength = 1024 * 1024 * 10; //Size = 1 MB  

                    IList<string> AllowedFileExtensions = new List<string> { ".jpg", ".gif", ".png" };
                    var ext = postedFile.FileName.Substring(postedFile.FileName.LastIndexOf('.'));
                    var extension = ext.ToLower();
                    if (!AllowedFileExtensions.Contains(extension))
                    {
                        return Content(HttpStatusCode.OK, new CustomResponse<Error>
                        {
                            Message = "UnsupportedMediaType",
                            StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                            Result = new Error { ErrorMessage = "Please Upload image of type .jpg, .gif, .png." }
                        });
                    }
                    else if (postedFile.ContentLength > MaxContentLength)
                    {

                        return Content(HttpStatusCode.OK, new CustomResponse<Error>
                        {
                            Message = "UnsupportedMediaType",
                            StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                            Result = new Error { ErrorMessage = "Please Upload a file upto 1 mb." }
                        });
                    }
                    else
                    {
                        int count = 1;
                        fileNameOnly = Path.GetFileNameWithoutExtension(postedFile.FileName);
                        newFullPath = HttpContext.Current.Server.MapPath("~/" + ConfigurationManager.AppSettings["UserImageFolderPath"] + postedFile.FileName);

                        while (File.Exists(newFullPath))
                        {
                            string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                            newFullPath = HttpContext.Current.Server.MapPath("~/" + ConfigurationManager.AppSettings["UserImageFolderPath"] + tempFileName + extension);
                        }
                        postedFile.SaveAs(newFullPath);
                    }
                }

                MessageViewModel successResponse = new MessageViewModel { StatusCode = "200 OK", Details = "Image Updated Successfully." };
                var filePath = Utility.BaseUrl + ConfigurationManager.AppSettings["UserImageFolderPath"] + Path.GetFileName(newFullPath);
                ImagePathViewModel model = new ImagePathViewModel { Path = filePath };

                using (SkriblContext ctx = new SkriblContext())
                {
                    ctx.Users.FirstOrDefault(x => x.Email == userEmail).ProfilePictureUrl = filePath;
                    ctx.SaveChanges();
                }

                return Content(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(SetPasswordBindingModel model)
        {
            try
            {
                var userEmail = User.Identity.Name;
                if (string.IsNullOrEmpty(userEmail))
                {
                    throw new Exception("User Email is empty in user.identity.name.");
                }
                else if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                using (SkriblContext ctx = new SkriblContext())
                {
                    if (model.SignInType == (int)RoleTypes.User)
                    {
                        var user = ctx.Users.FirstOrDefault(x => x.Email == userEmail && x.Password == model.OldPassword);
                        if (user != null)
                        {
                            user.Password = model.NewPassword;
                            ctx.SaveChanges();
                            return Ok(new CustomResponse<string> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK });
                        }
                        else
                            return Ok(new CustomResponse<Error> { Message = "Forbidden", StatusCode = (int)HttpStatusCode.Forbidden, Result = new Error { ErrorMessage = "Invalid old password." } });

                    }
                    else if (model.SignInType == (int)RoleTypes.Deliverer)
                    {
                        var deliveryMan = ctx.DeliveryMen.FirstOrDefault(x => x.Email == userEmail && x.Password == model.OldPassword);
                        if (deliveryMan != null)
                        {
                            deliveryMan.Password = model.NewPassword;
                            ctx.SaveChanges();
                            return Ok(new CustomResponse<string> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK });
                        }
                        else
                            return Ok(new CustomResponse<Error> { Message = "Forbidden", StatusCode = (int)HttpStatusCode.Forbidden, Result = new Error { ErrorMessage = "Invalid old password" } });
                    }
                    else
                        return Ok(new CustomResponse<Error> { Message = Global.ResponseMessages.BadRequest, StatusCode = (int)HttpStatusCode.BadRequest, Result = new Error { ErrorMessage = Global.ResponseMessages.GenerateInvalid("SignInType") } });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

                AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

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

                IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

                if (!result.Succeeded)
                {
                    //return GetErrorResult(result);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }


        /// <summary>
        /// Update user profile with image. This is multipart request. SignInType 0 for user, 1 for deliverer
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [Route("UpdateUserProfile")]
        public async Task<IHttpActionResult> UpdateUserProfileWithImage()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                string newFullPath = string.Empty;
                string fileNameOnly = string.Empty;

                #region InitializingModel
                EditUserProfileBindingModel model = new EditUserProfileBindingModel();
                model.Name = httpRequest.Params["name"];
                model.StreetAddress = httpRequest.Params["streetaddress"];
                model.Area = httpRequest.Params["area"];
                model.City = httpRequest.Params["city"];
                model.InstagramUrl = httpRequest.Params["InstagramUrl"];
                model.PhoneNumber = httpRequest.Params["phonenumber"];

                if (httpRequest.Params["ID"] != null)
                    model.ID = Convert.ToInt32(httpRequest.Params["ID"]);

                #endregion
                Validate(model);

                #region Validations
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (!Request.Content.IsMimeMultipartContent())
                {
                    return Content(HttpStatusCode.OK, new CustomResponse<Error>
                    {
                        Message = "UnsupportedMediaType",
                        StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                        Result = new Error { ErrorMessage = "Multipart data is not included in request." }
                    });
                }
                else if (httpRequest.Files.Count > 1)
                {
                    return Content(HttpStatusCode.OK, new CustomResponse<Error>
                    {
                        Message = "UnsupportedMediaType",
                        StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                        Result = new Error { ErrorMessage = "Multiple images are not supported, please upload one image." }
                    });
                }
                #endregion

                using (SkriblContext ctx = new SkriblContext())
                {
                    User userModel;
                    DeliveryMan delivererModel;

                    HttpPostedFile postedFile = null;
                    string fileExtension = string.Empty;

                    #region ImageSaving
                    if (httpRequest.Files.Count > 0)
                    {
                        postedFile = httpRequest.Files[0];
                        if (postedFile != null && postedFile.ContentLength > 0)
                        {

                            IList<string> AllowedFileExtensions = new List<string> { ".jpg", ".gif", ".png" };
                            //var ext = postedFile.FileName.Substring(postedFile.FileName.LastIndexOf('.'));
                            var ext = Path.GetExtension(postedFile.FileName);
                            fileExtension = ext.ToLower();
                            if (!AllowedFileExtensions.Contains(fileExtension))
                            {
                                return Content(HttpStatusCode.OK, new CustomResponse<Error>
                                {
                                    Message = "UnsupportedMediaType",
                                    StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                                    Result = new Error { ErrorMessage = "Please Upload image of type .jpg,.gif,.png." }
                                });
                            }
                            else if (postedFile.ContentLength > Global.MaximumImageSize)
                            {
                                return Content(HttpStatusCode.OK, new CustomResponse<Error>
                                {
                                    Message = "UnsupportedMediaType",
                                    StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                                    Result = new Error { ErrorMessage = "Please Upload a file upto " + Global.ImageSize + "." }
                                });
                            }
                            else
                            {
                                //int count = 1;
                                //fileNameOnly = Path.GetFileNameWithoutExtension(postedFile.FileName);
                                //newFullPath = HttpContext.Current.Server.MapPath("~/" + ConfigurationManager.AppSettings["UserImageFolderPath"] + postedFile.FileName);

                                //while (File.Exists(newFullPath))
                                //{
                                //    string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                                //    newFullPath = HttpContext.Current.Server.MapPath("~/" + ConfigurationManager.AppSettings["UserImageFolderPath"] + tempFileName + fileExtension);
                                //}

                                //postedFile.SaveAs(newFullPath);
                                //model.ProfilePictureUrl = Utility.BaseUrl + ConfigurationManager.AppSettings["UserImageFolderPath"] + Path.GetFileName(newFullPath);
                            }
                        }
                    }
                    #endregion

                    userModel = ctx.Users.Include(x => x.UserAddresses).Include(x => x.PaymentCards).FirstOrDefault(x => x.Id == model.ID);

                    if (userModel == null)
                    {
                        return Content(HttpStatusCode.OK, new CustomResponse<Error>
                        {
                            Message = "NotFound",
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Result = new Error { ErrorMessage = "UserId does not exist." }
                        });
                    }
                    else
                    {
                        userModel.FullName = model.Name;
                        userModel.Phone = model.PhoneNumber;
                        userModel.StreetAddress = model.StreetAddress;
                        userModel.City = model.City;
                        userModel.Area = model.Area;
                        userModel.InstagramUrl = model.InstagramUrl;

                        if (httpRequest.Files.Count > 0)
                        {
                            newFullPath = HttpContext.Current.Server.MapPath("~/" + ConfigurationManager.AppSettings["UserImageFolderPath"] + userModel.Id + fileExtension);
                            postedFile.SaveAs(newFullPath);
                            userModel.ProfilePictureUrl = ConfigurationManager.AppSettings["UserImageFolderPath"] + userModel.Id + fileExtension;
                        }

                        ctx.SaveChanges();
                        CustomResponse<User> response = new CustomResponse<User> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = userModel };
                        return Ok(response);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }





        /// <summary>
        /// An email will be sent to user containing password reset url. Which will redirect user to password reset page.
        /// </summary>
        /// <param name="Email">Email of user.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("ResetPasswordThroughEmail")]
        public async Task<IHttpActionResult> ResetPasswordThroughEmail(string Email)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {
                    var user = ctx.Users.Include(x => x.VerifyCodes).FirstOrDefault(x => x.Email == Email);

                    if (user != null)
                    {

                        Random _rdm = new Random();
                        string VerifyCode = _rdm.Next(1000, 9999).ToString();


                        foreach (var code in user.VerifyCodes)
                        {
                            code.IsDeleted = true;
                        }
                        ctx.SaveChanges();

                        ctx.VerifyCodes.Add(new VerificaionCodes
                        {
                            Code = VerifyCode,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false,
                            User_Id = user.Id
                        });
                        ctx.SaveChanges();

                        const string subject = "Reset your password - Basket App";
                        string body = "Use " + VerifyCode + " to reset your password.";

                        var smtp = new SmtpClient
                        {
                            Host = "smtp.gmail.com",
                            Port = 587,
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(EmailUtil.FromMailAddress.Address, EmailUtil.FromPassword)
                        };

                        var message = new MailMessage(EmailUtil.FromMailAddress, new MailAddress(Email))
                        {
                            Subject = subject,
                            Body = body
                        };

                        smtp.Send(message);
                        return Ok(new CustomResponse<string> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK });
                    }
                    else
                    {
                        return Ok(new CustomResponse<Error> { Message = "NotFound", StatusCode = (int)HttpStatusCode.NotFound, Result = new Error { ErrorMessage = "User with entered email doesn’t exist." } });
                    }

                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }


        [Authorize]
        [HttpPost]
        [Route("VerifyCodeOfEmail")]
        public IHttpActionResult VerifyCodeOfEmail(VerificationBindingModel model)
        {
            try
            {

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                using (SkriblContext ctx = new SkriblContext())
                {

                    var user = ctx.Users.FirstOrDefault(x => x.Email == model.Email);
                    if (user == null)
                    {
                        return Ok(new CustomResponse<Error> { Message = Global.ResponseMessages.NotFound, StatusCode = (int)HttpStatusCode.NotFound, Result = new Error { ErrorMessage = "Invalid email address." } });
                    }
                    else
                    {
                        var code = ctx.VerifyCodes.FirstOrDefault(x => x.Code == model.Code && x.User_Id == user.Id && !x.IsDeleted);
                        if (code != null)
                        {
                            if (DateTime.Now > code.CreatedDate.AddMinutes(2))
                            {
                                return Ok(new CustomResponse<Error> { Message = Global.ResponseMessages.Conflict, StatusCode = (int)HttpStatusCode.Conflict, Result = new Error { ErrorMessage = "Entered code has been expired." } });
                                // expired
                            }
                            else
                            {
                                code.IsDeleted = true;
                                ctx.SaveChanges();
                                return Ok(new CustomResponse<User> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK ,Result=user});

                            }
                        }
                        else
                        {
                            return Ok(new CustomResponse<Error> { Message = Global.ResponseMessages.Conflict, StatusCode = (int)HttpStatusCode.Conflict, Result = new Error { ErrorMessage = "Invalid entered code." } });
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        /// <summary>
        /// Register for getting push notifications
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [Route("RegisterPushNotification")]
        public async Task<IHttpActionResult> RegisterPushNotification(RegisterPushNotificationBindingModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                using (SkriblContext ctx = new SkriblContext())
                {
                    var user = ctx.Users.Include(x => x.UserDevices).FirstOrDefault(x => x.Id == model.User_Id);
                    if (user != null)
                    {
                        var existingUserDevice = user.UserDevices.FirstOrDefault(x => x.UDID.Equals(model.UDID));
                        if (existingUserDevice == null)
                        {
                            //foreach (var userDevice in user.UserDevices)
                            //    userDevice.IsActive = false;

                            var userDeviceModel = new UserDevice
                            {
                                Platform = model.IsAndroidPlatform,
                                ApplicationType = model.IsPlayStore ? UserDevice.ApplicationTypes.PlayStore : UserDevice.ApplicationTypes.Enterprise,
                                EnvironmentType = model.IsProduction ? UserDevice.ApnsEnvironmentTypes.Production : UserDevice.ApnsEnvironmentTypes.Sandbox,
                                UDID = model.UDID,
                                AuthToken = model.AuthToken,
                                IsActive = true
                            };

                            //PushNotificationsUtil.ConfigurePushNotifications(userDeviceModel);

                            user.UserDevices.Add(userDeviceModel);
                            ctx.SaveChanges();
                            return Ok(new CustomResponse<UserDevice> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = userDeviceModel });
                        }
                        else
                        {
                            //foreach (var userDevice in user.UserDevices)
                            //    userDevice.IsActive = false;

                            existingUserDevice.Platform = model.IsAndroidPlatform;
                            existingUserDevice.ApplicationType = model.IsPlayStore ? UserDevice.ApplicationTypes.PlayStore : UserDevice.ApplicationTypes.Enterprise;
                            existingUserDevice.EnvironmentType = model.IsProduction ? UserDevice.ApnsEnvironmentTypes.Production : UserDevice.ApnsEnvironmentTypes.Sandbox;
                            existingUserDevice.UDID = model.UDID;
                            existingUserDevice.AuthToken = model.AuthToken;
                            existingUserDevice.IsActive = true;
                            ctx.SaveChanges();
                            return Ok(new CustomResponse<UserDevice> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = existingUserDevice });
                        }
                    }
                    else
                        return Ok(new CustomResponse<Error> { Message = Global.ResponseMessages.NotFound, StatusCode = (int)HttpStatusCode.NotFound, Result = new Error { ErrorMessage = Global.ResponseMessages.GenerateNotFound("User") } });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }


        [Authorize]
        [HttpGet]
        [Route("GetUser")]
        public async Task<IHttpActionResult> GetUser(int UserId)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {
                    var userModel = ctx.Users.Include(x => x.UserAddresses).Include(x => x.PaymentCards).FirstOrDefault(x => x.Id == UserId && x.IsDeleted == false);

                    if (userModel != null)
                    {
                        BasketSettings.LoadSettings();
                        await userModel.GenerateToken(Request);
                        //userModel.BasketSettings = new Settings { HowItWorksUrl = BasketSettings.HowItWorksUrl, HowItWorksDescription = BasketSettings.HowItWorksDescription, BannerImage = BasketSettings.BannerImageUrl, Id = BasketSettings.Id, Currency = BasketSettings.Currency, DeliveryFee = BasketSettings.DeliveryFee, FreeDeliveryThreshold = BasketSettings.FreeDeliveryThreshold, InstagramImage = BasketSettings.InstagramImage };
                        return Ok(new CustomResponse<User> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = userModel });
                    }
                    else
                        return Ok(new CustomResponse<Error> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = new Error { ErrorMessage = "Invalid UserId" } });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        [HttpGet]
        [Route("MarkDeviceAsInActive")]
        public async Task<IHttpActionResult> MarkDeviceAsInActive(int UserId, int DeviceId)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {
                    var device = ctx.UserDevices.FirstOrDefault(x => x.Id == DeviceId && x.User_Id == UserId);
                    if (device != null)
                    {
                        device.IsActive = false;
                        ctx.SaveChanges();
                        return Ok(new CustomResponse<string> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK });
                    }
                    else
                        return Ok(new CustomResponse<Error> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = new Error { ErrorMessage = "Invalid UserId or DeviceId." } });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
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

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

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
    }
}
