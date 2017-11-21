using Microsoft.AspNet.Identity.EntityFramework;
using SP365.AddIn.Services.DataAccess.Configuration;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace SP365.AddIn.Services.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        #region Constructor

        public ApplicationDbContext() : base("DefaultConnection", throwIfV1Schema: false) { System.Data.Entity.Database.SetInitializer(new ApplicationDbInitializer()); }

        #endregion Constructor

        #region Constants

        public const string RoleName_Administrator = "Administrator";
        public static string[] RoleNames { get { if (_roleNames == null) { _roleNames = new string[] { RoleName_Administrator, }; } return _roleNames; } } private static string[] _roleNames = null;

        #endregion Constants

        #region Properties

        public virtual IDbSet<ApplicationUserToken> ApplicationUserTokens { get; set; }
        public virtual IDbSet<AppInstance> AppInstances { get; set; }

        #endregion Properties

        #region Methods

        public static ApplicationDbContext Create() { return new ApplicationDbContext(); }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            /// Generic Conventions
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Add(new AttributeToColumnAnnotationConvention<DefaultValueAttribute, string>(DefaultValueSqlServerMigrationSqlGenerator.AnnotationName, (p, attributes) => attributes.Select(_ => _.Value.ToString()).SingleOrDefault()));
            modelBuilder.Conventions.Add(new AttributeToColumnAnnotationConvention<UniqueIndex, string>(UniqueSqlServerMigrationSqlGenerator.AnnotationName, (p, attributes) => attributes.Select(_ => UniqueIndex.SerializeToString(_)).SingleOrDefault()));
            // 
            /// Supporting Entities
            modelBuilder.Configurations.Add(new ApplicationUserConfiguration());
            modelBuilder.Configurations.Add(new ApplicationUserTokenConfiguration());
            modelBuilder.Configurations.Add(new AppInstanceConfiguration());
            // 
            base.OnModelCreating(modelBuilder);
        }

        #endregion Methods
    }
}

namespace SP365.AddIn.Services.DataAccess.Configuration
{
    internal class ApplicationDbInitializer : MigrateDatabaseToLatestVersion<ApplicationDbContext, ApplicationDbConfiguration> //CreateDatabaseIfNotExists<ApplicationDbContext>
    {
        public override void InitializeDatabase(ApplicationDbContext context)
        {
            try
            {
                base.InitializeDatabase(context);
            }
            catch (Exception ex) { throw ex; }
        }
    }
}
