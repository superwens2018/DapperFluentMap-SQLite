using FluentMigrator;

namespace DapperFluentMap_SQLite.Migrations
{
    [Migration(1)]
    public class Migration_Initialization : AutoReversingMigration
    {
        public override void Up()
        {
            Create.Table("user_table")
                .WithColumn("user_id").AsInt32().PrimaryKey()
                .WithColumn("user_name").AsString();

            Insert.IntoTable("user_table")
                .Row(new { user_id = 1, user_name = "hoge" })
                .Row(new { user_id = 2, user_name = "fuga" });
        }
    }
}
