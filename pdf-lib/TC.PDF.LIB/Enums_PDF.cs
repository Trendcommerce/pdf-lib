namespace TC.PDF.LIB
{
    #region PDF-Conformance-Level

    // Enumeration of PDF-Conformance-Level (31.03.2023, SME)
    public enum PdfConformanceLevelEnum
    {
        None,
        PDF_A_1A,
        PDF_A_1B,
        PDF_A_2A,
        PDF_A_2B,
        PDF_A_2U,
        PDF_A_3A,
        PDF_A_3B,
        PDF_A_3U,
        Unknown
    }

    #endregion

    #region PDF-Actions

    // Enumeration der möglichen PDF-Aktionen (28.04.2023, SME)
    public enum PdfActionEnum
    {
        Compress,
        ShowStructure,
        ShowFontInfos,
        ShowFontInfosUnembedded,
        ExtractPages,
        EmbedFonts,
        EmbedFontsDetailed,
        UnembedFonts,
        RemoveEmptyPages,
        MultiplyPages,
        Merge,
        MergeWithSeparatorPage,
        Optimize,
        RestoreOriginal,
        RestoreLatestVersion,
        RestoreSpecificVersion,
        Crop_A4,
        Crop_3_12_mm,
        Split,
        InsertSeparatorPage,
        CompressImages,
        ExtractImages,
        ShowToDos,
        BackpageCheck,
        RemoveBackpagesWhenAllEmpty,
        RotatePages,
        CheckPrintPdf,
        ExtractPageTexts,                       // (05.04.2024, SME)
        ExtractMailpieces,                      // (22.05.2024, SME)
        RemoveVorNachlauf,                      // (28.06.2024, SME)
        InsertEmptyPage,                        // (05.07.2024, SME)
        TEST
    }

    #endregion

    #region PDF-Object-Types

    // Enumeration of PDF-Object-Types (26.05.2023, SME)
    public enum PdfObjectTypeEnum
    {
        PdfArray,
        PdfBoolean,
        PdfDictionary,
        PdfName,
        PdfNumber,
        PdfStream,
        PdfString,
        Unknown
    }

    #endregion

    #region PDF-ToDo-Types

    public enum PdfToDoTypeEnum
    {
        ToDo,
        Warning
    }

    #endregion

    #region PDF-Auto-Optimize-Options

    // PDF-Auto-Optimize-Options (05.07.2023, SME)
    public enum PdfAutoOptimizeOptionEnum
    {
        OnlyMoveToOutput,
        OnlyCompress,
        Optimize
    }

    #endregion

    #region PDF-Auto-Compress-Images-Options

    // PDF-Auto-Compress-Images-Options (07.07.2023, SME)
    public enum PdfAutoCompressImagesOptionEnum
    {
        OnlyMoveToOutput,
        CompressImages
    }

    #endregion

    #region Border-Styles

    // Border-Style (21.07.2023, SME)
    public enum BorderStyleEnum
    {
        None,
        Solid,
        Dotted,
        Dashed
    }

    #endregion

    #region Alignments

    // Text-Alignment (21.07.2023, SME)
    public enum TextAlignment
    {
        Left,
        Center,
        Right,
        Justified,
        Justified_All
    }

    // Vertical Alignment (21.07.2023, SME)
    public enum VerticalAlignment
    {
        Top,
        Middle,
        Bottom
    }

    #endregion

    #region PDF-Optimizer-Folder-Types

    // Enumeration der PDF-Optimizer-Ordnertypen (17.08.2023, SME)
    public enum PdfOptimizerFolderTypeEnum
    {
        InputFolder = 1,
        QuarantaeneFolder = 2,
        ToDoFolder = 3,
        OutputFolder = 4
    }

    #endregion

    #region Rotation-Options

    // Enumeration der Rotations-Richtung (16.10.2023, SME)
    public enum PdfRotationDirectionEnum
    {
        Left = -90,
        Right = 90,
        Rotate_180 = 180,
        Portrait_ToLeft = 0,
        Portrait_ToRight = 1,
        Landscape_ToLeft = -1,
        Landscape_ToRight = -2
    }

    // Enumeration der zu rotierenden Seiten (16.10.2023, SME)
    public enum PdfRotationPagesOptionEnum
    {
        AllPages,
        EvenPages,
        OddPages,
        SpecificPages
    }

    #endregion

    #region Print-PDF-Page-Types

    // Print-PDF-Page-Types (02.04.2024, SME)
    public enum PrintPdfPageTypeEnum
    {
        Vorlauf,
        Sendung,
        Fuellseite,
        Trennblatt,
        Nachlauf,
        Unbekannt
    }

    #endregion

    #region Print-PDF-Errors

    public enum PrintPdfErrorEnum
    {
        JobIdNotFound,
        SxDxNotFound,
        MissingInfos,
        NoSolInfo,
        TooManySolInfos,
        InvalidSolFileName,
        UnknownPageType,

    }

    #endregion

    #region ExtractTextFromPage-Mode (REMARKED)

    // ExtractTextFromPage-Mode (05.04.2024, SME)
    //public enum ExtractTextFromPageModeEnum
    //{
    //    GetTextFromPageViaPdfTextExtractor,
    //    GetResultantTextFromStrategy, // IMPORTANT: this doesn't return a text!!! (05.04.2024, SME)
    //    GetFromAllAndCompare
    //}

    #endregion
}
