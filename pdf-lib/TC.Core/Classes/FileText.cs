using System;
using System.IO;

namespace TC.Classes
{
    public class FileText
    {
        // Neue Instanz mit Pfad + Text (27.10.2023, SME)
        public FileText(string path, string text)
        {
            // error-handling
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            // set properties
            Path = path;
            Text = (string.IsNullOrEmpty(text) ? string.Empty : text);
        }

        // Neue Instanz mit Pfad + Stream (27.10.2023, SME)
        public FileText(string path, Stream stream, bool disposeStream = false)
        {
            // error-handling
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // set properties
            Path = path;
            using (var reader = new StreamReader(stream))
            {
                Text = reader.ReadToEnd();
            }
            if (disposeStream) stream.Dispose();
        }

        // Neue Instanz mit File-Info (27.10.2023, SME)
        public FileText(FileInfo file)
        {
            // error-handling
            if (file == null) throw new ArgumentNullException(nameof(file));

            // set properties
            Path = file.FullName;
            Text = File.ReadAllText(file.FullName);
        }

        // Properties
        public string Path { get; }
        public string Text { get; }
    }
}
