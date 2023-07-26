﻿using GPSOAuth.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// __init__.py
/// </summary>
namespace GKeepApi.Net
{
    public class APIAuth
    {
        public string Email { get; set; }
        public string MasterToken { get; set; }
        public string AuthToken { get; set; }
        public string DeviceId { get; set; }
        private readonly string[] _scopes;

        OAuth _gPSOAuth = new OAuth();

        public APIAuth(string[] scopes)
        {
            _scopes = scopes;
        }

        public bool Login(string email, string password, string deviceId)
        {
            Email = email;
            DeviceId = deviceId;

            // Obtain a master token.
            var res = _gPSOAuth.PerformMasterLogin(Email, password, DeviceId);

            // Bail if browser login is required.
            if (res.ContainsKey("Error") && res["Error"] == "NeedsBrowser")
            {
                throw new BrowserLoginRequiredException(res["Url"], res["Error"]);
            }

            // Bail if no token was returned.
            if (!res.ContainsKey("Token"))
            {
                throw new LoginException(res.ContainsKey("Error") ? res["Error"] : null, new Exception(res.ContainsKey("ErrorDetail") ? res["ErrorDetail"] : null));
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
            var res = _gPSOAuth.PerformOAuth(
                Email,
                MasterToken,
                DeviceId,
                service: _scopes,
                app: "com.google.android.keep",
                clientSig: "38918a453d07199354f8b19af05ec6562ced5788"
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
        private readonly HttpClient _httpClient = new HttpClient();
        protected readonly string _baseUrl;
        private APIAuth _auth;

        readonly string _version = "0.14.2";

        public API(string baseUrl, APIAuth auth = null)
        {
            _baseUrl = baseUrl;
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

        public async Task<Dictionary<string, object>> Send(Dictionary<string, object> reqKwargs)
        {
            int i = 0;
            while (true)
            {
                var response = await _Send(reqKwargs);
                var content = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

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

        protected async Task<HttpResponseMessage> _Send(Dictionary<string, object> reqKwargs)
        {
            var authToken = _auth.AuthToken;
            if (authToken == null)
            {
                throw new LoginException("Not logged in");
            }

            if (!reqKwargs.ContainsKey("headers"))
            {
                reqKwargs["headers"] = new Dictionary<string, object>();
            }

            var headers = (Dictionary<string, object>)reqKwargs["headers"];
            headers["Authorization"] = "OAuth " + authToken;

            var method = (string)reqKwargs["method"];
            var url = (string)reqKwargs["url"];

            if (method == "GET")
            {
                return await _httpClient.GetAsync(url);
            }
            else if (method == "POST")
            {
                if (reqKwargs.ContainsKey("json"))
                {
                    var jsonBody = JsonSerializer.Serialize(reqKwargs["json"]);
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

        private readonly string _sessionId;

        public KeepAPI(APIAuth auth = null) : base(API_URL, auth)
        {
            var createTime = DateTime.Now;
            _sessionId = GenerateId(createTime);
        }

        private static string GenerateId(DateTime tz)
        {
            return "s--" + ((long)(tz - new DateTime(1970, 1, 1)).TotalMilliseconds) + "--" + new Random().Next(1000000000, int.MaxValue);
        }

        public async Task<Dictionary<string, object>> Changes(string targetVersion = null, List<Dictionary<string, object>> nodes = null, List<Dictionary<string, object>> labels = null)
        {
            if (nodes == null)
            {
                nodes = new List<Dictionary<string, object>>();
            }

            if (labels == null)
            {
                labels = new List<Dictionary<string, object>>();
            }

            var currentTime = DateTime.Now;
            var parameters = new Dictionary<string, object>
            {
                { "nodes", nodes },
                { "clientTimestamp", ((long)(currentTime - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString() },
                { "requestHeader", new Dictionary<string, object>
                    {
                        { "clientSessionId", _sessionId },
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

            if (targetVersion != null)
            {
                parameters["targetVersion"] = targetVersion;
            }

            if (labels.Count > 0)
            {
                parameters["userInfo"] = new Dictionary<string, object>
                {
                    { "labels", labels }
                };
            }

            Console.WriteLine("Syncing " + labels.Count + " labels and " + nodes.Count + " nodes");

            return await Send(new Dictionary<string, object>
            {
                { "url", _baseUrl + "changes" },
                { "method", "POST" },
                { "json", parameters }
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
            var url = _baseUrl + blob.Parent.ServerId + "/" + blob.ServerId;
            if (blob.NodeBlob.Type == GKeepApi.Net.BlobType.Drawing)
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
        private readonly Dictionary<string, object> _staticParams = new Dictionary<string, object>
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

        public async Task<Dictionary<string, object>> Create(string nodeId, string nodeServerId, DateTime dtime)
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
                                { "clientNoteId", nodeId },
                                { "serverNoteId", nodeServerId }
                            }
                        }
                    }
                }
            };
            parameters["taskId"] = new Dictionary<string, object>
            {
                { "clientAssignedId", "KEEP/v2/" + nodeServerId }
            };

            return await Send(new Dictionary<string, object>
            {
                { "url", _baseUrl + "create" },
                { "method", "POST" },
                { "json", parameters }
            });
        }

        public async Task<Dictionary<string, object>> Update(string nodeId, string nodeServerId, DateTime dtime)
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
                                { "clientNoteId", nodeId },
                                { "serverNoteId", nodeServerId }
                            }
                        }
                    }
                }
            };
            parameters["taskId"] = new Dictionary<string, object>
            {
                { "clientAssignedId", "KEEP/v2/" + nodeServerId }
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
                { "url", _baseUrl + "update" },
                { "method", "POST" },
                { "json", parameters }
            });
        }

        public async Task<Dictionary<string, object>> Delete(string nodeServerId)
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
                                            new Dictionary<string, string> { { "clientAssignedId", "KEEP/v2/" + nodeServerId } }
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
                { "url", _baseUrl + "batchmutate" },
                { "method", "POST" },
                { "json", parameters }
            });
        }

        public async Task<Dictionary<string, object>> List(bool master = true)
        {
            var parameters = new Dictionary<string, object>(_staticParams);

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
                var currentTime = DateTime.Now;
                var startTime = (long)(currentTime - new DateTime(1970, 1, 1)).TotalMilliseconds - (365L * 24L * 60L * 60L) * 1000L;
                var endTime = (long)(currentTime - new DateTime(1970, 1, 1)).TotalMilliseconds + (24L * 60L * 60L) * 1000L;

                parameters["recurrenceOptions"] = new Dictionary<string, object>
                {
                    { "collapseMode", "INSTANCES_ONLY" },
                    { "recurrencesOnly", true }
                };
                parameters["includeArchived"] = false;
                parameters["includeCompleted"] = false;
                parameters["includeDeleted"] = false;
                parameters["dueAfterMs"] = startTime;
                parameters["dueBeforeMs"] = endTime;
                parameters["recurrenceId"] = new List<object>();
            }

            return await Send(new Dictionary<string, object>
            {
                { "url", _baseUrl + "list" },
                { "method", "POST" },
                { "json", parameters }
            });
        }

        public async Task<Dictionary<string, object>> History(string storageVersion)
        {
            var parameters = new Dictionary<string, object>
            {
                { "storageVersion", storageVersion },
                { "includeSnoozePresetUpdates", true }
            };
            parameters = parameters.Concat(_staticParams).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return await Send(new Dictionary<string, object>
            {
                { "url", _baseUrl + "history" },
                { "method", "POST" },
                { "json", parameters }
            });
        }

        public async Task<Dictionary<string, object>> Update()
        {
            var parameters = new Dictionary<string, object>();
            return await Send(new Dictionary<string, object>
            {
                { "url", _baseUrl + "update" },
                { "method", "POST" },
                { "json", parameters }
            });
        }
    }

    public class Keep : IKeep
    {
        // OAuth scopes
        private readonly string[] OAUTH_SCOPES = { "https://www.googleapis.com/auth/memento", "https://www.googleapis.com/auth/reminders" };

        private readonly KeepAPI _keepApi;
        private readonly RemindersAPI _remindersApi;
        private readonly MediaAPI _mediaApi;
        private string _keepVersion;
        private string _reminderVersion;
        private readonly Dictionary<string, Label> _labels;
        private readonly Dictionary<string, Node> _nodes;
        private readonly Dictionary<string, string> _sidMap;

        public Keep()
        {
            _keepApi = new KeepAPI();
            _remindersApi = new RemindersAPI();
            _mediaApi = new MediaAPI();
            _keepVersion = null;
            _reminderVersion = null;
            _labels = new Dictionary<string, Label>();
            _nodes = new Dictionary<string, Node>();
            _sidMap = new Dictionary<string, string>();

            Clear();
        }

        private void Clear()
        {
            _keepVersion = null;
            _reminderVersion = null;
            _labels.Clear();
            _nodes.Clear();
            _sidMap.Clear();

            var rootNode = new Root();
            _nodes[Root.ID] = rootNode;
        }

        string GetMac()
        {
            string firstMacAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            return firstMacAddress;
        }

        public async Task<bool> Login(string email = null, string password = null, Dictionary<string, object> state = null, bool sync = true, string deviceId = null)
        {
            var auth = new APIAuth(OAUTH_SCOPES);
            if (deviceId == null)
            {
                deviceId = GetMac();
            }

            var ret = auth.Login(email, password, deviceId);
            if (ret)
            {
                await Load(auth, state, sync);
            }

            return ret;
        }

        public async Task<bool> Resume(string email = null, string masterToken = null, Dictionary<string, object> state = null, bool sync = true, string deviceId = null)
        {
            var auth = new APIAuth(OAUTH_SCOPES);
            if (deviceId == null)
            {
                deviceId = GetMac();
            }

            var ret = auth.Load(email, masterToken, deviceId);
            if (ret)
            {
                await Load(auth, state, sync);
            }

            return ret;
        }

        public string GetMasterToken()
        {
            return _keepApi.GetAuth().MasterToken;
        }

        private async Task Load(APIAuth auth, Dictionary<string, object> state = null, bool sync = true)
        {
            _keepApi.SetAuth(auth);
            _remindersApi.SetAuth(auth);
            _mediaApi.SetAuth(auth);
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
            var nodes = new List<Node>();
            foreach (var node in All())
            {
                nodes.Add(node);
                nodes.AddRange(node.Children.Values);
            }

            var serializedLabels = new List<Dictionary<string, object>>();
            foreach (var label in Labels())
            {
                serializedLabels.Add(label.Save(false));
            }

            var serializedNodes = new List<Dictionary<string, object>>();
            foreach (var node in nodes)
            {
                serializedNodes.Add(node.Save(false));
            }

            return new Dictionary<string, object>
            {
                { "keep_version", _keepVersion },
                { "labels", serializedLabels },
                { "nodes", serializedNodes }
            };
        }

        private void Restore(Dictionary<string, object> state)
        {
            Clear();
            ParseUserInfo(new Dictionary<string, object> { { "labels", state["labels"] } });
            ParseNodes((List<Dictionary<string, object>>)state["nodes"]);
            _keepVersion = state["keep_version"].ToString();
        }

        public Node Get(string nodeId)
        {
            return _nodes[Root.ID].Get(nodeId) ?? _nodes.GetValueOrDefault(_sidMap.GetValueOrDefault(nodeId));
        }

        public void Add(Node node)
        {
            if (node.ParentId != Root.ID)
            {
                throw new Exception("Not a top level node");
            }

            _nodes[node.Id] = node;
            _nodes[node.ParentId].Append(node, false);
        }

        public IEnumerable<Node> Find(
            object query = null,
            Func<Node, bool> func = null,
            List<object> labels = null,
            List<ColorValue> colors = null,
            bool? pinned = null,
            bool? archived = null,
            bool trashed = false)
        {
            if (labels != null)
                labels = labels.Select(l => l is Label lbl ? lbl.Id : l).ToList();

            return All().Where(node =>
            {
                return (query == null ||
                    (query is string str && (node.Title.Contains(str) || node.Text.Contains(str))) ||
                    (query is Regex regex && (regex.IsMatch(node.Title) || regex.IsMatch(node.Text))))
                    && (func == null || func(node))
                    && (labels == null || (!labels.Any() && !node.Labels.Any()) || labels.Any(l => l is Label lbl ? node.Labels.ContainsKey(lbl.Id) : l is string id ? node.Labels.ContainsKey(id) : false))
                    && (colors == null || colors.Contains(node.Color))
                    && (pinned == null || node.Pinned == pinned)
                    && (archived == null || node.Archived == archived)
                    && (trashed == false || node.Trashed() == trashed);
            });
        }

        public Note CreateNote(string title = null, string text = null)
        {
            var node = new Note();
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

        public List CreateList(string title = null, List<(string, bool)> items = null)
        {
            if (items == null)
            {
                items = new List<(string, bool)>();
            }

            var node = new List();
            if (title != null)
            {
                node.Title = title;
            }

            var sort = new Random().Next(1000000000, int.MaxValue);
            foreach (var item in items)
            {
                node.Add(item.Item1, item.Item2, sort);
                sort -= GKeepApi.Net.List.SORT_DELTA;
            }
            Add(node);
            return node;
        }

        public Label CreateLabel(string name)
        {
            if (FindLabel(name) != null)
            {
                throw new Exception("Label exists");
            }
            var node = new Label();
            node.Name = name;
            _labels[node.Id] = node;
            return node;
        }

        public Label FindLabel(object query, bool create = false)
        {
            var isStr = query is string;
            var name = isStr ? (string)query : null;
            query = isStr ? ((string)query).ToLower() : null;

            foreach (var label in _labels.Values)
            {
                if ((isStr && query == label.Name.ToLower()) ||
                    (query is Regex regex && regex.IsMatch(label.Name)))
                {
                    return label;
                }
            }

            return create && isStr ? CreateLabel(name) : null;
        }

        public Label GetLabel(string labelId)
        {
            return _labels.GetValueOrDefault(labelId);
        }

        public void DeleteLabel(string labelId)
        {
            if (_labels.TryGetValue(labelId, out var label))
            {
                label.Delete();
                foreach (var node in All().OfType<TopLevelNode>())
                {
                    node.Labels.Remove(label);
                }
            }
        }

        public IEnumerable<Label> Labels()
        {
            return _labels.Values;
        }

        public async Task<string> GetMediaLink(Blob blob)
        {
            return await _mediaApi.Get(blob);
        }

        public IEnumerable<TopLevelNode> All()
        {
            return _nodes[Root.ID].Children.Values.OfType<TopLevelNode>();
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
                Console.WriteLine($"Starting keep sync: {_keepVersion}");

                bool labelsUpdated = _labels.Values.Any(label => label.Dirty);
                var changes = await _keepApi.Changes(
                    targetVersion: _keepVersion,
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

                _keepVersion = changes["toVersion"].ToString();
                Console.WriteLine($"Finishing sync: {_keepVersion}");

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
            var createdNodes = new List<Node>();
            var deletedNodes = new List<Node>();
            var listItemNodes = new List<ListItem>();

            foreach (var rawNode in raw)
            {
                if (_nodes.ContainsKey(rawNode["id"].ToString()))
                {
                    var node = _nodes[rawNode["id"].ToString()];
                    if (rawNode.ContainsKey("parentId"))
                    {
                        node.Load(rawNode);
                        _sidMap[node.ServerId] = node.Id;
                        Console.WriteLine($"Updated node: {rawNode["id"]}");
                    }
                    else
                    {
                        deletedNodes.Add(node);
                    }
                }
                else
                {
                    var node = FromJson(rawNode);
                    if (node != null)
                    {
                        _nodes[rawNode["id"].ToString()] = node;
                        _sidMap[node.ServerId] = node.Id;
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
                _sidMap.Remove(node.ServerId);
                Console.WriteLine($"Deleted node: {node.Id}");
            }

            foreach (var node in All().OfType<TopLevelNode>())
            {
                foreach (var labelId in node.Labels.Keys)
                {
                    node.Labels.Add(_labels.GetValueOrDefault(labelId));
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
                        node = new Label();
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

        private List<Node> FindDirtyNodes()
        {
            var foundIds = new Dictionary<string, object>();
            var nodes = new List<Node> { _nodes[Root.ID] };

            while (nodes.Count > 0)
            {
                var node = nodes[0];
                nodes.RemoveAt(0);
                foundIds[node.Id] = null;
                nodes.AddRange(node.Children.Values);
            }

            var dirtyNodes = new List<Node>();
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
            var nodes = new List<Node> { _nodes[Root.ID] };

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

        private static readonly Dictionary<NodeType, Type> _typeMap = new Dictionary<NodeType, Type>
        {
            { NodeType.Note, typeof(Note) },
            { NodeType.List, typeof(List) },
            { NodeType.ListItem, typeof(ListItem) },
            { NodeType.Blob, typeof(Blob) }
        };

        public static Node FromJson(Dictionary<string, object> raw)
        {
            Node node = null;
            if (raw.TryGetValue("type", out object _typeObj) && _typeObj is string _typeStr)
            {
                if (NodeType.TryFromName(_typeStr, out NodeType _type))
                {
                    if (_typeMap.TryGetValue(_type, out Type ncls))
                    {
                        node = (Node)Activator.CreateInstance(ncls);
                        node.Load(raw);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown node type: {_type}");
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid node type: {_typeStr}");
                }
            }
            else
            {
                Console.WriteLine("Node type not found in the dictionary.");
            }

            return node;
        }
    }
}
