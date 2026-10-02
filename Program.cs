using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace WindowsDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Any(a => a == "--help" || a == "-h" || a == "/?"))
                {
                    ShowUsage();
                    return;
                }

                var options = ParseOptions(args);
                RunAsync(options).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ОШИБКА]: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"         {ex.InnerException.Message}");
                Console.ResetColor();
            }

            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("\nНажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Использование: GetWindowsIso.exe [опции]");
            Console.WriteLine("Опции:");
            Console.WriteLine("  --lang <name>       Язык (по умолчанию: Russian)");
            Console.WriteLine("  --locale <code>     Локаль запроса (по умолчанию: ru-ru)");
            Console.WriteLine("  --edition <id>      ID редакции продукта (по умолчанию: 3262 - Win 11)");
            Console.WriteLine("  --arch <1|2>        Архитектура (1: x64, 2: x86/arm64, по умолчанию: 1)");
            Console.WriteLine("  --help, -h          Справка");
        }

        private static WindowsDownloadUrlProvider ParseOptions(string[] args)
        {
            var provider = new WindowsDownloadUrlProvider();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if ((arg == "--lang" || arg == "-l") && i + 1 < args.Length)
                {
                    provider.LanguageName = args[++i];
                }
                else if ((arg == "--locale") && i + 1 < args.Length)
                {
                    provider.Locale = args[++i];
                }
                else if ((arg == "--edition" || arg == "-e") && i + 1 < args.Length)
                {
                    provider.ProductEditionId = args[++i];
                }
                else if ((arg == "--arch" || arg == "-a") && i + 1 < args.Length && int.TryParse(args[i + 1], out int arch))
                {
                    provider.ArchType = arch;
                    i++;
                }
            }
            return provider;
        }

        private static async Task RunAsync(WindowsDownloadUrlProvider provider)
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                SecurityProtocolType.Tls13;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Microsoft Windows ISO Downloader ===");
            Console.WriteLine($"Параметры: Издание ID={provider.ProductEditionId}, Язык={provider.LanguageName}, Локаль={provider.Locale}");
            Console.ResetColor();

            string url = await provider.GetDownloadUrlAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[УСПЕХ] Прямая ссылка на ISO:");
            Console.WriteLine("--------------------------------------------------");
            Console.ResetColor();
            Console.WriteLine(url);
            Console.WriteLine("--------------------------------------------------");

            // Копирование через STA-поток
            try
            {
                CopyToClipboard(url);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[OK] Ссылка успешно скопирована в буфер обмена!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[!] Не удалось скопировать в буфер: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void CopyToClipboard(string text)
        {
            Exception threadEx = null;

            var thread = new Thread(() =>
            {
                try
                {
                    Clipboard.SetText(text);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (threadEx != null)
                throw threadEx;
        }
    }

    public class WindowsDownloadUrlProvider
    {
        public string ProductEditionId { get; set; } = "3262";   // Windows 11
        public string Locale { get; set; } = "ru-ru";
        public string LanguageName { get; set; } = "Russian";
        public int ArchType { get; set; } = 1;                   // x64
        private const string OrgId = "y6jn8c31";
        private const string ProfileId = "606624d44113";
        private const string InstanceId = "560dc9f3-1aa5-4a2f-b63c-9e18f8d0e175";

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public async Task<string> GetDownloadUrlAsync()
        {
            var sessionId = Guid.NewGuid().ToString("N");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language",
                "ru-RU,ru;q=0.9,en;q=0.8");

            await _httpClient.GetAsync(
                $"https://vlscppe.microsoft.com/tags?org_id={OrgId}&session_id={sessionId}");

            var mdtJs = await _httpClient.GetStringAsync(
                $"https://ov-df.microsoft.com/mdt.js?instanceId={InstanceId}&PageId=si&session_id={sessionId}");

            var w = Regex.Match(mdtJs, @"[?&]w=([A-F0-9]+)").Groups[1].Value;
            var rticks = Regex.Match(mdtJs, "rticks=\"\\+?(\\d+)").Groups[1].Value;

            // The PowerShell reference throws here; mirror that so a changed page
            // format fails loudly instead of sending an empty telemetry request.
            if (string.IsNullOrEmpty(w))
                throw new Exception("Не удалось извлечь параметр 'w' из mdt.js — формат страницы Microsoft изменился.");
            if (string.IsNullOrEmpty(rticks))
                throw new Exception("Не удалось извлечь параметр 'rticks' из mdt.js — формат страницы Microsoft изменился.");

            var mdt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            await _httpClient.GetAsync(
                $"https://ov-df.microsoft.com/?session_id={sessionId}&CustomerId={InstanceId}&PageId=si&w={w}&mdt={mdt}&rticks={rticks}");

            var skuUrl =
                $"https://www.microsoft.com/software-download-connector/api/getskuinformationbyproductedition" +
                $"?profile={ProfileId}&productEditionId={ProductEditionId}" +
                $"&SKU=undefined&friendlyFileName=undefined&Locale={Locale}&sessionID={sessionId}";

            var skuJson = await _httpClient.GetStringAsync(skuUrl);
            var skuObj = JObject.Parse(skuJson);

            var skus = skuObj["Skus"] as JArray;
            if (skus == null || !skus.HasValues)
                throw new Exception("Список языков пуст.");

            var targetSku = skus.FirstOrDefault(s =>
                s["Language"]?.Value<string>()?.Trim()
                    .Equals(LanguageName, StringComparison.OrdinalIgnoreCase) == true);

            if (targetSku == null)
            {
                var available = string.Join(", ",
                    skus.Select(s => s["Language"]?.Value<string>()));
                throw new Exception($"Язык '{LanguageName}' не найден. Доступны: {available}");
            }

            var skuId = targetSku["Id"]?.Value<string>();
            if (string.IsNullOrEmpty(skuId))
                throw new Exception("В ответе SKU отсутствует обязательное поле 'Id'.");

            var linkUrl =
                $"https://www.microsoft.com/software-download-connector/api/GetProductDownloadLinksBySku" +
                $"?profile={ProfileId}&productEditionId=undefined" +
                $"&SKU={skuId}&friendlyFileName=undefined" +
                $"&Locale={Locale}&sessionID={sessionId}";

            var request = new HttpRequestMessage(HttpMethod.Get, linkUrl)
            {
                Headers = { Referrer = new Uri("https://www.microsoft.com/software-download/windows11") }
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var linksJson = await response.Content.ReadAsStringAsync();
            var linksObj = JObject.Parse(linksJson);

            var options = linksObj["ProductDownloadOptions"] as JArray;
            var targetLink = options?.FirstOrDefault(o =>
                o["DownloadType"]?.Value<int>() == ArchType);

            if (targetLink == null)
                throw new Exception("Ссылка для x64 не найдена.");

            var uri = targetLink["Uri"]?.Value<string>();
            if (string.IsNullOrEmpty(uri))
                throw new Exception("В ответе отсутствует поле 'Uri' для x64-ссылки.");

            return uri;
        }
    }
}
