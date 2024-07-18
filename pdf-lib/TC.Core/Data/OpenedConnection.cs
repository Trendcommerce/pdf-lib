using System;
using System.Data;
using System.Data.SqlClient;
using TC.Functions;

namespace TC.Data
{
     // Opened SQL-Connection (13.11.2022, SME)
     // => will be used to open connection at start and disconnect on dispose, but only if it hasn't been connected already at the start
     public class OpenedConnection : IDisposable
     {
          #region IMPORTANT

          /*
           * ALL code in this class MUST BE in sync between TC.Core + TC_SQL_CLR !!!
           * => when ever a change is made, it must be applied in both code (13.11.2022, SRM)
           */

          #endregion

          // Important Variables + Properties
          public readonly SqlConnection Connection;
          public readonly ConnectionState ConnectionStateAtStart;

          // New Instance (13.11.2022, SME)
          public OpenedConnection(SqlConnection connection)
          {
               // error-handling
               if (connection == null) throw new ArgumentNullException(nameof(connection));

               // set local properties
               Connection = connection;
               ConnectionStateAtStart = connection.State;

               // connect
               DataFC.Connect(connection);
          }

          // Dispose
          public void Dispose()
          {
               // Disconnect if necessary
               if (ConnectionStateAtStart == ConnectionState.Closed)
                    Connection.Close();
          }
     }
}
