using System;
using System.IO;

namespace TC.Constants
{
    public static class CoreConstants
    {
        // Default Key/Value-Delimter
        public const string DEF_KeyValueDelimiter = "=";

        // Default Item-Delimiter
        public const string DEF_ItemDelimiter = ";";

        // Characters
        public const char CR = '\r';
        public const char LF = '\n';

        // Umgebung
        public const string DEV = "DEV";
        public const string PROD = "PROD";
        public const string TEST = "TEST";
        public const string TEST_INT = "TEST_INT";
        public const string TEST_DEV = "TEST_DEV";
        public const string DEBUG = "DEBUG";

        // Server + Datenbanken
        public const string PROD_Server = "PROD_Server";
        public const string CLIENT_Server = "CLIENT_Server";

        // Allgemein
        public const string EmptyString = "";
        public const char Semicolon = ';';
        public const string DOT = ".";
        public const char DOT_Char = '.';
        public const string STAR = "*";
        public const string StarDotStar = "*.*";
        public const string NullString = "NULL";
        public const string Slash = @"/";
        public const string Plus = @"+";
        public const string Equal = "=";
        public const string Space = " ";
        public const string SpaceReplacement = "#20";
        public static readonly char PathSep = Path.DirectorySeparatorChar;
        public static readonly string UncPathPrefix = PathSep.ToString() + PathSep.ToString();
        public const string DEVTASK = "DEVTASK";

        // Instance-Guid (27.05.2023, SME)
        public static readonly Guid InstanceGuid = Guid.NewGuid();

        // Letters + Numbers
        public const string ABC_ToUpperString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string ABC_ToLowerString = "abcdefghijklmnopqrstuvwxyz";
        public const string NumberString = "0123456798";
        public static readonly char[] ABC_ToUpper = ABC_ToUpperString.ToCharArray();
        public static readonly char[] ABC_ToLower = ABC_ToLowerString.ToCharArray();
        public static readonly char[] Numbers = NumberString.ToCharArray();

        // Datums-Formate
        public const string TimeStamp = "dd.MM.yyyy HH:mm:ss.fff";
        public const string dd_MM_yyyy_HH_mm_ss = "dd.MM.yyyy HH:mm:ss";
        public const string yyyyMMdd = "yyyyMMdd";
        public const string hhmmss = @"hh\:mm\:ss";

        // Nullen
        public const string ZweiNullen = "00";

        // File-Types
        public const string ZIP = "zip";
        public const string DotZIP = ".zip";
        public const string StarDotZIP = "*.zip";

        public const string PDF = "pdf";
        public const string DotPDF = ".pdf";
        public const string StarDotPDF = "*.pdf";
        public const string DotEZBdotPDF = ".ezb.pdf";
        public const string DotTempPDF = ".temppdf";

        public const string XML = "xml";
        public const string DotXML = ".xml";
        public const string StarDotXML = "*.xml";

        public const string LOG = "log";
        public const string DotLOG = ".log";
        public const string StarDotLOG = "*.log";

        public const string SOL = "sol";
        public const string DotSOL = ".sol";
        public const string StarDotSOL = "*.sol";

        public const string BAK = "bak";
        public const string TTF = "ttf";
        public const string DotTTF = ".ttf";
        public const string StarDotTTF = "*.ttf";
        public const string OTF = "otf";
        public const string DotOTF = ".otf";
        public const string StarDotOTF = "*.otf";
        public const string DotWatch = ".watch";
        public const string KnownImageFileTypesString = ".png;.bmp;.jpg;.jpeg";
        public static readonly string[] KnownImageFileTypes = KnownImageFileTypesString.Split(';');

        // Sonstige
        // CMYK-Image-Pixel-Format (05.07.2023, SME), copied from https://stackoverflow.com/questions/4315335/how-to-identify-cmyk-images-using-c-sharp
        public const int ImagePixelFormat32bppCMYK = (15 | (32 << 8));
        public const string LogDashes = "------------------------------------------------------------------------------------------------------------------------------------------------------";

        // Column-Names
        public const string ColumnName_AddedOn = "AddedOn";
        public const string ColumnName_ChangedOn = "ChangedOn";

        // Verpackungsarten
        public const string C5 = "C5", C4 = "C4", Pk = "Pk", P = "P";
        // Versandarten
        public const string AP = "AP", A = "A", B1 = "B1", B2 = "B2", B = "B", R = "R";
        public const string AP_Code = "08", A_Code = "01", B1_Code = "02", B2_Code = "04", B_Code = "00", R_Code = "09";
        // Destinationen
        public const string CH = "CH", EU = "EU", W = "W";
        // Duplex / Simplex
        public const string DX = "DX", SX = "SX";
        // Sprachen
        public const string DE = "DE", FR = "FR", IT = "IT", EN = "EN", XX = "XX";
        // Perfo + Lochung
        public const string keinePerfo = "00", zweierPerfo = "02";
        public const string keineLochung = "00";

        // Specific
        public const int JobIdLength = 9;
    }
}
