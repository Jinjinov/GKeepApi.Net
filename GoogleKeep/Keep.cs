using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

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

        public APIAuth(string[] scopes)
        {
            _scopes = scopes;
        }

        public bool Login(string email, string password, string deviceId)
        {
            Email = email;
            DeviceId = deviceId;

            // Obtain a master token.
            var res = gpsoauth.perform_master_login(Email, password, DeviceId);

            // Bail if browser login is required.
            if (res.ContainsKey("Error") && res["Error"] == "NeedsBrowser")
            {
                throw new BrowserLoginRequiredException(res["Url"]);
            }

            // Bail if no token was returned.
            if (!res.ContainsKey("Token"))
            {
                throw new LoginException(res.ContainsKey("Error") ? res["Error"] : null, res.ContainsKey("ErrorDetail") ? res["ErrorDetail"] : null);
            }

            MasterToken = res["Token"];

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
            // Obtain an OAuth token with the necessary scopes by pretending to be
            // the keep android client.
            var res = gpsoauth.perform_oauth(
                Email,
                MasterToken,
                DeviceId,
                service: _scopes,
                app: "com.google.android.keep",
                client_sig: "38918a453d07199354f8b19af05ec6562ced5788"
            );

            // Bail if no token was returned.
            if (!res.ContainsKey("Auth"))
            {
                if (!res.ContainsKey("Token"))
                {
                    throw new LoginException(res.ContainsKey("Error") ? res["Error"] : null);
                }
            }

            AuthToken = res["Auth"];
            return AuthToken;
        }

        public void Logout()
        {
            MasterToken = null;
            AuthToken = null;
            Email = null;
            DeviceId = null;
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
