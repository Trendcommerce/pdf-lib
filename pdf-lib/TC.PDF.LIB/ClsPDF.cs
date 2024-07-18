using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using TC.Classes;
using TC.Constants;
using TC.Functions;
using static TC.Constants.CoreConstants;
using static TC.PDF.LIB.CONST_PDF;

namespace TC.PDF.LIB
{
    public static class ClsPDF
    {
        #region Variables

        private static readonly int whitespace2mm_inPoints = int.Parse(Math.Floor(double.Parse(MillimetersToPoints(2).ToString())).ToString());

        private const string MailpieceVorlauf = "0000000000";
        private const string MailpieceNachlauf = "9999999999";
        private const string Sammeln = "2";
        private const string Verpacken = "1";
        private const string DMX1gelocht = "000201";
        private const string DMX1ungelocht = "000001";
        private const string KeineBeilagen = "000";
        private const string FUELLDMX3 = "99999999991001001000"; // Mailpiece, Verpacken, Blatt von Blatt, keine Beilagen
        private const string FUELLseite_pdf = "FUELLseite.pdf";
        private const string TRENNseite_pdf = "TRENNseite.pdf";
        private const string HelveticaTTF = "HelveticaNeue.ttf";
        private const string Versetzer_jpg = "Versetzer.jpg";
        private const string regexSDLsolErstesBlatt = @"\-\d*\/\d*\-1\/\d*\-\d{5}";
        private const string backslashdash = @"\-";
        private const string yyyyMMdd_HHmmss_fff = "yyyyMMdd_HHmmss_fff";
        private const char charComma = ',';
        private const int maxWaitIteration = 5;
        private const float mmToPoints = 2.8346456693F;

        #endregion Variables

        #region Enumerations

        public enum DMXArt
        {
            DMX1,
            DMX3,
            VAD,
            Verpackung
        }

        public enum InsertPageType
        {
            FUELL,
            TRENN
        }

        #endregion

        #region Classes

        public class InsertPageInfo
        {
            public string Mailpiece;
            public int Seite;
            public InsertPageInfo(string Mailpiece, int Seite)
            {
                this.Mailpiece = Mailpiece;
                this.Seite = Seite;
            }
        }

        #endregion

        #region Methods (OK)

        public static float MillimetersToPoints(double mm)
        {
            return float.Parse(mm.ToString()) * mmToPoints;
        }

        public static int PointsToMillimeters(float point)
        {
            return int.Parse(Math.Round(point / mmToPoints).ToString());
        }

        private static iText.Kernel.Font.PdfFont CreateFont(string APTVorlagen)
        {
            iText.Kernel.Font.PdfFont font = iText.Kernel.Font.PdfFontFactory.CreateFont(Path.Combine(APTVorlagen, HelveticaTTF), iText.Kernel.Font.PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
            font.SetSubset(true);
            return font;
        }

        // Add DMX
        // CHANGE: 04.03.2024 by SME: public => private
        public static void AddDMX(iText.Kernel.Pdf.PdfDocument pdfDocument, string dmxValue, int page, DMXArt dMXArt, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            AddDMX(pdfDocument, new() { { dmxValue, new() { page } } }, dMXArt, callerFilePath, callerLineNumber, callerMemberName);
        }

        private static void AddDMX(iText.Kernel.Pdf.PdfDocument pdfDocument, Dictionary<string, HashSet<int>> dicDMXSeiten, DMXArt dMXArt, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            int mmVomRand = 4;
            float dmxPosX = 0, dmxPosY = 0, moduleSize = 0;

            switch (dMXArt)
            {
                case DMXArt.DMX1:
                    dmxPosX = MillimetersToPoints(mmVomRand) - whitespace2mm_inPoints;
                    dmxPosY = (MillimetersToPoints(154) + whitespace2mm_inPoints - pdfDocument.GetPage(1).GetPageSize().GetHeight()) * -1;
                    moduleSize = MillimetersToPoints(0.5);
                    break;
                case DMXArt.DMX3:
                    dmxPosX = MillimetersToPoints(18) - whitespace2mm_inPoints;
                    dmxPosY = (MillimetersToPoints(292) + whitespace2mm_inPoints - pdfDocument.GetPage(1).GetPageSize().GetHeight()) * -1;
                    moduleSize = MillimetersToPoints(0.4);
                    break;
                case DMXArt.VAD:
                    dmxPosX = MillimetersToPoints(28) - whitespace2mm_inPoints;
                    dmxPosY = (MillimetersToPoints(290) + whitespace2mm_inPoints - pdfDocument.GetPage(1).GetPageSize().GetHeight()) * -1;
                    moduleSize = MillimetersToPoints(0.6);
                    break;
                case DMXArt.Verpackung:
                    dmxPosX = MillimetersToPoints(102) - whitespace2mm_inPoints;
                    dmxPosY = (MillimetersToPoints(290) + whitespace2mm_inPoints - pdfDocument.GetPage(1).GetPageSize().GetHeight()) * -1;
                    moduleSize = MillimetersToPoints(0.6);
                    break;
            }

            foreach (string dmxValue in dicDMXSeiten.Keys)
            {
                iText.Barcodes.BarcodeDataMatrix dataMatrix = new();
                dataMatrix.SetWs(whitespace2mm_inPoints);
                dataMatrix.SetCode(dmxValue);
                iText.Kernel.Pdf.Xobject.PdfFormXObject xDMX = dataMatrix.CreateFormXObject(iText.Kernel.Colors.ColorConstants.BLACK, moduleSize, pdfDocument);
                xDMX.SetBBox(new iText.Kernel.Pdf.PdfArray(new iText.Kernel.Geom.Rectangle(0, 0, xDMX.GetWidth() * moduleSize, xDMX.GetHeight() * moduleSize)));

                foreach (int p in dicDMXSeiten[dmxValue])
                {
                    iText.Kernel.Pdf.Canvas.PdfCanvas pdfCanvas = new(pdfDocument.GetPage(p).NewContentStreamAfter(), pdfDocument.GetPage(p).GetResources(), pdfDocument);
                    pdfCanvas.SaveState();
                    pdfCanvas.SetFillColor(iText.Kernel.Colors.ColorConstants.WHITE);
                    pdfCanvas.Rectangle(dmxPosX, dmxPosY, xDMX.GetWidth(), xDMX.GetHeight());
                    pdfCanvas.Fill();
                    pdfCanvas.RestoreState();
                    pdfCanvas.AddXObjectAt(xDMX, dmxPosX, dmxPosY);

                    if (dMXArt.Equals(DMXArt.DMX1))
                    {
                        float dmxPosX2 = pdfDocument.GetPage(p).GetPageSize().GetWidth() - xDMX.GetWidth() - MillimetersToPoints(mmVomRand) + whitespace2mm_inPoints;
                        iText.Kernel.Pdf.Canvas.PdfCanvas rechts = new(pdfDocument.GetPage(p).NewContentStreamAfter(), pdfDocument.GetPage(p).GetResources(), pdfDocument);
                        rechts.SaveState();
                        rechts.SetFillColor(iText.Kernel.Colors.ColorConstants.WHITE);
                        rechts.Rectangle(dmxPosX2, dmxPosY, xDMX.GetWidth(), xDMX.GetHeight());
                        rechts.Fill();
                        rechts.RestoreState();
                        rechts.AddXObjectAt(xDMX, dmxPosX2, dmxPosY);
                    }
                }
            }
            GC.Collect();
        }

        private static void CheckSDLOnPage(iText.Kernel.Pdf.PdfDocument pdfDocument, InsertPageInfo insertPageInfo, string JobID, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            string _sdl = JobID + backslashdash + insertPageInfo.Mailpiece + regexSDLsolErstesBlatt;
            string _text = ReaderExtensions.ExtractText(pdfDocument.GetPage(insertPageInfo.Seite), pdfDocument.GetPage(insertPageInfo.Seite).GetPageSize());
            // Ist dies das erste Blatt mit der entsprechenden SDL?
            while (!Regex.IsMatch(_text, _sdl) && insertPageInfo.Seite >= 0)
            {
                insertPageInfo.Seite--;
                _text = ReaderExtensions.ExtractText(pdfDocument.GetPage(insertPageInfo.Seite), pdfDocument.GetPage(insertPageInfo.Seite).GetPageSize());
            }
        }

        /// <summary>
        /// Erstellt die Vorlauf-/Nachlaufseiten
        /// </summary>
        public static string CreateVorNachlauf(string APTDirectory, string APTVorlagen, bool istVorlauf, string pdfJobName, string JobID, string Vorlage, int anzahlSeiten, bool istDuplex, bool istGelocht, string Letzte10StellenDMX3, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            string VorNachlauf = istVorlauf ? VORLAUF : NACHLAUF;
            string outputFile = Path.Combine(APTDirectory, VorNachlauf + DateTime.Now.ToString(yyyyMMdd_HHmmss_fff) + DotPDF);
            pdfJobName = Path.GetFileNameWithoutExtension(pdfJobName);
            Dictionary<string, HashSet<int>> dDMX3 = new();
            int anzLeer;

            #region DMX3 Values
            if (istVorlauf)
            {
                if (istDuplex)
                {
                    // 8 Vorlaufblätter
                    anzLeer = 16;
                    dDMX3.Add(JobID + MailpieceVorlauf + Sammeln + "001002" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 1, 5, 9, 13 });
                    dDMX3.Add(JobID + MailpieceVorlauf + Verpacken + "002002" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 3, 7, 11, 15 });
                }
                else
                {
                    // 8 Vorlaufblätter
                    anzLeer = 8;
                    dDMX3.Add(JobID + MailpieceVorlauf + Sammeln + "001002" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 1, 3, 5, 7 });
                    dDMX3.Add(JobID + MailpieceVorlauf + Verpacken + "002002" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 2, 4, 6, 8 });
                }
            }
            else
            {
                if (istDuplex)
                {
                    // 4 Nachlaufblätter
                    anzLeer = 8;
                    if (anzahlSeiten % 4 == 0)
                    {
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "001004" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 1 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "002004" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 3 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "003004" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 5 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Verpacken + "004004" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 7 });
                    }
                    else
                    {
                        anzLeer += 2;
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "001005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 1 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "002005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 3 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "003005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 5 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "004005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 7 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Verpacken + "005005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 9 });
                    }
                }
                else
                {
                    anzLeer = 4;
                    if (anzahlSeiten % 2 == 0)
                    {
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "001004" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 1 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "002004" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 2 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "003004" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 3 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Verpacken + "004004" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 4 });
                    }
                    else
                    {
                        anzLeer += 1;
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "001005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 1 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "002005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 2 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "003005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 3 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Sammeln + "004005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 4 });
                        dDMX3.Add(JobID + MailpieceNachlauf + Verpacken + "005005" + KeineBeilagen + Letzte10StellenDMX3.Substring(3), new HashSet<int>() { 5 });
                    }
                }
            }
            #endregion DMX3 Values

            string LeerPDF = Path.Combine(APTDirectory, DateTime.Now.ToString(yyyyMMdd_HHmmss_fff) + "_Leer.pdf");
            CoreFC.TryCopyFile(Vorlage, LeerPDF, true, maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
            FileInfo fileInfo = new(LeerPDF);
            if (fileInfo.Exists) { fileInfo.IsReadOnly = false; }
            using PdfDocWithWriter pwLeer = new(Vorlage, LeerPDF);

            if (istGelocht)
            {
                AddDMX(pwLeer, DMX1gelocht, 1, DMXArt.DMX1, callerFilePath, callerLineNumber, callerMemberName);
            }
            else
            {
                AddDMX(pwLeer, DMX1ungelocht, 1, DMXArt.DMX1, callerFilePath, callerLineNumber, callerMemberName);
            }
            

            pwLeer.Close();

            using iText.Kernel.Pdf.PdfDocument pdfLeer = new(new iText.Kernel.Pdf.PdfReader(LeerPDF));
            iText.Kernel.Font.PdfFont pDFFontHelvetica = CreateFont(APTVorlagen); ;
            iText.Layout.Element.Text textVorNachlauf = new iText.Layout.Element.Text(VorNachlauf).SetFont(pDFFontHelvetica).SetFontSize(72).SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY);
            iText.Layout.Element.Paragraph paragraphVorNachlauf = new iText.Layout.Element.Paragraph(textVorNachlauf);
            iText.Layout.Element.Text textJobname = new iText.Layout.Element.Text(pdfJobName).SetFont(pDFFontHelvetica).SetFontSize(20);
            iText.Layout.Element.Paragraph paragraphJobname = new iText.Layout.Element.Paragraph(textJobname);
            iText.Layout.Element.Text textSOL = new iText.Layout.Element.Text(JobID + DotSOL).SetFont(pDFFontHelvetica).SetFontSize(20);
            iText.Layout.Element.Paragraph paragraphSOL = new(textSOL);

            //using (iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfWriter(outputFile, new iText.Kernel.Pdf.WriterProperties().UseSmartMode().SetFullCompressionMode(true))))
            using (PdfDocWithWriter pdfDocument = new(outputFile, true))
            {
                iText.Kernel.Utils.PdfMerger pdfMerger = new(pdfDocument);
                using iText.Layout.Document document = new(pdfDocument);
                float pdfWidthInPoints = pdfLeer.GetPage(1).GetPageSize().GetWidth();
                float pdfHeightInPoints = pdfLeer.GetPage(1).GetPageSize().GetHeight();
                float x = pdfLeer.GetPage(1).GetPageSize().GetWidth() / 2;
                string VersetzerJPG = Path.Combine(APTVorlagen, Versetzer_jpg);

                for (int i = 1; i <= anzLeer; i++)
                {
                    pdfMerger.Merge(pdfLeer, 1, 1);

                    if (!istDuplex || i % 2 == 1)
                    {
                        document.ShowTextAligned(paragraphVorNachlauf, x, MillimetersToPoints(235), i, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 0);
                        document.ShowTextAligned(paragraphJobname, x, MillimetersToPoints(80), i, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 0);
                        document.ShowTextAligned(paragraphSOL, x, MillimetersToPoints(12), i, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 0);
                    }

                    if (i == 1 && istVorlauf)
                    {
                        iText.Layout.Element.Image VersetzerLinks = new(iText.IO.Image.ImageDataFactory.Create(VersetzerJPG));
                        iText.Layout.Element.Image VersetzerRechts = new(iText.IO.Image.ImageDataFactory.Create(VersetzerJPG));
                        VersetzerLinks.SetFixedPosition(i, 0, pdfHeightInPoints - 150);
                        VersetzerRechts.SetFixedPosition(i, pdfWidthInPoints - VersetzerRechts.GetImageWidth(), pdfHeightInPoints - 150);
                        document.Add(VersetzerLinks);
                        document.Add(VersetzerRechts);
                    }
                }

                AddDMX(pdfDocument, dDMX3, DMXArt.DMX3, callerFilePath, callerLineNumber, callerMemberName);

                document.Close();
                pdfMerger.Close();
                pdfDocument.Close();
            }
            pdfLeer.Close();
            GC.Collect();

            // Leeres PDF löschen
            CoreFC.TryDeleteFile(LeerPDF);

            return outputFile;
        }

        /// <summary>
        /// Fügt Text auf den angegebenen Seiten des PDF hinzu
        /// ACHTUNG: Nullpunkt ist unten links
        /// </summary>
        /// <param name="PDF">Das zu bearbeitende PDF</param>
        /// <param name="APTVorlagen">Absoluter Pfad</param>
        /// <param name="text">Der hinzuzufügende Text</param>
        /// <param name="fontSize">Die Schriftgrösse</param>
        /// <param name="x">Position X-Achse (Millimeter von links)</param>
        /// <param name="y">Position Y-Achse (Millimeter von unten)</param>
        /// <param name="seiten">1 basierend, wenn null, wird die erste Seite verwendet</param>
        /// <param name="rotation">0.0f --> keine Rotation</param>
        public static void AddTextToPDF(string APTVorlagen, string PDF, string text, int fontSize, float x, float y, List<int> seiten = null, float rotation = 0.0f, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            if (seiten == null) { seiten = new() { 1 }; }
            string tmpPDF = PDF.Replace(DotPDF, DotTempPDF);
            CoreFC.IsFileLocked(PDF, true);
            CoreFC.IsFileLocked(tmpPDF, false);

            //using (iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(PDF), new iText.Kernel.Pdf.PdfWriter(tmpPDF)))
            using (PdfDocWithWriter pdfDocument = new(PDF, tmpPDF))
            {
                iText.Kernel.Pdf.Canvas.PdfCanvas under = new(pdfDocument.GetFirstPage().NewContentStreamBefore(), pdfDocument.GetPage(1).GetResources(), pdfDocument);
                iText.Kernel.Font.PdfFont pDFFontHelvetica = CreateFont(APTVorlagen);
                iText.Layout.Element.Text elementText = new iText.Layout.Element.Text(text).SetFont(pDFFontHelvetica).SetFontSize(fontSize).SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK);
                iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph(elementText).SetFont(pDFFontHelvetica);
                foreach (int s in seiten)
                {
                    iText.Layout.Canvas canvasWatermark1 = new iText.Layout.Canvas(under, pdfDocument.GetDefaultPageSize()).ShowTextAligned(paragraph, MillimetersToPoints(x), MillimetersToPoints(y), s, iText.Layout.Properties.TextAlignment.LEFT, iText.Layout.Properties.VerticalAlignment.TOP, rotation);
                    canvasWatermark1.Close();
                }
                pdfDocument.Close();
            }
            CoreFC.TryCopyFile(tmpPDF, PDF, true, maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
            CoreFC.TryDeleteFileCatch(tmpPDF, maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
            GC.Collect();
        }

        // Add Text to PDF (04.03.2024, SME)
        // CHANGE: 04.03.2024 by SME: public => internal
        /// <summary>
        /// Fügt Text auf den angegebenen Seiten des PDF hinzu
        /// ACHTUNG: Nullpunkt ist unten links
        /// </summary>
        /// <param name="PDF">Das zu bearbeitende PDF</param>
        /// <param name="APTVorlagen">Absoluter Pfad</param>
        /// <param name="text">Der hinzuzufügende Text</param>
        /// <param name="fontSize">Die Schriftgrösse</param>
        /// <param name="x">Position X-Achse (Millimeter von links)</param>
        /// <param name="y">Position Y-Achse (Millimeter von unten)</param>
        /// <param name="seiten">1 basierend, wenn null, wird die erste Seite verwendet</param>
        /// <param name="rotation">0.0f --> keine Rotation</param>
        internal static void AddTextToPDF(string APTVorlagen, PdfDocWithWriter pdfDocument, string text, int fontSize, float x, float y, List<int> seiten = null, float rotation = 0.0f, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            if (seiten == null) { seiten = new() { 1 }; }

            iText.Kernel.Pdf.Canvas.PdfCanvas under = new(pdfDocument.GetFirstPage().NewContentStreamBefore(), pdfDocument.GetPage(1).GetResources(), pdfDocument);
            iText.Kernel.Font.PdfFont pDFFontHelvetica = CreateFont(APTVorlagen);
            iText.Layout.Element.Text elementText = new iText.Layout.Element.Text(text).SetFont(pDFFontHelvetica).SetFontSize(fontSize).SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK);
            iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph(elementText).SetFont(pDFFontHelvetica);
            foreach (int s in seiten)
            {
                iText.Layout.Canvas canvasWatermark1 = new iText.Layout.Canvas(under, pdfDocument.GetDefaultPageSize()).ShowTextAligned(paragraph, MillimetersToPoints(x), MillimetersToPoints(y), s, iText.Layout.Properties.TextAlignment.LEFT, iText.Layout.Properties.VerticalAlignment.TOP, rotation);
                canvasWatermark1.Close();
            }
        }

        /// <summary>
        /// Druckaufbereitung des PDF mit Vor-/Nachlauf, TRENN- und FUELL-Blätter
        /// </summary>
        /// <param name="JobID">JobID</param>
        /// <param name="JobPDF">absoluter Pfad</param>
        /// <param name="JobIstDuplex"></param>
        /// <param name="Sollliste">die verwendete Sollliste</param>
        /// <param name="IstGelocht"></param>
        public static void Druckaufbereitung(string APTDirectory, string APTVorlagen, string JobID, string JobPDF, bool JobIstDuplex, List<string> Sollliste, bool IstGelocht, List<string> FileList = null)
        {
            try
            {
                DateTime now = DateTime.Now;
                bool addTrennseiten = false;

                // Bei Paketen sollen jeweils Trennseiten eingefügt werden
                foreach (string s in Sollliste)
                {
                    if (s.StartsWith(SolConst.VERPACKUNG_))
                    {
                        if (s.StartsWith(SolConst.VERPACKUNG_P) || s.Equals(SolConst.VERPACKUNG_C4Man))
                        {
                            addTrennseiten = true;
                        }
                        break;
                    }
                }

                List<List<string>> MailPieces = new List<List<string>>();
                Sollliste.FindAll(p => p.StartsWith(SolConst.MAILPIECE_)).ForEach(s => MailPieces.Add(s.Split(charComma).ToList()));

                #region Check auf Eindeutigkeit der Mailpieces

                HashSet<string> mp = new HashSet<string>();
                string error = string.Empty;
                foreach (List<string> l in MailPieces)
                {
                    if (!mp.Contains(l[SolConst.iMailPieceIndex]))
                    {
                        mp.Add(l[SolConst.iMailPieceIndex]);
                    }
                    else
                    {
                        error += Environment.NewLine + l[SolConst.iMailPieceIndex];
                    }
                }
                if (error.Length > 0)
                {
                    error = "MAILPIECE ist nicht eindeutig:" + error + Environment.NewLine + Environment.NewLine;
                    error += "Dieser Fehler muss zuerst behoben werden!!!" + Environment.NewLine + Environment.NewLine;
                    error += "Verarbeitung wurde abgebrochen.";
                    throw new Exception(error);
                }

                #endregion Check auf Eindeutigkeit der Mailpieces

                List<InsertPageInfo> lFUELLInfos = new();
                List<InsertPageInfo> lTRENNInfos = new();
                int anzahlSeiten = 0;
                string Letzte10StellenDMX3 = MailPieces[0][SolConst.iLetzte10StellenDMX3Index];
                int AnzahlTrennseiten, AnzahlFUELLseiten;
                if (JobIstDuplex)
                {
                    AnzahlTrennseiten = 4;
                    AnzahlFUELLseiten = 2;
                }
                else
                {
                    AnzahlTrennseiten = 2;
                    AnzahlFUELLseiten = 1;
                }

                for (int mpPos = 0; mpPos < MailPieces.Count; mpPos++)
                {
                    if (anzahlSeiten > 0)
                    {
                        if (addTrennseiten)
                        {
                            lTRENNInfos.Add(new InsertPageInfo(MailPieces[mpPos][SolConst.iMailPieceIndex].Replace(SolConst.MAILPIECE_, string.Empty), anzahlSeiten + 1));
                        }
                        else if (MailPieces[mpPos].Count >= SolConst.iFUELLIndex + 1 && MailPieces[mpPos][SolConst.iFUELLIndex].Equals(SolConst.FUELL))
                        {
                            // Muss zum Füllen der Frames ein FUELLblatt eingefügt werden?
                            if ((JobIstDuplex && (anzahlSeiten + lFUELLInfos.Count * AnzahlFUELLseiten) % 4 != 0) || (anzahlSeiten + lFUELLInfos.Count * AnzahlFUELLseiten) % 2 != 0)
                            {
                                lFUELLInfos.Add(new InsertPageInfo(MailPieces[mpPos][SolConst.iMailPieceIndex].Replace(SolConst.MAILPIECE_, string.Empty), anzahlSeiten + 1));
                            }
                        }
                    }
                    anzahlSeiten += JobIstDuplex switch
                    {
                        true => int.Parse(MailPieces[mpPos][SolConst.iSeiteIndex]) * 2,
                        _ => int.Parse(MailPieces[mpPos][SolConst.iSeiteIndex])
                    };
                }

                string inputPDF = JobPDF.Replace(DotPDF, DotTempPDF);
                CoreFC.TryCopyFile(JobPDF, inputPDF, true, maxWaitIteration);
                FileList?.Add(inputPDF);

                using PdfDocWithWriter pdfDocument = new(inputPDF, JobPDF, true);

                if (lTRENNInfos.Count > 0)
                {
                    anzahlSeiten = InsertSpecialPages(APTDirectory, APTVorlagen, InsertPageType.TRENN, pdfDocument, JobID, lTRENNInfos, Letzte10StellenDMX3, AnzahlTrennseiten);
                }
                else if (lFUELLInfos.Count > 0)
                {
                    anzahlSeiten = InsertSpecialPages(APTDirectory, APTVorlagen, InsertPageType.FUELL, pdfDocument, JobID, lFUELLInfos, Letzte10StellenDMX3, AnzahlFUELLseiten);
                }

                int height = PointsToMillimeters(pdfDocument.GetFirstPage().GetMediaBox().GetHeight());
                int width = PointsToMillimeters(pdfDocument.GetFirstPage().GetMediaBox().GetWidth());

                string Vorlage = Path.Combine(APTVorlagen, $"Leer_{width}_{height}{DotPDF}");

                string Vorlauf = CreateVorNachlauf(APTDirectory, APTVorlagen, true, JobPDF, JobID, Vorlage, anzahlSeiten, JobIstDuplex, IstGelocht, Letzte10StellenDMX3);
                using iText.Kernel.Pdf.PdfDocument vorlaufPDF = new(new iText.Kernel.Pdf.PdfReader(Vorlauf));
                vorlaufPDF.CopyPagesTo(1, vorlaufPDF.GetNumberOfPages(), pdfDocument, 1);
                vorlaufPDF.Close();
                FileList?.Add(Vorlauf);

                string Nachlauf = CreateVorNachlauf(APTDirectory, APTVorlagen, false, JobPDF, JobID, Vorlage, anzahlSeiten, JobIstDuplex, IstGelocht, Letzte10StellenDMX3);
                using iText.Kernel.Pdf.PdfDocument nachlaufPDF = new(new iText.Kernel.Pdf.PdfReader(Nachlauf));
                nachlaufPDF.CopyPagesTo(1, nachlaufPDF.GetNumberOfPages(), pdfDocument);
                nachlaufPDF.Close();
                FileList?.Add(Nachlauf);

                pdfDocument.Close();
                FileList?.Add(JobPDF);

                CoreFC.TryDeleteFileCatch(Vorlauf, maxWaitIteration);
                CoreFC.TryDeleteFileCatch(Nachlauf, maxWaitIteration);
                CoreFC.TryDeleteFileCatch(inputPDF, maxWaitIteration);
                GC.Collect();
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        // Insert Special Pages
        // CHANGE: 04.03.2024 by SME: public => internal
        internal static int InsertSpecialPages(string APTDirectory, string APTVorlagen, InsertPageType insertPageType, PdfDocWithWriter pdfDocument, string JobID, List<InsertPageInfo> lstInsertInfos, string Letzte10StellenDMX3, int anzahlSeiten2Insert, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            try
            {
                string fuelltrennPDF = string.Empty;
                switch (insertPageType)
                {
                    case InsertPageType.FUELL: fuelltrennPDF = Path.Combine(APTVorlagen, FUELLseite_pdf); break;
                    case InsertPageType.TRENN: fuelltrennPDF = Path.Combine(APTVorlagen, TRENNseite_pdf); break;
                }
                string insertPDF = Path.Combine(APTDirectory, Path.GetFileNameWithoutExtension(fuelltrennPDF) + JobID + DotPDF);
                string DMX3 = JobID + FUELLDMX3 + Letzte10StellenDMX3.Substring(3); // die ersten 3 Stellen sind die Beilagen
                int anzahlSeiten = 0;

                CoreFC.IsFileLocked(fuelltrennPDF, true);
                CoreFC.IsFileLocked(insertPDF, false);
                //using (iText.Kernel.Pdf.PdfDocument insertPdfDoc = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(fuelltrennPDF), new iText.Kernel.Pdf.PdfWriter(insertPDF)))
                using (PdfDocWithWriter insertPdfDoc = new(fuelltrennPDF, insertPDF))
                {
                    AddDMX(insertPdfDoc, DMX3, 1, DMXArt.DMX3, callerFilePath, callerLineNumber, callerMemberName);
                    insertPdfDoc.Close();
                }

                using (iText.Kernel.Pdf.PdfDocument insertPdfDoc = new(new iText.Kernel.Pdf.PdfReader(insertPDF)))
                {
                    anzahlSeiten = pdfDocument.GetNumberOfPages();
                    for (int i = lstInsertInfos.Count - 1; i >= 0; i--)
                    {
                        CheckSDLOnPage(pdfDocument, lstInsertInfos[i], JobID, callerFilePath, callerLineNumber, callerMemberName);
                        for (int y = 0; y < anzahlSeiten2Insert; y++)
                        {
                            anzahlSeiten++;
                            insertPdfDoc.CopyPagesTo(1, 1, pdfDocument, lstInsertInfos[i].Seite);
                        }
                    }
                    insertPdfDoc.Close();
                }

                CoreFC.TryDeleteFileCatch(insertPDF, maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
                GC.Collect();
                return anzahlSeiten;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion Methods (OK)



        #region TO CHECK if still needed (04.03.2024, SME)

        public static int GetNumberOfPages(string inputFile)
        {
            CoreFC.IsFileLocked(inputFile, true);
            iText.Kernel.Pdf.PdfReader reader = null;
            try
            {
                reader = new iText.Kernel.Pdf.PdfReader(inputFile);
                using iText.Kernel.Pdf.PdfDocument document = new(reader);
                return document.GetNumberOfPages();
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

        }

        public static void Crop2A4(string inputFile, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            string outputFile = inputFile;
            inputFile = inputFile.Replace(DotPDF, DotTempPDF);
            CoreFC.TryCopyFile(outputFile, inputFile, true, maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);

            float widthTo_Trim = iText.Kernel.Geom.PageSize.A4.GetWidth();
            float heightTo_Trim = iText.Kernel.Geom.PageSize.A4.GetHeight();
            float width, height;

            //iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(inputFile), new iText.Kernel.Pdf.PdfWriter(outputFile, new iText.Kernel.Pdf.WriterProperties().UseSmartMode().SetFullCompressionMode(true)));
            using PdfDocWithWriter pdfDocument = new(inputFile, outputFile, true);
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                iText.Kernel.Pdf.PdfPage pdfPage = pdfDocument.GetPage(i);
                height = pdfPage.GetMediaBox().GetHeight();
                width = pdfPage.GetMediaBox().GetWidth();

                height = height > heightTo_Trim ? (height - heightTo_Trim) / 2 : 0;
                width = width > widthTo_Trim ? (width - widthTo_Trim) / 2 : 0;

                if (height > 0 || width > 0)
                {
                    iText.Kernel.Geom.Rectangle rectPortrait = new(width, height, widthTo_Trim, heightTo_Trim);
                    pdfPage.SetCropBox(rectPortrait);
                }
            }
            pdfDocument.Close();
            CoreFC.TryDeleteFileCatch(inputFile, maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
            GC.Collect();
        }

        // Extract Pages
        // CHANGE: 04.03.2024 by SME: public => private
        /// <summary>
        /// Extrahiert die angegebenen Seiten aus dem PdfDocument
        /// </summary>
        /// <param name="inputPDF">PdfDocument mit allen Seiten</param>
        /// <param name="APTVorlagen">APTVorlagen</param>
        /// <param name="outputFile">Name des PDF der extrahierten Seiten</param>
        /// <param name="startPage">1. zu extrahierende Seite (1 basierend)</param>
        /// <param name="stopPage">Letzte zu extrahierende Seite (1 basierend)</param>
        /// <param name="XvonY"></param>
        /// <param name="istEndlos"></param>
        /// <returns></returns>
        private static void ExtractPages(string APTVorlagen, iText.Kernel.Pdf.PdfDocument inputPDF, string outputFile, int startPage, int stopPage, string XvonY, bool istEndlos = true, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            if (File.Exists(outputFile))
            {
                CoreFC.TryDeleteFile(outputFile, maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
            }

            int numberOfPages = inputPDF.GetNumberOfPages();
            if (stopPage > numberOfPages) { stopPage = numberOfPages; }

            //using iText.Kernel.Pdf.PdfDocument outputPDF = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfWriter(outputFile));
            using PdfDocWithWriter outputPDF = new(outputFile);
            inputPDF.CopyPagesTo(startPage, stopPage, outputPDF);

            if (istEndlos)
            {
                using iText.Layout.Document document = new(outputPDF);
                iText.Kernel.Font.PdfFont pDFFontHelvetica = CreateFont(APTVorlagen);
                iText.Layout.Element.Text text = new iText.Layout.Element.Text(XvonY).SetFont(pDFFontHelvetica).SetFontSize(3).SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK);
                iText.Layout.Element.Paragraph paragraph = new(text);

                int n = 1;
                for (int p = startPage; p <= stopPage; p += 2, n += 2)
                {
                    document.ShowTextAligned(paragraph, 1, 5, n, iText.Layout.Properties.TextAlignment.LEFT, iText.Layout.Properties.VerticalAlignment.TOP, 0);
                }
                document.Close();
            }
            outputPDF.Close();

            GC.Collect();
        }

        public static bool CheckFontsEmbedded(string pdf, ref string error)
        {
            bool embedded = true;
            string errTemp;
            HashSet<string> errors = new();

            CoreFC.IsFileLocked_WaitMaxSeconds(pdf, maxWaitIteration, true);

            using iText.Kernel.Pdf.PdfReader reader = new(pdf);
            using iText.Kernel.Pdf.PdfDocument document = new(reader);

            for (int i = 1; i <= document.GetNumberOfPdfObjects(); i++)
            {
                iText.Kernel.Pdf.PdfObject obj = document.GetPdfObject(i);

                if (!obj?.IsDictionary() ?? true) { continue; }

                if (obj is not iText.Kernel.Pdf.PdfDictionary dict) { continue; }

                if (iText.Kernel.Pdf.PdfName.Font.Equals(dict.GetAsName(iText.Kernel.Pdf.PdfName.Type)))
                {
                    iText.Kernel.Pdf.PdfDictionary fontDescriptor = dict.GetAsDictionary(iText.Kernel.Pdf.PdfName.FontDescriptor);

                    if (fontDescriptor == null)
                    {
                        // wenn kein Font-Descriptor vorhanden ist, heisst das ebenfalls, dass die Schrift nicht eingebettet ist (20.04.2023, SME)
                        try
                        {
                            errTemp = $"{Path.GetFileName(pdf)}: Schrift '{dict.Get(iText.Kernel.Pdf.PdfName.BaseFont)}' ist nicht eingebettet";
                            if (!errors.Contains(errTemp)) { errors.Add(errTemp); }
                        }
                        catch
                        {
                            errTemp = $"{Path.GetFileName(pdf)}: Eine Schrift ist nicht eingebettet";
                            if (!errors.Contains(errTemp)) { errors.Add(errTemp); }
                        }
                        continue;
                    }

                    if (fontDescriptor.Get(iText.Kernel.Pdf.PdfName.FontFile) == null &&
                        fontDescriptor.Get(iText.Kernel.Pdf.PdfName.FontFile2) == null &&
                        fontDescriptor.Get(iText.Kernel.Pdf.PdfName.FontFile3) == null)
                    {
                        embedded = false;
                        try
                        {
                            errTemp = $"Schrift '{fontDescriptor.Get(iText.Kernel.Pdf.PdfName.FontName)}' ist nicht eingebettet";
                            if (!errors.Contains(errTemp)) { errors.Add(errTemp); }
                        }
                        catch
                        {
                            errTemp = $"Eine Schrift ist nicht eingebettet";
                            if (!errors.Contains(errTemp)) { errors.Add(errTemp); }
                        }
                    }
                }
            }

            document.Close();
            reader.Close();
            error = string.Join(Environment.NewLine, errors);

            return embedded;
        }

        /// <summary>
        /// PDFs mergen
        /// </summary>
        /// <param name="lstPDFsToMerge">Eine Liste der absoluten Pfade der zu mergenden PDFs</param>
        /// <param name="outputFile">Absoluter Pfad des Output-PDFs</param>
        /// <returns>AnzahlSeiten</returns>
        public static int MergeFiles(List<string> lstPDFsToMerge, string outputFile, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            int AnzahlSeiten = 0;
            List<string> pdfs2Delete = new();
            if (lstPDFsToMerge.Find(p => p == outputFile) != null)
            {
                outputFile = outputFile.Replace(DotPDF, DotTempPDF);
            }

            if (File.Exists(outputFile))
            {
                CoreFC.TryDeleteFile(outputFile, maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
            }

            if (lstPDFsToMerge.Count == 1)
            {
                CoreFC.TryCopyFile(lstPDFsToMerge[0], outputFile, true, maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
                AnzahlSeiten = GetNumberOfPages(outputFile);
            }
            else
            {
                CoreFC.IsFileLocked(outputFile, false);

                //using iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfWriter(outputFile, new iText.Kernel.Pdf.WriterProperties().UseSmartMode().SetFullCompressionMode(true)));
                using PdfDocWithWriter pdfDocWithWriter = new(outputFile, true);
                int numberOfPages;
                foreach (string pdf in lstPDFsToMerge)
                {
                    CoreFC.IsFileLocked(pdf, true);
                    using iText.Kernel.Pdf.PdfDocument inputPDF = new(new iText.Kernel.Pdf.PdfReader(pdf));
                    numberOfPages = inputPDF.GetNumberOfPages();
                    for (int s = 1; s <= numberOfPages; s++)
                    {
                        inputPDF.CopyPagesTo(s, s, pdfDocWithWriter);
                        if (s % 100 == 0)
                        {
                            // Ressourcen freigeben
                            // => passiert jedoch nur, wenn das PDF mehr als 100 Seiten hat, also sozusagen NIE (30.05.2024, SME)
                            pdfDocWithWriter.FlushCopiedObjects(inputPDF);
                        }
                    }
                    pdfDocWithWriter.FlushCopiedObjects(inputPDF); // Ressourcen freigeben (29.05.2024, SME)
                    inputPDF.Close();
                }
                AnzahlSeiten = pdfDocWithWriter.GetNumberOfPages();
                pdfDocWithWriter.Close();
            }

            if (outputFile.EndsWith(DotTempPDF))
            {
                CoreFC.TryDeleteFile(outputFile.Replace(DotTempPDF, DotPDF), maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
                CoreFC.TryMoveFile(outputFile, outputFile.Replace(DotTempPDF, DotPDF), maxWaitIteration, callerFilePath, callerLineNumber, callerMemberName);
            }

            foreach (string delme in pdfs2Delete)
            {
                CoreFC.TryDeleteFile(delme);
            }

            GC.Collect();
            return AnzahlSeiten;
        }

        public static Dictionary<int, int> GetPageRotations(string inputFile)
        {
            try
            {
                CoreFC.IsFileLocked(inputFile, true);
                using iText.Kernel.Pdf.PdfDocument pdf = new(new iText.Kernel.Pdf.PdfReader(inputFile));
                Dictionary<int, int> returnList = new();
                var pageCount = pdf.GetNumberOfPages();
                for (int i = 1; i <= pageCount; i++)
                {
                    try
                    {
                        var page = pdf.GetPage(i);
                        var rotation = page.GetRotation();

                        if (rotation.Equals(0))
                        {
                            var pageSize = page.GetPageSize();
                            if (pageSize.GetWidth() > pageSize.GetHeight())
                            {
                                returnList.Add(i, 90);
                            }
                            else
                            {
                                returnList.Add(i, rotation);
                            }
                        }
                        else
                        {
                            returnList.Add(i, rotation);
                        }

                    }
                    catch (Exception ex)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                        throw;
                    }
                }
                return returnList;
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        // PDF optimieren (20.02.2023, SME)
        public static void OptimizePDF(string inputPDF, string outputPDF)
        {
            // exit-handling
            if (string.IsNullOrEmpty(inputPDF)) return;
            if (!File.Exists(inputPDF)) return;

            // PDF optimieren
            using (var reader = new iText.Kernel.Pdf.PdfReader(inputPDF))
            {
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(reader))
                {
                    using (var writer = new PdfDocWithWriter(outputPDF, true))
                    {
                        pdf.CopyPagesTo(1, pdf.GetNumberOfPages(), writer);
                        pdf.Close();
                        writer.Close();
                        reader.Close();
                    }
                }
            }
        }

        #endregion TO CHECK

    }

    internal static class ReaderExtensions
    {
        internal static string[] ExtractTextArray(this iText.Kernel.Pdf.PdfPage page, params iText.Kernel.Geom.Rectangle[] rects)
        {
            var textEventListener = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
            iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, textEventListener);
            string[] result = new string[rects.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = textEventListener.GetResultantText(rects[i]);
            }
            return result;
        }

        internal static string ExtractText(this iText.Kernel.Pdf.PdfPage page, params iText.Kernel.Geom.Rectangle[] rects)
        {
            var textEventListener = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
            iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, textEventListener);
            string[] result = new string[rects.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = textEventListener.GetResultantText(rects[i]);
            }
            return string.Join("  ", result);
        }

        internal static String GetResultantText(this iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy strategy, iText.Kernel.Geom.Rectangle rect)
        {
            IList<iText.Kernel.Pdf.Canvas.Parser.Listener.TextChunk> locationalResult = (IList<iText.Kernel.Pdf.Canvas.Parser.Listener.TextChunk>)locationalResultField.GetValue(strategy);
            List<iText.Kernel.Pdf.Canvas.Parser.Listener.TextChunk> nonMatching = new List<iText.Kernel.Pdf.Canvas.Parser.Listener.TextChunk>();
            foreach (iText.Kernel.Pdf.Canvas.Parser.Listener.TextChunk chunk in locationalResult)
            {
                iText.Kernel.Pdf.Canvas.Parser.Listener.ITextChunkLocation location = chunk.GetLocation();
                iText.Kernel.Geom.Vector start = location.GetStartLocation();
                iText.Kernel.Geom.Vector end = location.GetEndLocation();
                if (!rect.IntersectsLine(start.Get(iText.Kernel.Geom.Vector.I1), start.Get(iText.Kernel.Geom.Vector.I2), end.Get(iText.Kernel.Geom.Vector.I1), end.Get(iText.Kernel.Geom.Vector.I2)))
                {
                    nonMatching.Add(chunk);
                }
            }
            nonMatching.ForEach(c => locationalResult.Remove(c));
            try
            {
                return strategy.GetResultantText();
            }
            finally
            {
                nonMatching.ForEach(c => locationalResult.Add(c));
            }
        }

        static System.Reflection.FieldInfo locationalResultField = typeof(iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy).GetField("locationalResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    }

    internal class PdfDocWithWriter : iText.Kernel.Pdf.PdfDocument
    {
        internal PdfDocWithWriter(string pdfwriter)
            : base(new iText.Kernel.Pdf.PdfWriter(pdfwriter)) { }
        internal PdfDocWithWriter(string pdfwriter, bool useFullCompressionMode)
            : base(new iText.Kernel.Pdf.PdfWriter(pdfwriter, new iText.Kernel.Pdf.WriterProperties().UseSmartMode().SetFullCompressionMode(useFullCompressionMode))) { }
        internal PdfDocWithWriter(string pdfreader, string pdfwriter)
            : base(new iText.Kernel.Pdf.PdfReader(pdfreader), new iText.Kernel.Pdf.PdfWriter(pdfwriter)) { }
        internal PdfDocWithWriter(string pdfreader, string pdfwriter, bool useFullCompressionMode)
            : base(new iText.Kernel.Pdf.PdfReader(pdfreader), new iText.Kernel.Pdf.PdfWriter(pdfwriter, new iText.Kernel.Pdf.WriterProperties().UseSmartMode().SetFullCompressionMode(useFullCompressionMode))) { }
        public override void Close()
        {
            if (!this.closed)
            {
                try
                {
                    base.Close();
                }
                catch
                {
                    for (int i = 0; i < this.GetNumberOfPdfObjects(); i++)
                    {
                        iText.Kernel.Pdf.PdfObject pdfObject = this.GetPdfObject(i);
                        if (pdfObject is iText.Kernel.Pdf.PdfStream)
                        {
                            iText.Kernel.Pdf.PdfStream castedObject = (iText.Kernel.Pdf.PdfStream)pdfObject;
                            iText.Kernel.Pdf.PdfObject filters = castedObject.Get(iText.Kernel.Pdf.PdfName.Filter);
                            if (filters != null && filters.IsIndirect() && filters.IsArray())
                            {
                                iText.Kernel.Pdf.PdfArray copy = new((iText.Kernel.Pdf.PdfArray)filters);
                                castedObject.Put(iText.Kernel.Pdf.PdfName.Filter, copy);
                            }
                        }

                    }
                    base.Close();
                }
            }
        }
    }
}
