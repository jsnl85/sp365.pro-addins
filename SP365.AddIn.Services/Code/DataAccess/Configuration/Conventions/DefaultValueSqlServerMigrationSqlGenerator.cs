using System.Collections.Generic;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.SqlServer;

namespace SP365.AddIn.Services.DataAccess.Configuration
{
    internal class DefaultValueSqlServerMigrationSqlGenerator : SqlServerMigrationSqlGenerator
    {
        public const string AnnotationName = "SqlDefaultValue";

        private int dropConstraintCount = 0;

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
                    column.DefaultValueSql = null;
                    using (var writer = Writer())
                    {
                        // Drop Constraint
                        string sql = GetSqlDropConstraintQuery(tableName, column.Name);
                        writer.WriteLine(sql);
                        Statement(writer);
                    }
                }
                else
                {
                    column.DefaultValueSql = newValue;
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

        private string GetSqlDropConstraintQuery(string tableName, string columnName)
        {
            var str = $@"DECLARE @var{dropConstraintCount} nvarchar(128)
SELECT @var{dropConstraintCount} = name
FROM sys.default_constraints
WHERE parent_object_id = object_id(N'{tableName}')
AND col_name(parent_object_id, parent_column_id) = '{columnName}';
IF @var{dropConstraintCount} IS NOT NULL
    EXECUTE('ALTER TABLE {tableName} DROP CONSTRAINT [' + @var{dropConstraintCount} + ']')";

            dropConstraintCount = dropConstraintCount + 1;
            return str;
        }
    }
}
