using System;
using System.Data;
using TC.Enums;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TC.Functions
{
     public static class TestFC
     {
          // Binary-Test-Value
          private static byte[] _TestValueBinary;
          public static byte[] TestValueBinary
          {
               get
               {
                    if (_TestValueBinary == null)
                    {
                         try
                         {
                              List<byte> bytes = new List<byte>();
                              for (byte i = 0; i <= byte.MaxValue; i++)
                              {
                                   bytes.Add(i);
                                   if (i == byte.MaxValue) break;
                              }
                              _TestValueBinary = bytes.ToArray();
                         }
                         catch (Exception ex)
                         {
                              CoreFC.ThrowError(ex); throw ex;
                         }
                    }
                    return _TestValueBinary;
               }

          }
          
          // get test-value according to data-type of column (31.10.2022, SRM)
          public static object GetTestValue(DataColumn column)
          {
               try
               {
                    var valueColumnEnum = DataFC.GetValueColumnEnum(column.DataType, column.MaxLength);
                    if (!valueColumnEnum.HasValue)
                    {
                         throw new Exception("Unhandled Data-Type: " + column.DataType.ToString());
                    }
                    else
                    {
                         switch (valueColumnEnum.Value)
                         {
                              case ValueColumnEnum.BooleanValue:
                                   return RandomFC.GetRandomNumber(0, 9) % 2 == 0;
                              case ValueColumnEnum.IntValue:
                                   return (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
                              case ValueColumnEnum.BigIntValue:
                                   return DateTime.Now.Ticks;
                              case ValueColumnEnum.DecimalValue:
                                   return DateTime.Now.TimeOfDay.TotalSeconds;
                              case ValueColumnEnum.DateTimeValue:
                                   return DateTime.Now;
                              case ValueColumnEnum.GuidValue:
                                   return Guid.NewGuid();
                              case ValueColumnEnum.ShortTextValue:
                                   return "Test on " + DateTime.Now.ToString();
                              case ValueColumnEnum.LongTextValue:
                                   return "Test with veeery long text on " + DateTime.Now.ToString();
                              case ValueColumnEnum.BinaryValue:
                                   return TestValueBinary;
                              default:
                                   throw new Exception("Unhandled Value-Column-Enum: " + valueColumnEnum.Value.ToString());
                         }
                    }
               }
               catch (Exception ex)
               {
                    CoreFC.ThrowError(ex); throw ex;
               }
          }

          // Set Test-Values in Row (17.11.2022, SRM)
          public static void SetTestValues(DataRow row)
          {
               try
               {
                    // exit-handling
                    if (row == null) return;

                    // loop throu columns to set test-value
                    foreach (DataColumn column in row.Table.Columns)
                    {
                         try
                         {
                              if (column.AutoIncrement)
                                   continue;
                              else if (!column.AllowDBNull)
                                   row[column] = GetTestValue(column);
                              else if (RandomFC.GetRandomNumber(0, 9) > 2)
                                   row[column] = GetTestValue(column);
                         }
                         catch (Exception ex)
                         {
                              CoreFC.ThrowError(ex); throw ex;
                         }
                    }
               }
               catch (Exception ex)
               {
                    CoreFC.ThrowError(ex); throw ex;
               }
          }

          // Add Test-Rows to Table (14.11.2022, SRM)
          public static void AddTestRows(DataTable table, int count)
          {
               try
               {
                    // exit-handling
                    if (table == null) return;
                    if (count <= 0) return;

                    // loop
                    for (int i = 1; i <= count; i++)
                    {
                         try
                         {
                              var row = table.NewRow();
                              SetTestValues(row);
                              table.Rows.Add(row);
                         }
                         catch (Exception ex)
                         {
                              CoreFC.ThrowError(ex); throw ex;
                         }
                    }
               }
               catch (Exception ex)
               {
                    CoreFC.ThrowError(ex); throw ex;
               }
          }
     }
}
