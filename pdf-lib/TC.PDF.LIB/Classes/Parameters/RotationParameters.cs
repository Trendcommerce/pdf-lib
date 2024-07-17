using System;
using System.Collections.Generic;
using System.Linq;

namespace TC.PDF.LIB.Classes.Parameters
{
    // Parameter-Klasse für Rotation (16.10.2023, SME)
    public class RotationParameters
    {
        // Neue Instanz
        public RotationParameters(PdfRotationPagesOptionEnum pagesOption, PdfRotationDirectionEnum direction, List<int> specificPages = null) 
        { 
            // error-handling
            if (!Enum.IsDefined(typeof(PdfRotationPagesOptionEnum), pagesOption)) 
            {
                throw new ArgumentOutOfRangeException(nameof(pagesOption), "Ungültige Seiten-Option: " + pagesOption.ToString());
            }
            else if (!Enum.IsDefined(typeof(PdfRotationDirectionEnum), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction), "Ungültige Rotationsrichtung: " + direction.ToString());
            }
            else if (pagesOption == PdfRotationPagesOptionEnum.SpecificPages && (specificPages == null || !specificPages.Any()))
            {
                throw new ArgumentNullException(nameof(specificPages));
            }

            // set properties
            PagesOption = pagesOption;
            Direction = direction;
            SpecificPages = specificPages;
        }

        // Properties
        public PdfRotationPagesOptionEnum PagesOption { get; }
        public PdfRotationDirectionEnum Direction { get; }
        public List<int> SpecificPages { get; }
    }
}
