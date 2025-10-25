using System.Windows;

namespace Compression_UI;

public partial class SettingsWindow : Window
{
    public enum WordReductionMethod
    {
        None,
        Stemming,
        Lemmatization
    }

    public class PipelineSettings
    {
        public bool EnableDiacritics { get; set; } = true;
        public bool EnableStopwords { get; set; } = true;
        public bool EnableSynonyms { get; set; } = true;
        public WordReductionMethod ReductionMethod { get; set; } = WordReductionMethod.Lemmatization;
        public bool EnableAggressive { get; set; } = false;
        public bool EnableArithmeticCoding { get; set; } = false;
        public bool UseLossyMode { get; set; } = false;
    }

    public PipelineSettings Settings { get; private set; }
    public bool SettingsSaved { get; private set; } = false;

    public SettingsWindow(PipelineSettings currentSettings)
    {
        InitializeComponent();
        Settings = new PipelineSettings
        {
            EnableDiacritics = currentSettings.EnableDiacritics,
            EnableStopwords = currentSettings.EnableStopwords,
            EnableSynonyms = currentSettings.EnableSynonyms,
            ReductionMethod = currentSettings.ReductionMethod,
            EnableAggressive = currentSettings.EnableAggressive,
            EnableArithmeticCoding = currentSettings.EnableArithmeticCoding,
            UseLossyMode = currentSettings.UseLossyMode
        };

        LoadSettings();
    }

    private void LoadSettings()
    {
        DiacriticsCheckBox.IsChecked = Settings.EnableDiacritics;
        StopwordsCheckBox.IsChecked = Settings.EnableStopwords;
        SynonymsCheckBox.IsChecked = Settings.EnableSynonyms;

        NoReductionRadio.IsChecked = Settings.ReductionMethod == WordReductionMethod.None;
        StemmingRadio.IsChecked = Settings.ReductionMethod == WordReductionMethod.Stemming;
        LemmatizationRadio.IsChecked = Settings.ReductionMethod == WordReductionMethod.Lemmatization;

        AggressiveModeCheckBox.IsChecked = Settings.EnableAggressive;
        ArithmeticCodingCheckBox.IsChecked = Settings.EnableArithmeticCoding;
        LossyModeCheckBox.IsChecked = Settings.UseLossyMode;
    }

    private void SaveSettings()
    {
        Settings.EnableDiacritics = DiacriticsCheckBox.IsChecked == true;
        Settings.EnableStopwords = StopwordsCheckBox.IsChecked == true;
        Settings.EnableSynonyms = SynonymsCheckBox.IsChecked == true;

        if (NoReductionRadio.IsChecked == true)
            Settings.ReductionMethod = WordReductionMethod.None;
        else if (StemmingRadio.IsChecked == true)
            Settings.ReductionMethod = WordReductionMethod.Stemming;
        else if (LemmatizationRadio.IsChecked == true)
            Settings.ReductionMethod = WordReductionMethod.Lemmatization;

        Settings.EnableAggressive = AggressiveModeCheckBox.IsChecked == true;
        Settings.EnableArithmeticCoding = ArithmeticCodingCheckBox.IsChecked == true;
        Settings.UseLossyMode = LossyModeCheckBox.IsChecked == true;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Reset all settings to defaults?",
            "Reset Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Settings = new PipelineSettings();
            LoadSettings();
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        SettingsSaved = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsSaved = false;
        Close();
    }
}
