using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SP365.AddIn.Services.DataAccess.Models
{
    public class AppInstance : IDbEntityBase
    {
        [Key, Column("Id")]             public int Id { get; set; }
        [Column("SiteId")]              public Guid? SiteId { get; set; }
        [Column("WebId")]               public Guid? WebId { get; set; }
        [Column("SiteUrl")]             public string SiteUrl { get; set; }
        [Column("WebUrl")]              public string WebUrl { get; set; }
        [Column("AccessToken")]         public string AccessToken { get; set; }
        [Column("ModifiedDate")]        public DateTime? ModifiedDate { get; set; }
        [Column("AppWebUrl")]           public string AppWebUrl { get; set; }
        [Column("UniqueCDNPath")]       public string UniqueCDNPath { get; set; }
        [Column("Version")]             public string Version { get; set; }
        [Column("Status")]              public string Status { get; set; }
        // 
        /// auxiliary methods
        public override string ToString() { return @"SP365 AppInstance"; }
    }
}
namespace SP365.AddIn.Services.DataAccess.Configuration
{
    public class AppInstanceConfiguration : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<AppInstance>
    {
        public AppInstanceConfiguration()
        {
            Property(_ => _.Id).IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(_ => _.SiteId).IsOptional();
            Property(_ => _.WebId).IsOptional();
            Property(_ => _.SiteUrl).IsOptional();
            Property(_ => _.WebUrl).IsOptional();
            Property(_ => _.AccessToken).IsOptional();
            Property(_ => _.ModifiedDate).IsOptional();
            Property(_ => _.AppWebUrl).IsOptional();
            Property(_ => _.UniqueCDNPath).IsOptional();
            Property(_ => _.Version).IsOptional();
            Property(_ => _.Status).IsOptional();
        }
    }
}
