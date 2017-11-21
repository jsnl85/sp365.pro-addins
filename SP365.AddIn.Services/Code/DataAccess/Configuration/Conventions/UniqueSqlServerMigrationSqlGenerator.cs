using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.SqlServer;
using System.Linq;

namespace SP365.AddIn.Services.DataAccess.Configuration
{
    public class UniqueIndex : Attribute
    {
        public UniqueIndex() : base() { }
        public UniqueIndex(params string[] columnNames) { this.ColumnNames = columnNames; }
        // 
        public bool AllowNull { get; set; } = false;
        public string[] ColumnNames { get; set; }
        // 
        public static string SerializeToString(UniqueIndex value) { return ((value != null) ? $@"{value.AllowNull},{string.Join(",", value.ColumnNames)}" : null); }
        public static UniqueIndex DeserializeFromString(string value) { UniqueIndex ret = null; if (string.IsNullOrEmpty(value) == false) { string[] tmp = value.Split(','); ret = new UniqueIndex() { AllowNull = tmp.Select(_ => bool.Parse(_)).ElementAtOrDefault(0), ColumnNames = tmp.Skip(1).ToArray(), }; } return ret; }
    }
}

namespace SP365.AddIn.Services.DataAccess.Configuration
{
    internal class UniqueSqlServerMigrationSqlGenerator : SqlServerMigrationSqlGenerator
    {
        public const string AnnotationName = "SqlUniqueIndex";

        private int _sqlCount = 0;

        protected override void Generate(AddColumnOperation addColumnOperation)
        {
            SetAnnotatedColumn(addColumnOperation.Column, addColumnOperation.Table);
            base.Generate(addColumnOperation);
        }

        protected override void Generate(AlterColumnOperation alterColumnOperation)
        {
            SetAnnotatedColumn(alterColumnOperation.Column, alterColumnOperation.Table);
            base.Generate(alterColumnOperation);
        }

        protected override void Generate(CreateTableOperation createTableOperation)
        {
            SetAnnotatedColumns(createTableOperation.Columns, createTableOperation.Name);
            base.Generate(createTableOperation);
        }

        protected override void Generate(AlterTableOperation alterTableOperation)
        {
            SetAnnotatedColumns(alterTableOperation.Columns, alterTableOperation.Name);
            base.Generate(alterTableOperation);
        }

        private void SetAnnotatedColumn(ColumnModel column, string tableName)
        {
            AnnotationValues values;
            if (column.Annotations.TryGetValue(AnnotationName, out values))
            {
                string newValue = (values.NewValue as string);
                if (newValue == null)
                {
                    using (var writer = Writer())
                    {
                        // Drop Constraint
                        string sql = GetSqlDropUniqueIndexQuery(tableName, column.Name);
                        writer.WriteLine(sql);
                        Statement(writer);
                    }
                }
                else
                {
                    UniqueIndex newValueIndex = UniqueIndex.DeserializeFromString(newValue);
                    using (var writer = Writer())
                    {
                        // Add Constraint
                        string sql = GetSqlAddUniqueIndexQuery(tableName, column.Name, newValueIndex.AllowNull, newValueIndex.ColumnNames);
                        writer.WriteLine(sql);
                        Statement(writer);
                    }
                }
            }
        }

        private void SetAnnotatedColumns(IEnumerable<ColumnModel> columns, string tableName)
        {
            foreach (var column in columns)
            {
                SetAnnotatedColumn(column, tableName);
            }
        }

        private string GetSqlAddUniqueIndexQuery(string tableName, string columnName, bool allowNull, string[] columnNames)
        {
            _sqlCount++;
            string indexName = $@"UIX_{columnName}"; //((index != null && string.IsNullOrEmpty(index.Name) == false) ? index.Name : $@"UIX_{columnName}");
            string nonClusteredClause = $@" NONCLUSTERED";
            string whereNotNullClause = (allowNull ? $@" WHERE [{columnName}] IS NOT NULL" : "");
            string sql = $@"
DECLARE @var{_sqlCount} nvarchar(128);
SELECT @var{_sqlCount} = name FROM sys.indexes WHERE name='{indexName}' AND object_id = object_id(N'{tableName}') -- AND is_unique = true;
IF @var{_sqlCount} IS NULL BEGIN
    EXECUTE('CREATE UNIQUE{nonClusteredClause} INDEX [{indexName}] ON {tableName}({string.Join(",", columnNames)}){whereNotNullClause}')
END
";
            return sql;
        }
        private string GetSqlDropUniqueIndexQuery(string tableName, string columnName)
        {
            _sqlCount++;
            string indexName = $@"UIX_{columnName}"; //((index != null && string.IsNullOrEmpty(index.Name) == false) ? index.Name : $@"UIX_{columnName}");
            string sql = $@"
DECLARE @var{_sqlCount} nvarchar(128);
SELECT @var{_sqlCount} = name FROM sys.indexes WHERE name='{indexName}' AND object_id = object_id(N'{tableName}') -- AND is_unique = true;
IF @var{_sqlCount} IS NOT NULL BEGIN
    EXECUTE('DROP INDEX [' + @var{_sqlCount} + '] ON {tableName}')
END
";
            return sql;
        }
    }
}
