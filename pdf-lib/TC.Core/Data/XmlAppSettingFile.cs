using TC.Functions;

namespace TC.Data
{
    // XML-App-Setting-File (05.07.2023, SME)
    public class XmlAppSettingFile: XmlSettingFile
    {
        #region OVERRIDES

        public override string DefaultFileType => ".AppSettings.xml";
        protected override string DataSetName => "AppSettings";
        protected override string DefaultFileName => CoreFC.GetEntryAssemblyFileNameWoExtension() + DefaultFileType;
        protected override string DefaultFolderPath => CoreFC.GetEntryAssemblyFolderPath();

        #endregion
    }
}
