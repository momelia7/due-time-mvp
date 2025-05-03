using System.Windows;

namespace DueTime.UI
{
    public partial class SummaryWindow : Window
    {
        public SummaryWindow(string summaryText)
        {
            InitializeComponent();
            SummaryTextBox.Text = summaryText;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(SummaryTextBox.Text);
            System.Windows.MessageBox.Show("Summary copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
} 