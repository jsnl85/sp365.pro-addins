using System.Data.Entity.Migrations;

namespace SP365.AddIn.Services.DataAccess.Configuration.Migrations.ApplicationDb
{
    internal sealed class Configuration : DbMigrationsConfiguration<SP365.AddIn.Services.DataAccess.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = @"Code\DataAccess\Configuration\Migrations\ApplicationDb";
        }

        protected override void Seed(SP365.AddIn.Services.DataAccess.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
