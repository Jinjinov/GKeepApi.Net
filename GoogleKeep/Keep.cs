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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

/// <summary>
/// __init__.py
/// </summary>
namespace GoogleKeep
{
    public class APIAuth
    {
        public string Email { get; set; }
        public string MasterToken { get; set; }
        public string AuthToken { get; set; }
        public string DeviceId { get; set; }
        private string[] _scopes;

        UserCredential _credential;

        // Path to a directory where the user's credentials will be stored (can be a temporary directory)
        readonly string _credentialPath = "path/to/credentials-directory";

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
            _scopes = new string[]
            {
                "https://www.googleapis.com/auth/drive.readonly", // Replace with your desired scopes
                // Add more scopes as needed for the services you want to access
            };

            // Create the credentials object
            using (var stream = new System.IO.FileStream("path/to/client-secrets.json", System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    System.Threading.CancellationToken.None,
                    new FileDataStore(_credentialPath, true)).Result;
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
            if (_credential.Token.IsExpired(SystemClock.Default))
            {
                // Refresh the access token
                bool success = _credential.RefreshTokenAsync(CancellationToken.None).Result;
                if (!success)
                {
                    Console.WriteLine("Failed to refresh access token.");
                    return null;
                }
            }

            // Retrieve the refreshed access token
            string accessToken = _credential.Token.AccessToken;

            // Now you have the refreshed access token, and you can use it to make authorized requests to Google APIs.

            Console.WriteLine("Refreshed Access Token: " + accessToken);

            return accessToken;
        }

        public void Logout()
        {
            try
            {
                // Revoke the access token to invalidate it
                _credential.RevokeTokenAsync(System.Threading.CancellationToken.None).Wait();

                // Clear the stored user credentials from the FileDataStore
                new FileDataStore(_credentialPath, true).ClearAsync().Wait();

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

        readonly string _version = "0.14.2";

        public API(string base_url, APIAuth auth = null)
        {
            _base_url = base_url;
            _auth = auth;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "x-gkeepapi/" + _version);
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

        protected async Task<HttpResponseMessage> _Send(Dictionary<string, object> req_kwargs)
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

        private readonly string _session_id;

        public KeepAPI(APIAuth auth = null) : base(API_URL, auth)
        {
            var create_time = DateTime.Now;
            _session_id = GenerateId(create_time);
        }

        private static string GenerateId(DateTime tz)
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
            var url = _base_url + blob.Parent.ServerId + "/" + blob.ServerId;
            if (blob.NodeBlob.Type == GoogleKeep.BlobType.Drawing)
            {
                url += "/" + (blob.NodeBlob as NodeDrawing).DrawingInfo.DrawingId;
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
        private readonly Dictionary<string, object> _static_params = new Dictionary<string, object>
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
            var parameters = new Dictionary<string, object>(_static_params);

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
            parameters = parameters.Concat(_static_params).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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

    public class Keep
    {
        // OAuth scopes
        private readonly string[] OAUTH_SCOPES = { "https://www.googleapis.com/auth/memento", "https://www.googleapis.com/auth/reminders" };

        private readonly KeepAPI _keep_api;
        private readonly RemindersAPI _reminders_api;
        private readonly MediaAPI _media_api;
        private string _keep_version;
        private string _reminder_version;
        private readonly Dictionary<string, GoogleKeep.Label> _labels;
        private readonly Dictionary<string, GoogleKeep.Node> _nodes;
        private readonly Dictionary<string, string> _sid_map;

        public Keep()
        {
            _keep_api = new KeepAPI();
            _reminders_api = new RemindersAPI();
            _media_api = new MediaAPI();
            _keep_version = null;
            _reminder_version = null;
            _labels = new Dictionary<string, GoogleKeep.Label>();
            _nodes = new Dictionary<string, GoogleKeep.Node>();
            _sid_map = new Dictionary<string, string>();

            Clear();
        }

        private void Clear()
        {
            _keep_version = null;
            _reminder_version = null;
            _labels.Clear();
            _nodes.Clear();
            _sid_map.Clear();

            var root_node = new GoogleKeep.Root();
            _nodes[GoogleKeep.Root.ID] = root_node;
        }

        public async Task<bool> Login(string email, string password, Dictionary<string, object> state = null, bool sync = true, string device_id = null)
        {
            var auth = new APIAuth(OAUTH_SCOPES);
            if (device_id == null)
            {
                device_id = GetMac();
            }

            var ret = auth.Login(email, password, device_id);
            if (ret)
            {
                await Load(auth, state, sync);
            }

            return ret;
        }

        public async Task<bool> Resume(string email, string master_token, Dictionary<string, object> state = null, bool sync = true, string device_id = null)
        {
            var auth = new APIAuth(OAUTH_SCOPES);
            if (device_id == null)
            {
                device_id = GetMac();
            }

            var ret = auth.Load(email, master_token, device_id);
            if (ret)
            {
                await Load(auth, state, sync);
            }

            return ret;
        }

        public string GetMasterToken()
        {
            return _keep_api.GetAuth().MasterToken;
        }

        private async Task Load(APIAuth auth, Dictionary<string, object> state = null, bool sync = true)
        {
            _keep_api.SetAuth(auth);
            _reminders_api.SetAuth(auth);
            _media_api.SetAuth(auth);
            if (state != null)
            {
                Restore(state);
            }
            if (sync)
            {
                await Sync(true);
            }
        }

        public Dictionary<string, object> Dump()
        {
            var nodes = new List<GoogleKeep.Node>();
            foreach (var node in All())
            {
                nodes.Add(node);
                nodes.AddRange(node.Children.Values);
            }

            var serialized_labels = new List<Dictionary<string, object>>();
            foreach (var label in Labels())
            {
                serialized_labels.Add(label.Save(false));
            }

            var serialized_nodes = new List<Dictionary<string, object>>();
            foreach (var node in nodes)
            {
                serialized_nodes.Add(node.Save(false));
            }

            return new Dictionary<string, object>
            {
                { "keep_version", _keep_version },
                { "labels", serialized_labels },
                { "nodes", serialized_nodes }
            };
        }

        private void Restore(Dictionary<string, object> state)
        {
            Clear();
            ParseUserInfo(new Dictionary<string, object> { { "labels", state["labels"] } });
            ParseNodes((List<Dictionary<string, object>>)state["nodes"]);
            _keep_version = state["keep_version"].ToString();
        }

        public GoogleKeep.Node Get(string node_id)
        {
            return _nodes[GoogleKeep.Root.ID].Get(node_id) ?? _nodes.GetValueOrDefault(_sid_map.GetValueOrDefault(node_id));
        }

        public void Add(GoogleKeep.Node node)
        {
            if (node.ParentId != GoogleKeep.Root.ID)
            {
                throw new Exception("Not a top level node");
            }

            _nodes[node.Id] = node;
            _nodes[node.ParentId].Append(node, false);
        }

        public IEnumerable<GoogleKeep.Node> Find(
            string query = null,
            Func<GoogleKeep.Node, bool> func = null,
            List<GoogleKeep.Label> labels = null,
            List<string> colors = null,
            bool? pinned = null,
            bool? archived = null,
            bool trashed = false)
        {
            labels = labels?.Select(l => l.Id).ToList();
            return All().Where(node =>
            {
                return (query == null ||
                    (query is string && (node.Title.Contains(query) || node.Text.Contains(query))) ||
                    (query is Regex regex && (regex.IsMatch(node.Title) || regex.IsMatch(node.Text))))
                    && (func == null || func(node))
                    && (labels == null || (!labels.Any() && !node.Labels.Any()) || labels.Any(l => node.Labels.ContainsKey(l)))
                    && (colors == null || colors.Contains(node.Color))
                    && (pinned == null || node.Pinned == pinned)
                    && (archived == null || node.Archived == archived)
                    && (trashed == false || node.Trashed() == trashed);
            });
        }

        public GoogleKeep.Note CreateNote(string title = null, string text = null)
        {
            var node = new GoogleKeep.Note();
            if (title != null)
            {
                node.Title = title;
            }
            if (text != null)
            {
                node.Text = text;
            }
            Add(node);
            return node;
        }

        public GoogleKeep.List CreateList(string title = null, List<(string, bool)> items = null)
        {
            if (items == null)
            {
                items = new List<(string, bool)>();
            }

            var node = new GoogleKeep.List();
            if (title != null)
            {
                node.Title = title;
            }

            var sort = new Random().Next(1000000000, int.MaxValue);
            foreach (var item in items)
            {
                node.Add(item.Item1, item.Item2, sort);
                sort -= GoogleKeep.List.SORT_DELTA;
            }
            Add(node);
            return node;
        }

        public GoogleKeep.Label CreateLabel(string name)
        {
            if (FindLabel(name) != null)
            {
                throw new Exception("Label exists");
            }
            var node = new GoogleKeep.Label();
            node.Name = name;
            _labels[node.Id] = node;
            return node;
        }

        public GoogleKeep.Label FindLabel(string query, bool create = false)
        {
            var is_str = query is string;
            var name = is_str ? (string)query : null;
            query = is_str ? ((string)query).ToLower() : null;

            foreach (var label in _labels.Values)
            {
                if ((is_str && query == label.Name.ToLower()) ||
                    (query is Regex regex && regex.IsMatch(label.Name)))
                {
                    return label;
                }
            }

            return create && is_str ? CreateLabel(name) : null;
        }

        public GoogleKeep.Label GetLabel(string label_id)
        {
            return _labels.GetValueOrDefault(label_id);
        }

        public void DeleteLabel(string label_id)
        {
            if (_labels.TryGetValue(label_id, out var label))
            {
                label.Delete();
                foreach (var node in All())
                {
                    node.Labels.Remove(label);
                }
            }
        }

        public IEnumerable<GoogleKeep.Label> Labels()
        {
            return _labels.Values;
        }

        public async Task<string> GetMediaLink(GoogleKeep.Blob blob)
        {
            return await _media_api.Get(blob);
        }

        public IEnumerable<GoogleKeep.Node> All()
        {
            return _nodes[GoogleKeep.Root.ID].Children.Values;
        }

        public async Task Sync(bool resync = false)
        {
            if (resync)
            {
                Clear();
            }

            await SyncNotes(resync);
        }

        private void SyncReminders(bool resync = false)
        {
            // TODO: Implementation for syncing reminders (if needed).
        }

        private async Task SyncNotes(bool resync = false)
        {
            while (true)
            {
                Console.WriteLine($"Starting keep sync: {_keep_version}");

                bool labelsUpdated = _labels.Values.Any(label => label.Dirty);
                var changes = await _keep_api.Changes(
                    target_version: _keep_version,
                    nodes: FindDirtyNodes().Select(n => n.Save()).ToList(),
                    labels: labelsUpdated ? _labels.Values.Select(l => l.Save(false)).ToList() : null
                );

                if (changes.ContainsKey("forceFullResync"))
                {
                    throw new ResyncRequiredException("Full resync required");
                }

                if (changes.ContainsKey("upgradeRecommended"))
                {
                    throw new UpgradeRecommendedException("Upgrade recommended");
                }

                if (changes.TryGetValue("userInfo", out var userInfoTemp) && userInfoTemp is Dictionary<string, object> userInfo)
                {
                    ParseUserInfo(userInfo);
                }

                if (changes.TryGetValue("nodes", out var nodesTemp) && nodesTemp is List<Dictionary<string, object>> nodes)
                {
                    ParseNodes(nodes);
                }

                _keep_version = changes["toVersion"].ToString();
                Console.WriteLine($"Finishing sync: {_keep_version}");

                if (!changes["truncated"].Equals(true))
                {
                    break;
                }
            }
        }

        private void ParseTasks(object raw)
        {
            // TODO: Implementation for parsing tasks (if needed).
        }

        private void ParseNodes(List<Dictionary<string, object>> raw)
        {
            var createdNodes = new List<GoogleKeep.Node>();
            var deletedNodes = new List<GoogleKeep.Node>();
            var listItemNodes = new List<GoogleKeep.ListItem>();

            foreach (var rawNode in raw)
            {
                if (_nodes.ContainsKey(rawNode["id"].ToString()))
                {
                    var node = _nodes[rawNode["id"].ToString()];
                    if (rawNode.ContainsKey("parentId"))
                    {
                        node.Load(rawNode);
                        _sid_map[node.ServerId] = node.Id;
                        Console.WriteLine($"Updated node: {rawNode["id"]}");
                    }
                    else
                    {
                        deletedNodes.Add(node);
                    }
                }
                else
                {
                    var node = GoogleKeep.FromJson(rawNode);
                    if (node != null)
                    {
                        _nodes[rawNode["id"].ToString()] = node;
                        _sid_map[node.ServerId] = node.Id;
                        createdNodes.Add(node);
                        Console.WriteLine($"Created node: {rawNode["id"]}");
                    }
                    else
                    {
                        Console.WriteLine("Discarded unknown node");
                    }
                }

                if (rawNode.TryGetValue("listItem", out var listItemTemp) && listItemTemp is Dictionary<string, object> listItem)
                {
                    var listItemId = listItem["id"].ToString();
                    var prevSuperListItemId = listItem["prevSuperListItemId"].ToString();
                    var superListItemId = listItem["superListItemId"].ToString();

                    var node = _nodes[prevSuperListItemId] as ListItem;
                    if (prevSuperListItemId != superListItemId)
                    {
                        node.Dedent(listItemNodes.Last(), false);
                    }

                    if (superListItemId != null)
                    {
                        node = _nodes[superListItemId] as ListItem;
                        node.Indent(listItemNodes.Last(), false);
                    }
                }
            }

            foreach (var node in listItemNodes)
            {
                if (node.NextListItemId != node.Id)
                {
                    var nextListItemNode = _nodes[node.NextListItemId];
                    node.Indent(nextListItemNode, false);
                }
            }

            foreach (var node in createdNodes)
            {
                var parent = _nodes[node.ParentId];
                parent.Append(node, false);
                Console.WriteLine($"Attached node: {node.Id} to {node.ParentId}");
            }

            foreach (var node in deletedNodes)
            {
                node.Parent.Remove(node);
                _nodes.Remove(node.Id);
                _sid_map.Remove(node.ServerId);
                Console.WriteLine($"Deleted node: {node.Id}");
            }

            foreach (var node in All())
            {
                foreach (var labelId in node.Labels.Keys)
                {
                    node.Labels[labelId] = _labels.GetValueOrDefault(labelId);
                }
            }
        }

        private void ParseUserInfo(Dictionary<string, object> raw)
        {
            var labels = raw["labels"] as List<Dictionary<string, object>>;
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    var labelId = label["mainId"].ToString();
                    if (_labels.TryGetValue(labelId, out var node))
                    {
                        _labels.Remove(labelId);
                        Console.WriteLine($"Updated label: {labelId}");
                    }
                    else
                    {
                        node = new GoogleKeep.Label();
                        Console.WriteLine($"Created label: {labelId}");
                    }

                    node.Load(label);
                    _labels[labelId] = node;
                }
            }

            foreach (var labelId in _labels.Keys.ToList())
            {
                if (!labels.Any(label => label["mainId"].ToString() == labelId))
                {
                    _labels.Remove(labelId);
                    Console.WriteLine($"Deleted label: {labelId}");
                }
            }
        }

        private List<GoogleKeep.Node> FindDirtyNodes()
        {
            var foundIds = new Dictionary<string, object>();
            var nodes = new List<GoogleKeep.Node> { _nodes[GoogleKeep.Root.ID] };

            while (nodes.Count > 0)
            {
                var node = nodes[0];
                nodes.RemoveAt(0);
                foundIds[node.Id] = null;
                nodes.AddRange(node.Children.Values);
            }

            var dirtyNodes = new List<GoogleKeep.Node>();
            foreach (var node in _nodes.Values)
            {
                if (node.Dirty)
                {
                    dirtyNodes.Add(node);
                }
            }

            return dirtyNodes;
        }

        private void Clean()
        {
            var foundIds = new Dictionary<string, object>();
            var nodes = new List<GoogleKeep.Node> { _nodes[GoogleKeep.Root.ID] };

            while (nodes.Count > 0)
            {
                var node = nodes[0];
                nodes.RemoveAt(0);
                foundIds[node.Id] = null;
                nodes.AddRange(node.Children.Values);
            }

            foreach (var nodeId in _nodes.Keys.ToList())
            {
                if (!foundIds.ContainsKey(nodeId))
                {
                    Console.WriteLine($"Dangling node: {nodeId}");
                }
            }

            foreach (var nodeId in foundIds.Keys)
            {
                if (!_nodes.ContainsKey(nodeId))
                {
                    Console.WriteLine($"Unregistered node: {nodeId}");
                }
            }
        }
    }
}
