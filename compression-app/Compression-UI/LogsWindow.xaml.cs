using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace Compression_UI;

public partial class LogsWindow : Window
{
    public LogsWindow(string logs)
    {
        InitializeComponent();
        LogsTextBox.Text = logs;
    }

    private void CopyLogsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(LogsTextBox.Text))
        {
            Clipboard.SetText(LogsTextBox.Text);
            MessageBox.Show("Logs copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SaveLogsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = $"compression-logs-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt",
            Title = "Save Logs to File"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, LogsTextBox.Text, Encoding.UTF8);
                MessageBox.Show($"Logs saved successfully to:\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
