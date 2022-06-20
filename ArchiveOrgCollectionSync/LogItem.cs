namespace ArchiveOrgCollectionSync
{
    using System.Windows;
    using System.Windows.Media;

    public class LogItem
    {
        public LogItem(string message, bool error, bool complete)
        {
            this.Message = message;

            if (error)
            {
                this.Brush = new SolidColorBrush(Colors.Red);
            }
            else if (complete)
            {
                this.Brush = new SolidColorBrush(Colors.Green);
            }
            else
            {
                this.Brush = SystemColors.ControlTextBrush;
            }
        }

        public string Message { get; set; }

        public Brush Brush { get; set; }
    }
}