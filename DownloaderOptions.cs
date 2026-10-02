using System;

namespace WindowsDownloader
{
    public class DownloaderOptions
    {
        public string LanguageName { get; set; } = "Russian";
        public string Locale { get; set; } = "ru-ru";
        public string ProductEditionId { get; set; } = "3262";
        public int ArchType { get; set; } = 1;
        public bool CopyToClipboard { get; set; } = true;
        public bool Quiet { get; set; } = false;
        public bool ShowHelp { get; set; } = false;

        public static DownloaderOptions Parse(string[] args)
        {
            var options = new DownloaderOptions();
            if (args == null || args.Length == 0) return options;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "--help" || arg == "-h" || arg == "/?")
                {
                    options.ShowHelp = true;
                }
                else if ((arg == "--lang" || arg == "-l") && i + 1 < args.Length)
                {
                    options.LanguageName = args[++i];
                }
                else if (arg == "--locale" && i + 1 < args.Length)
                {
                    options.Locale = args[++i];
                }
                else if ((arg == "--edition" || arg == "-e") && i + 1 < args.Length)
                {
                    options.ProductEditionId = args[++i];
                }
                else if ((arg == "--arch" || arg == "-a") && i + 1 < args.Length && int.TryParse(args[i + 1], out int arch))
                {
                    options.ArchType = arch;
                    i++;
                }
                else if (arg == "--no-clipboard")
                {
                    options.CopyToClipboard = false;
                }
                else if (arg == "--quiet" || arg == "-q")
                {
                    options.Quiet = true;
                }
            }
            return options;
        }
    }
}
