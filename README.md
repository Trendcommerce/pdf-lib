![GitHub](https://img.shields.io/github/license/Trendcommerce/pdf-lib)

# PDF Library

`PDF Library` is an efficient C# library that implements lots of functionalities on PDF files like compressing, font-embedding, cropping, merging, splitting, extracting, rotating and much more.
Because it does not need to display the PDF while performing the specified action, it is quite a lot faster then any PDF-Application like for example Adobe.
The library not only robustly handles errors but also maintains a comprehensive logging system for all operational activities.
Additionally for all operational activities, it creates a backup that can be restored later on.

## Featuresrückseite

- **Compress:** Removes duplicated references of images and other resources and therefore reduces a lot of space in the PDF-filesize.
- **Font-Infos:** Retrieves all the informations from the fonts used in a PDF file including base-font, sub-type, encoding, if embedded or not, if subset or not and more.
- **Embed Fonts:** Unembedded fonts can be embedded, when there is a font in the font-database that matches the font-informations.
- **Optimize:** Tries to embed all the unembedded fonts where possible and compresses PDF file.
- **Merge:** Multiple PDF files can be merged into one PDF file.
- **Split:** PDF files can be split by a certain amount of pages.
- **Crop:** Pages can be cropped to A4 or other sizes.
- **Page-Operations:** There are lots of operations that can be performed on a page-level like removing empty pages, extract or multiply or rotate pages, insert pages, and more.
- **Restore:** Before performing an operation that will change the PDF file, a backup will be created, that can be restored later on.

## Usage Guide

1. **Clone the Repository:** Clone this repository to your local machine.
2. **Font Database Setup:** This application needs a database with font-informations and -streams to be able to embed fonts. Set the database connection string as per your setup.
3. **Create Frontend:** Create a frontend to call the PDF file operations.

## Dependencies

The application requires **.NET Framework 4.8** runtime. It also makes use of the **iText7** library for the PDF processing tasks.

## License

This project is licensed under the GNU GENERAL PUBLIC LICENSE v3. For more information, see the [LICENSE](LICENSE) file in the repository.

## Note

Please note that this library only provides the functionality and currently does not have a user interface. For any questions or issues, please raise a ticket in the GitHub repository.
