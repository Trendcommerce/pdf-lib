using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using System;
using System.Collections.Generic;
using System.IO;
using TC.Functions;

namespace TC.PDF.LIB.Classes.EventListeners
{
    // Image-Render-Listener (15.06.2023, SME)
    internal class ImageRenderListener : IEventListener
    {
        public ImageRenderListener(string fileName)
        {
            FileName = fileName;
            RootFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TESTING", "PDFs", "_Images");
        }

        public readonly string FileName;
        public readonly string RootFolderPath;

        public void EventOccurred(IEventData data, EventType type)
        {
            try
            {
                switch (type)
                {
                    case EventType.BEGIN_TEXT:
                        break;
                    case EventType.RENDER_TEXT:
                        break;
                    case EventType.END_TEXT:
                        break;
                    case EventType.RENDER_IMAGE:
                        
                        ImageRenderInfo renderInfo = (ImageRenderInfo)data;
                        PdfImageXObject image = renderInfo.GetImage();
                        
                        if (image == null)
                        {
                            return;
                        }

                        // You can access various value from dictionary here:
                        PdfString decodeParamsPdfStr = image.GetPdfObject().GetAsString(PdfName.DecodeParms);
                        string decodeParams = decodeParamsPdfStr != null ? decodeParamsPdfStr.ToUnicodeString() : null;

                        byte[] imageByte = image.GetImageBytes(true);
                        var length = imageByte.Length;
                        var extension = image.IdentifyImageFileExtension();

                        // You can use raw image bytes directly, or write image to disk
                        var filename = image.GetPdfObject().GetIndirectReference().GetObjNumber() + "." + extension;
                        var folder = System.IO.Path.Combine(RootFolderPath, FileName);
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                        var filePath = System.IO.Path.Combine(folder, filename);
                        if (File.Exists(filePath)) File.Delete(filePath);
                        File.WriteAllBytes(filePath, imageByte);

                        break;
                    case EventType.RENDER_PATH:
                        break;
                    case EventType.CLIP_PATH_CHANGED:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CoreFC.DPrint("ERROR while retrieving image: " + ex.Message);
            }
        }

        public ICollection<EventType> GetSupportedEvents()
        {
            CoreFC.DPrint("GetSupportedEvents called");
            return null;
        }
    }
}
