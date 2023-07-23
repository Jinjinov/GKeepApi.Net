using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// __init__.py
/// </summary>
namespace GoogleKeep
{
    public class Keep : IKeep
    {

    }

    public class APIAuth
    {
        public string Email { get; set; }
        public string MasterToken { get; set; }
        public string AuthToken { get; set; }
        public string DeviceId { get; set; }
        private readonly string[] _scopes;

        UserCredential credential;

        // Path to a directory where the user's credentials will be stored (can be a temporary directory)
        string credentialPath = "path/to/credentials-directory";

        public APIAuth(string[] scopes)
        {
            _scopes = scopes;
        }

        public bool Login(string email, string password, string deviceId)
        {
            Email = email;
            DeviceId = deviceId;

            // Your Google API client ID and client secret
            string clientId = "YOUR_CLIENT_ID";
            string clientSecret = "YOUR_CLIENT_SECRET";

            // The scopes that your application needs access to
            string[] scopes = new string[]
            {
                "https://www.googleapis.com/auth/drive.readonly", // Replace with your desired scopes
                // Add more scopes as needed for the services you want to access
            };

            // Create the credentials object
            using (var stream = new System.IO.FileStream("path/to/client-secrets.json", System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    System.Threading.CancellationToken.None,
                    new FileDataStore(credentialPath, true)).Result;
            }

            // Obtain an OAuth token.
            Refresh();
            return true;
        }

        public bool Load(string email, string masterToken, string deviceId)
        {
            Email = email;
            DeviceId = deviceId;
            MasterToken = masterToken;

            // Obtain an OAuth token.
            Refresh();
            return true;
        }

        public string Refresh()
        {
            // Check if the access token needs to be refreshed
            if (credential.Token.IsExpired(SystemClock.Default))
            {
                // Refresh the access token
                bool success = credential.RefreshTokenAsync(CancellationToken.None).Result;
                if (!success)
                {
                    Console.WriteLine("Failed to refresh access token.");
                    return null;
                }
            }

            // Retrieve the refreshed access token
            string accessToken = credential.Token.AccessToken;

            // Now you have the refreshed access token, and you can use it to make authorized requests to Google APIs.

            Console.WriteLine("Refreshed Access Token: " + accessToken);

            return accessToken;
        }

        public void Logout()
        {
            try
            {
                // Revoke the access token to invalidate it
                credential.RevokeTokenAsync(System.Threading.CancellationToken.None).Wait();

                // Clear the stored user credentials from the FileDataStore
                new FileDataStore(credentialPath, true).ClearAsync().Wait();

                Console.WriteLine("Logout successful.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logout failed: " + ex.Message);
            }
        }
    }

    public class API
    {
        private const int RETRY_CNT = 2;

        private readonly HttpClient _httpClient;
        private readonly string _base_url;

        public API(string base_url, APIAuth auth = null)
        {
            _httpClient = new HttpClient();
            Auth = auth;
            _base_url = base_url;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"x-gkeepapi/{__version__} (https://github.com/kiwiz/gkeepapi)");
        }

        public APIAuth Auth { get; set; }

        public async Task<JsonDocument> SendAsync(HttpRequestMessage requestMessage)
        {
            var i = 0;
            while (true)
            {
                var response = await SendRequestAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);
                if (!responseJson.RootElement.TryGetProperty("error", out var errorElement))
                {
                    return responseJson;
                }

                var errorObject = errorElement.GetObject();
                var errorCode = errorObject.GetInt32("code");
                if (errorCode != 401)
                {
                    throw new APIException(errorCode, errorObject);
                }

                if (i >= RETRY_CNT)
                {
                    throw new APIException(errorCode, errorObject);
                }

                Auth.Refresh();
                i++;
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage requestMessage)
        {
            var authToken = Auth.AuthToken;

            if (string.IsNullOrEmpty(authToken))
            {
                throw new LoginException("Not logged in");
            }

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authToken);
            var response = await _httpClient.SendAsync(requestMessage);
            return response;
        }
    }

    public class MediaAPI : API
    {
        private const string API_URL = "https://keep.google.com/media/v2/";

        public MediaAPI(APIAuth auth = null)
            : base(API_URL, auth)
        {
        }

        public string Get(Blob blob)
        {
            string url = $"{_base_url}{blob.Parent.ServerId}/{blob.ServerId}";
            if (blob.Blob.Type == BlobType.Drawing)
            {
                url += $"/{blob.Blob.DrawingInfo.DrawingId}";
            }
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage response = _send(request);
            response.Headers.TryGetValues("location", out var locationHeaders);
            return locationHeaders?.FirstOrDefault();
        }
    }
}
