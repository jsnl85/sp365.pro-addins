using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using SP365.AddIn.Services.DataAccess;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace SP365.AddIn.Services
{
    public class EmailService : IIdentityMessageService
    {
        private NotificationSettings _notificationSettings = null;
        protected NotificationSettings NotificationSettings { get { if (_notificationSettings == null) { _notificationSettings = new NotificationSettings(); } return _notificationSettings; } }

        public Task SendAsync(IdentityMessage message)
        {
            Task ret = null;
            // 
            ret = sendEmailAsync(message);
#if DEBUG
            if (ret == null) { ret = saveLocalEmailAsync(message); }
#endif
            if (ret == null) { ret = Task.FromResult(0); }
            // 
            return ret;
        }

        private Task sendEmailAsync(IdentityMessage message)
        {
            Task ret = null;
            // 
            if (string.IsNullOrEmpty(this.NotificationSettings.Smtp_Host) == false && string.IsNullOrEmpty(this.NotificationSettings.Smtp_From) == false)
            {
                try
                {
                    string templateBodyHtml = this.NotificationSettings.EmailTemplate_Default_Body;
                    Dictionary<string, string> tokensToReplace = new Dictionary<string, string>() { { "{subject}", message.Subject }, { "{body}", message.Body } };
                    string subject = message.Subject;
                    string bodyHtml = replaceTokens(templateBodyHtml, tokensToReplace);
                    MailMessage emailMessage = new MailMessage(this.NotificationSettings.Smtp_From, message.Destination, subject, bodyHtml);
                    emailMessage.IsBodyHtml = true;
                    // 
                    SmtpClient client = new SmtpClient(this.NotificationSettings.Smtp_Host, this.NotificationSettings.Smtp_Port);
                    client.SendCompleted += delegate { client.Dispose(); emailMessage.Dispose(); };
                    ret = client.SendMailAsync(emailMessage);
                }
                catch (Exception) { } //{ Logger.Error(ex, "Could not send the Confirmation email for some reason..."); }
            }
            // 
            return ret;
        }
#if DEBUG
        private Task saveLocalEmailAsync(IdentityMessage message)
        {
            Task ret = null;
            // 
            //if (string.IsNullOrEmpty(this.NotificationSettings.Smtp_Host) == false && string.IsNullOrEmpty(this.NotificationSettings.Smtp_From) == false)
            {
                try
                {
                    string templateBodyHtml = this.NotificationSettings.EmailTemplate_Default_Body;
                    Dictionary<string, string> tokensToReplace = new Dictionary<string, string>() { { "{subject}", message.Subject }, { "{body}", message.Body } };
                    string subject = message.Subject;
                    string bodyHtml = replaceTokens(templateBodyHtml, tokensToReplace);
                    // 
                    DirectoryInfo folder = new DirectoryInfo(HostingEnvironment.MapPath("/Emails")); // look for the local /Emails folder
                    if (folder.Exists == false) { folder.Create(); }
                    // 
                    FileInfo newFile = new FileInfo(string.Format(@"{0}\[{1:yyyy.MM.dd_HH.mm.ss}] {2} ({3}).eml.html", folder.FullName, DateTime.UtcNow, Regex.Replace(message.Subject, "[^a-zA-Z0-9 -]", string.Empty), Guid.NewGuid()));
                    using (StreamWriter sw = newFile.CreateText())
                    {
                        sw.Write(bodyHtml);
                    }
                    // 
                    ret = Task.FromResult(0);
                }
                catch (Exception) { } //{ Logger.Error(ex, "Could not save the email locally in the Emails folder for some reason..."); }
            }
            // 
            return ret;
        }
#endif
        private static string replaceTokens(string content, Dictionary<string, string> tokensToReplace)
        {
            string ret = content;
            // 
            if (string.IsNullOrEmpty(ret) == false && tokensToReplace != null && tokensToReplace.Any() == true)
            {
                foreach (KeyValuePair<string, string> kvp in tokensToReplace)
                {
                    Regex rx = new Regex(Regex.Escape(kvp.Key), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    if (rx.IsMatch(ret) == true)
                    {
                        ret = rx.Replace(ret, kvp.Value);
                    }
                }
            }
            // 
            return ret;
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store) : base(store) { }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            AuthSettings settings = new AuthSettings();
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));

            if (settings.EnforceUserValidationQ != false)
            {
                // Configure validation logic for usernames
                manager.UserValidator = new UserValidator<ApplicationUser>(manager)
                {
                    AllowOnlyAlphanumericUserNames = false,
                    RequireUniqueEmail = true
                };
            }
            else { } // manager.UserValidator = new UserValidator();

            if (settings.EnforcePasswordValidationQ != false)
            {
                // Configure validation logic for passwords
                manager.PasswordValidator = new PasswordValidator
                {
                    RequiredLength = 6,
                    RequireNonLetterOrDigit = true,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireUppercase = true,
                };
            }
            else { } // manager.PasswordValidator = new PasswordValidator();

            if ((settings.EnableTwoFactorAuthenticationQ ?? false) == true)
            {
                // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
                // You can write your own provider and plug it in here.
                manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
                {
                    MessageFormat = "Your security code is {0}"
                });
                manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
                {
                    Subject = "Security Code",
                    BodyFormat = "Your security code is {0}"
                });
            }
            else { }

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = (settings.EnableUserLockoutQ ?? true);
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager) :
            base(userManager, authenticationManager) { }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
}
