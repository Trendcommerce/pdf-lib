using iText.Kernel.Pdf;

namespace TC.PDF.LIB.Classes
{
    // PDF-Objekt-Referenz (26.05.2023, SME)
    public class PdfObjectReference
    {
        #region Allgemein

        // Neue Instanz
        internal PdfObjectReference(PdfObject referenceTo, int pageNumber, PdfObject referencingObject, PdfObject referencingObjectParent, string referencingObjectPath) 
        { 
            // set local properties
            ReferenceTo = referenceTo;
            PageNumber = pageNumber;
            ReferencingObject = referencingObject;
            ReferencingObjectParent = referencingObjectParent;
            ReferencingObjectPath = referencingObjectPath;
        }

        #endregion

        #region Properties

        public PdfObject ReferenceTo { get; }
        public int PageNumber { get; }
        public PdfObject ReferencingObject { get; }
        public PdfObject ReferencingObjectParent { get; }
        public string ReferencingObjectPath { get; }
        
        #endregion
    }
}
