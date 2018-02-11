/* Copyright (C) 2017 Oversight - All rights reserved
 *
 * You may use, distribute and modify this code under the terms of the LICENSE file.
 *
 * You should have received a copy of the LICENSE with this file.
 * If not, please write to admin@oversight.co.il
 *
 * All of this code and content written by Oversight team.
 * Any exceptions are mentioned with comments and external sources.
 * 
 * --
 * 
 * OsSql class provides methods for sharing the same data within both script variables and an MySQL database.
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Data;
using System.Reflection;
using Newtonsoft.Json;
namespace OsSql
{
    /// <summary>
    /// An interface for a class that is being used with OsSql in order to save value in the database.
    /// </summary>
    public interface IOsSqlElement
    {
        /// <summary>
        /// Saves the key value of the current class.
        /// Mostly this one should return an ID, or AI integer key of data enteries.
        /// </summary>
        /// <returns>The key as int.</returns>
        int GetID();
    }
    /// <summary>
    /// Contains any type that is being used with OsSql.
    /// </summary>
    public class OsSqlTypes
    {
        /// <summary>
        /// The exception that is thrown when an internal error of OsSql has occured.
        /// </summary>
        public class OsSqlException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <c>OsSqlException</c> class.
            /// </summary>
            public OsSqlException()
            {
            }
            /// <summary>
            /// Initializes a new instance of the <c>OsSqlException</c> class with a message.
            /// </summary>
            public OsSqlException(string message)
                : base(message)
            {
            }
            /// <summary>
            /// Initializes a new instance of the <c>OsSqlException</c> class with a message and an inner exception.
            /// </summary>
            public OsSqlException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
        /// <summary>
        /// An attribute class for OsSql synchronization with the database.
        /// Use this attribute on a class which you want to be an SQL element.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class OsSqlSaveAttribute : Attribute
        {
            /// <summary>
            /// The property name within the SQL database.
            /// </summary>
            internal string DbName { get; set; }
            /// <summary>
            /// The property column type.
            /// </summary>
            internal ColumnType? ColType { get; set; }
            /// <summary>
            /// Indicates wether the property will be defined as auto increment.
            /// </summary>
            internal bool AutoInc { get; set; }
            /// <summary>
            /// Specifies the property as synchronized with the database.
            /// </summary>
            /// <param name="ColType">The property column type.</param>
            /// <param name="DbName">The property name within the SQL database.</param>
            /// <param name="AutoInc">Indicates wether the property will be defined as auto increment.</param>
            public OsSqlSaveAttribute(ColumnType ColType, string DbName = "", bool AutoInc = false)
            {
                this.ColType = ColType;
                this.AutoInc = AutoInc;
                this.DbName = DbName;
            }
            /// <summary>
            /// Specifies the property as synchronized with the database and finding the column type automatically.
            /// </summary>
            /// <param name="DbName">The property name within the SQL database.</param>
            /// <param name="AutoInc">Indicates if the property will be defined as auto increment.</param>
            public OsSqlSaveAttribute(string DbName = "", bool AutoInc = false)
            {
                ColType = null;
                this.AutoInc = AutoInc;
                this.DbName = DbName;
            }
        }
        /// <summary>
        /// Handles SQL parameters as key and value.
        /// </summary>
        public class Parameter
        {
            /// <summary>
            /// Column (field) name.
            /// </summary>
            public string Key;
            /// <summary>
            /// Value to use.
            /// </summary>
            public object Value;
            /// <summary>
            /// Indicates wether this parameter is being passed with data manipulation queries.
            /// </summary>
            public bool Func;
            /// <summary>
            /// Initializes a new instance of <c>Parameter</c> with key and value, based on a string key.
            /// </summary>
            /// <param name="key">The column (field) name.</param>
            /// <param name="value">The value to use.</param>
            /// <param name="function">If this parameter is passed to data manipulation function (like Update() or Insert()), set true to use the function or false to skip this parameter.</param>
            public Parameter(string key, object value, bool function = true)
            {
                Key = key;
                Value = Builder.GetObject(value, null, true, out bool _);
                Func = function;
            }
            /// <summary>
            /// Initializes a new instance of <c>Parameter</c> with key and value, based on a column object key.
            /// </summary>
            /// <param name="key">The column object.</param>
            /// <param name="value">The value to use.</param>
            /// <param name="function">If this parameter is passed to data manipulation function (like Update() or Insert()), set true to use the function or false to skip this parameter.</param>
            public Parameter(Table.Column key, object value, bool function = true)
            {
                Key = key.DbName;
                Value = Builder.GetObject(value, key.ColType, true, out bool _);
                Func = function;
            }
            /// <summary>
            /// Initializes a new instance of <c>Parameter</c> with key and value, based on key search in a given table.
            /// </summary>
            /// <param name="table">The table to search the key in.</param>
            /// <param name="key">The column (field) name in code (CodeName).</param>
            /// <param name="value">The value to use.</param>
            /// <param name="function">If this parameter is passed to data manipulation function (like Update() or Insert()), set true to use the function or false to skip this parameter.</param>
            public Parameter(Table table, string key, object value, bool function = true)
            {
                var c = table.FindC(key);
                Key = c.DbName;
                Value = Builder.GetObject(value, c.ColType, true, out bool _);
                Func = function;
            }
        }
        /// <summary>
        /// Structure class handles database structure of tables.
        /// </summary>
        public class Structure
        {
            /// <summary>
            /// Represents a list with all database tables and fields settings.
            /// </summary>
            public List<Table> Tables;
            /// <summary>
            /// Represents the database name.
            /// </summary>
            public string DBName;
            /// <summary>
            /// Constructs a new instance of <c>Structure</c> without any tables.
            /// </summary>
            public Structure()
            {
                Tables = new List<Table>();
            }
            /// <summary>
            /// Constructs a new instance of <c>Structure</c> with predefined tables.
            /// </summary>
            /// <param name="dbname">Database name.</param>
            /// <param name="tables">Table instances.</param>
            public Structure(string dbname, params Table[] tables)
            {
                DBName = dbname;
                (this.Tables = new List<Table>()).AddRange(tables);
            }
            /// <summary>
            /// Adds a new table to the database structure.
            /// </summary>
            /// <param name="name">Table name.</param>
            /// <returns>Returns the generated object for adding settings to the table.</returns>
            /// <remarks>If the table is already exist in the database, its name must be written exactly as it is in the database.</remarks>
            public Table AddTable(string name)
            {
                if (Tables.Any(x => x.Name == name))
                    OsSqlDebugger.Error("Table \"" + name + "\" already exist in the structure.");
                var t = new Table(name, DBName);
                Tables.Add(t);
                return t;
            }
            /// <summary>
            /// Finds a table within the tables by a name of table.
            /// </summary>
            /// <param name="name">The exact name of the table.</param>
            /// <returns>The table of the given name or null if nothing found.</returns>
            public Table GetTable(string name)
            {
                foreach (var t in Tables)
                    if (t.Name == name)
                        return t;
                return null;
            }
        }
        /// <summary>
        /// Possible types of table values.
        /// </summary>
        public enum ColumnType
        {   // Comments from https://www.w3schools.com/sql/sql_datatypes.asp
            /// <summary>
            /// -2147483648 to 2147483647.
            /// </summary>
            Int,
            /// <summary>
            /// -128 to 127.
            /// </summary>
            TinyInt,
            /// <summary>
            /// -32768 to 32767.
            /// </summary>
            SmallInt,
            /// <summary>
            /// -9223372036854775808 to 9223372036854775807
            /// </summary>
            BigInt,
            /// <summary>
            /// Holds a variable length string (can contain letters, numbers, and special characters). The maximum size is specified in parenthesis. Can store up to 255 characters.
            /// </summary>
            Varchar,
            /// <summary>
            /// Holds a string with a maximum length of 65,535 characters.
            /// </summary>
            Text,
            /// <summary>
            /// Holds a string with a maximum length of 255 characters.
            /// </summary>
            TinyText,
            /// <summary>
            /// Holds a string with a maximum length of 16,777,215 characters.
            /// </summary>
            MediumText,
            /// <summary>
            /// Holds a string with a maximum length of 4,294,967,295 characters.
            /// </summary>
            LongText,
            /// <summary>
            /// A small number with a floating decimal point.
            /// </summary>
            Float,
            /// <summary>
            /// A large number with a floating decimal point.
            /// </summary>
            Double,
            /// <summary>
            /// A DOUBLE stored as a string, allowing for a fixed decimal point.
            /// </summary>
            Decimal,
            /// <summary>
            /// A date and time, saved as int (unix time).
            /// </summary>
            DateTime,
            /// <summary>
            /// A boolean value.
            /// </summary>
            Boolean,
            /// <summary>
            /// An enum property, saved as int (based on hash code).
            /// </summary>
            Enum,
            /// <summary>
            /// An object, saved as string (Json).
            /// </summary>
            Object,
            /// <summary>
            /// An IOsSqlElement type, saved as int (based on Save() function).
            /// </summary>
            Element
        };
        /// <summary>
        /// Table class handles a single table in a structure.
        /// </summary>
        public class Table
        {
            /// <summary>
            /// Represents a single table column (field).
            /// </summary>
            public class Column
            {
                /// <summary>
                /// Property name of the column.
                /// </summary>
                public string CodeName;
                /// <summary>
                /// Database name of the column.
                /// </summary>
                public string DbName;
                /// <summary>
                /// Database column type.
                /// </summary>
                public ColumnType ColType;
                /// <summary>
                /// Auto increment.
                /// </summary>
                public bool AutoInc;
                /// <summary>
                /// Construct a new column instance.
                /// </summary>
                /// <param name="type">Type of the value as <c>ColumnType</c>.</param>
                /// <param name="sql">Column name to be set in the database.</param>
                /// <param name="code">Exact name of the property in the code.</param>
                /// <param name="autoInc">Indicates if this column is defined as auto increment.</param>
                public Column(ColumnType type, string sql, string code, bool autoInc = false)
                {
                    CodeName = code;
                    DbName = sql;
                    ColType = type;
                    AutoInc = autoInc;
                }
            }
            /// <summary>
            /// Table name.
            /// </summary>
            public string Name;
            /// <summary>
            /// Database name.
            /// </summary>
            public string DBName;
            /// <summary>
            /// List of table columns (fields).
            /// </summary>
            public List<Column> Columns;
            /// <summary>
            /// Constructs a new instance of <c>Table</c> without any columns.
            /// </summary>
            /// <param name="name">Table name.</param>
            /// <param name="dbname">Database name.</param>
            public Table(string name, string dbname = "")
            {
                Name = name;
                DBName = dbname;
                Columns = new List<Column>();
            }
            /// <summary>
            /// Constructs a new instance of <c>Table</c> without any columns.
            /// </summary>
            /// <param name="name">Table name.</param>
            /// <param name="columns">Columns to add.</param>
            public Table(string name, params Column[] columns)
            {
                Name = name;
                (Columns = new List<Column>()).AddRange(columns);
            }
            /// <summary>
            /// Adds a new column (field) to the table.
            /// </summary>
            /// <param name="type">Type of the value as <c>ColumnType</c>.</param>
            /// <param name="sql">Column name to be set in the database.</param>
            /// <param name="code">Exact name of the property in the code.</param>
            /// <param name="autoInc">Indicates if this column is defined as auto increment.</param>
            public Column AddColumn(ColumnType type, string sql, string code, bool autoInc = false)
            {
                if (Columns.Any(x => x.CodeName == code || x.DbName == sql))
                    OsSqlDebugger.Error("The table already contains a column with this name.");
                var c = new Column(type, sql, code, autoInc);
                Columns.Add(c);
                return c;
            }
            /// <summary>
            /// Adds a new column (field) to the table, with the same name of both database column and code property.
            /// </summary>
            /// <param name="type">Type of the value as <c>ColumnType</c>.</param>
            /// <param name="name">Exact name of the property in the code that is also the name in the database.</param>
            /// <param name="autoInc">Indicates if this column is defined as auto increment.</param>
            public Column AddColumn(ColumnType type, string name, bool autoInc = false)
            {
                if (Columns.Any(x => x.CodeName == name || x.DbName == name))
                    OsSqlDebugger.Error("The table already contains a column with this name.");
                var c = new Column(type, name, name, autoInc);
                Columns.Add(c);
                return c;
            }
            /// <summary>
            /// Adds a new column (field) to the table using an <c>Column</c> instance.
            /// </summary>
            /// <param name="c">The <c>Column</c> instance.</param>
            public void AddColumn(Column c)
            {
                if (Columns.Any(x => x.CodeName == c.CodeName || x.DbName == c.DbName))
                    OsSqlDebugger.Error("The table already contains a column with this name.");
                Columns.Add(c);
            }
            /// <summary>
            /// Finds a column by its database column name.
            /// </summary>
            /// <param name="name">The exact database column name.</param>
            /// <returns>The column instance or null if nothing found.</returns>
            public Column FindD(string name)
            {
                return Columns.FirstOrDefault(x => x.DbName == name);
            }
            /// <summary>
            /// Finds a column by its property code name.
            /// </summary>
            /// <param name="name">The exact property code name.</param>
            /// <returns>The column instance or null if nothing found.</returns>
            public Column FindC(string name)
            {
                return Columns.FirstOrDefault(x => x.CodeName == name);
            }
            /// <summary>
            /// Adds all of the class properties as columns to the table.
            /// Column names will be the same as code names.
            /// </summary>
            /// <param name="classType">Type of the source class.</param>
            public void AddColumnsByClassProperties(Type classType)
            {
                foreach (var c in classType.GetProperties())
                    AddColumn(Builder.ColumnTypeFromObjectType(c.PropertyType), c.Name);
            }
            /// <summary>
            /// Adds all of the class properties that are marked with <c>OsSqlSaveAttribute</c> as columns to the table.
            /// </summary>
            /// <param name="classType">Type of the source class.</param>
            public void AddColumnsByAttributes(Type classType)
            {
                var list = classType.GetProperties();
                OsSqlSaveAttribute[] att;
                foreach (var prop in list)
                {
                    att = (OsSqlSaveAttribute[])prop.GetCustomAttributes(typeof(OsSqlSaveAttribute));
                    if (att.Length > 0)
                        AddColumn(att[0].ColType == null ? Builder.ColumnTypeFromObjectType(prop.GetType()) : att[0].ColType.Value, att[0].DbName.Length == 0 ? prop.Name : att[0].DbName, prop.Name, att[0].AutoInc);
                }
            }
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override string ToString() =>
                string.IsNullOrEmpty(DBName) ? Name : (DBName + "." + Name);
        }
    }
    /// <summary>
    /// Handles the internal debugger of OsSql.
    /// </summary>
    public class OsSqlDebugger
    {
        /// <summary>
        /// Types of OsSql debug messages.
        /// </summary>
        public enum OsSqlDebugType
        {
            /// <summary>
            /// Information message.
            /// </summary>
            Info,
            /// <summary>
            /// Error message.
            /// </summary>
            Error,
            /// <summary>
            /// Query debugging.
            /// </summary>
            Queries
        }
        /// <summary>
        /// Event handler for debug messages.
        /// </summary>
        /// <param name="type">Message type.</param>
        /// <param name="message">Message content.</param>
        public delegate void DebugDelegate(OsSqlDebugType type, string message);
        /// <summary>
        /// An event used to trace OsSql debug messages.
        /// </summary>
        public static event DebugDelegate OnOsSqlDebugMessage;
        /// <summary>
        /// An event used to trace OsSql queries.
        /// </summary>
        public static event DebugDelegate OnOsSqlQuery;
        /// <summary>
        /// Sends a debug information message.
        /// </summary>
        /// <param name="msg">The message content.</param>
        internal static void Message(string msg)
        {
            OnOsSqlDebugMessage?.Invoke(OsSqlDebugType.Info, msg);
        }
        /// <summary>
        /// Sends a debug error message.
        /// </summary>
        /// <param name="msg">The message content.</param>
        internal static void Error(string msg)
        {
            OnOsSqlDebugMessage?.Invoke(OsSqlDebugType.Error, msg);
            throw new OsSqlTypes.OsSqlException(msg);
        }
        /// <summary>
        /// Query debug.
        /// </summary>
        /// <param name="cmd">The query command object.</param>
        internal static void Query(MySqlCommand cmd)
        {
            if (OnOsSqlQuery == null)
                return;
            string cmdtext = cmd.CommandText;
            for (int i = 0; i < cmd.Parameters.Count; i++)
                cmdtext = cmdtext.Replace("@" + cmd.Parameters[i].ParameterName, cmd.Parameters[i].Value.ToString());
            OnOsSqlQuery?.Invoke(OsSqlDebugType.Queries, cmdtext);
        }
    }
    /// <summary>
    /// SQL class allows to handle the MySQL database.
    /// </summary>
    public class SQL
    {
        /// <summary>
        /// Represents a connection to an SQL server database.
        /// </summary>
        public MySqlConnection Connection = null;
        /// <summary>
        /// Represents the connection details.
        /// </summary>
        public MySqlConnectionStringBuilder ConnectionDetails = null;
        /// <summary>
        /// Constructs a new SQL instance.
        /// </summary>
        /// <param name="server">The server IP address or host name.</param>
        /// <param name="database">The database name.</param>
        /// <param name="uid">The login username.</param>
        /// <param name="passwd">The login password</param>
        public SQL(string server, string database, string uid, string passwd)
        {
            ConnectionDetails = new MySqlConnectionStringBuilder()
            {
                Server = server,
                Database = database,
                UserID = uid,
                Password = passwd
            };
        }
        /// <summary>
        /// Creates a direct connection for the purpose of reading and writing to the database.
        /// </summary>
        public bool Connect()
        {
            try
            {
                Connection = new MySqlConnection(ConnectionDetails.ToString());
                Connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                OsSqlDebugger.Error("MySql error " + ex.Number + ": " + ex.Message);
            }
            return false;
        }
        /// <summary>
        /// Closes the connection to the database.
        /// </summary>
        public void Disconnect()
        {
            Connection.Close();
            Connection.Dispose();
        }
        /// <summary>
        /// Sends a query to the database.
        /// </summary>
        /// <param name="query">The command content.</param>
        /// <param name="parameters">Parameters to pass with the query.</param>
        public void Query(string query, params OsSqlTypes.Parameter[] parameters)
        {
            MySqlCommand cmd = new MySqlCommand(query, Connection);
            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.Key, p.Value);
            cmd.Prepare();
            OsSqlDebugger.Query(cmd);
            cmd.ExecuteNonQuery();
        }
        /// <summary>
        /// Condition format for queries.
        /// </summary>
        /// <param name="condition">Condition SQL code.</param>
        /// <returns>The formatted string.</returns>
        /// <remarks>Yeah, we're that lazy.</remarks>
        private string Condition(string condition)
        {
            return (condition.Length > 0 ? (" WHERE " + condition) : "");
        }
        /// <summary>
        /// Select statement for SQL.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="key">Keys to select.</param>
        /// <param name="conditionparams">Parameters to pass with the condition and key.</param>
        /// <returns>A list of Dictionary elements based on the data.</returns>
        public List<Dictionary<string, object>> Select(string table, string condition = "", string key = "*", params OsSqlTypes.Parameter[] conditionparams)
        {
            var ret = new List<Dictionary<string, object>>();
            string query = "SELECT " + key + " FROM `" + table + "`" + Condition(condition);
            using (MySqlCommand cmd = new MySqlCommand(query, Connection))
            {
                foreach (var p in conditionparams)
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                cmd.Prepare();
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var newDic = new Dictionary<string, object>();
                        for (int i = 0; i < rdr.FieldCount; i++)
                            newDic.Add(rdr.GetName(i), rdr.GetValue(i));
                        ret.Add(newDic);
                    }
                    rdr.Close();
                }
            }
            return ret;
        }
        /// <summary>
        /// Select statement for SQL.
        /// </summary>
        /// <param name="table">Table object.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="key">Keys to select.</param>
        /// <param name="conditionparams">Parameters to pass with the condition and key.</param>
        /// <returns>A list of Dictionary elements based on the data.</returns>
        public List<Dictionary<string, object>> Select(OsSqlTypes.Table table, string condition = "", string key = "*", params OsSqlTypes.Parameter[] conditionparams)
        {
            return Select(table.ToString(), condition, key, conditionparams);
        }
        /// <summary>
        /// Select statement for SQL based on a new instance of DataTable.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="key">Key to select.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        /// <returns>The new DataTable.</returns>
        public DataTable Read(string table, string condition = "", string key = "*", params OsSqlTypes.Parameter[] conditionparams)
        {
            MySqlCommand cmd = new MySqlCommand("SELECT " + key + " FROM `" + table + "`" + Condition(condition), Connection);
            foreach (var p in conditionparams)
                cmd.Parameters.AddWithValue(p.Key, p.Value);
            cmd.Prepare();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds, table);
            return ds.Tables[table];
        }
        /// <summary>
        /// Select statement for SQL based on a new instance of DataTable.
        /// </summary>
        /// <param name="table">Table object.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="key">Key to select.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        /// <returns>The new DataTable.</returns>
        public DataTable Read(OsSqlTypes.Table table, string condition = "", string key = "*", params OsSqlTypes.Parameter[] conditionparams)
        {
            return Read(table.ToString(), condition, key, conditionparams);
        }
        /// <summary>
        /// Select statement for SQL based on an existing instance of DataSet and a new instance of DataTable.
        /// </summary>
        /// <param name="ds">Reference to DataSet element to set the data in.</param>
        /// <param name="table">Table name.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="key">Key to select.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        /// <returns>The new DataTable.</returns>
        public DataTable Read(ref DataSet ds, string table, string condition = "", string key = "*", params OsSqlTypes.Parameter[] conditionparams)
        {
            MySqlCommand cmd = new MySqlCommand("SELECT " + key + " FROM `" + table + "`" + Condition(condition), Connection);
            foreach (var p in conditionparams)
                cmd.Parameters.AddWithValue(p.Key, p.Value);
            cmd.Prepare();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);
            da.Fill(ds, table);
            return ds.Tables[table];
        }
        /// <summary>
        /// Select statement for SQL based on an existing instance of DataSet and a new instance of DataTable.
        /// </summary>
        /// <param name="ds">Reference to DataSet element to set the data in.</param>
        /// <param name="table">Table object.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="key">Key to select.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        /// <returns>The new DataTable.</returns>
        public DataTable Read(ref DataSet ds, OsSqlTypes.Table table, string condition = "", string key = "*", params OsSqlTypes.Parameter[] conditionparams)
        {
            return Read(ref ds, table.ToString(), condition, key, conditionparams);
        }
        /// <summary>
        /// Updates the database.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="parameters">List of keys and values to update. Only parameters with <c>function</c> set to <c>true</c> will be updated.</param>
        public void Update(string table, string condition, params OsSqlTypes.Parameter[] parameters)
        {
            if (parameters.Length == 0)
                OsSqlDebugger.Error("This function requires parameters.");
            string execQuery = "UPDATE `" + table + "`" + " SET ";
            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].Func)
                    execQuery += "`" + parameters[i].Key + "`=@" + parameters[i].Key + (i == parameters.Length - 1 ? "" : ", ");
            execQuery = execQuery + Condition(condition);
            Query(execQuery, parameters);
        }
        /// <summary>
        /// Updates the database.
        /// </summary>
        /// <param name="table">Table object.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="parameters">List of keys and values to update. Only parameters with <c>function</c> set to <c>true</c> will be updated.</param>
        public void Update(OsSqlTypes.Table table, string condition, params OsSqlTypes.Parameter[] parameters)
        {
            Update(table.ToString(), condition, parameters);
        }
        /// <summary>
        /// Returns the number of rows based on a condition.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        /// <returns>The count.</returns>
        public int Count(string table, string condition = "", params OsSqlTypes.Parameter[] conditionparams)
        {
            MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM `" + table + "`" + Condition(condition), Connection);
            foreach (var p in conditionparams)
                cmd.Parameters.AddWithValue(p.Key, p.Value);
            cmd.Prepare();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        /// <summary>
        /// Returns the number of rows based on a condition.
        /// </summary>
        /// <param name="table">Table object.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        /// <returns>The count.</returns>
        public int Count(OsSqlTypes.Table table, string condition = "", params OsSqlTypes.Parameter[] conditionparams)
        {
            return Count(table.ToString(), condition, conditionparams);
        }
        /// <summary>
        /// Adds a new entry to the table.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="parameters">List of keys and values to insert. Only parameters with <c>function</c> set to <c>true</c> will be inserted.</param>
        public int Insert(string table, params OsSqlTypes.Parameter[] parameters)
        {
            if (parameters.Length == 0)
                OsSqlDebugger.Error("This function requires parameters.");
            string execQuery = "INSERT INTO `" + table + "` (";
            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].Func)
                    execQuery += "`" + parameters[i].Key + "`" + (i == parameters.Length - 1 ? "" : ",");
            execQuery += ") VALUES (";
            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].Func)
                    execQuery += "@" + parameters[i].Key + (i == parameters.Length - 1 ? "" : ",");
            execQuery += "); SELECT last_insert_id()";
            MySqlCommand cmd = new MySqlCommand(execQuery, Connection);
            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.Key, p.Value);
            cmd.Prepare();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        /// <summary>
        /// Adds a new entry to the table.
        /// </summary>
        /// <param name="table">Table object.</param>
        /// <param name="parameters">List of keys and values to insert. Only parameters with <c>function</c> set to <c>true</c> will be inserted.</param>
        public int Insert(OsSqlTypes.Table table, params OsSqlTypes.Parameter[] parameters)
        {
            return Insert(table.ToString(), parameters);
        }
        /// <summary>
        /// Deletes a row from the table.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        public void Delete(string table, string condition, params OsSqlTypes.Parameter[] conditionparams)
        {
            Query("DELETE FROM `" + table + "`" + Condition(condition), conditionparams);
        }
        /// <summary>
        /// Deletes a row from the table.
        /// </summary>
        /// <param name="table">Table object.</param>
        /// <param name="condition">Condition code.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        public void Delete(OsSqlTypes.Table table, string condition, params OsSqlTypes.Parameter[] conditionparams)
        {
            Delete(table.ToString(), condition, conditionparams);
        }
        /// <summary>
        /// Checks if column exists in a table.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="column">Column name.</param>
        /// <returns>True if the column exists, false otherwise.</returns>
        public bool IsColumnExists(string table, string column)
        {
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '" + ConnectionDetails.Database +
                "' AND TABLE_NAME = '" + table + "' AND COLUMN_NAME = '" + column + "'", Connection);
            return cmd.ExecuteScalar() != null;
        }
        /// <summary>
        /// Adds a new column to a table.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="type">Column type.</param>
        /// <param name="column">Column name.</param>
        /// <param name="ai">Auto increment.</param>
        public void AddColumn(string table, OsSqlTypes.ColumnType type, string column, bool ai = false)
        {
            Query("ALTER TABLE " + table + " ADD " + column + " " + Builder.ColTypeSQLTitle(type) + (ai ? " NOT NULL AUTO_INCREMENT" : ""));
        }
        /// <summary>
        /// Deletes a column from a table.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="column">Column name.</param>
        public void DropColumn(string table, string column)
        {
            Query("ALTER TABLE " + table + " DROP " + column);
        }
        /// <summary>
        /// Updates the table columns in database to match the table that defined in the code.
        /// </summary>
        /// <param name="table">Table object.</param>
        /// <param name="sqlname">SQL table name, use empty string to set the same as the <c>table</c> name.</param>
        /// <param name="delete">Use true to delete any unrelevant columns found while updating. Note that this will delete both the columns and their rows.</param>
        public void UpdateColumns(OsSqlTypes.Table table, string sqlname = "", bool delete = false)
        {
            string TName = sqlname.Length == 0 ? table.ToString() : sqlname;
            List<string> TColumns = new List<string>();
            using (MySqlCommand command = Connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '" + (string.IsNullOrEmpty(table.DBName) ? ConnectionDetails.Database : table.DBName) + "' AND TABLE_NAME = N'" + table.ToString() + "'";
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        TColumns.Add(reader.GetString(3));
            }
            foreach (var c in table.Columns)
            {
                int index = TColumns.IndexOf(c.DbName);
                if (index != -1)
                    TColumns.RemoveAt(index);
                if (!IsColumnExists(TName, c.DbName))
                    AddColumn(TName, c.ColType, c.DbName);
            }
            if (delete && TColumns.Count > 0)
                foreach (var d in TColumns)
                    DropColumn(TName, d);
        }
        /// <summary>
        /// Updates the database tables columns to match the tables that defined in a specific structure.
        /// </summary>
        /// <param name="structure">Structure object.</param>
        /// <param name="updateTables">Use true if you want to also create any missing tables.</param>
        /// <param name="delete">Use true to delete any unrelevant columns found while updating. Note that this will delete both the columns and their rows.</param>
        public void UpdateStructure(OsSqlTypes.Structure structure, bool updateTables = false, bool delete = false)
        {
            if (updateTables)
                Query(Builder.GetCreationQuery(structure));
            foreach (var t in structure.Tables)
                UpdateColumns(t, t.Name, delete);
        }
        /// <summary>
        /// Auto-updates database rows.
        /// This means that any existing data within a <c>classPointer</c> will be updated in the database.
        /// </summary>
        /// <param name="classPointer">Pointer to an object that needs to be updated in the database</param>
        /// <param name="table">Table object that contains the columns that needs to be updated.</param>
        /// <param name="condition">Condition of the update.</param>
        /// <param name="name">Table name in database. Leave blank if it's the same as <c>table.Name</c>.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        /// <param name="skipnull">Use true to skip any property that is equal to null so the query won't contain it.</param>
        public void AutoUpdate(object classPointer, OsSqlTypes.Table table, string condition, string name = "", bool skipnull = false, params OsSqlTypes.Parameter[] conditionparams)
        {
            string t = name == string.Empty ? table.ToString() : name;
            List<OsSqlTypes.Parameter> uplist = Builder.ListAllClassValues(classPointer, table, t, skipnull);
            uplist.ForEach(i => i.Func = true);
            uplist.AddRange(conditionparams);
            Update(t, condition, uplist.ToArray());
        }
        /// <summary>
        /// Auto-inserts database row.
        /// This means that any existing data within a <c>classPointer</c> will be inserted to the database.
        /// </summary>
        /// <param name="classPointer">Pointer to an object that needs to be inserted to the database</param>
        /// <param name="table">Table object that contains the columns that needs to be inserted.</param>
        /// <param name="name">Table name in database. Leave blank if it's the same as <c>table.Name</c>.</param>
        /// <param name="skipnull">Use true to skip any property that is equal to null so the query won't contain it.</param>
        public int AutoInsert(object classPointer, OsSqlTypes.Table table, string name = "", bool skipnull = false)
        {
            string t = name == string.Empty ? table.ToString() : name;
            List<OsSqlTypes.Parameter> uplist = Builder.ListAllClassValues(classPointer, table, t, skipnull);
            uplist.ForEach(i => i.Func = true);
            var arr = uplist.ToArray();
            return Insert(t, arr);
        }
        /// <summary>
        /// Auto-reads from the database based on the synchronized table parameter.
        /// This is just a regular select statement, filtered only with fields that are both in the class and in the database.
        /// </summary>
        /// <param name="table">Table object that contains the columns that needs to be selected.</param>
        /// <param name="condition">Select condition.</param>
        /// <param name="name">Table name in database. Leave blank if it's the same as <c>table.Name</c>.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        public List<Dictionary<string, object>> AutoSelect(OsSqlTypes.Table table, string condition, string name = "", params OsSqlTypes.Parameter[] conditionparams)
        {
            return Select(name == string.Empty ? table.ToString() : name, condition, string.Join(",", table.Columns.Select(x => x.DbName)), conditionparams);
        }
        /// <summary>
        /// Auto-reads from the database based on the synchronized table parameter, then loads all of the data to a new constructed instance of the specified class type.
        /// Any custom proprety type in the code is skipped. Only properties defined in the table parameter gets set.
        /// </summary>
        /// <typeparam name="T">The type to create the instance from.</typeparam>
        /// <param name="table">Table object that contains the columns that needs to be loaded.</param>
        /// <param name="condition">Select condition.</param>
        /// <param name="data">Any data loaded from the database, including skipped columns.</param>
        /// <param name="name">Table name in database. Leave blank if it's the same as <c>table.Name</c>.</param>
        /// <param name="conditionparams">Parameters to pass with the condition.</param>
        /// <returns>The object with the loaded</returns>
        public T AutoLoad<T>(OsSqlTypes.Table table, string condition, out Dictionary<OsSqlTypes.Table.Column, object> data, string name = "", params OsSqlTypes.Parameter[] conditionparams)
        {
            data = new Dictionary<OsSqlTypes.Table.Column, object>();
            var selectedData = Read(name == string.Empty ? table.ToString() : name, condition, string.Join(",", table.Columns.Select(x => x.DbName)), conditionparams);
            object loadedData = null;
            Type instanceType = typeof(T);
            T instance = (T)Activator.CreateInstance(instanceType);
            foreach (var c in table.Columns)
            {
                data.Add(c, selectedData.Rows[0][c.DbName]);
                PropertyInfo prop = instanceType.GetProperty(c.CodeName);
                if (prop != null && prop.CanWrite && (loadedData = Builder.GetObject(selectedData.Rows[0][c.DbName], c.ColType, false, out bool success, prop.PropertyType)) != null)
                {
                    if (success)
                        prop.SetValue(instance, loadedData);
                    else
                        OsSqlDebugger.Message("Unable to load variable \"" + c.CodeName + "\" as its either not supported or the value has invalid syntax.");
                }
            }
            return instance;
        }
    }
    /// <summary>
    /// Builder class contains SQL query construction methods.
    /// </summary>
    public class Builder
    {
        /// <summary>
        /// Creates a list of <c>OsSqlTypes.Parameter</c> of all class properties in order to manipulate data (insert/update).
        /// </summary>
        /// <param name="classPointer">The source class object to get the property values from.</param>
        /// <param name="table">The table with the defined columns in order to tell which properties to get.</param>
        /// <param name="name">Table name in the database, leave blank for the name to be the same as the class name.</param>
        /// <param name="skipnull">Use true to skip properties that their values are null.</param>
        /// <returns>Returns array with all key and value parameters based on the class object properties.</returns>
        public static List<OsSqlTypes.Parameter> ListAllClassValues(object classPointer, OsSqlTypes.Table table, string name = "", bool skipnull = false)
        {
            var retList = new List<OsSqlTypes.Parameter>();
            string t = name == string.Empty ? classPointer.GetType().Name : name;
            PropertyInfo result = null;
            object val = null;
            foreach (var i in table.Columns)
            {
                result = classPointer.GetType().GetProperty(i.CodeName);
                if (result == null)
                    continue;
                val = result.GetValue(classPointer);
                if (skipnull && val == null)
                    continue;
                val = GetObject(val, i.ColType, true, out bool success);
                if (val == null || !success)
                    OsSqlDebugger.Error("Invalid object type. If you've tried to use your own class, please implement the IOsSqlElement interface.");
                else
                    retList.Add(new OsSqlTypes.Parameter(i.DbName, val));
            }
            return retList;
        }
        /// <summary>
        /// Gets an object value with its relevant type.
        /// </summary>
        /// <param name="content">Value content of the object.</param>
        /// <param name="type">Known type.</param>
        /// <param name="save">Use true to save value or false to load value.</param>
        /// <param name="success">Value saying if the conversion successed.</param>
        /// <param name="pt">Property type.</param>
        /// <returns>The object with its relevant type, or null if the type is not supported.</returns>
        internal static object GetObject(object content, OsSqlTypes.ColumnType? type, bool save, out bool success, Type pt = null)
        {
            if (content == null)
            {
                success = true;
                return DBNull.Value;
            }
            Type t = pt ?? content.GetType();
            success = false;
            if (t.IsArray)
                return null;
            if (typeof(IOsSqlElement).IsAssignableFrom(t))
            {
                type = OsSqlTypes.ColumnType.Element;
                if (save && (content as IOsSqlElement) == null)
                {
                    OsSqlDebugger.Error("Variable type \"" + t.Name + "\" passed as a null value.");
                    return null;
                }
            }
            else if (t.IsEnum)
                type = OsSqlTypes.ColumnType.Enum;
            else if (type == null)
                type = ColumnTypeFromObjectType(t);
            try
            {
                switch (type)
                {
                    case OsSqlTypes.ColumnType.BigInt:
                        {
                            success = long.TryParse(content.ToString(), out long val);
                            return val;
                        }
                    case OsSqlTypes.ColumnType.Decimal:
                        {
                            success = decimal.TryParse(content.ToString(), out decimal val);
                            return val;
                        }
                    case OsSqlTypes.ColumnType.Double:
                        {
                            success = double.TryParse(content.ToString(), out double val);
                            return val;
                        }
                    case OsSqlTypes.ColumnType.Element:
                        {
                            success = save;
                            if (save)
                                return (content as IOsSqlElement).GetID();
                            else
                                return Convert.ToInt32(content);
                        }
                    case OsSqlTypes.ColumnType.Enum:
                        {
                            success = true;
                            if (save)
                                return (int)content;
                            else
                                return Enum.ToObject(t, Convert.ToInt32(content));
                        }
                    case OsSqlTypes.ColumnType.Float:
                        {
                            success = float.TryParse(content.ToString(), out float val);
                            return val;
                        }
                    case OsSqlTypes.ColumnType.Int:
                        {
                            success = int.TryParse(content.ToString(), out int val);
                            return val;
                        }
                    case OsSqlTypes.ColumnType.LongText:
                    case OsSqlTypes.ColumnType.MediumText:
                    case OsSqlTypes.ColumnType.TinyText:
                    case OsSqlTypes.ColumnType.Text:
                    case OsSqlTypes.ColumnType.Varchar:
                        {
                            success = true;
                            return TextValue(Convert.ToString(content), !save);
                        }
                    case OsSqlTypes.ColumnType.Object:
                        {
                            success = true;
                            if (save)
                                return TextValue(JsonConvert.SerializeObject(content));
                            else
                                return JsonConvert.DeserializeObject(TextValue(content.ToString(), true), pt);
                        }
                    case OsSqlTypes.ColumnType.SmallInt:
                        {
                            success = short.TryParse(content.ToString(), out short val);
                            return val;
                        }
                    case OsSqlTypes.ColumnType.TinyInt:
                        {
                            success = sbyte.TryParse(content.ToString(), out sbyte val);
                            return val;
                        }
                    case OsSqlTypes.ColumnType.Boolean:
                        {
                            success = bool.TryParse(content.ToString(), out bool val);
                            return val;
                        }
                    case OsSqlTypes.ColumnType.DateTime:
                        {
                            success = true;
                            if (save)
                                return Timestamp.UnixTimeFromDateTime((DateTime)content);
                            else
                                return Timestamp.DateTimeFromUnixTime(Convert.ToInt32(content));
                        }
                }
            }
            catch
            {
                OsSqlDebugger.Error("Variable type \"" + t.Name + "\" conversion failed. It's either not supported by OsSql or passed as null or invalid value.");
                return null;
            }
            return null;
        }
        /// <summary>
        /// Replaces quotes with double-quotes.
        /// </summary>
        /// <param name="value">Text value.</param>
        /// <param name="reverse">True if the operation should be reversed, meaning double-quotes will become quotes.</param>
        /// <returns>The modified text value.</returns>
        private static string TextValue(string value, bool reverse = false)
        {
            return !reverse ? value.Replace("'", "''") : value.Replace("''", "'");
        }
        /// <summary>
        /// Generates a full database creation query based on a collection of tables.
        /// </summary>
        /// <param name="list">Collection of tables.</param>
        /// <returns>The creation query string.</returns>
        public static string GetCreationQuery(ICollection<OsSqlTypes.Table> list)
        {
            string createQuery = string.Empty, toEnd = string.Empty;
            foreach (var x in list)
            {
                createQuery += (createQuery.Length == 0 ? "" : Environment.NewLine) + "CREATE TABLE IF NOT EXISTS " + x.Name + " (";
                foreach (var i in x.Columns)
                {
                    createQuery += "`" + i.DbName + "` " + ColTypeSQLTitle(i.ColType) +
                        (i.AutoInc ? (" NOT NULL AUTO_INCREMENT") : ("")) + ", ";
                    if (i.AutoInc)
                        toEnd += "PRIMARY KEY (`" + i.DbName + "`)";
                }
                if (toEnd.Length > 0)
                    createQuery += toEnd;
                else
                    createQuery = createQuery.Remove(createQuery.Length - 2);
                createQuery += ");";
                toEnd = string.Empty;
            }
            return createQuery;
        }
        /// <summary>
        /// Gets the SQL column type name based on <c>ColumnType</c> element.
        /// </summary>
        /// <param name="type">The column type.</param>
        /// <returns>The title.</returns>
        internal static string ColTypeSQLTitle(OsSqlTypes.ColumnType type)
        {
            switch (type)
            {
                case OsSqlTypes.ColumnType.Element:
                case OsSqlTypes.ColumnType.Enum:
                case OsSqlTypes.ColumnType.DateTime:
                    return "INT";
                case OsSqlTypes.ColumnType.Object:
                    return "MEDIUMTEXT";
                default:
                    return type.ToString().ToUpper();
            }
        }
        /// <summary>
        /// Generates a full database creaton query based on a <c>Structure</c> object.
        /// </summary>
        /// <param name="structure">The <c>Structure</c> object.</param>
        /// <returns>The creation query string.</returns>
        public static string GetCreationQuery(OsSqlTypes.Structure structure)
        {
            return GetCreationQuery(structure.Tables);
        }
        /// <summary>
        /// Generates a full database creaton query based on specified tables.
        /// </summary>
        /// <param name="list">The tables.</param>
        /// <returns>The creation query.</returns>
        public static string GetCreationQuery(params OsSqlTypes.Table[] list)
        {
            return GetCreationQuery(list);
        }
        /// <summary>
        /// Gets a <c>ColumnType</c> of object by its type.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <returns>The relevant <c>ColumnType</c> (default is Int).</returns>
        internal static OsSqlTypes.ColumnType ColumnTypeFromObjectType(Type type)
        {
            OsSqlTypes.ColumnType ret = OsSqlTypes.ColumnType.Int;
            if (type.IsEnum)
                return OsSqlTypes.ColumnType.Enum;
            if (type is IOsSqlElement)
                return ret;
            switch (type.Name)
            {
                case "SByte":
                    ret = OsSqlTypes.ColumnType.TinyInt;
                    break;
                case "Byte":
                case "Int16":
                    ret = OsSqlTypes.ColumnType.SmallInt;
                    break;
                case "Int32":
                    ret = OsSqlTypes.ColumnType.Int;
                    break;
                case "Boolean":
                    ret = OsSqlTypes.ColumnType.Boolean;
                    break;
                case "Int64":
                    ret = OsSqlTypes.ColumnType.BigInt;
                    break;
                case "DateTime":
                    ret = OsSqlTypes.ColumnType.DateTime;
                    break;
                case "String":
                    ret = OsSqlTypes.ColumnType.Text;
                    break;
                case "Double":
                    ret = OsSqlTypes.ColumnType.Double;
                    break;
                case "Float":
                    ret = OsSqlTypes.ColumnType.Float;
                    break;
                case "Decimal":
                    ret = OsSqlTypes.ColumnType.Decimal;
                    break;
            }
            return ret;
        }
    }
    /// <summary>
    /// Timestamp class handles conversion between unix time and DateTime.
    /// </summary>
    internal class Timestamp
    {
        /// <summary>
        /// Converts a DateTime object to a unix time int value.
        /// </summary>
        /// <param name="datetime">DateTime object.</param>
        /// <returns>Returns int value that represents a unix time.</returns>
        internal static int UnixTimeFromDateTime(DateTime datetime)
        {
            return (int)Math.Truncate((datetime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
        }
        /// <summary>
        /// Converts a unix time to a DateTime object.
        /// </summary>
        /// <param name="unixtime">The unix time int value.</param>
        /// <returns>DateTime object.</returns>
        internal static DateTime DateTimeFromUnixTime(int unixtime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixtime).ToLocalTime();
        }
    }
    /* Irrelevant since I use Newtonsoft.Json.
    /// <summary>
    /// Json class handles conversion of JSON objects.
    /// </summary>
    internal class Json
    {
        internal static string Serialize(object obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            MemoryStream ms = new MemoryStream();
            serializer.WriteObject(ms, obj);
            string retVal = Encoding.Default.GetString(ms.ToArray());
            ms.Dispose();
            return retVal;
        }
        internal static object Deserialize(string json, Type type)
        {
            object obj = Activator.CreateInstance(type);
            MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(json));
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
            obj = serializer.ReadObject(ms);
            ms.Close();
            ms.Dispose();
            return obj;
        }
    }*/
}