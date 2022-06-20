namespace ArchiveOrgCollectionSync
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Xml.Serialization;

    using Microsoft.WindowsAPICodePack.Dialogs;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.UrlTextBox.Focus();
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            this.UrlTextBox.Text = Clipboard.GetText();
            this.UrlTextBox.Focus();
            this.UrlTextBox.SelectAll();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string folder = string.IsNullOrWhiteSpace(this.FolderTextBox.Text) || !Directory.Exists(this.FolderTextBox.Text) ? null : this.FolderTextBox.Text;

            var dialog = new CommonOpenFileDialog
            {
                Title = "Browse for Destination Folder",
                IsFolderPicker = true,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true,
                InitialDirectory = folder,
                DefaultDirectory = folder
            };

            if (dialog.ShowDialog(Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive)) == CommonFileDialogResult.Ok)
            {
                this.FolderTextBox.Text = dialog.FileName;
                this.FolderTextBox.Focus();
                this.FolderTextBox.SelectAll();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.StartPauseButton.IsEnabled = this.FolderTextBox.Text.Trim().Length > 0 && this.UrlTextBox.Text.Trim().Length > 0;
        }

        private void StartPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.UrlTextBox.Text.Trim().StartsWith("https://archive.org/details/", StringComparison.OrdinalIgnoreCase) || this.UrlTextBox.Text.Trim().Length < 29)
            {
                MessageBox.Show(this, "Incorrect Archive.org Collection URL. A URL in the format of https://archive.org/details/<collection> is expected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string collectionName = this.UrlTextBox.Text.Trim().Substring(28);
            string folder = this.FolderTextBox.Text.Trim();

            if (!Directory.Exists(folder))
            {
                MessageBox.Show(this, "Destination folder does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.Start(collectionName, folder);
        }

        private void Start(string collectionName, string folder)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string collectionXmlUrl = $"https://archive.org/download/{collectionName}/{collectionName}_files.xml";
                string collectionXml;

                try
                {
                    using (var client = new WebClient())
                    {
                        collectionXml = client.DownloadString(collectionXmlUrl);
                    }
                }
                catch (Exception ex)
                {
                    this.Dispatcher.Invoke(() => MessageBox.Show(this, $"Unable to parse collection metadata. Are you sure that it exists?\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }

                var serializer = new XmlSerializer(typeof(FileCollection));
                File[] files;
                int count = 0;

                using (var reader = new StringReader(collectionXml))
                {
                    files = ((FileCollection)serializer.Deserialize(reader)).Files;
                }

                this.Dispatcher.Invoke(() => this.ProgressBar.Maximum = files.Length);

                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 10 }, file =>
                {
                    this.Dispatcher.Invoke(() => this.ProgressBar.Value = count);
                    count++;

                    string destinationFilePath = Path.Combine(folder, file.Name);

                    if (System.IO.File.Exists(destinationFilePath))
                    {
                        if (new FileInfo(destinationFilePath).Length == file.Size && MainWindow.ConfirmMd5(destinationFilePath, file.Md5))
                        {
                            return;
                        }

                        System.IO.File.Delete(destinationFilePath);
                    }

                    using (var client = new WebClient())
                    {
                        try
                        {
                            client.DownloadFile($"https://archive.org/download/{collectionName}/{file.Name}", destinationFilePath);
                        }
                        catch (Exception ex)
                        {
                            this.Dispatcher.Invoke(() => MessageBox.Show(this, $"An error occurred downloading file {file.Name}.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                            return;
                        }
                    }

                    if (!MainWindow.ConfirmMd5(destinationFilePath, file.Md5))
                    {
                        this.Dispatcher.Invoke(() => MessageBox.Show(this, $"Checksum does not match on file {file.Name}!", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                });

                this.Dispatcher.Invoke(() => MessageBox.Show(this, $"Process is complete!", "Success", MessageBoxButton.OK, MessageBoxImage.Information));
            });
        }

        private static bool ConfirmMd5(string filePath, string md5Hash)
        {
            using (var stream = System.IO.File.OpenRead(filePath))
            {
                using (var md5 = MD5.Create())
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() == md5Hash;
                }
            }
        }
    }
}