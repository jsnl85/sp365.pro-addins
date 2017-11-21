using Newtonsoft.Json;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SP365.AddIn.Services.DataAccess.Models
{
    public class ApplicationUserToken : IDbEntityBase
    {
        [Key, Column("Id")]             public int Id { get; set; }
        [Column("ApplicationUserId")]   public string ApplicationUserId { get; set; }
        [Column("ProviderName")]        public string ProviderName { get; set; }
        [Column("AccessToken")]         public string AccessToken { get; set; }
        [Column("SecretToken")]         public string SecretToken { get; set; }
        [Column("ModifiedDate")]        public DateTime? ModifiedDate { get; set; }
        [Column("Expires")]             public DateTime? Expires { get; set; }
        // 
        /// linked entities
        [ForeignKey("ApplicationUserId"), JsonIgnore]public virtual ApplicationUser ApplicationUser { get; set; }
        // 
        /// auxiliary methods
        public override string ToString() { return $@"{this.ProviderName} Token"; }
    }
}
namespace SP365.AddIn.Services.DataAccess.Configuration
{
    public class ApplicationUserTokenConfiguration : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<ApplicationUserToken>
    {
        public ApplicationUserTokenConfiguration()
        {
            Property(_ => _.Id).IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(_ => _.ApplicationUserId).IsRequired();
            Property(_ => _.ProviderName).IsOptional();
            Property(_ => _.AccessToken).IsOptional();
            Property(_ => _.SecretToken).IsOptional();
            Property(_ => _.ModifiedDate).IsOptional();
            Property(_ => _.Expires).IsOptional();
            /// linked entities
            HasRequired(_ => _.ApplicationUser).WithMany().HasForeignKey(_ => _.ApplicationUserId).WillCascadeOnDelete(false); // on delete, keep parent
        }
    }
}
