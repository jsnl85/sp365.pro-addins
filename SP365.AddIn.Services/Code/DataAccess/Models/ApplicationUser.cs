using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Newtonsoft.Json;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SP365.AddIn.Services.DataAccess.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Column("FirstName")]           public string FirstName { get; set; }
        [Column("LastName")]            public string LastName { get; set; }
        [Column("AvatarUrl")]           public string AvatarUrl { get; set; }
        [Column("Company")]             public string Company { get; set; }
        [Column("CreatedDate")]         public DateTime? CreatedDate { get; set; }
        [Column("LastLogonDate")]       public DateTime? LastLogonDate { get; set; }
        // 
        /// linked entities
        [ForeignKey("ApplicationUserId"), JsonIgnore]public virtual ICollection<ApplicationUserToken> Tokens { get; set; } = new HashSet<ApplicationUserToken>();
#if AUTH_GOOGLE
        internal const                  string PROVIDERNAME_GOOGLE = @"Google";
        [NotMapped, JsonIgnore]         public ApplicationUserToken GoogleToken { get { return this.GetToken(PROVIDERNAME_GOOGLE); } set { this.SetToken(PROVIDERNAME_GOOGLE, value); } }
#endif
#if AUTH_FACEBOOK
        internal const                  string PROVIDERNAME_FACEBOOK = @"Facebook";
        [NotMapped, JsonIgnore]         public ApplicationUserToken FacebookToken { get { return this.GetToken(PROVIDERNAME_FACEBOOK); } set { this.SetToken(PROVIDERNAME_FACEBOOK, value); } }
#endif
#if AUTH_MICROSOFT
        internal const                  string PROVIDERNAME_MICROSOFT = @"Microsoft";
        [NotMapped, JsonIgnore]         public ApplicationUserToken MicrosoftToken { get { return this.GetToken(PROVIDERNAME_MICROSOFT); } set { this.SetToken(PROVIDERNAME_MICROSOFT, value); } }
#endif
#if AUTH_LINKEDIN
        internal const                  string PROVIDERNAME_LINKEDIN = @"LinkedIn";
        [NotMapped, JsonIgnore]         public ApplicationUserToken LinkedInToken { get { return this.GetToken(PROVIDERNAME_LINKEDIN); } set { this.SetToken(PROVIDERNAME_LINKEDIN, value); } }
#endif
#if AUTH_TWITTER
        internal const                  string PROVIDERNAME_TWITTER = @"Twitter";
        [NotMapped, JsonIgnore]         public ApplicationUserToken TwitterToken { get { return this.GetToken(PROVIDERNAME_TWITTER); } set { this.SetToken(PROVIDERNAME_TWITTER, value); } }
#endif
#if AUTH_GITHUB
        internal const                  string PROVIDERNAME_GITHUB = @"Github";
        [NotMapped, JsonIgnore]         public ApplicationUserToken GithubToken { get { return this.GetToken(PROVIDERNAME_GITHUB); } set { this.SetToken(PROVIDERNAME_GITHUB, value); } }
#endif
        // 
        /// auxiliary properties
        [NotMapped, JsonIgnore]         public string FullName { get { return ((string.IsNullOrEmpty(this.FirstName) == false) ? (string.IsNullOrEmpty(this.LastName) == false) ? $@"{this.FirstName} {this.LastName}" : this.FirstName : this.LastName); } }
        [NotMapped, JsonIgnore]         public string FullNameOrEmail { get { string fullName = this.FullName; return ((string.IsNullOrEmpty(fullName) == false) ? fullName : this.Email); } }
        [NotMapped, JsonIgnore]         internal List<string> ResolvedRoleNames { get; set; }
        // 
        /// auxiliary methods
        public override string ToString() { return this.FullNameOrEmail; }

        internal ClaimsIdentity GenerateUserIdentity(ApplicationUserManager manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = manager.CreateIdentity(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
        internal Task<ClaimsIdentity> GenerateUserIdentityAsync(ApplicationUserManager manager)
        {
            return Task.FromResult(this.GenerateUserIdentity(manager));
        }
        internal IEnumerable<ApplicationUserToken> GetTokens(string providerName) { return this.Tokens?.Where(_ => _.ProviderName == providerName); }
        internal ApplicationUserToken GetToken(string providerName) { return this.GetTokens(providerName)?.FirstOrDefault(); }
        internal void SetToken(string providerName, string accessToken, string secretToken = null, DateTime? expires = null)
        {
            ApplicationUserToken token = this.GetToken(providerName);
            if (token == null) { token = new ApplicationUserToken() { ProviderName = providerName, }; }
            // 
            //token.ProviderName = providerName;
            token.ApplicationUserId = this.Id;
            token.AccessToken = accessToken;
            token.SecretToken = secretToken;
            token.Expires = expires;
            token.ModifiedDate = DateTime.UtcNow;
            // 
            this.SetToken(providerName, token);
        }
        protected void SetToken(string providerName, ApplicationUserToken token)
        {
            var curr = this.GetToken(providerName);
            if (token != null)
            {
                token.ProviderName = providerName;
                if (curr == null) { this.Tokens.Add(token); }
                else if (curr != token)
                {
                    if (curr.Id != token.Id) { this.Tokens.Remove(curr); this.Tokens.Add(token); }
                    else
                    {
                        curr.ProviderName = token.ProviderName;
                        curr.ApplicationUserId = token.ApplicationUserId;
                        curr.AccessToken = token.AccessToken;
                        curr.ModifiedDate = token.ModifiedDate;
                        curr.Expires = token.Expires;
                    }
                }
                else { } // no change
            }
            else
            {
                if (curr != null) { this.GetTokens(providerName)?.ToList().ForEach(_ => this.Tokens.Remove(_)); }
                else { } // no change
            }
        }
    }
}
namespace SP365.AddIn.Services.DataAccess.Configuration
{
    public class ApplicationUserConfiguration : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<ApplicationUser>
    {
        public ApplicationUserConfiguration()
        {
            Property(_ => _.FirstName).IsOptional();
            Property(_ => _.LastName).IsOptional();
            Property(_ => _.AvatarUrl).IsOptional();
            Property(_ => _.Company).IsOptional();
            Property(_ => _.CreatedDate).IsOptional();
            Property(_ => _.LastLogonDate).IsOptional();
            /// linked entities
            HasMany(_ => _.Tokens).WithOptional().HasForeignKey(_ => _.ApplicationUserId).WillCascadeOnDelete(true); // on delete, delete children
            // Ignores
#if AUTH_LINKEDIN
            Ignore(_ => _.LinkedInToken);
#endif
            Ignore(_ => _.FullName);
            Ignore(_ => _.FullNameOrEmail);
            Ignore(_ => _.ResolvedRoleNames);
        }
    }
}
