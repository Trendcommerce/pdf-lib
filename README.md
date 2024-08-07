![GitHub](https://img.shields.io/github/license/Trendcommerce/pdf-lib)

# PDF Library

`PDF Page Splitter` is an efficient, console-based C# application that reads and processes PDF files from a specified folder, identifies any existing whitespace labels, and subsequently creates separate, paginated PDF files. The application not only robustly handles errors but also maintains a comprehensive logging system for all operational activities.

## Features

- **Whitespace Detection:** Capable of pinpointing whitespace labels within PDF files.
- **PDF Pagination:** Paginates and creates separate PDF files upon detection of whitespace labels.
- **Logging System:** Maintains an extensive log of all operations, errors, and informational messages.
- **File Management:** Handles duplicate files by moving them to a "rejected files" folder and updating the log.
- **Database Interactions:** Communicates with a database to track file status and handle duplications.
- **Archiving:** Upon successful processing, files are transferred to an "archived" folder.

## Usage Guide

1. **Clone the Repository:** Clone this repository to your local machine.
2. **Database Setup:** This application needs a database for smooth operation. Set the database connection string as per your setup.
3. **Configure Paths:** Set your desired paths for input files, output files, archived files, and rejected files within the application.
4. **Launch the Application:** Start the application. It will automatically process all the PDF files in the input directory.
5. **Log Review:** Review the application logs to track and understand all operations carried out on each file.
6. **Output Verification:** Paginated PDFs will be stored in the output folder, as specified in your configuration.

## Dependencies

The application requires **.NET Core 3.1+** runtime. It also makes use of the **iText7** library for the PDF processing tasks.

## License

This project is licensed under the GNU GENERAL PUBLIC LICENSE v3. For more information, see the [LICENSE](LICENSE) file in the repository.

## Note

Please note that this application is intended for use via the console/terminal and currently does not have a user interface. For any questions or issues, please raise a ticket in the GitHub repository.
