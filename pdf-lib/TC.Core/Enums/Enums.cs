using System;
using TC.Attributes;

namespace TC.Enums
{
    #region Data-Change-Types

    // Enumeration of Data-Change-Type (13.11.2022, SRM)
    public enum DataChangeTypeEnum
    {
        Insert,
        Update,
        Delete
    }

    #endregion

    #region FSO-Size-Types

    // FSO-Size-Types (31.03.2023, SME)
    public enum FsoSizeTypeEnum : long
    {
        None = 0,
        Byte = 1,
        KB = 1024,
        MB = 1048576,
        GB = 1073741824,
        TB = 1099511627776
    }

    #endregion

    #region Log-Types

    // Enumeration of Log-Types (13.11.2022, SRM)
    public enum LogTypeEnum
    {
        Debug,
        Info,
        Warn,
        Error
    }

    #endregion

    #region Value-Columns

    // Enumeration of Value-Columns (13.11.2022, SRM)
    public enum ValueColumnEnum
    {
        BooleanValue = 1,
        IntValue = 2,
        BigIntValue = 3,
        DecimalValue = 4,
        DateTimeValue = 5,
        ShortTextValue = 6,
        LongTextValue = 7,
        GuidValue = 8,
        BinaryValue = 9
    }

    #endregion

    #region Image-Formats

    // Image-Formats (16.06.2023, SME)
    public enum ImageFormatEnum
    {
        Bmp,
        Emf, 
        Exif,
        Gif,
        Icon,
        Jpeg,
        MemoryBmp,
        Png,
        Tiff,
        Wmf,
        Unknown
    }

    #endregion

    #region TC-Netzwerk-Typen

    // TC-Netzwerk-Typen (20.06.2023, SME)
    // kopiert von TCCHContextMenu
    public enum NetzwerkTyp
    {
        [Caption("PROD-Netz")]
        ProdNetz,
        [Caption("CLIENT-Netz")]
        ClientNetz
    }

    #endregion

    #region Sql-Servers

    // SQL-Servers (02.02.2024, SME)
    public enum SqlServerEnum
    {
        PROD_Server,       // PROD-Netz
        CLIENT_Server        // CLIENT-Netz
    }

    // SQL-Server-Types (02.02.2024, SME)
    public enum SqlServerTypeEnum
    {
        PROD,
        TEST
    }

    #endregion

    #region Umgebungen

    public enum CurrentEnvironment
    {
        DEV = 0,
        PROD = 1,
        TEST = 2,
        TEST_INT = 3
    }

    #endregion

    #region SX / DX

    public enum SxDxEnum
    {
        SX,
        DX
    }

    #endregion

    #region RAM-Usage-Status

    // RAM-Usage-Status (30.05.2024, SME)
    public enum RamUsageStatusEnum
    {
        OK,
        Warning,
        Critical
    }

    #endregion
}