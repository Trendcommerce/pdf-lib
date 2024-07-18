using System;
using TC.Functions;

namespace TC.Errors
{
    // File-Locked-Error (02.01.2024, SME)
    public class FileLockedError: CoreError
    {
        // New Instance with File-Path§ (02.01.2024, SME)
        public FileLockedError(string filePath): base() 
        { 
            // error-handling
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            // set properties
            FilePath = filePath;
            MessageText = "Der Prozess kann nicht auf die Datei zugreifen, da sie bereits von einem anderen Prozess verwendet wird.";
        }

        // New Instance with File-Path + Message (02.01.2024, SME)
        public FileLockedError(string filePath, string message) : base()
        {
            // error-handling
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

            // set properties
            FilePath = filePath;
            MessageText = message;
        }


        // FilePath
        public string FilePath { get; }

        // Message-Text
        private string MessageText;

        // OVERRIDE: Get Message without Parameter
        protected override string GetMessageWithoutParameter()
        {
            return this.MessageText + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + FilePath;
        }
    }
}
