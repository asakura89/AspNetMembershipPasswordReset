using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Databossy {
    public interface IDatabaseFactory {
        IDatabase CreateSession();
        IDatabase CreateSession(String connectionStringOrName, Boolean useConnectionString = false, String provider = Database.SqlServerProvider);
    }

    public class DatabaseFactory : IDatabaseFactory {
        public IDatabase CreateSession() => CreateSession(ConfigurationManager.ConnectionStrings[0].Name);

        public IDatabase CreateSession(String connectionStringOrName, Boolean useConnectionString = false, String provider = Database.SqlServerProvider) => new Database(connectionStringOrName, useConnectionString, provider);
    }

    public interface IDatabase : IDisposable {
        String ContextName { get; set; }
        String ConnectionString { get; set; }
        String Provider { get; set; }

        DataTable QueryDataTable(String queryString);
        DataTable QueryDataTable(String queryString, params Object[] queryParams);
        DataTable NQueryDataTable(String queryString, Object paramObj);
        DataSet QueryDataSet(String queryString);
        DataSet QueryDataSet(String queryString, params Object[] queryParams);
        DataSet NQueryDataSet(String queryString, Object paramObj);
        IEnumerable<T> Query<T>(String queryString);
        IEnumerable<T> Query<T>(String queryString, params Object[] queryParams);
        IEnumerable<T> NQuery<T>(String queryString, Object paramObj);
        T QuerySingle<T>(String queryString);
        T QuerySingle<T>(String queryString, params Object[] queryParams);
        T NQuerySingle<T>(String queryString, Object paramObj);
        T QueryScalar<T>(String queryString);
        T QueryScalar<T>(String queryString, params Object[] queryParams);
        T NQueryScalar<T>(String queryString, Object paramObj);
        Int32 Execute(String queryString);
        Int32 Execute(String queryString, params Object[] queryParams);
        Int32 NExecute(String queryString, Object paramObj);
        Int32 WithTransaction(Func<Database, Int32> doThisWithTrx);
    }

    public class Database : IDatabase {
        internal const String SqlServerProvider = "System.Data.SqlClient";
        DbConnection connection;
        DbProviderFactory factory;

        public String ContextName { get; set; }
        public String ConnectionString { get; set; }
        public String Provider { get; set; }

        public Database() : this(ConfigurationManager.ConnectionStrings[0].Name) { }

        public Database(String connectionStringOrName, Boolean useConnectionString = false, String provider = SqlServerProvider) {
            Provider = provider;
            if (useConnectionString)
                ConnectionString = connectionStringOrName;
            else {
                String connString = ConfigurationManager.ConnectionStrings[connectionStringOrName].ConnectionString;
                ConnectionString = connString;
                ContextName = connectionStringOrName;
            }
        }

        void Open() {
            factory = DbProviderFactories.GetFactory(Provider);
            connection = factory.CreateConnection();
            if (connection == null)
                throw new InvalidOperationException("Connection creation from factory failed.");

            connection.ConnectionString = ConnectionString;
            connection.Open();
        }

        DbCommand BuildSqlCommand(String queryString) {
            DbCommand builtSqlCommand = connection.CreateCommand();
            builtSqlCommand.CommandText = queryString;
            builtSqlCommand.CommandType = CommandType.Text;

            return builtSqlCommand;
        }

        DbCommand BuildSqlCommand(String queryString, Object[] queryParams) {
            DbCommand builtSqlCommand = connection.CreateCommand();
            builtSqlCommand.CommandText = queryString;
            builtSqlCommand.CommandType = CommandType.Text;
            BuildSqlCommandParameter(ref builtSqlCommand, queryParams);

            return builtSqlCommand;
        }

        DbCommand NBuildSqlCommand<TParam>(String queryString, TParam paramObj) {
            DbCommand builtSqlCommand = connection.CreateCommand();
            builtSqlCommand.CommandText = queryString;
            builtSqlCommand.CommandType = CommandType.Text;
            NBuildSqlCommandParameter(ref builtSqlCommand, paramObj);

            return builtSqlCommand;
        }

        void BuildSqlCommandParameter(ref DbCommand builtSqlCommand, Object[] queryParams) {
            builtSqlCommand.Parameters.Clear();
            for (Int32 paramIdx = 0; paramIdx < queryParams.Length; paramIdx++) {
                Object currentqueryParams = queryParams[paramIdx] ?? DBNull.Value;
                DbParameter param = builtSqlCommand.CreateParameter();
                param.ParameterName = paramIdx.ToString();
                param.Value = currentqueryParams;
                builtSqlCommand.Parameters.Add(param);
            }
        }

        void NBuildSqlCommandParameter<TParam>(ref DbCommand builtSqlCommand, TParam paramObj) {
            PropertyInfo[] properties = ValidateAndGetNParam(builtSqlCommand.CommandText, paramObj);

            builtSqlCommand.Parameters.Clear();
            foreach (PropertyInfo currentProperty in properties) {
                Object currentParam = currentProperty.GetValue(paramObj, null) ?? DBNull.Value;
                DbParameter param = builtSqlCommand.CreateParameter();
                param.ParameterName = currentProperty.Name;
                param.Value = currentParam;
                builtSqlCommand.Parameters.Add(param);
            }
        }

        PropertyInfo[] ValidateAndGetNParam(String queryString, Object paramObj) {
            var queryParamRgx = new Regex("(?<!@)@\\w+", RegexOptions.Compiled);
            MatchCollection definedParams = queryParamRgx.Matches(queryString);
            PropertyInfo[] properties = paramObj.GetType().GetProperties();
            foreach (Match param in definedParams) {
                String closureParam = param.Value.Replace("@", "");
                PropertyInfo foundProperty = properties.FirstOrDefault(p => p.Name == closureParam);
                if (foundProperty == null)
                    throw new InvalidOperationException($"Sql Param \"{closureParam}\" is defined but value isn't supplied.");
            }

            return properties;
        }

        DbDataAdapter BuildSelectDataAdapter(DbCommand builtSqlCommand) {
            DbDataAdapter builtDataAdapter = factory.CreateDataAdapter();
            if (builtDataAdapter == null)
                throw new InvalidOperationException("Data Adapter creation from factory failed.");

            builtDataAdapter.SelectCommand = builtSqlCommand;
            return builtDataAdapter;
        }

        public DataTable QueryDataTable(String queryString) {
            Open();
            var dt = new DataTable();
            using (DbCommand cmd = BuildSqlCommand(queryString)) {
                using (DbDataAdapter dataAdapter = BuildSelectDataAdapter(cmd))
                    dataAdapter.Fill(dt);
            }

            return dt;
        }

        public DataTable QueryDataTable(String queryString, params Object[] queryParams) {
            Open();
            var dt = new DataTable();
            using (DbCommand cmd = BuildSqlCommand(queryString, queryParams)) {
                using (DbDataAdapter dataAdapter = BuildSelectDataAdapter(cmd))
                    dataAdapter.Fill(dt);
            }

            return dt;
        }

        public DataTable NQueryDataTable(String queryString, Object paramObj) {
            Open();
            var dt = new DataTable();
            using (DbCommand cmd = NBuildSqlCommand(queryString, paramObj)) {
                using (DbDataAdapter dataAdapter = BuildSelectDataAdapter(cmd))
                    dataAdapter.Fill(dt);
            }

            return dt;
        }

        public DataSet QueryDataSet(String queryString) {
            Open();
            var ds = new DataSet();
            using (DbCommand cmd = BuildSqlCommand(queryString)) {
                using (DbDataAdapter dataAdapter = BuildSelectDataAdapter(cmd))
                    dataAdapter.Fill(ds);
            }

            return ds;
        }

        public DataSet QueryDataSet(String queryString, params Object[] queryParams) {
            Open();
            var ds = new DataSet();
            using (DbCommand cmd = BuildSqlCommand(queryString, queryParams)) {
                using (DbDataAdapter dataAdapter = BuildSelectDataAdapter(cmd))
                    dataAdapter.Fill(ds);
            }

            return ds;
        }

        public DataSet NQueryDataSet(String queryString, Object paramObj) {
            Open();
            var ds = new DataSet();
            using (DbCommand cmd = NBuildSqlCommand(queryString, paramObj)) {
                using (DbDataAdapter dataAdapter = BuildSelectDataAdapter(cmd))
                    dataAdapter.Fill(ds);
            }

            return ds;
        }

        // NOTE: must be materialized to List
        // --> https://softwareengineering.stackexchange.com/questions/300242/will-the-database-connection-be-closed-if-we-yield-the-datareader-row-and-not-re
        public IEnumerable<T> Query<T>(String queryString) {
            Open();
            var result = new List<T>();
            using (DbCommand cmd = BuildSqlCommand(queryString))
            using (DbDataReader reader = cmd.ExecuteReader())
                while (reader.Read())
                    result.Add(ToTResult<T>(reader));

            return result;
        }

        public IEnumerable<T> Query<T>(String queryString, params Object[] queryParams) {
            Open();
            var result = new List<T>();
            using (DbCommand cmd = BuildSqlCommand(queryString, queryParams))
            using (DbDataReader reader = cmd.ExecuteReader())
                while (reader.Read())
                    result.Add(ToTResult<T>(reader));

            return result;
        }

        public IEnumerable<T> NQuery<T>(String queryString, Object paramObj) {
            Open();
            var result = new List<T>();
            using (DbCommand cmd = NBuildSqlCommand(queryString, paramObj))
            using (DbDataReader reader = cmd.ExecuteReader())
                while (reader.Read())
                    result.Add(ToTResult<T>(reader));

            return result;
        }

        public T QuerySingle<T>(String queryString) => Query<T>(queryString).FirstOrDefault();

        public T QuerySingle<T>(String queryString, params Object[] queryParams) => Query<T>(queryString, queryParams).FirstOrDefault();

        public T NQuerySingle<T>(String queryString, Object paramObj) => NQuery<T>(queryString, paramObj).FirstOrDefault();

        public T QueryScalar<T>(String queryString) {
            Open();
            using (DbCommand cmd = BuildSqlCommand(queryString))
                return (T) Convert.ChangeType(cmd.ExecuteScalar(), Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }

        public T QueryScalar<T>(String queryString, params Object[] queryParams) {
            Open();
            using (DbCommand cmd = BuildSqlCommand(queryString, queryParams))
                return (T) Convert.ChangeType(cmd.ExecuteScalar(), Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }

        public T NQueryScalar<T>(String queryString, Object paramObj) {
            Open();
            using (DbCommand cmd = NBuildSqlCommand(queryString, paramObj))
                return (T) Convert.ChangeType(cmd.ExecuteScalar(), Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }

        public Int32 Execute(String queryString) {
            Open();
            Int32 result;
            using (DbCommand cmd = BuildSqlCommand(queryString))
                result = cmd.ExecuteNonQuery();

            return result;
        }

        public Int32 Execute(String queryString, params Object[] queryParams) {
            Open();
            Int32 result;
            using (DbCommand cmd = BuildSqlCommand(queryString, queryParams))
                result = cmd.ExecuteNonQuery();

            return result;
        }

        public Int32 NExecute(String queryString, Object paramObj) {
            Open();
            Int32 result;
            using (DbCommand cmd = NBuildSqlCommand(queryString, paramObj))
                result = cmd.ExecuteNonQuery();

            return result;
        }

        public Int32 WithTransaction(Func<Database, Int32> doThisWithTrx) {
            Open();
            Int32 result;
            using (TransactionScope scope = CreateTransactionScope()) {
                result = doThisWithTrx(this);
                scope.Complete();
            }

            return result;
        }

        // NOTE: https://blogs.msdn.microsoft.com/dbrowne/2010/06/03/using-new-transactionscope-considered-harmful/
        TransactionScope CreateTransactionScope() {
            var options = new TransactionOptions {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.MaximumTimeout
            };

            return new TransactionScope(TransactionScopeOption.Required, options);
        }

        public void Dispose() {
            if (connection != null) {
                if (connection.State != ConnectionState.Closed)
                    connection.Close();

                connection.Dispose();
                connection = null;
            }

            GC.SuppressFinalize(this);
        }

        TResult ToTResult<TResult>(IDataRecord record) {
            TResult t = Activator.CreateInstance<TResult>();
            Type tType = typeof(TResult);
            PropertyInfo[] tProperties = tType.GetProperties();
            FieldInfo[] tFields = tType.GetFields();

            if (tProperties.Length != 0) {
                foreach (PropertyInfo property in tProperties) {
                    Object result = record[record.GetOrdinal(property.Name)];
                    if (result != DBNull.Value) {
                        Type safeType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                        Object safeResult = Convert.ChangeType(result, safeType);

                        property.SetValue(t, safeResult, null);
                    }
                }
            }

            if (tFields.Length != 0) {
                foreach (FieldInfo field in tFields) {
                    Object result = record[record.GetOrdinal(field.Name)];
                    if (result != DBNull.Value) {
                        Type safeType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                        Object safeResult = Convert.ChangeType(result, safeType);

                        field.SetValue(t, safeResult);
                    }
                }
            }

            return t;
        }
    }
}
