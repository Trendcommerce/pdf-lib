using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using TC.Functions;
using TC.Resources.Fonts.LIB.Data;
using TC.Resources.Fonts.LIB.Data.FontDataDBTableAdapters;
using TC.Resources.Fonts.LIB.Interfaces;
using static TC.Constants.CoreConstants;

namespace TC.Resources.Fonts.LIB.Functions
{
    // Font-Functions (15.05.2023, SME)
    public static class FC_Fonts
    {
        #region Konstanten

        // Standard-Schriften-Ordner (07.06.2023, SME)
        public const string DefaultFontFolderPaths_Client = @"P:\_FONTS";
        public const string DefaultFontFolderPaths_Prod = @"";

        #endregion

        #region Get Font-Family

        // Get Font-Family (03.04.2023, SME)
        // Code-Snippet von https://stackoverflow.com/questions/24022411/loading-a-font-directly-from-a-file-in-c-sharp
        public static FontFamily GetFontFamily(string fontFilePath)
        {
            PrivateFontCollection fontCol = new PrivateFontCollection();
            fontCol.AddFontFile(fontFilePath);
            var fontFamily = fontCol.Families[0];
            return fontFamily;
        }

        // Code-Snippet von https://stackoverflow.com/questions/24022411/loading-a-font-directly-from-a-file-in-c-sharp
        //public static readonly PrivateFontCollection FontCollection = new PrivateFontCollection();
        //public static FontFamily AddToFontCollection(string path)
        //    => AddToFontCollection(File.ReadAllBytes(path));
        //public static FontFamily AddToFontCollection(byte[] fontBytes)
        //{
        //    var handle = System.Runtime.InteropServices.GCHandle.Alloc(fontBytes, System.Runtime.InteropServices.GCHandleType.Pinned);
        //    IntPtr pointer = handle.AddrOfPinnedObject();
        //    try
        //    {
        //        FontCollection.AddMemoryFont(pointer, fontBytes.Length);
        //    }
        //    finally
        //    {
        //        handle.Free();
        //    }
        //    return FontCollection.Families.LastOrDefault();
        //}

        #endregion

        #region Fonts-Data

        // Update Connection-String depending on Netzwerk-Typ (22.06.2023, SME)
        public static void UpdateConnectionString()
        {
            if (CoreFC.GetNetzwerkTyp() == Enums.NetzwerkTyp.ProdNetz)
            {
                Properties.Settings.Default["TC_Fonts_ConnectionString"] = Properties.Settings.Default.TC_Fonts_ConnectionString.Replace(CLIENT_Server, PROD_Server);
            }
        }

        // Get Fonts-DB-FullName (22.06.2023, SME)
        public static string GetFontsDBFullName()
        {
            string server = DataFC.GetDBServer(Properties.Settings.Default.TC_Fonts_ConnectionString);
            string db = DataFC.GetDatabase(Properties.Settings.Default.TC_Fonts_ConnectionString);
            return db + " @ " + server;
        }

        // Check Fonts-Connection (22.06.2023, SME)
        public static bool CheckFontsConnection(bool throwErrorBack = false)
        {
            return DataFC.CheckConnection(Properties.Settings.Default.TC_Fonts_ConnectionString, throwErrorBack);
        }

        // Get Fonts-Table (20.06.2023, SME)
        private static FontDataDB.FontsDataTable _FontsTable;
        public static FontDataDB.FontsDataTable GetFontsTable()
        {
            try
            {
                if (_FontsTable == null)
                {
                    var sw = Stopwatch.StartNew();
                    var fontsAdapter = new FontsTableAdapter();
                    var fontsTable = fontsAdapter.GetData();
                    fontsTable.AcceptChanges();
                    sw.Stop();
                    CoreFC.DPrint($"Retrieving Fonts-Table from DB took {sw.Elapsed.TotalSeconds} sec.");
                    _FontsTable = fontsTable;
                }
                return _FontsTable;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Font-Stream (20.06.2023, SME)
        public static byte[] GetFontStream(FontDataDB.FontsRow fontRow)
        {
            try
            {
                // exit-handling
                if (fontRow == null) return null;

                // use adapter
                using (var adapter = new FontsStreamTableAdapter())
                {
                    // use data
                    using (var data = adapter.GetData(fontRow.ID_Font))
                    {
                        var row = data.FirstOrDefault();
                        if (row == null) return null;
                        return row.FileStream;
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Fonts-Row anhand von Font-Objekt ermitteln (26.04.2023, SME)
        // CHANGE: 16.05.2023 by SME: ITcFont statt TcPdfFont
        // CHANGE: 17.05.2023 by SME: Parameter hinzugefügt => onlyOKFonts
        public static FontDataDB.FontsRow GetBestFontRow(FontDataDB.FontsDataTable fontTable, ITcFont fontToEmbed, bool onlyOKFonts = true)
        {
            try
            {
                // error-handling
                if (fontTable == null) return null;
                if (fontTable.Count == 0) return null;  
                if (fontToEmbed == null) return null;
                // if (fontToEmbed.IsEmbedded) throw new ArgumentOutOfRangeException(nameof(fontToEmbed)); // => DONT throw error when font is embedded, because maybe it wants to be replaced (25.05.2023, SME)

                // Font-Name setzen
                string fontName = fontToEmbed.FontName;

                // exit-handling
                if (fontName == "NULL") return null;

                // Liste der Rows zwischenspeichern
                // => nur nicht gelöschte und IsOK != false (20.06.2023, SME)
                var rows = fontTable.Where(r => r.IsDeletedOnNull() && (r.IsIsOKNull() || r.IsOK)).ToList();

                // Nur OK-Fonts, falls Flag gesetzt ist (17.05.2023, SME)
                if (onlyOKFonts) rows = rows.Where(r => !r.IsIsOKNull() && r.IsOK.Equals(true)).ToList();

                // Suchen in Font-Name
                var fontNameRows = rows.Where(x => x.FontName.Equals(fontName)).ToArray();
                if (fontNameRows.Length == 1) return fontNameRows.First();
                if (fontNameRows.Any(x => x.FontFileName.Equals(fontName))) return fontNameRows.First(x => x.FontFileName.Equals(fontName));
                if (fontNameRows.Any()) return fontNameRows.First();

                // Suchen in File-Name
                var fileNameRows = rows.Where(x => x.FontFileName.Equals(fontName)).ToArray();
                if (fileNameRows.Length == 1) return fileNameRows.First();
                if (fileNameRows.Any()) return fileNameRows.First();

                // Suchen in Font-Family
                var fontFamilyRows = rows.Where(x => x.FontFamily.Equals(fontName)).ToArray();
                if (fontFamilyRows.Length == 1) return fontFamilyRows.First();
                if (fontFamilyRows.Any()) return fontFamilyRows.First();

                // Suchen in Synonyms (10.05.2023, SME)
                var synonymRows = rows.Where(x => x.ContainsSynonym(fontName)).ToArray();
                if (synonymRows.Length == 1) return synonymRows.First();
                if (synonymRows.Any()) return synonymRows.First();

                // Nichts gefunden
                return null;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #region REMARKED

        // REMARKED (20.06.2023, SME)

        //// Global geladene Fonts-Daten (12.05.2023, SME)
        //private static List<FontData.FontsDataTable> _GlobalFontsData = new List<FontData.FontsDataTable>();
        //public static FontData.FontsDataTable[] GlobalFontsData
        //{
        //    get
        //    {
        //        return _GlobalFontsData.ToArray();
        //    }
        //}

        //// Fonts-Daten hinzufügen (12.05.2023, SME)
        //public static FontData.FontsDataTable AddFontsData(FontData.FontsDataTable fontsData)
        //{
        //    // exit-handling
        //    if (fontsData == null) return null;
        //    if (_GlobalFontsData.Contains(fontsData)) return fontsData;

        //    // Fonts-Daten der globalen Liste hinzufügen
        //    _GlobalFontsData.Add(fontsData);
        //    return fontsData;
        //}

        //// Fonts-Daten laden von Ordnerpfad (12.05.2023, SME)
        //public static FontData.FontsDataTable LoadFontsData(string fontsFolderPath, bool throwErrorOnEmptyString = true, bool throwErrorOnFolderNotFound = true)
        //{
        //    try
        //    {
        //        // error-handling
        //        if (string.IsNullOrEmpty(fontsFolderPath))
        //        {
        //            if (throwErrorOnEmptyString)
        //                throw new ArgumentNullException(nameof(fontsFolderPath));
        //            else
        //                return null;
        //        }
        //        if (!Directory.Exists(fontsFolderPath))
        //        {
        //            if (throwErrorOnFolderNotFound)
        //                throw new DirectoryNotFoundException("Der Schriften-Ordner wurde nicht gefunden, somit können die Schriften-Daten nicht geladen werden!" + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + fontsFolderPath);
        //            else
        //                return null;
        //        }

        //        // exit-handling
        //        if (_GlobalFontsData.Any(x => x.RootFolderPath.Equals(fontsFolderPath)))
        //            return _GlobalFontsData.First(x => x.RootFolderPath.Equals(fontsFolderPath));

        //        // Font-Daten einlesen + zur globalen Liste hinzufügen + zurückliefern
        //        return AddFontsData(new FontData.FontsDataTable(fontsFolderPath));
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFC.ThrowError(ex); throw ex;
        //    }
        //}

        #endregion

        #endregion

        #region Font-Styles + -Weights (ONLY TEMPORARY) (REMARKED)

        //// Font-Styles
        //private static readonly List<string> FontStyleList = new List<string>();
        //public static string[] FontStyles => FontStyleList.ToArray();
        //public static void AddFontStyle(string fontStyle)
        //{
        //    if (!string.IsNullOrEmpty(fontStyle) && !FontStyleList.Contains(fontStyle)) { FontStyleList.Add(fontStyle); }
        //}
        //public static void PrintFontStyles()
        //{
        //    CoreFC.DPrint("Font-Styles:");
        //    foreach (var fontStyle in FontStyleList.OrderBy(x => x))
        //    {
        //        CoreFC.DPrint("- " + fontStyle);
        //    }
        //}

        //// Font-Weights
        //private static readonly List<string> FontWeightList = new List<string>();
        //public static string[] FontWeights => FontWeightList.ToArray();
        //public static void AddFontWeight(string fontWeight)
        //{
        //    if (!string.IsNullOrEmpty(fontWeight) && !FontWeightList.Contains(fontWeight)) { FontWeightList.Add(fontWeight); }
        //}
        //public static void PrintFontWeights()
        //{
        //    CoreFC.DPrint("Font-Weights:");
        //    foreach (var fontWeight in FontWeightList.OrderBy(x => x))
        //    {
        //        CoreFC.DPrint("- " + fontWeight);
        //    }
        //}

        #endregion
    }
}
