using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;

namespace SP365.AddIn.Services.DataAccess.Configuration
{
    internal class ApplicationDbConfiguration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public ApplicationDbConfiguration()
        {
            this.AutomaticMigrationsEnabled = false;
            this.AutomaticMigrationDataLossAllowed = false;
            this.MigrationsAssembly = typeof(SP365.AddIn.Services.DataAccess.Configuration.Migrations.ApplicationDb.Configuration).Assembly;
            this.MigrationsNamespace = typeof(SP365.AddIn.Services.DataAccess.Configuration.Migrations.ApplicationDb.Configuration).Namespace;
            this.MigrationsDirectory = @"Code\DataAccess\Configuration\Migrations\ApplicationDb";
        }

        protected override void Seed(ApplicationDbContext context)
        {
            base.Seed(context);
            // 
            EnsureApplicationRoles(context);
        }
        protected void EnsureApplicationRoles(ApplicationDbContext context)
        {
            IEnumerable<string> roleNames = (ApplicationDbContext.RoleNames ?? new string[0]);
            foreach (string roleName in roleNames)
            {
                IdentityRole role = context.Roles.SingleOrDefault(_ => _.Name == roleName);
                if (role == null)
                {
                    role = context.Roles.Add(new IdentityRole()
                    {
                        Name = roleName,
                    });
                    context.SaveChanges();
                }
            }
        }
    }
}
