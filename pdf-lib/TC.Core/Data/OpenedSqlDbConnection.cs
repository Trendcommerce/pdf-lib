using System;
using System.Data;
using System.Data.SqlClient;

namespace TC.Data
{
     // Opened SQL-DB-Connection (11.11.2022, SME)
     // => will be used to open connection at start and disconnect on dispose, but only if it hasn't been connected already at the start
     public class OpenedSqlDbConnection : OpenedConnection
     {
          // Important Variables + Properties
          public readonly SqlDbConnection SqlDbConnection;

          // New Instance (11.11.2022, SME)
          public OpenedSqlDbConnection(SqlDbConnection connection): base(connection.Connection)
          {
               // error-handling
               if (connection == null) throw new ArgumentNullException(nameof(connection));

               // set local properties
               SqlDbConnection = connection;
          }
     }
}
