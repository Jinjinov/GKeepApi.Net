using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        private readonly HttpClient _httpClient = new HttpClient();
        protected readonly string _base_url;
        private APIAuth _auth;

        string __version__ = "0.14.2";

        public API(string base_url, APIAuth auth = null)
        {
            _base_url = base_url;
            _auth = auth;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "x-gkeepapi/" + __version__);
        }

        public APIAuth GetAuth()
        {
            return _auth;
        }

        public void SetAuth(APIAuth auth)
        {
            _auth = auth;
        }

        public async Task<Dictionary<string, object>> Send(Dictionary<string, object> req_kwargs)
        {
            int i = 0;
            while (true)
            {
                var response = await _Send(req_kwargs);
                var content = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

                if (!responseData.ContainsKey("error"))
                {
                    return responseData;
                }

                var error = (Dictionary<string, object>)responseData["error"];
                if ((int)error["code"] != 401)
                {
                    throw new APIException((int)error["code"], error.ToString());
                }

                if (i >= RETRY_CNT)
                {
                    throw new APIException((int)error["code"], error.ToString());
                }

                Console.WriteLine("Refreshing access token");
                _auth.Refresh();
                i++;
            }
        }

        private async Task<HttpResponseMessage> _Send(Dictionary<string, object> req_kwargs)
        {
            var auth_token = _auth.AuthToken;
            if (auth_token == null)
            {
                throw new LoginException("Not logged in");
            }

            if (!req_kwargs.ContainsKey("headers"))
            {
                req_kwargs["headers"] = new Dictionary<string, object>();
            }

            var headers = (Dictionary<string, object>)req_kwargs["headers"];
            headers["Authorization"] = "OAuth " + auth_token;

            var method = (string)req_kwargs["method"];
            var url = (string)req_kwargs["url"];

            if (method == "GET")
            {
                return await _httpClient.GetAsync(url);
            }
            else if (method == "POST")
            {
                if (req_kwargs.ContainsKey("json"))
                {
                    var jsonBody = JsonConvert.SerializeObject(req_kwargs["json"]);
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(url, content);
                }
            }

            throw new ArgumentException("Unsupported method: " + method);
        }
    }

    public class KeepAPI : API
    {
        private const string API_URL = "https://www.googleapis.com/notes/v1/";

        private string _session_id;

        public KeepAPI(APIAuth auth = null) : base(API_URL, auth)
        {
            var create_time = DateTime.Now;
            _session_id = _generateId(create_time);
        }

        private static string _generateId(DateTime tz)
        {
            return "s--" + ((long)(tz - new DateTime(1970, 1, 1)).TotalMilliseconds) + "--" + new Random().Next(1000000000, int.MaxValue);
        }

        public async Task<Dictionary<string, object>> Changes(string target_version = null, List<Dictionary<string, object>> nodes = null, List<Dictionary<string, object>> labels = null)
        {
            if (nodes == null)
            {
                nodes = new List<Dictionary<string, object>>();
            }

            if (labels == null)
            {
                labels = new List<Dictionary<string, object>>();
            }

            var current_time = DateTime.Now;
            var params1 = new Dictionary<string, object>
        {
            { "nodes", nodes },
            { "clientTimestamp", ((long)(current_time - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString() },
            { "requestHeader", new Dictionary<string, object>
                {
                    { "clientSessionId", _session_id },
                    { "clientPlatform", "ANDROID" },
                    { "clientVersion", new Dictionary<string, string>
                        {
                            { "major", "9" },
                            { "minor", "9" },
                            { "build", "9" },
                            { "revision", "9" },
                        }
                    },
                    { "capabilities", new List<Dictionary<string, string>>
                        {
                            new Dictionary<string, string> { { "type", "NC" } },
                            new Dictionary<string, string> { { "type", "PI" } },
                            new Dictionary<string, string> { { "type", "LB" } },
                            new Dictionary<string, string> { { "type", "AN" } },
                            new Dictionary<string, string> { { "type", "SH" } },
                            new Dictionary<string, string> { { "type", "DR" } },
                            new Dictionary<string, string> { { "type", "TR" } },
                            new Dictionary<string, string> { { "type", "IN" } },
                            new Dictionary<string, string> { { "type", "SNB" } },
                            new Dictionary<string, string> { { "type", "MI" } },
                            new Dictionary<string, string> { { "type", "CO" } },
                        }
                    }
                }
            }
        };

            if (target_version != null)
            {
            params1["targetVersion"] = target_version;
            }

            if (labels.Count > 0)
            {
            params1["userInfo"] = new Dictionary<string, object>
            {
                { "labels", labels }
            };
            }

            Console.WriteLine("Syncing " + labels.Count + " labels and " + nodes.Count + " nodes");

            return await Send(new Dictionary<string, object>
        {
            { "url", _base_url + "changes" },
            { "method", "POST" },
            { "json", params1 }
        });
        }
    }

    public class MediaAPI : API
    {
        private const string API_URL = "https://keep.google.com/media/v2/";

        public MediaAPI(APIAuth auth = null) : base(API_URL, auth)
        {
        }

        public async Task<string> Get(Blob blob)
        {
            var url = _base_url + blob.parent.server_id + "/" + blob.server_id;
            if (blob.blob.type == _node.BlobType.Drawing)
            {
                url += "/" + blob.blob._drawing_info.drawing_id;
            }

            var response = await _Send(new Dictionary<string, object>
        {
            { "url", url },
            { "method", "GET" },
            { "allow_redirects", false }
        });

            return response.Headers.GetValues("location").FirstOrDefault();
        }
    }

    public class RemindersAPI : API
    {
        private const string API_URL = "https://www.googleapis.com/reminders/v1internal/reminders/";
        private readonly Dictionary<string, object> static_params = new Dictionary<string, object>
    {
        { "taskList", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "systemListId", "MEMENTO" } }
            }
        },
        { "requestParameters", new Dictionary<string, object>
            {
                { "userAgentStructured", new Dictionary<string, object>
                    {
                        { "clientApplication", "KEEP" },
                        { "clientApplicationVersion", new Dictionary<string, string>
                            {
                                { "major", "9" },
                                { "minor", "9.9.9.9" }
                            }
                        },
                        { "clientPlatform", "ANDROID" }
                    }
                }
            }
        }
    };

        public RemindersAPI(APIAuth auth = null) : base(API_URL, auth)
        {
        }

        public async Task<Dictionary<string, object>> Create(string node_id, string node_server_id, DateTime dtime)
        {
            var parameters = new Dictionary<string, object>();
            parameters["task"] = new Dictionary<string, object>
        {
            { "dueDate", new Dictionary<string, object>
                {
                    { "year", dtime.Year },
                    { "month", dtime.Month },
                    { "day", dtime.Day },
                    { "time", new Dictionary<string, int>
                        {
                            { "hour", dtime.Hour },
                            { "minute", dtime.Minute },
                            { "second", dtime.Second }
                        }
                    }
                }
            },
            { "snoozed", true },
            { "extensions", new Dictionary<string, object>
                {
                    { "keepExtension", new Dictionary<string, string>
                        {
                            { "reminderVersion", "V2" },
                            { "clientNoteId", node_id },
                            { "serverNoteId", node_server_id }
                        }
                    }
                }
            }
        };
            parameters["taskId"] = new Dictionary<string, object>
        {
            { "clientAssignedId", "KEEP/v2/" + node_server_id }
        };

            return await Send(new Dictionary<string, object>
        {
            { "url", _base_url + "create" },
            { "method", "POST" },
            { "json", parameters }
        });
        }

        public async Task<Dictionary<string, object>> Update(string node_id, string node_server_id, DateTime dtime)
        {
            var parameters = new Dictionary<string, object>();
            parameters["newTask"] = new Dictionary<string, object>
        {
            { "dueDate", new Dictionary<string, object>
                {
                    { "year", dtime.Year },
                    { "month", dtime.Month },
                    { "day", dtime.Day },
                    { "time", new Dictionary<string, int>
                        {
                            { "hour", dtime.Hour },
                            { "minute", dtime.Minute },
                            { "second", dtime.Second }
                        }
                    }
                }
            },
            { "snoozed", true },
            { "extensions", new Dictionary<string, object>
                {
                    { "keepExtension", new Dictionary<string, string>
                        {
                            { "reminderVersion", "V2" },
                            { "clientNoteId", node_id },
                            { "serverNoteId", node_server_id }
                        }
                    }
                }
            }
        };
            parameters["taskId"] = new Dictionary<string, object>
        {
            { "clientAssignedId", "KEEP/v2/" + node_server_id }
        };
            parameters["updateMask"] = new Dictionary<string, object>
        {
            { "updateField", new List<string>
                {
                    "ARCHIVED",
                    "DUE_DATE",
                    "EXTENSIONS",
                    "LOCATION",
                    "TITLE"
                }
            }
        };

            return await Send(new Dictionary<string, object>
        {
            { "url", _base_url + "update" },
            { "method", "POST" },
            { "json", parameters }
        });
        }

        public async Task<Dictionary<string, object>> Delete(string node_server_id)
        {
            var parameters = new Dictionary<string, object>
        {
            { "batchedRequest", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "deleteTask", new Dictionary<string, List<Dictionary<string, string>>>
                            {
                                { "taskId", new List<Dictionary<string, string>>
                                    {
                                        new Dictionary<string, string> { { "clientAssignedId", "KEEP/v2/" + node_server_id } }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

            return await Send(new Dictionary<string, object>
        {
            { "url", _base_url + "batchmutate" },
            { "method", "POST" },
            { "json", parameters }
        });
        }

        public async Task<Dictionary<string, object>> List(bool master = true)
        {
            var parameters = new Dictionary<string, object>(static_params);

            if (master)
            {
                parameters["recurrenceOptions"] = new Dictionary<string, object>
            {
                { "collapseMode", "MASTER_ONLY" }
            };
                parameters["includeArchived"] = true;
                parameters["includeDeleted"] = false;
            }
            else
            {
                var current_time = DateTime.Now;
                var start_time = (long)(current_time - new DateTime(1970, 1, 1)).TotalMilliseconds - (365L * 24L * 60L * 60L) * 1000L;
                var end_time = (long)(current_time - new DateTime(1970, 1, 1)).TotalMilliseconds + (24L * 60L * 60L) * 1000L;

                parameters["recurrenceOptions"] = new Dictionary<string, object>
            {
                { "collapseMode", "INSTANCES_ONLY" },
                { "recurrencesOnly", true }
            };
                parameters["includeArchived"] = false;
                parameters["includeCompleted"] = false;
                parameters["includeDeleted"] = false;
                parameters["dueAfterMs"] = start_time;
                parameters["dueBeforeMs"] = end_time;
                parameters["recurrenceId"] = new List<object>();
            }

            return await Send(new Dictionary<string, object>
        {
            { "url", _base_url + "list" },
            { "method", "POST" },
            { "json", parameters }
        });
        }

        public async Task<Dictionary<string, object>> History(string storage_version)
        {
            var parameters = new Dictionary<string, object>
        {
            { "storageVersion", storage_version },
            { "includeSnoozePresetUpdates", true }
        };
            parameters = parameters.Concat(static_params).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return await Send(new Dictionary<string, object>
        {
            { "url", _base_url + "history" },
            { "method", "POST" },
            { "json", parameters }
        });
        }

        public async Task<Dictionary<string, object>> Update()
        {
            var parameters = new Dictionary<string, object>();
            return await Send(new Dictionary<string, object>
        {
            { "url", _base_url + "update" },
            { "method", "POST" },
            { "json", parameters }
        });
        }
    }

}
