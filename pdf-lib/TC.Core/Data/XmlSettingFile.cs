using System;
using System.Linq;
using TC.Attributes;
using TC.Functions;

namespace TC.Data
{
    // XML-Setting-File (07.12.2022, SME)
    public class XmlSettingFile : XmlDataFile<XmlSettingFile.TableEnum>
    {
        #region General

        // New empy Instance
        public XmlSettingFile() : base() { }

        // New Instance from Filepath
        public XmlSettingFile(string filePath) : base(filePath) { }

        #endregion

        #region Enumerations

        #region Schema-Enumerations

        // Config-Tables
        public enum TableEnum
        {
            [TableEnumProperties(typeof(SettingsColumnNames), "SettingName")]
            Settings
        }

        // Column-Names of Settings
        public enum SettingsColumnNames
        {
            [ColumnProperties(typeof(string), true, true, true)]
            SettingName,
            [ColumnProperties(typeof(string))]
            SettingValue
        }

        #endregion

        #endregion

        #region Setting-Handling

        // Get Setting-Value
        public string GetSettingValue(string settingName, string alternativeValue = "")
        {
            // set filter
            var filter = SettingsColumnNames.SettingName.ToString() + " = " + DataFC.GetSqlString(settingName);
            // get row
            var row = GetRows(TableEnum.Settings, filter).FirstOrDefault();
            if (row == null) return alternativeValue;
            // get value
            var value = row[SettingsColumnNames.SettingValue.ToString()].ToString();
            if (string.IsNullOrEmpty(value)) return alternativeValue;
            return value;
        }

        // Set Setting-Value
        public void SetSettingValue(string settingName, string value, bool saveImmediately = false)
        {
            // exit-handling
            if (GetSettingValue(settingName) == value) return;

            // get table
            var table = GetTable(TableEnum.Settings);
            if (table == null) throw new Exception("Einstellungs-Tabelle nicht gefunden");
            // set filter
            var filter = SettingsColumnNames.SettingName.ToString() + " = " + DataFC.GetSqlString(settingName);
            // get row
            var row = table.Select(filter).FirstOrDefault();
            if (row != null)
            {
                row[SettingsColumnNames.SettingValue.ToString()] = value;
            }
            else
            {
                row = table.NewRow();
                row[SettingsColumnNames.SettingName.ToString()] = settingName;
                row[SettingsColumnNames.SettingValue.ToString()] = value;
                table.Rows.Add(row);
            }
            // save
            if (saveImmediately) Save();
        }

        #endregion
    }

    // XML-Setting-File of Setting-Enum (07.12.2022, SME)
    public class XmlSettingFile<TSettingEnum> : XmlSettingFile where TSettingEnum : struct
    {
        #region General

        // New empy Instance
        public XmlSettingFile() : base() { }

        // New Instance from Filepath
        public XmlSettingFile(string filePath) : base(filePath) { }

        #endregion

        #region Setting-Handling

        // Get Setting-Value by Enum
        public string GetSettingValue(TSettingEnum settingEnum, string alternativeValue = "") => GetSettingValue(settingEnum.ToString(), alternativeValue);

        // Set Setting-Value by Enum
        public void SetSettingValue(TSettingEnum setting, string value, bool saveImmediately = true) => SetSettingValue(setting.ToString(), value, saveImmediately);

        #endregion
    }
}
