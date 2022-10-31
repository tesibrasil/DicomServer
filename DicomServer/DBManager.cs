using System;
using System.Data;
using System.Data.Odbc;

namespace DicomServer
{
    public enum CommandType { SELECT, UPDATE, INSERT, UNKNOWN };
    enum ExectuionType { READER, SCALAR, NONQUERY, UNKNOWN };
    
    public partial class DBManager : IDisposable
    {
        protected OdbcConnection Connection;
        protected CommandType CType;
        protected string[] Fields;
        protected string Table;
        protected string Condition;
        protected int FromTop;
        protected bool Asc;
        protected string OrderBy;


        /// <summary>
        /// Sets the connection string and open the ODBC Connection
        /// If the connection fails Database will automatically be disposed
        /// </summary>
        /// <param name="connStr">ODBC Connection String</param>
        public DBManager(string connStr)
        {
            try
            {
                Connection = new OdbcConnection(connStr);
                Connection.Open();
            }
            catch
            {
                Dispose();
            }
        }
        public void Dispose()
        {
            if (Connection != null)
            {
                if (Connection.State != ConnectionState.Closed)
                    Connection.Close();

                Connection.Dispose();
                Connection = null;
            }
            GC.SuppressFinalize(this);
        }

        private string CreateCommandText()
        {
            string text = string.Empty;

            if (string.IsNullOrEmpty(Table)) return null;

            if (CType == CommandType.SELECT)
            {                
                text = $"SELECT {FieldsArrayAsString()} FROM {Table}"
                    + (!string.IsNullOrEmpty(Condition) ? $" WHERE {Condition}" : string.Empty)
                    + (!string.IsNullOrEmpty(OrderBy) ? $" ORDER BY {OrderBy}" + (Asc ? "ASC" : "DESC") : string.Empty);
                ResetParameters();                
            }

            if (CType == CommandType.UPDATE)
            {
                text = $"UPDATE {Table} SET {FieldsArrayAsString()}" + (!string.IsNullOrEmpty(Condition) ? $" WHERE {Condition}" : string.Empty);
                ResetParameters();
            }

            return text;

            string FieldsArrayAsString()
            {
                if (Fields.Length == 0) return "*";

                string ret = string.Empty;
                foreach (string f in Fields)
                {
                    ret += $",{f}";
                }
                return ret.Substring(1);
            }

            void ResetParameters()
            {
                CType = CommandType.UNKNOWN;
                Fields = null;
                Table = string.Empty;
                Condition = string.Empty;
                FromTop = 0;
                Asc = true;
                OrderBy = string.Empty;
            }
        }

        #region Commands
        public DBManager Select(params string[] fields)
        {
            Fields = fields;
            CType = CommandType.SELECT;
            return this;
        }
        public DBManager From(string table)
        {
            Table = table;
            return this;
        }
        
        public DBManager Update(string table)
        {
            Table = table;
            CType = CommandType.UPDATE;
            return this;
        }
        public DBManager Set(params string[] updatedFields)
        {
            Fields = updatedFields;
            return this;
        }

        public DBManager Where(string condition)
        {
            Condition = condition;
            return this;
        }
        public DBManager Top(int value, string orderby = null, bool asc = true)
        {
            FromTop = value;
            OrderBy = orderby;
            Asc = asc;
            return this;
        }
        #endregion

        public OdbcDataReader ExecuteReader()
        {
            return (OdbcDataReader)Execute(ExectuionType.READER);
        }
        public object ExecuteScalar()
        {
            return Execute(ExectuionType.SCALAR);
        }
        public int ExecuteNonQuery()
        {
            return (int)Execute(ExectuionType.NONQUERY);
        }
        private object Execute(ExectuionType type)
        {
            try
            {
                using (OdbcCommand cmd = new OdbcCommand())
                {
                    cmd.Connection = this.Connection;
                    cmd.CommandText = CreateCommandText();
                    
                    switch(type)
                    {
                        case ExectuionType.READER: return cmd.ExecuteReader();
                        case ExectuionType.SCALAR: return cmd.ExecuteScalar();
                        case ExectuionType.NONQUERY: return cmd.ExecuteNonQuery();
                        default: return null;
                    }
                };
            }
            catch (Exception ex)
            {
                Dispose();
                return null;
            }
        }

 
    }
    public partial class DBManager
    {        
        public string GetDriver()
        {
            return Connection.Driver;
        }

        public string GetDataSource()
        {
            return Connection.DataSource;
        }

        public string GetDatabase()
        {
            return Connection.Database;
        }

        public OdbcConnection GetConnection()
        {
            return Connection;
        }

        public string GetConnectionString()
        {
            return Connection.ConnectionString;
        }
    }
}
