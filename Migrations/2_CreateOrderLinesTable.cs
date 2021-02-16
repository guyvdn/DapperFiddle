using FluentMigrator;

namespace DapperFiddle.Migrations
{
    [Migration(2)]
    public class CreateOrderLinesTable : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("OrderLines").InSchema("dbo")
                .WithColumn("Id").AsInt32().PrimaryKey("PK_OrderLines").Identity()
                .WithColumn("OrderId").AsInt32().ForeignKey("Orders", "Id").NotNullable()
                .WithColumn("Product").AsAnsiString(255).NotNullable()
                .WithColumn("Amount").AsDecimal().NotNullable();
        }
    }
}