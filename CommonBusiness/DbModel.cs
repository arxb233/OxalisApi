using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Data;
using Tool;
using Tool.SqlCore;
using System.Data.OleDb;
using System.Runtime.Versioning;
using Tool.Utils;
using Tool.Utils.Data;

namespace HKRM_Server_C.CommonBusiness
{
    public class DbModel
    {
        private const string secretkey = "{B84E25BE-8C5C-4704-8E47-6892B14B0BA6}";

        internal static ILoggerFactory? LoggerFactory => ObjectExtension.Provider.GetService<ILoggerFactory>();

        public static void AddDbService(IServiceCollection services)
        {
            services.AddSingleton<DynamicDbFactory>();
        }

        public static void UseDbService()
        {
#if DEBUG
            var logger = LoggerFactory?.CreateLogger("Sql.Encrypt");
            logger?.LogInformation("DynamicLibrary：{Encrypt}", Tool.Utils.Encryption.AES.Encrypt("user id=sa;password=Qaz741236985.;initial catalog={0};data source=sqlserver;TrustServerCertificate=true", secretkey));
#endif
        }

        public static string DbDecrypt(string key) 
        {
            return Tool.Utils.Encryption.AES.Decrypt(AppSettings.Get(key), secretkey);
        }
    }

    public class DynamicDbFactory
    {
        private readonly LazyConcurrentDictionary<string, DynamicDbHelper> Pairs;

        public DynamicDbFactory()
        {
            Pairs = new();
        }

        public DynamicDbHelper GetDbHelper(string libraryName) 
        {
            return Pairs.GetOrAdd(libraryName, (name) => new DynamicDbHelper(name));
        }
    }

    public class DynamicDbHelper : BaseDbHelper
    {
        public DynamicDbHelper(string libraryName) : base(string.Format(DbModel.DbDecrypt("ConnectionStrings:DynamicLibrary"), libraryName), DbProviderType.SqlServer1, new SqlServerProvider(), DbModel.LoggerFactory.CreateLogger($"Db:{libraryName}"))
        {
            IsSqlLog = true;
            CommandTimeout = 180;
        }
    }

    public abstract class BaseDbHelper : DbHelper
    {
        private readonly LazyConcurrentDictionary<string, ITableProvider> Tables;
        public string ConnectionDbString => base.ConnectionString;

        protected BaseDbHelper(string connString, DbProviderType dbProviderType, IDbProvider dbProvider, ILogger logger) : base(connString, dbProviderType, dbProvider, logger)
        {
            Tables = [];
        }

        public virtual ITableProvider GetProvider(string name)
        {
            return Tables.GetOrAdd(name, Add);
            ITableProvider Add(string name) 
            {
                return new TableProvider(this, name);
            }
        }
    }

    public class SqlServerProvider : IDbProvider<SqlDbType>
    {
        /// <summary>
        /// 根据<see cref="Type"/>类型获取对应的类型
        /// </summary>
        /// <param name="t"><see cref="Type"/>类型</param>
        /// <returns>类型</returns>
        public SqlDbType ConvertToLocalDbType(Type t)
        {
            string key = t.ToString();
            return key switch
            {
                "System.Boolean" => SqlDbType.Bit,
                "System.DateTime" => SqlDbType.DateTime,
                "System.Decimal" => SqlDbType.Decimal,
                "System.Single" => SqlDbType.Float,
                "System.Double" => SqlDbType.Float,
                "System.Byte[]" => SqlDbType.Image,
                "System.Int64" => SqlDbType.BigInt,
                "System.Int32" => SqlDbType.Int,
                "System.String" => SqlDbType.NVarChar,
                "System.Int16" => SqlDbType.SmallInt,
                "System.Byte" => SqlDbType.TinyInt,
                "System.Guid" => SqlDbType.UniqueIdentifier,
                "System.TimeSpan" => SqlDbType.Time,
                "System.Object" => SqlDbType.Variant,
                _ => SqlDbType.NVarChar,
            };
        }

        /// <summary>
        /// 验证对象信息，并填充进<see cref="SqlCommand"/>集合中
        /// </summary>
        /// <param name="cmd">参数</param>
        public void DeriveParameters(IDbCommand cmd)
        {
            if (cmd is SqlCommand command)
            {
                SqlCommandBuilder.DeriveParameters(command);
            }
        }

        /// <summary>
        /// 绑定数据
        /// </summary>
        /// <param name="paraName">键</param>
        /// <param name="paraValue">值</param>
        /// <param name="direction">指定查询内的有关 <see cref="DataSet"/> 的参数的类型。</param>
        /// <param name="paraType">类型</param>
        /// <param name="sourceColumn">源列</param>
        /// <param name="size">大小</param>
        /// <returns></returns>
        public void GetParam(ref DbParameter paraName, object paraValue, ParameterDirection direction, Type paraType, string sourceColumn, int size)
        {
            SqlParameter? sqlParameter = paraName as SqlParameter;
            if (paraType != null && sqlParameter is not null)
            {
                sqlParameter.SqlDbType = ConvertToLocalDbType(paraType);
            }
        }


        public string ParameterPrefix => "@";


        /// <summary>
		/// 获取插入数据的主键ID（SQL）
		/// </summary>
		/// <returns></returns>
        public string GetLastIdSql()
        {
            return "SELECT SCOPE_IDENTITY()";
        }

    }
}
