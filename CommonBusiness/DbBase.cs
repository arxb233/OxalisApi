﻿using System.Data;
using Tool;
using Tool.Utils.Data;
using Tool.SqlCore;
using Tool.Utils;

namespace OxalisApi.CommonBusiness
{
    class DbBase
    {
        public static DynamicDbHelper GetDynamicDb(string libraryName)
        {
            var dbFactory = ObjectExtension.Provider.GetService<DynamicDbFactory>() ?? throw new Exception("未注册服务！");
            return dbFactory.GetDbHelper(libraryName);
        }

        public static ITableProvider GetDynamicDbTable(string libraryName, string tableName)
        {
            return GetDynamicDb(libraryName).GetProvider(tableName);
        }

        public static async Task<DataTable> GetMAJORAsync(string sql)
        {
            var dbHelper = GetDynamicDb("MAJOR");

            var dataSet = await dbHelper.QueryAsync(sql);
            if (!dataSet.IsEmpty())
            {
                return dataSet.Tables[0];
            }
            else
            {
                return new DataTable();
            }
        }
    }
}
