using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TC.Functions;

namespace TC.PDF.LIB.Classes
{
    // Neues PDF (21.07.2023, SME)
    public class PdfNewFile
    {
        #region Allgemein

        // New Instance (21.07.2023, SME)
        public PdfNewFile(string filePath)
        {
            // error-handling
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            // set properties
            FilePath = filePath;
            Writer = new FullCompressionPdfWriter(FilePath);
            PdfDocument = new PdfDocument(Writer);
            Document = new Document(PdfDocument);

            // add 1st page
            AddNewPage();
        }

        #endregion

        #region Properties

        public readonly string FilePath;
        private readonly FullCompressionPdfWriter Writer;
        private readonly PdfDocument PdfDocument;
        private readonly Document Document;
        private PdfPage CurrentPage;
        private int CurrentPageIndex = 0;

        #endregion

        #region Methods

        // Save (21.07.2023, SME)
        public void Save()
        {
            Document.Close();
        }

        // Add new Page (21.07.2023, SME)
        public void AddNewPage()
        {
            CurrentPage = PdfDocument.AddNewPage();
            CurrentPageIndex++;
        }

        // Add Paragraph (21.07.2023, SME)
        public void AddParagraph(string text, float fontSize, float? left = null, float? bottom = null, float? width = null, bool bold = false, float? height = null, BorderStyleEnum borderStyle = BorderStyleEnum.None, TextAlignment textAlignment = TextAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle, float? paddingLeft = null, float lineSpacing = 1.5F)
        {
            try
            {
                // create paragraph
                Paragraph p = new Paragraph(text);

                // set page-number
                p.SetPageNumber(CurrentPageIndex);

                // set text-alignment
                p.SetTextAlignment((iText.Layout.Properties.TextAlignment)textAlignment);

                // set vertical alignment
                p.SetVerticalAlignment((iText.Layout.Properties.VerticalAlignment)verticalAlignment);

                // set font-size
                p.SetFontSize(fontSize);

                // set line-spacing
                p.SetFixedLeading(fontSize * lineSpacing);

                // set border-style
                switch (borderStyle)
                {
                    case BorderStyleEnum.None:
                        break;
                    case BorderStyleEnum.Solid:
                        p.SetBorder(new SolidBorder(1));
                        break;
                    case BorderStyleEnum.Dotted:
                        p.SetBorder(new DottedBorder(1));
                        break;
                    case BorderStyleEnum.Dashed:
                        p.SetBorder(new DashedBorder(1));
                        break;
                    default:
                        break;
                }

                // set fixed position
                if (left.HasValue && bottom.HasValue && width.HasValue)
                {
                    p.SetFixedPosition(left.Value, bottom.Value, width.Value);
                }

                // set bold
                if (bold)
                {
                    p.SetBold();
                }

                // set height
                if (height.HasValue)
                {
                    p.SetHeight(height.Value);
                }

                // set padding left
                if (paddingLeft.HasValue)
                {
                    p.SetPaddingLeft(paddingLeft.Value);
                }

                // add to document
                Document.Add(p);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Add Table (21.07.2023, SME)
        public void AddTable(DataTable dataTable, bool addColumnHeaders, float fontSize, float? left = null, float? bottom = null, float? width = null, float lineSpacing = 1.5F)
        {
            try
            {
                // create table
                Table table = new Table(dataTable.Columns.Count, false);

                // set table-properties
                table.SetFontSize(fontSize);
                table.SetPageNumber(CurrentPageIndex);

                // add column-headers
                if (addColumnHeaders)
                {
                    foreach (DataColumn dataColumn in dataTable.Columns)
                    {
                        var p = new Paragraph(dataColumn.ColumnName);
                        p.SetFixedLeading(fontSize * lineSpacing);
                        var c = new Cell().Add(p);
                        table.AddCell(c);
                    }
                }

                // add rows
                foreach (DataRow row in dataTable.Rows)
                {
                    foreach (DataColumn dataColumn in dataTable.Columns)
                    {
                        var p = new Paragraph(row[dataColumn].ToString());
                        p.SetFixedLeading(fontSize * lineSpacing);
                        var c = new Cell().Add(p);
                        table.AddCell(c);
                    }
                }

                // set fixed position
                if (left.HasValue && bottom.HasValue && width.HasValue)
                {
                    table.SetFixedPosition(left.Value, bottom.Value, width.Value);
                }

                // add to document
                Document.Add(table);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Add Image (21.07.2023, SME)
        public void AddImage(System.Drawing.Image image, float? left = null, float? bottom = null, float? width = null, float? height = null)
        {
            try
            {
                // exit-handling
                if (image == null) return;

                // create image-data
                var imageData = ImageDataFactory.Create(image, null);

                // create pdf-image
                Image pdfImage = new Image(imageData);

                // set page-number
                pdfImage.SetPageNumber(CurrentPageIndex);

                // set fixed position
                if (left.HasValue && bottom.HasValue && width.HasValue)
                {
                    pdfImage.SetFixedPosition(left.Value, bottom.Value, width.Value);
                }

                // set height
                if (height.HasValue)
                {
                    pdfImage.SetHeight(height.Value);
                }

                // add to document
                Document.Add(pdfImage);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region TO CHECK

        // TODO: check if needed (21.07.2023, SME)
        //public void AddTextToPDF(string text, int fontSize, float x, float y, List<int> seiten = null, float rotation = 0.0f)
        //{
        //    if (seiten == null) { seiten = new List<int>() { 1 }; }

        //    //iText.Kernel.Pdf.Canvas.PdfCanvas x = new iText.Kernel.Pdf.Canvas.PdfCanvas()
        //    iText.Kernel.Pdf.Canvas.PdfCanvas under = new iText.Kernel.Pdf.Canvas.PdfCanvas(PdfDocument.GetFirstPage().NewContentStreamBefore(), PdfDocument.GetPage(1).GetResources(), PdfDocument);
        //    iText.Kernel.Font.PdfFont pDFFontHelvetica = CreateFont();
        //    iText.Layout.Element.Text elementText = new iText.Layout.Element.Text(text).SetFont(pDFFontHelvetica).SetFontSize(fontSize).SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK);
        //    iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph(elementText).SetFont(pDFFontHelvetica);
        //    foreach (int s in seiten)
        //    {
        //        iText.Layout.Canvas canvasWatermark1 = new iText.Layout.Canvas(under, PdfDocument.GetDefaultPageSize()).ShowTextAligned(paragraph, FC_PDF.MillimetersToPoints(x), FC_PDF.MillimetersToPoints(y), s, iText.Layout.Properties.TextAlignment.LEFT, iText.Layout.Properties.VerticalAlignment.TOP, rotation);
        //        canvasWatermark1.Close();
        //    }
        //}

        // TODO: check if needed (21.07.2023, SME)
        //private static iText.Kernel.Font.PdfFont CreateFont()
        //{
        //    string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        //    path = System.IO.Path.Combine(path, "HelveticaNeue.ttf");
        //    if (!System.IO.File.Exists(path)) throw new System.IO.FileNotFoundException("Font-File not found!", path);

        //    iText.Kernel.Font.PdfFont font = iText.Kernel.Font.PdfFontFactory.CreateFont(path, iText.Kernel.Font.PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
        //    font.SetSubset(true);
        //    return font;
        //}

        #endregion
    }
}
