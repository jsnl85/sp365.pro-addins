using System;
using System.Data.Entity.SqlServer;

namespace SP365.AddIn.Services.DataAccess.Configuration
{
    public class AutoNumberAttribute : Attribute
    {
        public AutoNumberAttribute(params string[] columnNames) { this.ColumnNames = columnNames; }
        // 
        public string[] ColumnNames { get; set; }
    }

    internal class AutoNumberSqlServerMigrationSqlGenerator : SqlServerMigrationSqlGenerator
    {
    }
}
