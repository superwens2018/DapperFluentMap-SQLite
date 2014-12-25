using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using Dapper;
using Dapper.FluentMap;
using Dapper.FluentMap.Conventions;
using Dapper.FluentMap.Mapping;
using System.Data.SQLite;


namespace DapperFluentMap_SQLite
{
    class Program
    {
        const string SQLiteFile = @"test.db";

        static void Main(string[] args)
        {
            var target = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            var connectionString = string.Format("\"Data Source={0}\"", SQLiteFile);
            var options = string.Format(@"--target {0} --provider sqlite --verbose true --connection {1}", target, connectionString);
            var p = Process.Start("Migrate.exe", options);
            p.WaitForExit();

            Console.WriteLine("なにもマッピングしない場合、結果は表示されません。");
            ExecuteDapperQuery();

            Console.WriteLine(
                Environment.NewLine +
                "何番の方法でマッピングしますか？。" + Environment.NewLine +
                "1 - EntityMap<T>を使う" + Environment.NewLine +
                "2 - AddConvention<T>().ForEntitiesInAssembly()を使う" + Environment.NewLine +
                "3 - AddConvention<T>().ForEntitiesInCurrentAssembly()を使う"
                );


            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.D1:
                    FluentMapper.Intialize(config => config.AddMap(new UserMap()));
                    break;

                case ConsoleKey.D2:
                    FluentMapper.Intialize(config =>
                    {
                        // See https://github.com/henkmollema/Dapper-FluentMap/blob/master/src%2FDapper.FluentMap%2FConfiguration%2FFluentConventionConfiguration.cs#L69
                        // パラメータはオプションでなくて必須っぽい
                        // `PropertyTransformConvention`でも使える
                        var type = typeof(User);
                        config.AddConvention<TypePrefixConvention>().ForEntitiesInAssembly(type.Assembly, type.Namespace);
                    });
                    break;

                case ConsoleKey.D3:
                    FluentMapper.Intialize(config =>
                    {
                        // See: https://github.com/henkmollema/Dapper-FluentMap/blob/master/src%2FDapper.FluentMap%2FConfiguration%2FFluentConventionConfiguration.cs#L49
                        // パラメータはオプションでなくて必須っぽい
                        // `TypePrefixConvention`でも使える

                        // exeはハイフンだが名前空間がアンダースコアなので、名前空間用に置換(力技...)
                        var currentNamespace = Assembly.GetExecutingAssembly().GetName().Name.Replace("-", "_");
                        config.AddConvention<PropertyTransformConvention>().ForEntitiesInCurrentAssembly(currentNamespace);
                    });
                    break;

                default:
                    return;
            }

            Console.WriteLine("");
            ExecuteDapperQuery();
            Console.WriteLine("データがマッピングされています。" + Environment.NewLine);
        }


        /// <summary>
        /// Dapperの実行
        /// </summary>
        private static void ExecuteDapperQuery()
        {
            using (var cn = new SQLiteConnection(string.Format("Data Source={0}", SQLiteFile)))
            {
                cn.Open();
                var results = cn.Query<User>(@"SELECT * FROM user_table");

                foreach (var r in results)
                {
                    Console.WriteLine(string.Format("ユーザID：{0}、ユーザ名：{1}", r.UserId, r.UserName));
                }
                cn.Close();
            }
        }
    }


    /// <summary>
    /// CamelCaseで書かれたC#のクラス
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
    }


    /// <summary>
    /// 指定したテーブルに対して、C#のCamelCaseで書かれたプロパティを、snake_caseで書かれたデータベースカラムへとマッピング
    /// </summary>
    public class UserMap : EntityMap<User>
    {
        public UserMap()
        {
            Map(p => p.UserId).ToColumn("user_id");
            Map(p => p.UserName).ToColumn("user_name");
        }
    }


    /// <summary>
    /// 条件を元に、一括でマッピング
    /// </summary>
    public class TypePrefixConvention : Convention
    {
        public TypePrefixConvention()
        {
            Properties<int>()
                .Where(c => c.Name == "UserId")                 // クラスのプロパティ
                .Configure(c => c.HasColumnName("user_id"));    // DBのテーブルのカラム

            Properties<string>()
                .Where(c => c.Name == "UserName")               // クラスのプロパティ
                .Configure(c => c.HasColumnName("user_name"));  // DBのテーブルのカラム

            // 未使用：Prefixの指定もできるっぽい
            Properties<int>().Configure(c => c.HasPrefix("int"));
        }
    }


    /// <summary>
    /// Transformを使った、C#のCamelCaseで書かれたプロパティを、snake_caseで書かれたデータベースカラムへとマッピング
    /// </summary>
    public class PropertyTransformConvention : Convention
    {
        public PropertyTransformConvention()
        {
            // patternはクラスのプロパティ、replacementはDBのテーブルのカラム
            Properties().Configure(c => c.Transform(s => Regex.Replace(s, "([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4").ToLower()));
        }
    }
}