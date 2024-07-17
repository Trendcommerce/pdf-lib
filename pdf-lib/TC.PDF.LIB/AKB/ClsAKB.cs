using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TC.Functions;
using static TC.Constants.CoreConstants;

namespace TC.PDF.LIB.AKB
{
    #region AKB-Entry

    public class AKBEntry
    {
        #region Variables

        public readonly string Auftragnummer;
        public readonly string JobID;
        public readonly string Filename_wo_Extension;
        public readonly string Kunde;
        public readonly string Versandart;
        public readonly string Destination;
        public readonly string Verpackung;
        public readonly int AnzahlBlatt;
        public readonly int AnzahlSendungen;
        public readonly int AnzahlMuster;
        public readonly int AnzahlEU = 0;
        public readonly int AnzahlW = 0;
        public readonly bool IstEndlos;
        public readonly bool IstVAD;
        public readonly string Aufgabedatum;
        public readonly bool HasAULE;
        public readonly dsAKBEntry.BeilageDataTable dtBeilage = new();
        public readonly dsAKBEntry.LochungDataTable dtLochung = new();
        public readonly dsAKBEntry.PerforationDataTable dtPerforation = new();
        public readonly dsAKBEntry.ArtCodeDataTable dtArtCode = new();

        #endregion

        // Neue Instanz mit allen Parametern
        public AKBEntry
            (string Kunde, 
            string Auftragnummer, 
            string JobID, 
            string Filename_wo_Extension, 
            string Verpackung, 
            string Versandart, 
            string Destination, 
            int AnzahlBlatt, 
            int AnzahlSendungen, 
            int AnzahlEU, 
            int AnzahlW,
            bool IstEndlos, 
            bool IstVAD, 
            string Aufgabedatum, 
            bool HasAULE, 
            dsAKBEntry.ArtCodeDataTable dtArtCode, 
            dsAKBEntry.BeilageDataTable dtBeilage, 
            dsAKBEntry.LochungDataTable dtLochung, 
            dsAKBEntry.PerforationDataTable dtPerforation)
        {
            this.Auftragnummer = Auftragnummer;
            this.JobID = JobID;
            this.Filename_wo_Extension = Filename_wo_Extension;
            this.Kunde = Kunde;
            this.Verpackung = Verpackung;
            this.Versandart = Versandart;
            this.Destination = Destination;
            this.AnzahlBlatt = AnzahlBlatt;
            this.AnzahlSendungen = AnzahlSendungen;
            this.IstEndlos = IstEndlos;
            this.AnzahlEU = AnzahlEU;
            this.AnzahlW = AnzahlW;
            this.IstVAD = IstVAD;
            this.Aufgabedatum = Aufgabedatum;
            this.HasAULE = HasAULE;

            if (dtBeilage.Any())
            {
                foreach (dsAKBEntry.BeilageRow row in dtBeilage)
                {
                    this.dtBeilage.AddBeilageRow(row.Station, row.BID, row.Anzahl);
                }
            }

            if (dtLochung.Any())
            {
                foreach (dsAKBEntry.LochungRow row in dtLochung)
                {
                    this.dtLochung.AddLochungRow(row.Lochung);
                }
            }
            else
            {
                this.dtLochung.AddLochungRow(keineLochung);
            }

            if (dtPerforation.Any())
            {
                foreach (dsAKBEntry.PerforationRow row in dtPerforation)
                {
                    this.dtPerforation.AddPerforationRow(row.Perforation);
                }
            }

            if (dtArtCode.Any())
            {
                foreach (dsAKBEntry.ArtCodeRow row in dtArtCode)
                {
                    this.dtArtCode.AddArtCodeRow(row.ArtCode, row.Anzahl);
                }
            }
            else
            {
                this.dtArtCode.AddArtCodeRow("white", AnzahlBlatt);
            }

            if (this.Destination.Equals(EU))
            {
                this.AnzahlEU = AnzahlSendungen - AnzahlMuster;
            }
            else if (this.Destination.Equals(W))
            {
                this.AnzahlW = AnzahlSendungen - AnzahlMuster;
            }
        }
    }

    #endregion

    public static class ClsAKB
    {
        // Konstanten
        private const string n0 = "n0";

        // AKB erstellen
        /// <summary>
        /// Erstellt AKB
        /// </summary>
        /// <param name="AKBVorlage"></param>
        /// <param name="AKBList"></param>
        /// <returns></returns>
        public static List<string> CreateAKB(string APTDirectory, string APTVorlagen, string AKBVorlage, List<AKBEntry> AKBList, string KuBe, string PapierEL, List<string> FileList = null)
        {
            // Deklarationen
            string akbDatei = string.Empty;
            string action = string.Empty;

            try
            {
                // Rückgabe-List erstellen
                List<string> AKBs = new();

                // Sicherstellen, dass AKB-Vorlage nicht schreibgeschützt ist
                FileInfo fileInfo = new(AKBVorlage);
                if (fileInfo.Exists && fileInfo.IsReadOnly) { fileInfo.IsReadOnly = false; }

                // Loop durch AKB-Einträge
                foreach (AKBEntry ae in AKBList)
                {
                    try
                    {
                        // AKB-Dateipfad setzen
                        action = "AKB-Dateipfad setzen";
                        akbDatei = string.Empty;
                        akbDatei = Path.Combine(APTDirectory, "AKB_" + ae.Filename_wo_Extension + DotPDF);

                        // PDF-Document + Acrobat-Form setzen
                        action = "PDF-Document + Acrobat-Form setzen";
                        using PdfDocWithWriter pdfDocument = new(AKBVorlage, akbDatei, true);
                        iText.Forms.PdfAcroForm pdfAcroForm = iText.Forms.PdfAcroForm.GetAcroForm(pdfDocument, true);

                        // Felder abfüllen
                        action = "Felder abfüllen";
                        foreach (var formfield in pdfAcroForm.GetFormFields())
                        {
                            try
                            {
                                switch (formfield.Key)
                                {
                                    case "Auftragsnummer":
                                        SetPdfFieldValue(formfield.Value, ae.Auftragnummer);
                                        break;
                                    case "JobID":
                                        SetPdfFieldValue(formfield.Value, ae.JobID, 1);
                                        break;
                                    case "Dateiname":
                                        SetPdfFieldValue(formfield.Value, ae.Filename_wo_Extension + (ae.IstEndlos ? DotPDF : DotEZBdotPDF), 1);
                                        break;
                                    case "Kunde":
                                        SetPdfFieldValue(formfield.Value, ae.Kunde);
                                        break;
                                    case "Versanddatum":
                                        SetPdfFieldValue(formfield.Value, ae.Aufgabedatum);
                                        break;
                                    case "Kube":
                                        SetPdfFieldValue(formfield.Value, KuBe);
                                        break;
                                    case "Hotfolder":
                                        SetPdfFieldValue(formfield.Value, (ae.IstEndlos ? "Endlos" : "Einzelblatt"));
                                        break;
                                    case "MA":
                                        SetPdfFieldValue(formfield.Value, Environment.UserName);
                                        break;
                                    case "Aufbereitungsdatum":
                                        SetPdfFieldValue(formfield.Value, DateTime.Now.ToShortDateString());
                                        break;
                                    case "Blattanzahl":
                                        SetPdfFieldValue(formfield.Value, ae.AnzahlBlatt.ToString(n0));
                                        break;
                                    case "Perfobild":
                                        if (ae.dtPerforation.Count > 0)
                                        {
                                            List<string> lstPerforation = new();
                                            foreach (var row in ae.dtPerforation)
                                            {
                                                lstPerforation.Add(row.Perforation);
                                            }
                                            SetPdfFieldValue(formfield.Value, string.Join(", ", lstPerforation));
                                        }
                                        break;
                                    case "SendungenTotal":
                                        SetPdfFieldValue(formfield.Value, ae.AnzahlSendungen.ToString(n0));
                                        break;
                                    case "SendungenCH":
                                        if (ae.AnzahlSendungen - ae.AnzahlEU - ae.AnzahlW - ae.AnzahlMuster > 0)
                                        {
                                            SetPdfFieldValue(formfield.Value, (ae.AnzahlSendungen - ae.AnzahlEU - ae.AnzahlW - ae.AnzahlMuster).ToString(n0));
                                        }
                                        break;
                                    case "EU":
                                        if (ae.AnzahlEU > 0) 
                                        { 
                                            SetPdfFieldValue(formfield.Value, ae.AnzahlEU.ToString(n0));
                                        }
                                        break;
                                    case "W":
                                        if (ae.AnzahlW > 0) 
                                        { 
                                            SetPdfFieldValue(formfield.Value, ae.AnzahlW.ToString(n0));
                                        }
                                        break;
                                    case "Muster":
                                        if (ae.AnzahlMuster > 0) 
                                        { 
                                            SetPdfFieldValue(formfield.Value, ae.AnzahlMuster.ToString(n0));
                                        }
                                        break;
                                    case "Versandart":
                                        SetPdfFieldValue(formfield.Value, ae.Versandart);
                                        break;
                                    case "Verpackung":
                                        SetPdfFieldValue(formfield.Value, ae.Verpackung);
                                        break;
                                    default:
                                        Console.WriteLine($"Das AKB-Feld '{formfield.Key}' wurde ignoriert.");
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Beim Abfüllen des AKB-Feldes '{formfield.Key}' ist ein Fehler aufgetreten.", ex);
                            }
                        }

                        // Papier-Info-Felder abfüllen
                        action = "Papier-Info-Felder abfüllen";
                        if (ae.IstEndlos)
                        {
                            SetPdfFieldValue(pdfAcroForm, "PapierEL", PapierEL);
                        }
                        else
                        {
                            int counter = 1;
                            foreach (var row in ae.dtArtCode)
                            {
                                // TODO: check MaWiNr for PapierEZB (07.03.2024, SME)
                                //if (row.MaWiNr.StartsWith("Pa") || row.MaWiNr.StartsWith("KB"))
                                //{
                                //    pdfAcroForm.GetField($"PapierEZB{counter++}").SetValue($"{row.MaWiNr}${TC.Global.Global_TC_Core.ClientId} ({row.Anzahl:n0})");
                                //}
                                //else
                                //{
                                    SetPdfFieldValue(pdfAcroForm, $"PapierEZB{counter++}", $"{row.ArtCode} ({row.Anzahl:n0})");
                                //}
                            }
                        }

                        // Verpackungs-DMX hinzufügen
                        action = "Verpackungs-DMX hinzufügen";
                        ClsPDF.AddDMX(pdfDocument, ae.JobID, 1, ClsPDF.DMXArt.Verpackung);

                        // VAD-DMX hinzufügen
                        action = "VAD-DMX hinzufügen";
                        if (ae.IstVAD)
                        {
                            ClsPDF.AddDMX(pdfDocument, ae.JobID, 1, ClsPDF.DMXArt.VAD);
                        }

                        // Aule-Feld entfernen
                        action = "Aule-Feld entfernen";
                        if (!ae.HasAULE)
                        {
                            pdfAcroForm.RemoveField("mitAule");
                        }

                        // Beilagen setzen
                        action = "Beilagen setzen";
                        foreach (var row in ae.dtBeilage)
                        {
                            SetPdfFieldValue(pdfAcroForm, "Beilage" + row.Station, $"{row.BID} ({row.Anzahl:n0})");
                        }

                        // PDF schliessen
                        action = "PDF schliessen";
                        pdfDocument.Close();

                        // AKB zur Rückgabeliste hinzufügen
                        action = "AKB zur Rückgabeliste hinzufügen";
                        AKBs.Add(akbDatei);

                        // AKB zur mitgelieferten Dateiliste hinzufügen
                        action = "AKB zur mitgelieferten Dateiliste hinzufügen";
                        FileList?.Add(akbDatei);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Beim Erstellen des AKBs '{akbDatei}' ist ein Fehler aufgetreten während der Aktion '{action}'.", ex);
                    }
                }

                // return
                return AKBs;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #region CreateAKB (alte Version) (REMARKED)

        /// <summary>
        /// Erstellt AKB
        /// </summary>
        /// <param name="AKBVorlage"></param>
        /// <param name="AKBList"></param>
        /// <returns></returns>
        //public static List<string> CreateAKB_Alt(string APTDirectory, string APTVorlagen, string AKBVorlage, List<AKBEntry> AKBList, List<string> FileList = null)
        //{
        //    List<string> AKBs = new();

        //    FileInfo fileInfo = new(AKBVorlage);
        //    if (fileInfo.Exists) { fileInfo.IsReadOnly = false; }

        //    foreach (AKBEntry ae in AKBList)
        //    {
        //        AKBDatei = Path.Combine(APTDirectory, "AKB_" + ae.Filename_wo_Extension + DotPDF);

        //        //using iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(AKBVorlage), new iText.Kernel.Pdf.PdfWriter(AKBDatei, new iText.Kernel.Pdf.WriterProperties().UseSmartMode().SetFullCompressionMode(true)));
        //        using PdfDocWithWriter pdfDocument = new(AKBVorlage, AKBDatei, true);
        //        iText.Forms.PdfAcroForm pdfAcroForm = iText.Forms.PdfAcroForm.GetAcroForm(pdfDocument, true);

        //        foreach (KeyValuePair<string, iText.Forms.Fields.PdfFormField> formfield in pdfAcroForm.GetFormFields())
        //        {
        //            switch (formfield.Key)
        //            {
        //                case "Auftragnummer":
        //                    formfield.Value.SetValue(ae.Auftragnummer);
        //                    break;
        //                case "Sollliste":
        //                    formfield.Value.SetValue(ae.JobID).SetJustification(1);
        //                    break;
        //                case "Dateiname":
        //                    formfield.Value.SetValue(ae.Filename_wo_Extension + (ae.IstEndlos ? DotPDF : DotEZBdotPDF)).SetJustification(1);
        //                    break;
        //                case "Kunde":
        //                    formfield.Value.SetValue(ae.Kunde);
        //                    break;
        //                case "Erstellt am":
        //                    formfield.Value.SetValue(DateTime.Now.ToShortDateString());
        //                    break;
        //                case "MA":
        //                    formfield.Value.SetValue(CoreFC.GetUserName());
        //                    break;
        //                case "Blattanzahl":
        //                    formfield.Value.SetValue(ae.AnzahlBlatt.ToString(n0));
        //                    break;
        //                case "Datamatrix":
        //                    formfield.Value.SetValue("SOL");
        //                    break;
        //                case "Sendungen Total":
        //                    formfield.Value.SetValue(ae.AnzahlSendungen.ToString(n0));
        //                    break;
        //                case "EU":
        //                    if (ae.AnzahlEU > 0) { formfield.Value.SetValue(ae.AnzahlEU.ToString(n0)); }
        //                    break;
        //                case "W":
        //                    if (ae.AnzahlW > 0) { formfield.Value.SetValue(ae.AnzahlW.ToString(n0)); }
        //                    break;
        //                case "Muster":
        //                    if (ae.AnzahlMuster > 0)
        //                    {
        //                        formfield.Value.SetValue(ae.AnzahlMuster.ToString(n0));
        //                        //ClsPDF.AddMusterWatermark(pdfDocument);
        //                    }
        //                    break;
        //                case "Kuvertformat":
        //                    formfield.Value.SetValue(ae.Verpackung);
        //                    break;
        //                case "Versandart":
        //                    formfield.Value.SetValue(ae.Versandart);
        //                    break;
        //                case "Beilagen":
        //                    #region Beilagen
        //                    if (ae.dtBeilage.Count.Equals(0))
        //                    {
        //                        formfield.Value.SetValue("Nein");
        //                    }
        //                    else
        //                    {
        //                        formfield.Value.SetValue("Ja");
        //                        foreach (dsAKBEntry.BeilageRow row in ae.dtBeilage)
        //                        {
        //                            pdfAcroForm.GetField("Beilage0" + row.Station).SetValue($"{row.BID} ({row.Anzahl})");
        //                        }
        //                    }
        //                    #endregion Beilagen
        //                    break;
        //                case "Lochung":
        //                    #region Lochung
        //                    if (ae.dtLochung[0].Lochung.Equals(keineLochung) && ae.dtLochung.Count.Equals(1))
        //                    {
        //                        formfield.Value.SetValue("Nein");
        //                    }
        //                    else
        //                    {
        //                        formfield.Value.SetValue("Ja");
        //                        if (ae.dtLochung.Count > 1)
        //                        {
        //                            ClsPDF.AddTextToPDF(APTVorlagen, pdfDocument, "Dynamische Lochung", 12, 74.2f, 173f);
        //                        }
        //                    }
        //                    #endregion Lochung
        //                    break;
        //                case "Perfobild":
        //                    #region Perforation
        //                    if (ae.dtPerforation.Count > 0)
        //                    {
        //                        List<string> lstPerforation = new();
        //                        foreach (dsAKBEntry.PerforationRow row in ae.dtPerforation)
        //                        {
        //                            lstPerforation.Add(row.Perforation);
        //                        }
        //                        formfield.Value.SetValue(string.Join(", ", lstPerforation));

        //                    }
        //                    #endregion Perforation
        //                    break;
        //                case "Papier":
        //                    #region Papier
        //                    if (ae.IstEndlos)
        //                    {
        //                        pdfAcroForm.GetField("Hotfolder").SetValue("Endlos");
        //                        formfield.Value.SetValue(ae.dtArtCode[0].ArtCode);
        //                    }
        //                    else
        //                    {
        //                        pdfAcroForm.GetField("Hotfolder").SetValue("Einzelblatt");
        //                        #region ArtCode für PS
        //                        List<string> lstMediaAnzahl = new();
        //                        foreach (dsAKBEntry.ArtCodeRow row in ae.dtArtCode)
        //                        {
        //                            lstMediaAnzahl.Add($"{TC.Global.Global_TC_Core.ClientId}_{row.ArtCode.Substring(0, 1).ToUpper()}{row.ArtCode.Substring(1)} ({row.Anzahl:n0})");
        //                        }
        //                        formfield.Value.SetValue(string.Join(", ", lstMediaAnzahl).Trim());
        //                        #endregion ArtCode für PS
        //                    }
        //                    #endregion Papier
        //                    break;
        //                case "Versand Datum":
        //                    if (!string.IsNullOrEmpty(ae.Aufgabedatum))
        //                    {
        //                        formfield.Value.SetValue(ae.Aufgabedatum);
        //                    }
        //                    break;
        //            }
        //        }

        //        if (ae.IstVAD)
        //        {
        //            ClsPDF.AddDMX(pdfDocument, ae.JobID, 1, ClsPDF.DMXArt.VAD);
        //        }

        //        if (ae.HasAULE)
        //        {
        //            ClsPDF.AddTextToPDF(APTVorlagen, pdfDocument, "AULE", 40, 150, 277);
        //        }

        //        pdfDocument.Close();

        //        FileList?.Add(AKBDatei);

        //        AKBs.Add(AKBDatei);
        //    }

        //    return AKBs;
        //}

        #endregion

        // PDF-Feld-Wert setzen (08.03.2024, SME)
        private static void SetPdfFieldValue(iText.Forms.Fields.PdfFormField field, string value, int? justification = null)
        {
            // error-handling
            if (field == null) throw new ArgumentNullException("PDF-Feld");

            // set value
            field.SetValue(value);

            // set justification
            if (justification.HasValue)
            {
                field.SetJustification(justification.Value);
            }
        }
        private static void SetPdfFieldValue(iText.Forms.PdfAcroForm pdfAcroForm, string fieldName, string value, int? justification = null)
        {
            // error-handling
            if (pdfAcroForm == null) throw new ArgumentNullException("PDF-Formular");
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException("PDF-Feldname");

            // get field
            var pdfField = pdfAcroForm.GetField(fieldName);
            if (pdfField == null) throw new Exception($"PDF-Feld '{fieldName}' wurde nicht gefunden!");

            // set field-value
            SetPdfFieldValue(pdfField, value, justification);
        }
    }
}
