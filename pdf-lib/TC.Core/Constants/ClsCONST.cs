using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace TC.Constants
{
    //public static class ClsCONST
    //{
    //    // WICHTIGE / Allgemeine Konstanten
    //    public const int MaxBlattProJob = 30000;
    //    public const int MaxPhysischeBeilagen = 4;
    //    public const int indexFileID = 3;
    //    public const string ClientID_2 = "MEDA";
    //    public const string FixType = "pj";
    //    public const int JobIdLength = 10;
    //    public const int DateLength = 8;
    //    public const int TimeLength = 6;
    //    public const string DefaultCoverPage = "cp_tc_default";
    //    public const int TSPLength = 11;

    //    // Umgebung
    //    public const string DEV = "DEV";
    //    public const string PROD = "PROD";
    //    public const string TEST = "TEST";
    //    public const string TEST_INT = "TEST_INT";
    //    public const string JobIDProd = "78" + KundenJobnummer;
    //    public const string JobIDTest = "79" + KundenJobnummer;
    //    public const string ProdEnvironment = "Prod";
    //    public const string TestEnvironment = "Test";
    //    public const string ProdModus = "p";
    //    public const string TestModus = "t";

    //    // Startup-Ordner-Pfad
    //    public static readonly string StartupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

    //    // Datei-Pfade
    //    public static readonly string AlreadyRunning = Path.Combine(StartupPath, $"{Assembly.GetEntryAssembly().GetName().Name}.AlreadyRunning.txt");
    //    public static readonly string BoeweAlreadyRunning = Path.Combine(StartupPath, $"{Customer}_Boewe.AlreadyRunning.txt");
    //    public static readonly string Stoptxt = Path.Combine(StartupPath, "Stop.txt");

    //    // Ordner-Namen
    //    public const string APTDirectoryFolderName = "APT";
    //    public const string APTVorlagenFolderName = "Vorlagen";
    //    public const string BackgroundFolderName = "Background";
    //    public const string IN_DatafilesFolderName = "IN_Datafiles";
    //    public const string IN_GMCFolderName = "IN_GMC";
    //    public const string IN_ProcessfilesFolderName = "IN_Processfiles";
    //    public const string LogDirectoryFolderName = "Logs";
    //    public const string OUT_GMCFolderName = "OUT_GMC";
    //    public const string OUT_Print_PDFFolderName = "OUT_Print_PDF";
    //    public const string OUT_ReceiptsFolderName = "OUT_Receipts";
    //    public const string PDF_StoreFolderName = "PDF_Store";
    //    public const string ZIP_StoreFolderName = "ZIP_Store";

    //    // Ordner-Pfade
    //    public static readonly string APTDirectory = Path.Combine(StartupPath, APTDirectoryFolderName);
    //    public static readonly string APTVorlagen = Path.Combine(APTDirectory, APTVorlagenFolderName);
    //    public static readonly string Background = Path.Combine(StartupPath, BackgroundFolderName);
    //    public static readonly string IN_Datafiles = Path.Combine(StartupPath, IN_DatafilesFolderName);
    //    public static readonly string IN_GMC = Path.Combine(StartupPath, IN_GMCFolderName);
    //    public static readonly string IN_Processfiles = Path.Combine(StartupPath, IN_ProcessfilesFolderName);
    //    public static readonly string LogDirectory = Path.Combine(StartupPath, LogDirectoryFolderName);
    //    public static readonly string OUT_GMC = Path.Combine(StartupPath, OUT_GMCFolderName);
    //    public static readonly string OUT_Print_PDF = Path.Combine(StartupPath, OUT_Print_PDFFolderName);
    //    public static readonly string OUT_Receipts = Path.Combine(StartupPath, OUT_ReceiptsFolderName);
    //    public static readonly string PDF_Store = GetPDF_Store();
    //    public static readonly string ZIP_Store = Path.Combine(StartupPath, ZIP_StoreFolderName);

    //    // Ordnerpfad von PDF-Store ermitteln
    //    private static string GetPDF_Store()
    //    {
    //        if (!Path.GetFileName(StartupPath).Equals("Debug")) return Path.Combine(StartupPath, PDF_StoreFolderName);
    //        var folder = new DirectoryInfo(StartupPath);
    //        var medaFolder = folder.Parent.Parent.Parent;
    //        var path = Path.Combine(medaFolder.FullName, "MEDA_IMPORT", "bin", "Debug", PDF_StoreFolderName);
    //        return path;
    //    }

    //    // Datei-Endungen
    //    public const string dotImporting = ".Importing";
    //    public const string dotJSON = ".json";
    //    public const string sternDotJSON = "*.json";
    //    public const string dotLOG = ".log";
    //    public const string sternDotLOG = "*.log";
    //    public const string dotPDF = ".pdf";
    //    public const string sternDotPDF = "*.pdf";
    //    public const string dotZIP = ".zip";
    //    public const string sternDotZIP = "*.zip";
    //    public const string dotXML = ".xml";
    //    public const string sternDotXML = "*.xml";
    //    public const string dotSOL = ".sol";
    //    public const string sternDotSOL = "*.sol";
    //    public const string dotDORB = ".dorb";
    //    public const string sternDotDORB = "*.dorb";
    //    public const string dotREP = ".rep";
    //    public const string sternDotREP = "*.rep";
    //    public const string dotSTL = ".stl";
    //    public const string sternDotSTL = "*.stl";
    //    public const string dotEZB = ".ezb";
    //    public const string sternDotEZB = "*.ezb";
    //    public const string dotEZBdotPDF = ".ezb.pdf";
    //    public const string sternDotEZBdotPDF = "*.ezb.pdf";
    //    public const string dotTempPDF = ".temppdf";
    //    public const string sternDotTempPDF = "*.temppdf";

    //    // Dateigrössen
    //    public const long KB = 1024;
    //    public const long MB = KB * 1024;
    //    public const long GB = MB * 1024;

    //    // Datums-Formate
    //    public const string hhmmss = @"hh\:mm\:ss";
    //    public const string HHmmss = "HHmmss";
    //    public const string ddMMyyyy = "dd.MM.yyyy";
    //    public const string dd_MM_yyyy_HH_mm_ss = "dd.MM.yyyy HH:mm:ss";
    //    public const string yyyyMMdd = "yyyyMMdd";
    //    public const string yyyyMMddHHmmss = "yyyyMMddHHmmss";

    //    // Allgemeine / Diverse Konstanten
    //    public const string Coversheet = "Coversheet";
    //    public const string White = "White";
    //    public const string LogDashes = "------------------------------------------------------------------------------------------------------------------------------------------------------";
    //    public const string Stoptxt_existiert = "Stop.txt existiert, Exit Application";
    //    public const string keineLochung = "00";
    //    public const string Zahlen = "1234567890";
    //    public static readonly char[] ZahlenArray = Zahlen.ToCharArray();
    //    public const string StringEmpty = "";
    //    public const string slash = "/";
    //    public const string keinePerfo = "00", zweierPerfo = "02";

    //    // Verpackungsarten
    //    public const string C5 = "C5", C4 = "C4", Pk = "Pk", P = "P";
    //    // Versandarten
    //    public const string AP = "AP", A = "A", B1 = "B1", B2 = "B2", B = "B", R = "R";
    //    public const string AP_Code = "08", A_Code = "01", B1_Code = "02", B2_Code = "04", B_Code = "00", R_Code = "09";
    //    // Destinationen
    //    public const string CH = "CH", EU = "EU", W = "W";
    //    // Duplex / Simplex
    //    public const string DX = "DX", SX = "SX";
    //    // Sprachen
    //    public const string DE = "DE", FR = "FR", IT = "IT", EN = "EN", XX = "XX";

    //    // Entwicklungs-Konstanten
    //    public static readonly bool IsDevTask = ClsUtils.IsDevTaskUser();
    //    public static readonly string C_DEV_Folder = ClsUtils.GetCDEVFolderpath();

    //    // Einschreiben-Konstanten
    //    public const string Einschreiben_AP = "AP";
    //    public const string Einschreiben_Inland = "Inland";
    //    public const string Einschreiben_Ausland = "Ausland";
    //    public const string Einschreiben_Paket = "Paket";

    //    // Neue Konstanten
    //    public const string stern = "*";
    //    public const string Done_txt = "Done.txt";
    //    public const char underscore = '_';
    //    public const string DEF_DEL_Split = " + ";

    //    // VAD-Handling
    //    public const string VAD_Versandarten_A = A;
    //    public const string VAD_Versandarten_B_B1 = B + DEF_DEL_Split + B1;
    //    public const string VAD_Destinationen = CH + DEF_DEL_Split + EU + DEF_DEL_Split + W;
    //    public static readonly List<string> VAD_VersandartenList_A = new() { A };
    //    public static readonly List<string> VAD_VersandartenList_B_B1 = new() { B, B1 };
    //    public static readonly List<string> VAD_DestinationenList = new() { CH, EU, W };
    //    public static readonly List<string> VAD_VerpackungList = new() { C4, C5 };

    //}

    public static class EzbConst
    {
        public const string Job = "Job";
        public const string JobID = "JobID";
        public const string Pages = "Pages";
        public const string PdfName = "PdfName";
        public const string Shipment = "Shipment";
        public const string Mailpiece = "Mailpiece";
        public const string Page = "Page";
        public const string PageNr = "PageNr";
        public const string Media = "Media";
        public const string IsFront = "IsFront";
        public const string Punching = "Punching";
    }

    public static class SolConst
    {
        public const string START_APPLICATION = "START APPLICATION";
        public const string APPLICATION_ID_ = "APPLICATION-ID=";
        public const string SYSTEM_DPS = "SYSTEM=DPS";
        public const string AUFTRAG_ = "AUFTRAG=";
        public const string KUNDE_ = "KUNDE=";
        public const string BEILAGE_ = "BEILAGE=";
        public const string VERSANDART_ = "VERSANDART=";
        public const string LIEFERART_P = "LIEFERART=P";
        public const string LIEFERART_K = "LIEFERART=K";
        public const string VERPACKUNG_ = "VERPACKUNG=";
        public const string VERPACKUNG_P = "VERPACKUNG=P";
        public const string VERPACKUNG_Pk = "VERPACKUNG=Pk";
        public const string VERPACKUNG_C4Man = "VERPACKUNG=C4Man";
        public const string INFO_ = "INFO=";
        public const string AVOR_ = "AVOR=";
        public const string DESIGNATION_ = "DESIGNATION=";
        public const string AUFGABEDATUM_ = "AUFGABEDATUM=";
        public const string AVZ_SLA_ID_ = "AVZ_SLA_ID=";
        public const string AVZ_AUFTRAGSNUMMER_ = "AVZ_AUFTRAGSNUMMER=";
        public const string PUNCHING_Dynamic = "PUNCHING=Dynamic";
        public const string PUNCHING_None = "PUNCHING=None";
        public const string PUNCHING_Static = "PUNCHING=Static";
        public const string DATE_ = "DATE=";
        public const string STACKDESIGNATION_ = "STACKDESIGNATION=";
        public const string START_STACK_ID_ = "START STACK-ID=";
        public const string START_STACK_ID_1 = START_STACK_ID_ + "1";
        public const string END_STACK = "END STACK";
        public const string TOTAL_NUMBER_OF_STACKS_ = "TOTAL NUMBER OF STACKS=";
        public const string TOTAL_NUMBER_OF_STACKS_1 = TOTAL_NUMBER_OF_STACKS_ + "1";
        public const string END_APPLICATION = "END APPLICATION";
        public const string MAILPIECE_ = "MAILPIECE=";
        public const string START_REPRINT_LIST = "START REPRINT LIST";
        public const string TYPE_MANUAL = "TYPE=MANUAL";
        public const string END_REPRINT_LIST = "END REPRINT LIST";
        public const string AUDITENTRY_ = "AUDITENTRY=";

        public const string comma = ",";
        public const string comma3 = ",,,";
        public const string comma4 = ",,,,";
        public const string comma6 = ",,,,,,";
        public const string FUELL = "FUELL";

        public const int iMailPieceIndex = 0;
        public const int iSeiteIndex = 2;
        public const int iLetzte10StellenDMX3Index = 5;
        public const int iFUELLIndex = 16;
        public const int iReprintIndex = 1;
        public const int iAuditStatusIndex = 2;
        public const int iVerpackungsGerätIndex = 1;
        public const int iVerpackungIndex = 3;

        public const string keinReprintNotwendig = "1";
        public const string dorbVerpackt = "0";
        public const string dorbManuellOK = "7";
        public const string dorbOKBut = "13";
        public const string dorbDestroyed = "16";
        public const int manualProcessIDDestroyed = 3;
    }
}
