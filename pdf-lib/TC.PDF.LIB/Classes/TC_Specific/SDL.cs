using System;
using System.Collections.Generic;
using System.Linq;
using TC.Functions;
using static TC.Constants.CoreConstants;

namespace TC.PDF.LIB.Classes.TC_Specific
{
    // SDL-Klasse (28.03.2024, SME)
    public class SDL
    {
        #region INFO

        /*
         * Aufbau von SDL: {JobID}-{Mailpiece}-{Sendung}/{TotalSendungen}-{Blatt}/{TotalBlatt}-{BlattNummer}[-{Beilagen}][ RS]
         * - JobID:             9-stellige Zahl
         * - Mailpiece:         10-stellige Zahl mit führenden Nullen (TODO: Frage an Dani: ist 10-stellig korrekt?)
         * - Sendung:           Index der aktuellen Sendung
         * - TotalSendungen:    Total-Anzahl der Sendungen
         * - Blatt:             Index des aktuellen Blatts innerhalb der aktuellen Sendung
         * - TotalBlatt:        Total-Anzahl der Blätter innerhalb der aktuellen Sendung
         * - BlattNummer:       5-stellige aufsteigende Zahl mit führenden Nullen, welche die Blattnummer innerhalb eines Jobs definiert
         * - Beilagen:          Beilagenstation-Abzug (optional)
         * - RS:                Flag das definiert ob Rückseite (optional, nur bei Rückseiten)
         */

        #endregion

        #region SHARED

        // SHARED: Get Errors OR SDL (28.03.2024, SME)
        private static object GetErrorsOrSDL(string sdl, bool returnOnlyErrorList)
        {
            #region Deklarationen

            var errorList = new List<Exception>();
            long jobID;
            long mailpiece;
            int sendung = 0;
            int sendungen = 0;
            int blatt = 0;
            int blattTotal = 0;
            int blattNummer;
            string beilagen = string.Empty;
            bool istRS = false;
            bool hasErrors;

            #endregion

            try
            {
                // Prüfen ob gesetzt
                if (string.IsNullOrEmpty(sdl) || string.IsNullOrEmpty(sdl.Trim()))
                {
                    errorList.Add(new SDLError(sdl, "SDL ist nicht gesetzt!"));
                    return errorList.ToArray();
                }

                // Trimmen
                sdl = sdl.Trim();

                // RS-Flag abhandeln
                if (sdl.EndsWith(" RS"))
                {
                    istRS = true;
                    sdl = sdl.Substring(0, sdl.Length - " RS".Length).TrimEnd();
                }

                // Teile zwischenspeichern + Anzahl prüfen
                var parts = sdl.Split(SdlPartDelimiter);
                if (parts.Length < SdlPartCountMin)
                {
                    errorList.Add(new SDLError(sdl, $"SDL muss mindestens {SdlPartCountMin} Teile haben! Gelieferter Wert = {parts.Length}"));
                    return errorList.ToArray();
                }

                // Teile prüfen
                // => 1. Teil ist Job-ID
                //    - muss nummerisch + positiv sein
                //    - muss 9-stellig
                string part = parts.ElementAt(0);
                if (long.TryParse(part, out jobID))
                {
                    if (jobID <= 0)
                    {
                        errorList.Add(new SDLError(sdl, $"Job-ID muss grösser als 0 sein! Gelieferter Wert = {jobID}"));
                    }
                    else if (jobID.ToString().Length != JobIdLength)
                    {
                        errorList.Add(new SDLError(sdl, $"Job-ID muss {JobIdLength}-stellig sein! Gelieferter Wert = {jobID}"));
                    }
                }
                else
                {
                    errorList.Add(new SDLError(sdl, $"Job-ID ist nicht nummerisch! Gelieferter Wert = {part}"));
                }


                // => 2. Teil ist die Mailpiece
                //    - muss nummerisch + positiv sein
                //    - TODO: Frage an Dani: Hat Mailpiece immer die gleiche Anzahl an Zeichen? (28.03.2024, SME)
                part = parts.ElementAt(1);
                if (long.TryParse(part, out mailpiece))
                {
                    if (mailpiece <= 0)
                    {
                        errorList.Add(new SDLError(sdl, $"Mailpiece muss grösser als 0 sein! Gelieferter Wert = {mailpiece}"));
                    }
                }
                else
                {
                    errorList.Add(new SDLError(sdl, $"Mailpiece ist nicht nummerisch! Gelieferter Wert = {part}"));
                }


                // => 3. Teil ist {Sendung/Sendungen}
                //    - Gesplittet bei "/" müssen es genau 2 Teile sein
                //    - Beide Teile müssen positive Zahlen sein
                //    - Sendung darf nicht grösser sein als Sendungen
                part = parts.ElementAt(2);
                var SvonS = part.Split('/');
                if (SvonS.Length != 2)
                {
                    errorList.Add(new SDLError(sdl, $"Sendung/Sendungen hat nicht genau 2 Teile, sondern {SvonS.Length} Teil(e)! Gelieferter Wert = {part}"));
                }
                else
                {
                    // Flag zurücksetzen
                    hasErrors = false;

                    // Sendung
                    if (int.TryParse(SvonS.First(), out sendung))
                    {
                        if (sendung <= 0)
                        {
                            errorList.Add(new SDLError(sdl, $"Sendung muss grösser als 0 sein! Gelieferter Wert = {sendung}"));
                            hasErrors = true;
                        }
                    }
                    else
                    {
                        errorList.Add(new SDLError(sdl, $"Sendung ist nicht nummerisch! Gelieferter Wert = {SvonS.First()}"));
                        hasErrors = true;
                    }

                    // Sendungen
                    if (int.TryParse(SvonS.Last(), out sendungen))
                    {
                        if (sendungen <= 0)
                        {
                            errorList.Add(new SDLError(sdl, $"Sendungen muss grösser als 0 sein! Gelieferter Wert = {sendungen}"));
                            hasErrors = true;
                        }
                    }
                    else
                    {
                        errorList.Add(new SDLError(sdl, $"Sendungen ist nicht nummerisch! Gelieferter Wert = {SvonS.Last()}"));
                        hasErrors = true;
                    }

                    // Prüfen ob Sendung > Sendungen
                    if (!hasErrors)
                    {
                        if (sendung > sendungen)
                        {
                            errorList.Add(new SDLError(sdl, $"Sendung darf nicht grösser als Sendungen sein!! Gelieferte Werte: Sendung = {sendung}, Sendungen = {sendungen}"));
                        }
                    }
                }


                // => 4. Teil ist {Blatt/Blätter}
                //    - Gesplittet bei "/" müssen es genau 2 Teile sein
                //    - Beide Teile müssen positive Zahlen sein
                //    - Blatt darf nicht grösser sein als Blätter
                part = parts.ElementAt(3);
                var BvonB = part.Split('/');
                if (BvonB.Length != 2)
                {
                    errorList.Add(new SDLError(sdl, $"Blatt/Blätter hat nicht genau 2 Teile, sondern {BvonB.Length} Teil(e)! Gelieferter Wert = {part}"));
                }
                else
                {
                    // Flag zurücksetzen
                    hasErrors = false;

                    // Blatt
                    if (int.TryParse(BvonB.First(), out blatt))
                    {
                        if (blatt <= 0)
                        {
                            errorList.Add(new SDLError(sdl, $"Blatt muss grösser als 0 sein! Gelieferter Wert = {blatt}"));
                            hasErrors = true;
                        }
                    }
                    else
                    {
                        errorList.Add(new SDLError(sdl, $"Blatt ist nicht nummerisch! Gelieferter Wert = {BvonB.First()}"));
                        hasErrors = true;
                    }

                    // Blatt-Total
                    if (int.TryParse(BvonB.Last(), out blattTotal))
                    {
                        if (blattTotal <= 0)
                        {
                            errorList.Add(new SDLError(sdl, $"Blatt-Total muss grösser als 0 sein! Gelieferter Wert = {blattTotal}"));
                            hasErrors = true;
                        }
                    }
                    else
                    {
                        errorList.Add(new SDLError(sdl, $"Blatt-Total ist nicht nummerisch! Gelieferter Wert = {BvonB.Last()}"));
                        hasErrors = true;
                    }

                    // Prüfen ob Blatt > Blatt-Total
                    if (!hasErrors)
                    {
                        if (blatt > blattTotal)
                        {
                            errorList.Add(new SDLError(sdl, $"Blatt darf nicht grösser als Blatt-Total sein!! Gelieferte Werte: Blatt = {blatt}, Blatt-Total = {blattTotal}"));
                        }
                    }
                }


                // => 5. Teil ist Blattnummer
                //    - muss nummerisch + grösser als 0 sein
                part = parts.ElementAt(4);
                if (int.TryParse(part, out blattNummer))
                {
                    if (blattNummer <= 0)
                    {
                        errorList.Add(new SDLError(sdl, $"Blattnummer muss grösser als 0 sein! Gelieferter Wert = {blattNummer}"));
                    }
                }
                else
                {
                    errorList.Add(new SDLError(sdl, $"Blattnummer ist nicht nummerisch! Gelieferter Wert = {part}"));
                }


                // => 6. Teil und alle folgenden Teile sind Beilagen-Info (optional)
                // TODO: Frage an Dani: Beginnt es immer mit B gefolgt von Zahlen? (09.04.2024, SME)
                if (parts.Length >= 6)
                {
                    // Beilagen extrahieren (alle ab 6. Teil)
                    var beilagenList = new List<string>();
                    for (int i = 5; i < parts.Length; i++)
                    {
                        beilagenList.Add(parts.ElementAt(i));
                    }
                    part = string.Join("-", beilagenList);
                    //part = parts.ElementAt(5); // remarked (23.05.2024, SME)
                    if (string.IsNullOrEmpty(part))
                    {
                        errorList.Add(new SDLError(sdl, "Beilage-Info ist nicht gesetzt!"));
                    }
                    else if (!part.StartsWith("B"))
                    {
                        errorList.Add(new SDLError(sdl, $"Beilage-Info ist ungültig, weil sie nicht mit einem 'B' beginnt! Gelieferter Wert = {part}"));
                    }
                    else
                    {
                        string beilagenZahlen = part.Substring(1);
                        if (string.IsNullOrEmpty(beilagenZahlen))
                        {
                            // Das kommt vor bei Domtrac => zulassen (23.05.2024, SME)
                            //errorList.Add(new SDLError(sdl, $"Beilage-Info ist ungültig, weil nach dem Anfangs-'B' keine Zahlen kommen! Gelieferter Wert = {part}"));
                            beilagen = part;
                        }
                        else if (!int.TryParse(beilagenZahlen, out _))
                        {
                            // Das kommt vor bei Domtrac => zulassen (23.05.2024, SME)
                            //errorList.Add(new SDLError(sdl, $"Beilage-Info ist ungültig, weil nach dem Anfangs-'B' keine gültigen Zahlen kommen! Gelieferter Wert = {part}"));
                            beilagen = part;
                        }
                        else
                        {
                            beilagen = part;
                        }
                    }
                }
                

                // return
                if (errorList.Any())
                {
                    // return errors
                    return errorList.ToArray();
                }
                else if (returnOnlyErrorList)
                {
                    // dont create sdl but return errors
                    return errorList.ToArray();
                }
                else
                {
                    // return SDL
                    return new SDL(
                        jobID: jobID, 
                        mailpiece: mailpiece, 
                        sendung: sendung, 
                        sendungen: sendungen, 
                        blatt: blatt, 
                        blattTotal: blattTotal, 
                        blattNummer: blattNummer, 
                        beilagen: beilagen,
                        istRS: istRS);
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // SHARED: Get Errors (28.03.2024, SME)
        public static Exception[] GetErrors(string sdl)
        {
            return GetErrorsOrSDL(sdl, true) as Exception[];
        }

        // SHARED: Is valid SQL (28.03.2024, SME)
        public static bool IsValidSDL(string sdl)
        {
            return !GetErrors(sdl).Any();
        }

        // SHARED: Get SDL from String (28.03.2024, SME)
        public static SDL FromString(string sdl)
        {
            var returnValue = GetErrorsOrSDL(sdl, false);
            if (returnValue is SDL sdlInstance) return sdlInstance;
            if (returnValue is Exception[] errors)
            {
                if (!errors.Any()) throw new Exception("SDL konnte nicht erstellt werden!");
                if (errors.Length == 1)
                {
                    CoreFC.ThrowError(errors[0]);
                    throw errors[0];
                }
                throw new AggregateException(errors);
            }
            throw new InvalidCastException($"Die SDL konnte nicht erstellt werden, weil der Rückgabe-Wert nicht behandlet ist: {returnValue.GetType()}");
        }

        #endregion

        #region General

        // New Instance (28.03.2024, SME)
        private SDL(long jobID, long mailpiece, int sendung, int sendungen, int blatt, int blattTotal, int blattNummer, string beilagen, bool istRS)
        {
            // error-handling
            if (jobID <= 0) throw new ArgumentOutOfRangeException(nameof(jobID), $"Job-ID muss grösser als 0 sein! Gelieferter Wert = {jobID}");
            if (jobID.ToString().Length != JobIdLength) throw new ArgumentOutOfRangeException(nameof(jobID), $"Job-ID muss {JobIdLength}-stellig sein! Gelieferter Wert = {jobID}");
            if (mailpiece < 0) throw new ArgumentOutOfRangeException(nameof(mailpiece), $"Mailpiece darf nicht kleiner als 0 sein! Gelieferter Wert = {mailpiece}");
            if (sendung <= 0) throw new ArgumentOutOfRangeException(nameof(sendung), $"Sendung muss grösser als 0 sein! Gelieferter Wert = {sendung}");
            if (sendungen <= 0) throw new ArgumentOutOfRangeException(nameof(sendungen), $"Sendungen muss grösser als 0 sein! Gelieferter Wert = {sendungen}");
            if (sendung > sendungen) throw new ArgumentOutOfRangeException(nameof(sendung), $"Sendung darf nicht grösser als Sendungen sein!! Gelieferte Werte: Sendung = {sendung}, Sendungen = {sendungen}");
            if (blatt <= 0) throw new ArgumentOutOfRangeException(nameof(blatt), $"Blatt muss grösser als 0 sein! Gelieferter Wert = {blatt}");
            if (blattTotal <= 0) throw new ArgumentOutOfRangeException(nameof(blattTotal), $"Blatt-Total muss grösser als 0 sein! Gelieferter Wert = {blattTotal}");
            if (blatt > blattTotal) throw new ArgumentOutOfRangeException(nameof(blatt), $"Blatt darf nicht grösser als Blatt-Total sein!! Gelieferte Werte: Blatt = {blatt}, Blatt-Total = {blattTotal}");
            if (blattNummer <= 0) throw new ArgumentOutOfRangeException(nameof(blattTotal), $"Blatt-Nummer muss grösser als 0 sein! Gelieferter Wert = {blattNummer}");

            // set properties
            this.JobID = jobID;
            this.Mailpiece = mailpiece;
            this.Sendung = sendung;
            this.Sendungen = sendungen;
            this.Blatt = blatt;
            this.BlattTotal = blattTotal;
            this.BlattNummer = blattNummer;
            this.Beilagen = beilagen;
            this.IstRS = istRS;
            Beilagen = beilagen;

        }

        // ToString
        public override string ToString()
        {
            try
            {
                if (string.IsNullOrEmpty(Beilagen))
                {
                    return $"{JobID}-{MailpieceString}-{Sendung}/{Sendungen}-{Blatt}/{BlattTotal}-{BlattNummerString}" + (IstRS ? "  RS" : "");
                }
                else
                {
                    return $"{JobID}-{MailpieceString}-{Sendung}/{Sendungen}-{Blatt}/{BlattTotal}-{BlattNummerString}-{Beilagen}" + (IstRS ? "  RS" : "");
                }
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        // Contants
        public const char SdlPartDelimiter = '-';
        public const int SdlPartCountMin = 5;
        public const int MailpieceLength = 10;
        public const string MailpieceFormat = "0000000000";
        public const int BlattNummerLength = 5;
        public const string BlattNummerFormat = "00000";

        #endregion

        #region Errors

        public class SDLError: TC.Errors.CoreError
        {
            #region General

            // New Instance with Message (28.03.2024, SME)
            public SDLError(string sdl, string message): base(message) { Initialize(sdl); }

            // Initialize
            private void Initialize(string sdl)
            {
                // set properties
                this.SDL = sdl;

                // add parameters
                base.AddParameter(new TC.Classes.ClsNamedParameter("SDL", sdl));
            }

            #endregion

            #region Properties

            public string SDL { get; private set; }

            #endregion
        }
        
        #endregion

        #region Properties

        public long JobID { get; }
        public long Mailpiece { get; }
        public string MailpieceString => Mailpiece.ToString(MailpieceFormat);
        public int Sendung { get; }
        public int Sendungen { get; }
        public int Blatt { get; }
        public int BlattTotal { get; }
        public int BlattNummer { get; }
        public string BlattNummerString => BlattNummer.ToString(BlattNummerFormat);
        public string Beilagen { get; }
        public bool IstRS { get; }

        #endregion
    }
}
