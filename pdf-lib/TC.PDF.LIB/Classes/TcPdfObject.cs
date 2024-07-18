using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using TC.Functions;

namespace TC.PDF.LIB.Classes
{
    // TC-PDF-Object (26.06.2023, SME)
    public class TcPdfObject
    {
        #region General

        // Neue Instanz
        internal TcPdfObject(PdfObject pdfObject, PdfName key, bool loadChildObjects = false, bool loadChildObjectsRecursive = false)
        {
            // error-handling
            if (pdfObject == null) throw new ArgumentNullException(nameof(pdfObject));

            // set local properties
            Type = FC_PDF.GetPdfObjectType(pdfObject).Value;
            PdfObjectToString = pdfObject.ToString();
            Key = FC_PDF.GetPdfNameString(key);
            CanHaveChildObjects = FC_PDF.CanHaveChildObjects(pdfObject);
            PdfObject = pdfObject;

            // load child-objects if flag is set
            if (loadChildObjects)
            {
                this.LoadChildObjects(PdfObject, loadChildObjectsRecursive);
            }
        }

        // ToString
        public override string ToString()
        {
            return PdfObjectToString;
        }

        #endregion

        #region Properties

        // PDF-Object
        internal PdfObject PdfObject { get; }

        // Key
        public string Key { get; }

        // Type
        public PdfObjectTypeEnum Type { get; }

        // PdfObject.ToString
        public string PdfObjectToString { get; }

        // Value-String
        public string GetValueString(bool includeCountChildren = true)
        {
            return FC_PDF.GetPdfObjectValueString(PdfObject, includeCountChildren);
        }

        #endregion

        #region Child-Objects

        // List of Child-Objects
        private readonly List<TcPdfObject> ChildObjectList = new List<TcPdfObject>();
        public TcPdfObject[] ChildObjects
        {
            get
            {
                if (ChildObjectList.Any()) return ChildObjectList.ToArray();
                if (!CanHaveChildObjects) return ChildObjectList.ToArray();
                LoadChildObjects(this.PdfObject);
                return ChildObjectList.ToArray();
            }
        }

        // Can have Child-Objects
        public bool CanHaveChildObjects { get; }

        // Load Child-Objects
        private void LoadChildObjects(PdfObject pdfObject, bool loadChildObjectsRecursive = false)
        {
            try
            {
                // clear list
                ChildObjectList.Clear();

                // load child-objects depending on type
                if (pdfObject != null)
                {
                    if (pdfObject is PdfDictionary)
                    {
                        var dic = (PdfDictionary)pdfObject;
                        bool isAnnot = FC_PDF.IsDictionaryOfType(pdfObject, PdfName.Annot);
                        foreach (var key in dic.KeySet())
                        {
                            // skip parent
                            if (PdfName.Parent.Equals(key)) continue;

                            // get value
                            var value = dic.Get(key);

                            // skip page when annot (= parent)
                            if (isAnnot && FC_PDF.IsDictionaryOfType(value, PdfName.Page)) continue;
                            
                            // add child-object
                            try
                            {
                                AddChildObject(value, key, loadChildObjectsRecursive);
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }
                    }
                    else if (pdfObject is PdfArray)
                    {
                        var arr = (PdfArray)pdfObject;
                        foreach (var item in arr)
                        {
                            // add child-object
                            try
                            {
                                AddChildObject(item, null, loadChildObjectsRecursive);
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Add Child-Object
        private TcPdfObject AddChildObject(PdfObject pdfObject, PdfName key, bool loadChildObjectsRecursive = false)
        {
            try
            {
                // exit-handling
                if (pdfObject == null) return null;

                // create new child-object
                var childObject = new TcPdfObject(pdfObject, key, loadChildObjectsRecursive, loadChildObjectsRecursive);
                // add to list
                ChildObjectList.Add(childObject);
                // return
                return childObject;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Is Child-Object
        internal bool IsChildObject(PdfObject pdfObject, bool recursive = false)
        {
            try
            {
                if (pdfObject == null) return false;
                if (ChildObjectList.Any(x => x.PdfObject.Equals(pdfObject))) return true;
                if (recursive && ChildObjectList.Any())
                {
                    foreach (var childObject in ChildObjectList)
                    {
                        if (childObject.IsChildObject(pdfObject, recursive)) return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Count of Child-Objects
        //public int GetCountOfChildObjects(bool recursive = false)
        //{
        //    if (!recursive) return ChildObjectList.Count;
        //    else return ChildObjectList.Count + ChildObjectList.Sum(x => x.GetCountOfChildObjects(recursive));
        //}

        #endregion
    }
}
