using System;
using System.Linq;

namespace TC.PDF.LIB.Classes.Parameters
{
    // Parameter-Klasse für die Extrahierung von Mailpieces (22.05.2024, SME)
    public class MailpieceExtractionParameters
    {
        // Neue Instanz (22.05.2024, SME)
        public MailpieceExtractionParameters(bool includeVorNachlauf, params string[] mailpieces)
        {
            // error-handling
            if (mailpieces == null || !mailpieces.Any()) throw new ArgumentNullException(nameof(mailpieces));
            
            // set properties
            IncludeVorNachlauf = includeVorNachlauf;
            Mailpieces = mailpieces.Distinct().OrderBy(x => x).ToArray();
        }

        // Properties
        public bool IncludeVorNachlauf { get; private set; }
        public string[] Mailpieces { get; private set; }
    }
}
