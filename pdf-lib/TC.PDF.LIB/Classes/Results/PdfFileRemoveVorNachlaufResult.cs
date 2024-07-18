using System;
using System.Collections.Generic;
using System.Linq;

namespace TC.PDF.LIB.Classes.Results
{
    // Remove-Vor/Nachlauf-Result (28.06.2024, SME)
    public class PdfFileRemoveVorNachlaufResult : Core.PdfFileActionResult
    {
        #region General

        // PROTECTED: New Instance with Action + PDF-File
        protected internal PdfFileRemoveVorNachlaufResult(PdfFile pdfFile) : base(PdfActionEnum.RemoveVorNachlauf, pdfFile) { }

        // PROTECTED: New Instance with Action + PDF-Filepath
        protected internal PdfFileRemoveVorNachlaufResult(string pdfFilePath) : base(PdfActionEnum.RemoveVorNachlauf, pdfFilePath) { }

        // ToString
        public override string ToString()
        {
            try
            {
                return this.GetType().Name + $": Action = {Action}, FileName = {FileName}, Size = {FileSize}, Path = {FilePath}";
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        #endregion

        #region Specific

        // Vorlauf-Seiten
        private readonly List<int> VorlaufSeitenListe = new List<int>();
        public int[] VorlaufSeiten => VorlaufSeitenListe.ToArray();
        internal void AddVorlaufSeite(int seite)
        {
            if (!VorlaufSeitenListe.Contains(seite))
            {
                VorlaufSeitenListe.Add(seite);
            }
        }
        public bool HatVorlaufSeiten => VorlaufSeitenListe.Any();
        public bool IstVorlaufSeite(int seite) => VorlaufSeitenListe.Contains(seite);

        // Nachlauf-Seiten
        private readonly List<int> NachlaufSeitenListe = new List<int>();
        public int[] NachlaufSeiten => NachlaufSeitenListe.ToArray();
        internal void AddNachlaufSeite(int seite)
        {
            if (!NachlaufSeitenListe.Contains(seite))
            {
                NachlaufSeitenListe.Add(seite);
            }
        }
        public bool HatNachlaufSeiten => NachlaufSeitenListe.Any();
        public bool IstNachlaufSeite(int seite) => NachlaufSeitenListe.Contains(seite);

        #endregion
    }
}
