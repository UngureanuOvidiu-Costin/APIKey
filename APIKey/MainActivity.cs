using Android.App;
using Android.OS;
using Android.Widget;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIKey
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        EditText? ipInput;
        Button? checkButton;
        TextView? resultText;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            ipInput = FindViewById<EditText>(Resource.Id.ipInput);
            checkButton = FindViewById<Button>(Resource.Id.checkButton);
            resultText = FindViewById<TextView>(Resource.Id.resultText);

            checkButton!.Click += async (sender, e) =>
            {
                var ip = ipInput!.Text?.Trim();
                if (string.IsNullOrEmpty(ip))
                {
                    resultText!.Text = "Please enter an IP address.";
                    return;
                }

                resultText!.Text = "Checking...";
                string response = await CheckIpAbuseAsync(ip);
                resultText.Text = response;
            };
        }

        private async Task<string> CheckIpAbuseAsync(string ip)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Key", BuildInfo.GetApiKey());
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            try
            {
                string url = $"https://api.abuseipdb.com/api/v2/check?ipAddress={ip}&maxAgeInDays=90";
                var httpResponse = await client.GetAsync(url);
                httpResponse.EnsureSuccessStatusCode();

                string json = await httpResponse.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");

                string? country = data.TryGetProperty("countryCode", out var countryProp) ? countryProp.GetString() : "N/A";
                string? isp = data.TryGetProperty("isp", out var ispProp) ? ispProp.GetString() : "N/A";
                int totalReports = data.TryGetProperty("totalReports", out var reportsProp) ? reportsProp.GetInt32() : 0;
                bool isPublicCloud = data.TryGetProperty("isPublicCloud", out var cloudProp) ? cloudProp.GetBoolean() : false;

                return $"Country: {country}\nISP: {isp}\nReports: {totalReports}\nCloud Provider: {isPublicCloud}";
            }
            catch (HttpRequestException ex)
            {
                return $"Network error: {ex.Message}";
            }
            catch (JsonException ex)
            {
                return $"JSON parsing error: {ex.Message}";
            }
            catch (System.Exception ex)
            {
                return $"Unexpected error: {ex.Message}";
            }
        }

    }
}
