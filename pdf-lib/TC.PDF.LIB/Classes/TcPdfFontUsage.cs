using System;
using System.Collections.Generic;
using System.Linq;

namespace TC.PDF.LIB.Classes
{
    // TC-PDF-Font-Usage (23.06.2023, SME)
    public class TcPdfFontUsage
    {
        // Neue Instanz
        public TcPdfFontUsage(TcPdfFont font)
        {
            // error-handling
            if (font == null) throw new ArgumentNullException(nameof(font));

            // set local properties
            Font = font;
            Count = 1;
        }

        // Properties
        public readonly TcPdfFont Font;
        public int Count { get; internal set; }
        private readonly List<int> PagesList = new List<int>();
        public int[] Pages => PagesList.OrderBy(x => x).ToArray();

        // Add Page
        internal void AddPage(int page)
        {
            if (!PagesList.Contains(page)) PagesList.Add(page);
        }

        // Add Pages
        internal void AddPages(params int[] pages)
        {
            if (pages != null && pages.Any())
            {
                foreach (var page in pages)
                {
                    AddPage(page);
                }
            }
        }
    }
}
