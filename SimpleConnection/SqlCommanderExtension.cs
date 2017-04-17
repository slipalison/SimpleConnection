using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Newtonsoft.Json;

namespace SimpleConnection
{
    public static class SqlCommanderExtension
    {

        public static bool Any(this IDbConnection cnn, string querySql, bool isStoredProcedure, object param = null)
        {
            ExecuteToJson(cnn, querySql, isStoredProcedure, param);
            return false;
        }


        private static IDataReader CreateDataReader(IDbConnection cnn, string querySql, bool isStoredProcedure, object param = null)
        {
            var command = cnn.CreateCommand();
            command.CommandText = querySql;

            if (isStoredProcedure)
                command.CommandType = CommandType.StoredProcedure;
            if (param != null)
                command.GenerateParameters(param);

            cnn.Open();
            IDataReader result;
            using (var reader = command.ExecuteReader())
                result = reader;
            cnn.Close();

            return result;
        }

        public static string ExecuteToJson(this IDbConnection cnn, string querySql, bool isStoredProcedure, object param = null)
        {
            var result = "{}";
            result = SerializeJson(CreateDataReader(cnn, querySql, isStoredProcedure, param));
            return result;
        }

        private static void GenerateParameters(this IDbCommand command, object param)
        {
            var parameters = param.GetType().GetProperties().ToList();
            parameters.ForEach(x =>
            {
                var parameter = command.CreateParameter();
                var value = x.GetValue(param);
                parameter.ParameterName = $"@{x.Name}";
                parameter.Value = x.GetValue(param);
                parameter.DbType = Cast(value);
                command.Parameters.Add(parameter);
            });
            return;
        }

        private static DbType Cast(object obj)
        {
            switch (obj.GetType().Name)
            {
                case "Int32":
                    return DbType.Int32;
                case "Int64":
                    return DbType.Int64;
                case "String":
                case "Char":
                    return DbType.String;
                case "Boolean":
                    return DbType.Boolean;
                default:
                    return DbType.Object;
            }
        }

        private static string SerializeJson(IDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();

            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));

            while (reader.Read())
                results.Add(SerializeRow(cols, reader));

            var result = JsonConvert.SerializeObject(results, Formatting.None);

            return result;
        }
        private static Dictionary<string, object> SerializeRow(IEnumerable<string> cols, IDataReader reader)
        {
            var result = new Dictionary<string, object>();
            cols.ToList().ForEach(col =>
            {
                result.Add(col, reader[col]);
            });
            return result;
        }

        private static IEnumerable<T> Select<T>(this IDataReader reader, Func<IDataReader, T> projection)
        {
            while (reader.Read())
            {
                yield return projection(reader);
            }
        }
    }
}
