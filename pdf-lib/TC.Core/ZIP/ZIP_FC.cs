using System;
using System.IO;
using System.IO.Compression;
using TC.Functions;

namespace TC.ZIP
{
    public static class ZIP_FC
    {

        // Datei komprimieren (09.05.2023, SME)
        public static void CompressFile(string filePath, string zipFilePath)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
                if (!File.Exists(filePath)) throw new FileNotFoundException("Zu komprimierende Datei wurde nicht gefunden.", filePath);
                if (string.IsNullOrEmpty(zipFilePath)) throw new ArgumentNullException(nameof(zipFilePath));
                if (File.Exists(zipFilePath)) throw new Exception("ZIP-Datei existiert bereits!" + CoreFC.Lines(2) + "Pfad:" + Environment.NewLine + zipFilePath);

                // use file-stream
                using (var fs = new FileStream(zipFilePath, FileMode.CreateNew))
                {
                    // use zip-archive
                    using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                    {
                        // create zip-entry from file
                        zip.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
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
