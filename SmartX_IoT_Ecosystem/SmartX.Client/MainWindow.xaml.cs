using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace SmartX.Client;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
    private string _selectedFilePath = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnAttachLog_Click(object sender, RoutedEventArgs e)
    {
        // OpenFileDialog implementation
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Log & Images|*.txt;*.log;*.jpg;*.png|All Files|*.*";

        if (openFileDialog.ShowDialog() == true)
        {
            _selectedFilePath = openFileDialog.FileName;
            TxtSelectedFile.Text = Path.GetFileName(_selectedFilePath);
        }
    }

    private async void BtnSubmitIngest_Click(object sender, RoutedEventArgs e)
    {
        string mac = TxtMac.Text.Trim();
        if (string.IsNullOrEmpty(mac))
        {
            MessageBox.Show("Please enter a valid Device MAC Address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!float.TryParse(TxtTelemetryVal.Text, out float val))
        {
            MessageBox.Show("Please enter a valid numerical telemetry value.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 1. Post Ingestion Data via API
        var packetPayload = new { MacAddress = mac, Value = val, Timestamp = DateTime.UtcNow };
        var jsonContent = new StringContent(JsonSerializer.Serialize(packetPayload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("api/telemetry/ingest", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                // Execute Dynamic Telemetry Engagement Visual Feedback Strategy
                UpdateDashboardVisualFeedback(mac, val);

                // Upload attached log file if provided
                if (!string.IsNullOrEmpty(_selectedFilePath))
                {
                    await UploadLogFileAsync(mac, _selectedFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"API Gateway Connection Error: {ex.Message}", "Connection Refused", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task UploadLogFileAsync(string mac, string filePath)
    {
        using var content = new MultipartFormDataContent();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

        await _httpClient.PostAsync($"api/sensor/upload-log/{mac}", content);
    }

    /// <summary>
    /// Executes the dynamic visual telemetry thresholding & alert engagement strategy.
    /// </summary>
    private void UpdateDashboardVisualFeedback(string mac, float value)
    {
        TxtDeviceTitle.Text = $"Node: {mac}";
        TxtDeviceVal.Text = $"{value:F2}";

        // Dynamic visual feedback threshold check
        if (value > 80.0f) // Spike Anomaly threshold
        {
            CardVisualFeedback.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F38BA8")); // High-Contrast Red
            TxtDeviceVal.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11111B"));
            TxtAlertStatus.Text = "⚠️ CRITICAL SPIKE ANOMALY DETECTED!";
            TxtAlertStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11111B"));
        }
        else
        {
            CardVisualFeedback.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#313244")); // Nominal Hues
            TxtDeviceVal.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A6E3A1")); // Crisp Green
            TxtAlertStatus.Text = "Status: Nominal Operational Parameters";
            TxtAlertStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAC2DE"));
        }
    }
}