using FluentMigrator;

namespace DapperFiddle.Migrations
{
    [Migration(1)]
    public class CreateOrdersTable: ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("Orders").InSchema("dbo")
                .WithColumn("Id").AsInt32().PrimaryKey("PK_Orders").Identity()
                .WithColumn("Date").AsDateTime().NotNullable()
                .WithColumn("Amount").AsDecimal().NotNullable()
                .WithColumn("Customer").AsAnsiString(255).NotNullable()
                .WithColumn("Remarks").AsAnsiString(1024).Nullable();
        }
    }
}
