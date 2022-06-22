namespace ArchiveOrgCollectionSync
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
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
            this.StartButton.IsEnabled = this.FolderTextBox.Text.Trim().Length > 0 && this.UrlTextBox.Text.Trim().Length > 0;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
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

        private void ChangeState(bool running)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.StartButton.IsEnabled =
                    this.UrlTextBox.IsEnabled =
                    this.FolderTextBox.IsEnabled =
                    this.PasteButton.IsEnabled =
                    this.BrowseButton.IsEnabled =
                    !running;

                if (!running)
                {
                    this.ProgressBar.Value = 0;
                }
            });
        }

        private void Report(string message, bool error = false, bool complete = false)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.ProgressTextBlock.Text = message;
                this.ProgressTextBlock.Visibility = Visibility.Visible;
                
                if (error)
                {
                    this.ProgressTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (complete)
                {
                    this.ProgressTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    this.ProgressTextBlock.Foreground = SystemColors.ControlTextBrush;
                }

                var item = new LogItem(message, error, complete);
                this.LogListBox.Items.Add(item);
                this.LogListBox.SelectedIndex = this.LogListBox.Items.Count - 1;
                this.LogListBox.ScrollIntoView(item);
            });
        }

        private void Start(string collectionName, string folder)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                this.ChangeState(true);
                this.Report("Scanning archive.org collection files...");

                string collectionXmlUrl = $"https://archive.org/download/{collectionName}/{collectionName}_files.xml";
                string collectionXml;

                try
                {
                    using (var client = new PatientWebClient())
                    {
                        collectionXml = client.DownloadString(collectionXmlUrl);
                    }
                }
                catch (Exception ex)
                {
                    this.Report($"Unable to parse collection metadata; the XML list of files could not be downloaded. - {ex.Message}", true);
                    return;
                }

                File[] files;

                try
                {
                    var serializer = new XmlSerializer(typeof(FileCollection));

                    using (var reader = new StringReader(collectionXml))
                    {
                        files = ((FileCollection)serializer.Deserialize(reader)).Files;
                    }
                }
                catch (Exception ex)
                {
                    this.Report($"Unable to deserialize the list of files from archive.org. - {ex.Message}", true);
                    return;
                }

                this.Dispatcher.Invoke(() => this.ProgressBar.Maximum = files.Length);

                int count = 0;
                int skippingCount = 0;
                int downloadErrorCount = 0;
                int downloadCorruptCount = 0;
                int downloadSuccessCount = 0;

                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 10 }, file =>
                {                   
                    count++;

                    this.Dispatcher.Invoke(() =>
                    {
                        this.ProgressBar.Value = count;
                    });

                    this.Report($"Reading disk file {file.Name}...");

                    string destinationFilePath = Path.Combine(folder, file.Name);

                    if (System.IO.File.Exists(destinationFilePath))
                    {
                        if (destinationFilePath.EndsWith($"{collectionName}_files.xml") ||
                            new FileInfo(destinationFilePath).Length == file.Size && MainWindow.ConfirmMd5(destinationFilePath, file.Md5))
                        {
                            this.Report($"File {file.Name} already exists with the correct file size and checksum. Skipping!");
                            skippingCount++;
                            return;
                        }

                        this.Report($"Existing file {file.Name} has an incorrect file size or checksum! Redownloading file...", true);
                        System.IO.File.Delete(destinationFilePath);
                    }
                    else
                    {
                        this.Report($"File {file.Name} does not exist on disk! Downloading file...");
                    }

                    using (var client = new PatientWebClient())
                    {
                        try
                        {
                            client.DownloadFile($"https://archive.org/download/{collectionName}/{file.Name}", destinationFilePath);
                        }
                        catch (Exception ex)
                        {
                            this.Report($"An error occurred downloading file {file.Name}. - {ex.Message}", true);
                            downloadErrorCount++;
                            return;
                        }
                    }

                    if (destinationFilePath.EndsWith($"{collectionName}_files.xml") || MainWindow.ConfirmMd5(destinationFilePath, file.Md5))
                    {
                        this.Report($"File {file.Name} downloaded successfully and checksum confirmed!");
                        downloadSuccessCount++;
                    }
                    else
                    {
                        this.Report($"Checksum does not match on file {file.Name} after downloading!", true);
                        downloadCorruptCount++;
                    }
                });

                this.Report($"{count} file(s) from Archive.org were processed!", false, true);
                this.Report($"{skippingCount} file(s) already existed with the correct file size and checksum.", false, true);
                this.Report($"{downloadSuccessCount} file(s) were successfully downloaded.", false, true);
                this.Report($"{downloadErrorCount} file(s) were not downloaded successfully.", downloadErrorCount > 0, downloadErrorCount == 0);
                this.Report($"{downloadCorruptCount} file(s) had an incorrect checksum after downloading.", downloadCorruptCount > 0, downloadCorruptCount == 0);
                this.Report($"Process is complete!", false, true);
                this.ChangeState(false);
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

        private void LogTabItem_Selected(object sender, RoutedEventArgs e)
        {
            this.LogListBox.ScrollIntoView(this.LogListBox.SelectedItem);
        }
    }
}