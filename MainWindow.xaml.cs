// copyright 2020 Garrett Skelton
// MIT license
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Manga_Scraper {
    public partial class MainWindow : Window {
        static readonly HttpClient httpClient = new HttpClient();
        public MainWindow() {
            InitializeComponent();
        }

        private async Task<string> DownloadUrlToString(string url) {
            string responseBody = "";

            try {
                responseBody = await httpClient.GetStringAsync(url);
            } catch (Exception) { }

            return responseBody;
        }

        private async Task DownloadUrlToFile(string url, string folderPath, string desiredfilename) {      
            try {
                var response = await httpClient.GetAsync(url);
                Directory.CreateDirectory(folderPath);
                using var fs = new FileStream(string.Format("{0}/{1}.{2}", folderPath, desiredfilename, url.Split(".").LastOrDefault()), FileMode.CreateNew);
                await response.Content.CopyToAsync(fs);
            } catch (Exception) { }
        }

        private async void ScrapeButton_Click(object sender, RoutedEventArgs e) {
            var url = UrlBox.Text;
            var outName = (string)NameBox.Content;

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(outName)) {
                StatusBox.Text = "Incomplete info";
                return;
            }

            StatusBox.Text = "Loading page";
            var folderPath = outName[0..^4] + "MangaScrapTemp";
            string responseBody = await DownloadUrlToString(url);
            var dots = new List<string> { ".", "..", "..." };
            int count = 0;
            var matches = new Regex("https[^\"]*\\.(webp|png|jpe?g)").Matches(responseBody);
            foreach (Match match in matches) {
                ++count;
                ProgressBox.Value = count / (double)matches.Count;
                if (match.Value.Contains("icon")) {
                    continue;
                }
                StatusBox.Text = "Loading images " + dots[count % 3];
                await DownloadUrlToFile(match.Value, folderPath, count.ToString("000.##"));
            }

            StatusBox.Text = "Zipping";
            File.Delete(outName);
            ZipFile.CreateFromDirectory(folderPath, outName);
            Directory.Delete(folderPath, true);
            StatusBox.Text = "Done! Ready!";
        }

        private void NameBox_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog fileDialog = new SaveFileDialog { FileName = "Manga", DefaultExt = ".cbz", Filter = "Comic Book Zip (.cbz)|*.cbz" };

            if (fileDialog.ShowDialog() == true) {
                NameBox.Content = fileDialog.FileName;
            }
        }
    }
}
