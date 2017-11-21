using SP365.AddIn.Services.DataAccess.Models;
using System.Data.Entity.ModelConfiguration;

namespace SP365.AddIn.Services.DataAccess.Configuration
{
    public class EntityBaseConfiguration<T> : EntityTypeConfiguration<T> where T : class, IDbEntityBase
    {
        public EntityBaseConfiguration()
        {
            //HasKey(_ => _.Id);
        }
    }
}
