using Newtonsoft.Json;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SP365.AddIn.Services.Models
{
    [DataContract]
    public class UserInfo
    {
        [DataMember(Name = "id", EmitDefaultValue = false, IsRequired = false)]         public string Id { get; set; }
        [DataMember(Name = "username", EmitDefaultValue = false, IsRequired = false)]   public string UserName { get; set; }
        [DataMember(Name = "roles", EmitDefaultValue = false, IsRequired = false)]      public List<string> Roles { get; set; }
        [DataMember(Name = "email", EmitDefaultValue = false, IsRequired = false)]      public string Email { get; set; }
        //[DataMember(Name = "emailConfirmed", EmitDefaultValue = false, IsRequired = false)]public bool EmailConfirmed { get; set; }
        [DataMember(Name = "phoneNumber", EmitDefaultValue = false, IsRequired = false)]public string PhoneNumber { get; set; }
        //[DataMember(Name = "phoneNumberConfirmed", EmitDefaultValue = false, IsRequired = false)]public bool PhoneNumberConfirmed { get; set; }
        /// basic fields
        [DataMember(Name = "firstName", EmitDefaultValue = false, IsRequired = false)]  public string FirstName { get; set; }
        [DataMember(Name = "lastName", EmitDefaultValue = false, IsRequired = false)]   public string LastName { get; set; }
        [DataMember(Name = "avatarUrl", EmitDefaultValue = false, IsRequired = false)]  public string AvatarUrl { get; set; }
        [DataMember(Name = "company", EmitDefaultValue = false, IsRequired = false)]    public string Company { get; set; }
        //[DataMember(Name = "companyConfirmed", EmitDefaultValue = false, IsRequired = false)]public bool CompanyConfirmed { get; set; }
        /// extra fields
        [DataMember(Name = "createdDate", EmitDefaultValue = false, IsRequired = false)]public DateTime? CreatedDate { get; set; }
        [DataMember(Name = "lastLogonDate", EmitDefaultValue = false, IsRequired = false)]public DateTime? LastLogonDate { get; set; }
        [DataMember(Name = "hasPasswordQ", EmitDefaultValue = false, IsRequired = false)]public bool? HasPasswordQ { get; set; }
        /// composite fields
        [IgnoreDataMember]public string FullName { get { return ((string.IsNullOrEmpty(this.FirstName) == false) ? (string.IsNullOrEmpty(this.LastName) == false) ? $@"{this.FirstName} {this.LastName}" : this.FirstName : this.LastName); } }
        [IgnoreDataMember]public string FullNameOrEmail { get { string fullName = this.FullName; return ((string.IsNullOrEmpty(fullName) == false) ? fullName : this.Email); } }
        // 
        public override string ToString() { return this.FullNameOrEmail; }
        public string ToJSON() { return JsonConvert.SerializeObject(this); }

        public static UserInfo Create(ApplicationUser value, bool includeRolesQ = true, bool includeDetailsQ = true, bool includeCreatedDateQ = false, bool includeLastLogonDateQ = false, bool? hasPasswordQ = null)
        {
            return (
                (value != null) ? new UserInfo()
                {
                    Id = value.Id,
                    UserName = value.UserName,
                    Roles = ((includeRolesQ && value.ResolvedRoleNames?.Any() == true) ? value.ResolvedRoleNames : null),
                    Email = (includeDetailsQ ? value.Email : null),
                    //EmailConfirmed = (includeDetailsQ ? value.EmailConfirmed : null),
                    PhoneNumber = (includeDetailsQ ? value.PhoneNumber : null),
                    //PhoneNumberConfirmed = (includeDetailsQ ? value.PhoneNumberConfirmed : null),
                    /// basic fields
                    FirstName = value.FirstName,
                    LastName = value.LastName,
                    AvatarUrl = (includeDetailsQ ? value.AvatarUrl : null),
                    Company = (includeDetailsQ ? value.Company : null),
                    //CompanyConfirmed = (includeDetailsQ ? value.CompanyConfirmed : null),
                    /// extra fields
                    CreatedDate = (includeCreatedDateQ ? value.CreatedDate : (DateTime?)null),
                    LastLogonDate = (includeLastLogonDateQ ? value.LastLogonDate : (DateTime?)null),
                    HasPasswordQ = hasPasswordQ,
                }
                : null
            );
        }
    }
}
