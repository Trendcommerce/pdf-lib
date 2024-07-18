using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using System;
using System.Collections.Generic;
using System.IO;
using TC.Functions;
using TC.PDF.LIB.Classes.Results;

namespace TC.PDF.LIB.Classes.EventListeners
{
    // Image-Extractor (16.06.2023, SME)
    public class ImageExtractor : IEventListener
    {
        #region Allgemein

        // Neue Instance
        public ImageExtractor(string targetFolderPath, PdfFileImageExtractionResult result)
        {
            // error-handling
            if (string.IsNullOrEmpty(targetFolderPath)) throw new ArgumentNullException(nameof(targetFolderPath));
            if (result == null) throw new ArgumentNullException(nameof(result));

            // set local properties
            TargetFolderPath = targetFolderPath;
            Result = result;
        }

        #endregion

        #region Properties

        // Target-Folder-Path
        public readonly string TargetFolderPath;

        // Result
        public readonly PdfFileImageExtractionResult Result;

        #endregion

        #region IEventListener

        public void EventOccurred(IEventData data, EventType type)
        {
            try
            {
                switch (type)
                {
                    case EventType.RENDER_IMAGE:

                        // convert image-render-info
                        ImageRenderInfo renderInfo = (ImageRenderInfo)data;

                        // get pdf-image-object
                        PdfImageXObject image = renderInfo.GetImage();

                        // exit-handling
                        if (image == null)
                        {
                            return;
                        }

                        // get bytes, length + extension
                        byte[] imageByte = image.GetImageBytes(true);
                        var length = imageByte.Length;
                        var extension = image.IdentifyImageFileExtension().ToLower();
                        if (!extension.StartsWith(".")) extension = "." + extension;

                        // get name
                        string name = image.GetPdfObject().GetIndirectReference().GetObjNumber().ToString();

                        // set file-name without extension
                        string filePathWithoutExtension = Path.Combine(TargetFolderPath, name);

                        // make sure folder exists
                        var folder = System.IO.Path.GetDirectoryName(filePathWithoutExtension);
                        if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);

                        // check existance + set path
                        string path = filePathWithoutExtension;
                        if (File.Exists(path + extension))
                        {
                            // add counter
                            int counter = 2;
                            while (File.Exists(path + $" - #{counter}" + extension))
                            {
                                counter++;
                            }
                            path += $" - #{counter}";
                        }
                        path += extension;

                        // save image
                        File.WriteAllBytes(path, imageByte);

                        // update counter
                        Result.CountExtractedImages++;

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CoreFC.DPrint("ERROR while retrieving image: " + ex.Message);
                Result.AddError(ex);
            }
        }

        public ICollection<EventType> GetSupportedEvents()
        {
            CoreFC.DPrint("GetSupportedEvents called");
            return null;
        }

        #endregion
    }
}
