using iText.Kernel.Font;
using iText.Kernel.Pdf;
using System.Collections.Generic;
using TC.Resources.Fonts.LIB.Data;
using TC.PDF.LIB.Classes.Results;
using TC.Classes;

namespace TC.PDF.LIB.Classes.Parameters
{
    // Parameter-Klasse für Embed-Fonts (24.05.2023, SME)
    public class EmbedFontsParameters
    {
        // Neue Instanz
        public EmbedFontsParameters
        (
            FontDataDB.FontsDataTable fontTable,
            Dictionary<TcPdfFont, PdfFont> newFontList,
            PdfDocument targetPdf,
            PdfFileEmbedFontsResult result,
            bool onlyOKFonts,
            List<PdfObject> doneList,
            ProgressInfo progressInfo = null,
            bool loopThrouAllObjects = false,
            FontDataDB.FontsRow fontRow = null,
            TcPdfFont[] fonts = null
        ) 
        {
            FontsTable = fontTable;
            NewFontList = newFontList;
            TargetPdf = targetPdf;
            Result = result;
            OnlyOKFonts = onlyOKFonts;
            DoneList = doneList;
            ProgressInfo = progressInfo;
            LoopThrouAllObjects = loopThrouAllObjects;
            FontRow = fontRow;
            Fonts = fonts;
        }

        // Properties
        public readonly FontDataDB.FontsDataTable FontsTable;
        public readonly Dictionary<TcPdfFont, PdfFont> NewFontList;
        public readonly PdfDocument TargetPdf;
        public readonly PdfFileEmbedFontsResult Result;
        public readonly bool OnlyOKFonts;
        public readonly List<PdfObject> DoneList;
        public readonly ProgressInfo ProgressInfo;
        public readonly bool LoopThrouAllObjects;
        public readonly FontDataDB.FontsRow FontRow;
        public readonly TcPdfFont[] Fonts;
    }
}
