using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// node.py
/// </summary>
namespace GoogleKeep
{
    public enum NodeType
    {
        [JsonPropertyName("NOTE")]
        Note,
        [JsonPropertyName("LIST")]
        List,
        [JsonPropertyName("LIST_ITEM")]
        ListItem,
        [JsonPropertyName("BLOB")]
        Blob
    }

    public enum BlobType
    {
        [JsonPropertyName("AUDIO")]
        Audio,
        [JsonPropertyName("IMAGE")]
        Image,
        [JsonPropertyName("DRAWING")]
        Drawing
    }

    public enum ColorValue
    {
        [JsonPropertyName("DEFAULT")]
        White,
        [JsonPropertyName("RED")]
        Red,
        [JsonPropertyName("ORANGE")]
        Orange,
        [JsonPropertyName("YELLOW")]
        Yellow,
        [JsonPropertyName("GREEN")]
        Green,
        [JsonPropertyName("TEAL")]
        Teal,
        [JsonPropertyName("BLUE")]
        Blue,
        [JsonPropertyName("CERULEAN")]
        DarkBlue,
        [JsonPropertyName("PURPLE")]
        Purple,
        [JsonPropertyName("PINK")]
        Pink,
        [JsonPropertyName("BROWN")]
        Brown,
        [JsonPropertyName("GRAY")]
        Gray
    }

    public enum CategoryValue
    {
        [JsonPropertyName("BOOKS")]
        Books,
        [JsonPropertyName("FOOD")]
        Food,
        [JsonPropertyName("MOVIES")]
        Movies,
        [JsonPropertyName("MUSIC")]
        Music,
        [JsonPropertyName("PLACES")]
        Places,
        [JsonPropertyName("QUOTES")]
        Quotes,
        [JsonPropertyName("TRAVEL")]
        Travel,
        [JsonPropertyName("TV")]
        TV
    }

    public enum SuggestValue
    {
        [JsonPropertyName("GROCERY_ITEM")]
        GroceryItem
    }

    public enum NewListItemPlacementValue
    {
        [JsonPropertyName("TOP")]
        Top,
        [JsonPropertyName("BOTTOM")]
        Bottom
    }

    public enum GraveyardStateValue
    {
        [JsonPropertyName("EXPANDED")]
        Expanded,
        [JsonPropertyName("COLLAPSED")]
        Collapsed
    }

    public enum CheckedListItemsPolicyValue
    {
        [JsonPropertyName("DEFAULT")]
        Default,
        [JsonPropertyName("GRAVEYARD")]
        Graveyard
    }

    public enum ShareRequestValue
    {
        [JsonPropertyName("WR")]
        Add,
        [JsonPropertyName("RM")]
        Remove
    }

    public enum RoleValue
    {
        [JsonPropertyName("O")]
        Owner,
        [JsonPropertyName("W")]
        User
    }

    public interface IElement
    {
        void Load(Dictionary<string, object> raw);
        Dictionary<string, object> Save(bool clean = true);
        bool Dirty { get; }
    }

    public class Element : IElement
    {
        protected bool _dirty;

        public Element()
        {
            _dirty = false;
        }

        protected void FindDiscrepancies(Dictionary<string, object> raw)
        {
            // Implementation omitted for brevity since it is not directly translatable to C#
            // You may need to handle dictionary operations, logging, and comparison logic manually
        }

        public void Load(Dictionary<string, object> raw)
        {
            try
            {
                _load(raw);
            }
            catch (Exception e) when (e is KeyNotFoundException || e is FormatException)
            {
                throw new ParseException($"Parse error in {GetType()}", raw, e);
            }
        }

        protected virtual void _load(Dictionary<string, object> raw)
        {
            _dirty = raw.TryGetValue("_dirty", out var dirtyValue) && Convert.ToBoolean(dirtyValue);
        }

        public virtual Dictionary<string, object> Save(bool clean = true)
        {
            var ret = new Dictionary<string, object>();
            if (clean)
                _dirty = false;
            else
                ret["_dirty"] = _dirty;
            return ret;
        }

        public bool Dirty => _dirty;
    }

    public class Annotation : Element, IElement
    {
        public string Id { get; private set; }

        public Annotation()
        {
            Id = GenerateAnnotationId();
        }

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            Id = raw.ContainsKey("id") ? raw["id"].ToString() : null;
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = new Dictionary<string, object>();
            if (Id != null)
                ret = base.Save(clean);
            if (Id != null)
                ret["id"] = Id;
            return ret;
        }

        private static string GenerateAnnotationId()
        {
            return string.Format("{0:x8}-{1:x4}-{2:x4}-{3:x4}-{4:x12}",
                new Random().Next(0x00000000, 0xffffffff),
                new Random().Next(0x0000, 0xffff),
                new Random().Next(0x0000, 0xffff),
                new Random().Next(0x0000, 0xffff),
                new Random().Next(0x000000000000, 0xffffffffffff));
        }
    }

    public class WebLink : Annotation, IElement
    {
        private string _title = string.Empty;
        private string _url = string.Empty;
        private string _imageUrl = null;
        private string _provenanceUrl = string.Empty;
        private string _description = string.Empty;

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            var webLink = raw["webLink"] as Dictionary<string, object>;
            _title = webLink["title"].ToString();
            _url = webLink["url"].ToString();
            _imageUrl = webLink.ContainsKey("imageUrl") ? webLink["imageUrl"].ToString() : _imageUrl;
            _provenanceUrl = webLink["provenanceUrl"].ToString();
            _description = webLink["description"].ToString();
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["webLink"] = new Dictionary<string, object>
        {
            { "title", _title },
            { "url", _url },
            { "imageUrl", _imageUrl },
            { "provenanceUrl", _provenanceUrl },
            { "description", _description }
        };
            return ret;
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                _dirty = true;
            }
        }

        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                _dirty = true;
            }
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                _imageUrl = value;
                _dirty = true;
            }
        }

        public string ProvenanceUrl
        {
            get => _provenanceUrl;
            set
            {
                _provenanceUrl = value;
                _dirty = true;
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                _dirty = true;
            }
        }
    }

    public class Category : Annotation, IElement
    {
        private CategoryValue _category;

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            _category = (CategoryValue)Enum.Parse(typeof(CategoryValue), raw["topicCategory"]["category"].ToString());
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["topicCategory"] = new Dictionary<string, object>
        {
            { "category", _category.ToString() }
        };
            return ret;
        }

        public CategoryValue Category
        {
            get => _category;
            set
            {
                _category = value;
                _dirty = true;
            }
        }
    }

    public class TaskAssist : Annotation, IElement
    {
        private string _suggest;

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            _suggest = raw["taskAssist"]["suggestType"].ToString();
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["taskAssist"] = new Dictionary<string, object>
        {
            { "suggestType", _suggest }
        };
            return ret;
        }

        public string Suggest
        {
            get => _suggest;
            set
            {
                _suggest = value;
                _dirty = true;
            }
        }
    }

    public class Context : Annotation, IElement
    {
        private readonly Dictionary<string, IElement> _entries = new Dictionary<string, IElement>();

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            _entries.Clear();
            if (raw.ContainsKey("context"))
            {
                var context = raw["context"] as Dictionary<string, object>;
                foreach (var (key, entry) in context)
                    _entries[key] = NodeAnnotations.FromJson(new Dictionary<string, object> { { key, entry } });
            }
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            var context = _entries.Values.Select(entry => entry.Save(clean)).SelectMany(dict => dict).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            ret["context"] = context;
            return ret;
        }

        public List<IElement> All() => _entries.Values.ToList();

        public bool Dirty => base.Dirty || _entries.Values.Any(annotation => annotation.Dirty);

        public void AddEntry(string key, IElement entry)
        {
            _entries[key] = entry;
            _dirty = true;
        }

        public bool RemoveEntry(string key)
        {
            var result = _entries.Remove(key);
            if (result)
                _dirty = true;
            return result;
        }
    }

    public class NodeAnnotations : Element
    {
        private Dictionary<string, Annotation> _annotations = new Dictionary<string, Annotation>();

        public int Count => _annotations.Count;

        public static Annotation FromJson(Dictionary<string, object> raw)
        {
            // The implementation of this method is not provided in the Python code,
            // so you will need to provide appropriate definitions or adjust the code accordingly
            // based on the complete context of your application.
            throw new NotImplementedException();
        }

        public List<Annotation> All()
        {
            return new List<Annotation>(_annotations.Values);
        }

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            _annotations.Clear();
            if (!raw.ContainsKey("annotations"))
                return;

            foreach (var rawAnnotation in raw["annotations"] as List<object>)
            {
                if (rawAnnotation is Dictionary<string, object> rawDict)
                {
                    var annotation = FromJson(rawDict);
                    if (annotation != null)
                        _annotations[annotation.Id] = annotation;
                }
            }
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["kind"] = "notes#annotationsGroup";
            if (_annotations.Count > 0)
            {
                var annotationsList = new List<Dictionary<string, object>>();
                foreach (var annotation in _annotations.Values)
                {
                    annotationsList.Add(annotation.Save(clean));
                }
                ret["annotations"] = annotationsList;
            }
            return ret;
        }

        private Annotation _getCategoryNode()
        {
            foreach (var annotation in _annotations.Values)
            {
                if (annotation is Category categoryAnnotation)
                    return categoryAnnotation;
            }
            return null;
        }

        public CategoryValue? Category
        {
            get
            {
                var node = _getCategoryNode();
                return node?.Category;
            }
            set
            {
                var node = _getCategoryNode();
                if (value == null)
                {
                    if (node != null)
                        _annotations.Remove(node.Id);
                }
                else
                {
                    if (node == null)
                    {
                        node = new Category();
                        _annotations[node.Id] = node;
                    }
                    node.Category = value.Value;
                }
                _dirty = true;
            }
        }

        public List<WebLink> Links
        {
            get
            {
                var links = new List<WebLink>();
                foreach (var annotation in _annotations.Values)
                {
                    if (annotation is WebLink webLink)
                        links.Add(webLink);
                }
                return links;
            }
        }

        public void Append(Annotation annotation)
        {
            _annotations[annotation.Id] = annotation;
            _dirty = true;
        }

        public void Remove(Annotation annotation)
        {
            _annotations.Remove(annotation.Id);
            _dirty = true;
        }
    }

    public class NodeTimestamps : Element
    {
        private static readonly string TZ_FMT = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public DateTime Created { get; set; }
        public DateTime? Deleted { get; set; }
        public DateTime? Trashed { get; set; }
        public DateTime Updated { get; set; }
        public DateTime? Edited { get; set; }

        public NodeTimestamps(double createTime = 0)
        {
            if (createTime == 0)
                createTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            Created = IntToDt(createTime);
            Deleted = null;
            Trashed = null;
            Updated = IntToDt(createTime);
            Edited = null;
        }

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            if (raw.ContainsKey("created"))
                Created = StrToDt(raw["created"].ToString());
            Deleted = raw.ContainsKey("deleted") ? StrToDt(raw["deleted"].ToString()) : null;
            Trashed = raw.ContainsKey("trashed") ? StrToDt(raw["trashed"].ToString()) : null;
            Updated = StrToDt(raw["updated"].ToString());
            Edited = raw.ContainsKey("userEdited") ? StrToDt(raw["userEdited"].ToString()) : null;
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["kind"] = "notes#timestamps";
            ret["created"] = DtToStr(Created);
            if (Deleted != null)
                ret["deleted"] = DtToStr(Deleted.Value);
            if (Trashed != null)
                ret["trashed"] = DtToStr(Trashed.Value);
            ret["updated"] = DtToStr(Updated);
            if (Edited != null)
                ret["userEdited"] = DtToStr(Edited.Value);
            return ret;
        }

        public static DateTime StrToDt(string tzs)
        {
            return DateTime.ParseExact(tzs, TZ_FMT, null);
        }

        public static DateTime IntToDt(double tz)
        {
            return DateTimeOffset.FromUnixTimeSeconds((long)tz).DateTime;
        }

        public static string DtToStr(DateTime dt)
        {
            return dt.ToString(TZ_FMT);
        }

        public static string IntToStr(double tz)
        {
            return DtToStr(IntToDt(tz));
        }
    }

    public class NodeSettings : Element
    {
        public NewListItemPlacementValue NewListItemPlacement { get; set; } = NewListItemPlacementValue.Bottom;
        public GraveyardStateValue GraveyardState { get; set; } = GraveyardStateValue.Collapsed;
        public CheckedListItemsPolicyValue CheckedListItemsPolicy { get; set; } = CheckedListItemsPolicyValue.Graveyard;

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            NewListItemPlacement = new NewListItemPlacementValue(raw["newListItemPlacement"].ToString());
            GraveyardState = new GraveyardStateValue(raw["graveyardState"].ToString());
            CheckedListItemsPolicy = new CheckedListItemsPolicyValue(raw["checkedListItemsPolicy"].ToString());
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["newListItemPlacement"] = NewListItemPlacement.Value;
            ret["graveyardState"] = GraveyardState.Value;
            ret["checkedListItemsPolicy"] = CheckedListItemsPolicy.Value;
            return ret;
        }
    }

    public class NodeCollaborators : Element
    {
        private Dictionary<string, RoleValue> _collaborators = new Dictionary<string, RoleValue>();

        public int Count => _collaborators.Count;

        public void Load(List<Dictionary<string, object>> collaboratorsRaw, List<Dictionary<string, object>> requestsRaw)
        {
            if (requestsRaw.Count > 0 && requestsRaw[requestsRaw.Count - 1] is Dictionary<string, object> lastReqDict && lastReqDict.ContainsKey("type") && lastReqDict["type"] is bool)
            {
                _dirty = (bool)lastReqDict["type"];
                requestsRaw.RemoveAt(requestsRaw.Count - 1);
            }
            else
            {
                _dirty = false;
            }

            _collaborators.Clear();
            foreach (var collaborator in collaboratorsRaw)
            {
                if (collaborator.ContainsKey("email") && collaborator["email"] is string email)
                {
                    _collaborators[email] = new RoleValue(collaborator["role"].ToString());
                }
            }

            foreach (var collaborator in requestsRaw)
            {
                if (collaborator.ContainsKey("email") && collaborator["email"] is string email && collaborator.ContainsKey("type") && collaborator["type"] is string type)
                {
                    _collaborators[email] = new ShareRequestValue(type);
                }
            }
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = new Dictionary<string, object>();
            var collaborators = new List<Dictionary<string, object>>();
            var requests = new List<Dictionary<string, object>>();

            foreach (var (email, action) in _collaborators)
            {
                if (action is ShareRequestValue requestValue)
                {
                    requests.Add(new Dictionary<string, object>
                {
                    { "email", email },
                    { "type", requestValue.Value }
                });
                }
                else if (action is RoleValue roleValue)
                {
                    collaborators.Add(new Dictionary<string, object>
                {
                    { "email", email },
                    { "role", roleValue.Value },
                    { "auxiliary_type", "None" }
                });
                }
            }

            if (!clean)
                requests.Add(new Dictionary<string, object> { { "type", _dirty } });

            ret["collaborators"] = collaborators;
            ret["requests"] = requests;
            return ret;
        }

        public void Add(string email)
        {
            if (!_collaborators.ContainsKey(email))
            {
                _collaborators[email] = ShareRequestValue.Add;
            }
            _dirty = true;
        }

        public void Remove(string email)
        {
            if (_collaborators.ContainsKey(email))
            {
                if (_collaborators[email] == ShareRequestValue.Add)
                    _collaborators.Remove(email);
                else
                    _collaborators[email] = ShareRequestValue.Remove;
            }
            _dirty = true;
        }

        public List<string> All()
        {
            var collaboratorsList = new List<string>();
            foreach (var (email, action) in _collaborators)
            {
                if (action == RoleValue.Owner || action == RoleValue.User || action == ShareRequestValue.Add)
                    collaboratorsList.Add(email);
            }
            return collaboratorsList;
        }
    }

    public class NodeLabels : Element
    {
        private Dictionary<string, Label> _labels = new Dictionary<string, Label>();

        public int Count => _labels.Count;

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            if (raw.Count > 0 && raw[raw.Count - 1] is bool)
            {
                _dirty = (bool)raw[raw.Count - 1];
                raw.RemoveAt(raw.Count - 1);
            }
            else
            {
                _dirty = false;
            }
            _labels.Clear();
            foreach (var rawLabel in raw)
            {
                if (rawLabel.Value is Dictionary<string, object> labelDict)
                {
                    var label = new Label();
                    label.Load(labelDict);
                    _labels[rawLabel.Key] = label;
                }
            }
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = new Dictionary<string, object>();
            foreach (var (labelId, label) in _labels)
            {
                var labelDict = label.Save(clean);
                ret[labelId] = labelDict.Count == 0 ? null : labelDict;
            }

            if (!clean)
                ret[_dirty.ToString()] = null;

            return ret;
        }

        public void Add(Label label)
        {
            _labels[label.Id] = label;
            _dirty = true;
        }

        public void Remove(Label label)
        {
            if (_labels.ContainsKey(label.Id))
                _labels[label.Id] = null;
            _dirty = true;
        }

        public Label Get(string labelId)
        {
            _labels.TryGetValue(labelId, out var label);
            return label;
        }

        public List<Label> All()
        {
            var labelList = new List<Label>();
            foreach (var label in _labels.Values)
            {
                if (label != null)
                    labelList.Add(label);
            }
            return labelList;
        }
    }

    public interface ITimestamps
    {
        bool Dirty { get; set; }
        NodeTimestamps Timestamps { get; set; }
    }

    public static class TimestampsExtensions
    {
        public static void Touch(this ITimestamps timestamps, bool edited = false)
        {
            timestamps.Dirty = true;
            DateTime dt = DateTime.UtcNow;
            timestamps.Timestamps.Updated = dt;
            if (edited)
                timestamps.Timestamps.Edited = dt;
        }

        public static bool Trashed(this ITimestamps timestamps)
        {
            return timestamps.Timestamps.Trashed != null && timestamps.Timestamps.Trashed > NodeTimestamps.IntToDt(0);
        }

        public static void Trash(this ITimestamps timestamps)
        {
            timestamps.Timestamps.Trashed = DateTime.UtcNow;
        }

        public static void Untrash(this ITimestamps timestamps)
        {
            timestamps.Timestamps.Trashed = NodeTimestamps.IntToDt(0);
        }

        public static bool Deleted(this ITimestamps timestamps)
        {
            return timestamps.Timestamps.Deleted != null && timestamps.Timestamps.Deleted > NodeTimestamps.IntToDt(0);
        }

        public static void Delete(this ITimestamps timestamps)
        {
            timestamps.Timestamps.Deleted = DateTime.UtcNow;
        }

        public static void Undelete(this ITimestamps timestamps)
        {
            timestamps.Timestamps.Deleted = null;
        }
    }

    public class Node : Element, ITimestamps
    {
        public bool Dirty { get; set; }
        public NodeTimestamps Timestamps { get; set; }

        public Node(string id = null, NodeType? type = null, string parentId = null)
        {
            base();
            double createTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            this.Parent = null;
            this.Id = id ?? this.GenerateId(createTime);
            this.ServerId = null;
            this.ParentId = parentId;
            this.Type = type;
            this.Sort = new Random().Next(1000000000, 9999999999);
            this.Version = null;
            this.Text = "";
            this._children = new Dictionary<string, Node>();
            this.Timestamps = new NodeTimestamps(createTime);
            this.Settings = new NodeSettings();
            this.Annotations = new NodeAnnotations();

            // Set if there is no baseVersion in the raw data
            this.Moved = false;
        }

        private string GenerateId(double tz)
        {
            return $"{(long)(tz * 1000):x}.{RandomId()}";
        }

        private long RandomId()
        {
            return new Random().Next(0x0000000000000000, 0xffffffffffffffff);
        }

        public void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            // Verify this is a valid type
            NodeType rawType = raw["type"];
            if (!Enum.IsDefined(typeof(NodeType), rawType))
                throw new InvalidOperationException("Invalid node type: " + rawType);

            if (raw["kind"] != "notes#node")
                Console.WriteLine("Unknown node kind: " + raw["kind"]);

            if (raw.ContainsKey("mergeConflict"))
                throw new Exception("Merge exception");

            Id = raw["id"];
            ServerId = raw.ContainsKey("serverId") ? raw["serverId"] : ServerId;
            ParentId = raw["parentId"];
            Sort = raw.ContainsKey("sortValue") ? (long)raw["sortValue"] : Sort;
            Version = raw.ContainsKey("baseVersion") ? raw["baseVersion"] : Version;
            Text = raw.ContainsKey("text") ? raw["text"] : Text;
            Timestamps.Load(raw["timestamps"]);
            Settings.Load(raw["nodeSettings"]);
            Annotations.Load(raw["annotationsGroup"]);
        }

        public Dictionary<string, dynamic> Save(bool clean = true)
        {
            Dictionary<string, dynamic> ret = base.Save(clean);
            ret["id"] = Id;
            ret["kind"] = "notes#node";
            ret["type"] = (int)Type;
            ret["parentId"] = ParentId;
            ret["sortValue"] = Sort;
            if (!Moved && Version != null)
                ret["baseVersion"] = Version;
            ret["text"] = Text;
            if (ServerId != null)
                ret["serverId"] = ServerId;
            ret["timestamps"] = Timestamps.Save(clean);
            ret["nodeSettings"] = Settings.Save(clean);
            ret["annotationsGroup"] = Annotations.Save(clean);
            return ret;
        }

        public long Sort { get; set; }
        public string Id { get; set; }
        public string ServerId { get; set; }
        public string ParentId { get; set; }
        public NodeType? Type { get; set; }
        public string Text { get; set; }
        private Dictionary<string, Node> _children;

        public Node Parent { get; set; }
        public IReadOnlyDictionary<string, Node> Children => _children;

        public void SortChildren()
        {
            List<Node> children = new List<Node>(_children.Values);
            children.Sort((a, b) => a.Sort.CompareTo(b.Sort));

            _children.Clear();
            foreach (Node child in children)
                _children[child.Id] = child;
        }

        public void TrashChildren()
        {
            foreach (Node child in _children.Values)
                child.Trash();
        }

        public void UntrashChildren()
        {
            foreach (Node child in _children.Values)
                child.Untrash();
        }

        public void DeleteChildren()
        {
            foreach (Node child in _children.Values)
                child.Delete();
        }

        public void UndeleteChildren()
        {
            foreach (Node child in _children.Values)
                child.Undelete();
        }

        public void SetVersion(string version)
        {
            Version = version;
            Moved = false;
        }

        public void ClearVersion()
        {
            Version = null;
            Moved = false;
        }

        public bool Moved { get; set; }

        public bool New
        {
            get { return ServerId == null; }
        }

        public bool Edited
        {
            get { return Timestamps.Edited != null; }
        }

        public bool Deleted
        {
            get { return Timestamps.Deleted != null && Timestamps.Deleted > NodeTimestamps.IntToDt(0); }
        }

        public void Load(Data raw)
        {
            // verify this is a valid type
            NodeType rawType = raw.Type;
            if (!Enum.IsDefined(typeof(NodeType), rawType))
                throw new InvalidOperationException("Invalid node type: " + rawType);

            Id = raw.Id;
            ServerId = raw.ServerId ?? ServerId;
            ParentId = raw.ParentId;
            Sort = raw.Sort ?? Sort;
            Version = raw.BaseVersion ?? Version;
            Text = raw.Text ?? Text;
            Timestamps.Load(raw.Timestamps);
            Settings.Load(raw.NodeSettings);
            Annotations.Load(raw.AnnotationsGroup);
        }

        public Data Save()
        {
            Data ret = new Data();
            ret.Kind = "notes#node";
            ret.Type = Type;
            ret.Id = Id;
            ret.ParentId = ParentId;
            ret.Sort = Sort;
            ret.Text = Text;
            ret.ServerId = ServerId;
            ret.Timestamps = Timestamps.Save();
            ret.NodeSettings = Settings.Save();
            ret.AnnotationsGroup = Annotations.Save();
            return ret;
        }

        public void SetVersion(string version)
        {
            Version = version;
            Moved = false;
        }

        public void ClearVersion()
        {
            Version = null;
            Moved = false;
        }

        public void Load(Data raw)
        {
            // verify this is a valid type
            NodeType rawType = raw.Type;
            if (!Enum.IsDefined(typeof(NodeType), rawType))
                throw new InvalidOperationException("Invalid node type: " + rawType);

            Id = raw.Id;
            ServerId = raw.ServerId ?? ServerId;
            ParentId = raw.ParentId;
            Sort = raw.Sort ?? Sort;
            Version = raw.BaseVersion ?? Version;
            Text = raw.Text ?? Text;
            Timestamps.Load(raw.Timestamps);
            Settings.Load(raw.NodeSettings);
            Annotations.Load(raw.AnnotationsGroup);
        }

        public Data Save()
        {
            Data ret = new Data();
            ret.Kind = "notes#node";
            ret.Type = Type;
            ret.Id = Id;
            ret.ParentId = ParentId;
            ret.Sort = Sort;
            ret.Text = Text;
            ret.ServerId = ServerId;
            ret.Timestamps = Timestamps.Save();
            ret.NodeSettings = Settings.Save();
            ret.AnnotationsGroup = Annotations.Save();
            return ret;
        }
    }

    public class Root : Node
    {
        public const string ID = "root";

        public Root() : base(ID) { }

        public override bool Dirty => false;
    }

    public class TopLevelNode : Node
    {
        protected static NodeType _TYPE = null;
        protected ColorValue _color = ColorValue.White;
        protected bool _archived = false;
        protected bool _pinned = false;
        protected string _title = "";
        public NodeLabels labels { get; set; }
        public NodeCollaborators collaborators { get; set; }

        public TopLevelNode(Dictionary<string, dynamic> kwargs) : base(parentId: Root.ID, id_: kwargs.GetValueOrDefault("id"))
        {
            this._color = (ColorValue)kwargs.GetValueOrDefault("color", ColorValue.White);
            this._archived = kwargs.GetValueOrDefault("isArchived", false);
            this._pinned = kwargs.GetValueOrDefault("isPinned", false);
            this._title = kwargs.GetValueOrDefault("title", "");
            this.labels = new NodeLabels();
            this.collaborators = new NodeCollaborators();
        }

        public override bool Dirty => base.Dirty || labels.Dirty || collaborators.Dirty;

        public void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            this._color = (ColorValue)(raw.ContainsKey("color") ? raw["color"] : ColorValue.White);
            this._archived = raw.ContainsKey("isArchived") ? raw["isArchived"] : false;
            this._pinned = raw.ContainsKey("isPinned") ? raw["isPinned"] : false;
            this._title = raw.ContainsKey("title") ? raw["title"] : "";
            this.labels.Load(raw.ContainsKey("labelIds") ? raw["labelIds"] : new List<string>());
            this.collaborators.Load(
                raw.ContainsKey("roleInfo") ? raw["roleInfo"] : new List<string>(),
                raw.ContainsKey("shareRequests") ? raw["shareRequests"] : new List<string>()
            );
            this._moved = raw.ContainsKey("moved");
        }

        public Dictionary<string, dynamic> Save(bool clean = true)
        {
            Dictionary<string, dynamic> ret = base.Save(clean);
            ret["color"] = this._color;
            ret["isArchived"] = this._archived;
            ret["isPinned"] = this._pinned;
            ret["title"] = this._title;
            var labels = this.labels.Save(clean);
            var (collaborators, requests) = this.collaborators.Save(clean);
            if (labels.Count > 0)
                ret["labelIds"] = labels;
            ret["collaborators"] = collaborators;
            if (requests.Count > 0)
                ret["shareRequests"] = requests;
            return ret;
        }

        public ColorValue Color
        {
            get => this._color;
            set
            {
                this._color = value;
                this.Touch(true);
            }
        }

        public bool Archived
        {
            get => this._archived;
            set
            {
                this._archived = value;
                this.Touch(true);
            }
        }

        public bool Pinned
        {
            get => this._pinned;
            set
            {
                this._pinned = value;
                this.Touch(true);
            }
        }

        public string Title
        {
            get => this._title;
            set
            {
                this._title = value;
                this.Touch(true);
            }
        }

        public string Url => "https://keep.google.com/u/0/#" + _TYPE.ToString().ToLower() + "/" + this.Id;

        public bool Dirty => base.Dirty || this.labels.Dirty || this.collaborators.Dirty;

        public List<Blob> Blobs => this.Children.FindAll(node => node is Blob).Cast<Blob>().ToList();

        public List<NodeImage> Images => this.Blobs.FindAll(blob => blob.Blob is NodeImage).Select(blob => blob.Blob as NodeImage).ToList();

        public List<NodeDrawing> Drawings => this.Blobs.FindAll(blob => blob.Blob is NodeDrawing).Select(blob => blob.Blob as NodeDrawing).ToList();

        public List<NodeAudio> Audio => this.Blobs.FindAll(blob => blob.Blob is NodeAudio).Select(blob => blob.Blob as NodeAudio).ToList();
    }

    public class Note : TopLevelNode
    {
        private static NodeType _TYPE = NodeType.Note;

        public Note(Dictionary<string, dynamic> kwargs) : base(kwargs: kwargs, type_: _TYPE) { }

        public ListItem GetTextNode()
        {
            foreach (var child_node in this.Children)
            {
                if (child_node is ListItem listItem)
                {
                    return listItem;
                }
            }

            return null;
        }

        public string Text
        {
            get
            {
                var node = this.GetTextNode();
                return node != null ? node.Text : this._Text;
            }
            set
            {
                var node = this.GetTextNode();
                if (node == null)
                {
                    node = new ListItem(this.Id);
                    this.Append(node, true);
                }

                node.Text = value;
                this.Touch(true);
            }
        }
    }

    public class List : TopLevelNode
    {
        private static NodeType _TYPE = NodeType.List;
        private const int SORT_DELTA = 10000;

        public List(Dictionary<string, dynamic> kwargs) : base(kwargs: kwargs, type_: _TYPE) { }

        public ListItem Add(string text, bool check = false, int? sort = null)
        {
            var node = new ListItem(this.Id, this.ServerId);
            node.Checked = check;
            node.Text = text;

            var items = this.Items;
            if (sort.HasValue)
            {
                node.Sort = sort.Value;
            }
            else if (items.Count > 0)
            {
                var func = new Func<int, int, int>((a, b) => Math.Max(a, b));
                var delta = SORT_DELTA;
                if (sort == NewListItemPlacementValue.Bottom)
                {
                    func = new Func<int, int, int>((a, b) => Math.Min(a, b));
                    delta *= -1;
                }

                node.Sort = func(items.Max(item => (int)item.Sort), node.Sort) + delta;
            }

            this.Append(node, true);
            this.Touch(true);
            return node;
        }

        public string Text => string.Join(Environment.NewLine, new List<string> { this.Title }.Concat(this.Items.Select(node => node.ToString())));

        public static List<SortedListItem> SortedItems(List<SortedListItem> items)
        {
            List<SortedListItem> SortFunc(SortedListItem x) => x.Items.SelectMany(item =>
            {
                var res = new List<SortedListItem> { item };
                res.AddRange(SortFunc(item));
                return res;
            }).ToList();

            return SortFunc(items[0]);
        }

        public List<SortedListItem> Items => this.SortedItems(this.GetItems());

        public List<SortedListItem> Checked => this.SortedItems(this.GetItems(checked_: true));

        public List<SortedListItem> Unchecked => this.SortedItems(this.GetItems(checked_: false));

        public void SortItems(Comparison<SortListItem> comparison)
        {
            this.GetItems().Sort(comparison);
            var sortValue = new Random().Next(1000000000, 9999999999);
            foreach (var node in this.GetItems())
            {
                node.Sort = sortValue;
                sortValue -= SORT_DELTA;
            }
        }

        public override string ToString() => string.Join(Environment.NewLine, new List<string> { this.Title }.Concat(this.Items.Select(item => item.ToString())));

        private List<SortedListItem> GetItems(bool? checked_ = null) => this.Children
            .Where(node => node is SortedListItem listItem && (!checked_.HasValue || listItem.Checked == checked_.Value))
            .Cast<SortedListItem>()
            .ToList();
    }

    public class SortedListItem : ListItem
    {
        public SortedListItem(string parentId = null, string parentServerId = null, string superListItemId = null, Dictionary<string, dynamic> kwargs = null)
            : base(parentId, parentServerId, superListItemId, kwargs: kwargs)
        {
            this.Items = new List<SortedListItem>();
        }

        public List<SortedListItem> Items { get; }

        public void Indent(SortedListItem node, bool dirty = true)
        {
            if (node.Items.Count > 0)
            {
                return;
            }

            this.Items.Add(node);
            node.SuperListItemId = this.Id;
            node.ParentItem = this;
            if (dirty)
            {
                node.Touch(true);
            }
        }

        public void Dedent(SortedListItem node, bool dirty = true)
        {
            if (!this.Items.Contains(node))
            {
                return;
            }

            this.Items.Remove(node);
            node.SuperListItemId = "";
            node.ParentItem = null;
            if (dirty)
            {
                node.Touch(true);
            }
        }

        public bool Indented => this.ParentItem != null;

        public bool Checked
        {
            get => this._Checked;
            set
            {
                this._Checked = value;
                this.Touch(true);
            }
        }

        public class SortedListItemComparer : IComparer<SortedListItem>
        {
            public int Compare(SortedListItem x, SortedListItem y)
            {
                int CompareSubitems(SortedListItem a, SortedListItem b)
                {
                    var aSort = a.ParentItem == null ? a.Sort : a.ParentItem.Sort;
                    var bSort = b.ParentItem == null ? b.Sort : b.ParentItem.Sort;
                    if (aSort != bSort)
                    {
                        return aSort.CompareTo(bSort);
                    }

                    var aSubitems = a.Items.Count == 0 ? new List<SortedListItem> { a } : a.Items;
                    var bSubitems = b.Items.Count == 0 ? new List<SortedListItem> { b } : b.Items;
                    var cmp = new SortedListItemComparer();
                    for (var i = 0; i < Math.Max(aSubitems.Count, bSubitems.Count); i++)
                    {
                        if (i >= aSubitems.Count)
                        {
                            return -1;
                        }

                        if (i >= bSubitems.Count)
                        {
                            return 1;
                        }

                        var res = cmp.Compare(aSubitems[i], bSubitems[i]);
                        if (res != 0)
                        {
                            return res;
                        }
                    }

                    return 0;
                }

                return CompareSubitems(x, y);
            }
        }
    }

    public class ListItem : Node
    {
        public ListItem(string parentId = null, string parentServerId = null, string superListItemId = null, Dictionary<string, dynamic> kwargs = null)
            : base(type_: NodeType.ListItem, parentId: parentId, kwargs: kwargs)
        {
            this.ParentItem = null;
            this.ParentServerId = parentServerId;
            this.SuperListItemId = superListItemId;
            this.PrevSuperListItemId = null;
            this._Subitems = new Dictionary<string, ListItem>();
            this._Checked = false;
        }

        public ListItem ParentItem { get; set; }
        public string ParentServerId { get; set; }
        public string SuperListItemId { get; set; }
        public string PrevSuperListItemId { get; set; }
        private Dictionary<string, ListItem> _Subitems { get; }
        private bool _Checked { get; set; }

        public void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            this.PrevSuperListItemId = this.SuperListItemId;
            this.SuperListItemId = raw.GetValueOrDefault("superListItemId");
            this._Checked = raw.GetValueOrDefault("checked", false);
        }

        public Dictionary<string, dynamic> Save(bool clean = true)
        {
            Dictionary<string, dynamic> ret = base.Save(clean);
            ret["parentServerId"] = this.ParentServerId;
            ret["superListItemId"] = this.SuperListItemId;
            ret["checked"] = this._Checked;
            return ret;
        }

        public ListItem Add(string text, bool check = false, int? sort = null)
        {
            if (this.Parent == null)
            {
                throw new Exception("Item has no parent");
            }

            var node = new ListItem(this.Parent.Id, this.Parent.ServerId);
            node.Checked = check;
            node.Text = text;

            if (this.Parent is List list)
            {
                if (sort.HasValue)
                {
                    node.Sort = sort.Value;
                }
                else if (list.Items.Count > 0)
                {
                    var min = list.Items.Select(item => (int)item.Sort).Min();
                    var max = list.Items.Select(item => (int)item.Sort).Max();
                    node.Sort = check ? max + SORT_DELTA : min - SORT_DELTA;
                }
            }

            this.Parent.Append(node, true);
            this.Parent.Touch(true);
            return node;
        }

        public List<ListItem> Subitems => this.GetSortedSubitems().ToList();

        public IEnumerable<SortedListItem> GetSortedSubitems(bool? check = null)
        {
            foreach (var subitem in this.Children.OfType<SortedListItem>())
            {
                if (check.HasValue && subitem.Checked != check.Value)
                {
                    continue;
                }

                yield return subitem;

                foreach (var item in subitem.GetSortedSubitems(check))
                {
                    yield return item;
                }
            }
        }

        public bool Checked
        {
            get => this._Checked;
            set
            {
                this._Checked = value;
                this.Touch(true);
            }
        }

        public void SortItems(Func<ListItem, ListItem, int> comparison)
        {
            this.GetSortedSubitems().ToList().Sort(new SortedListItem.SortedListItemComparer());
        }

        public override string ToString() => $"{(this.Indented ? "  " : "")}{(this.Checked ? "☑" : "☐")} {this.Text}";
    }

    public class NodeBlob : Element
    {
        public BlobType Type { get; set; }

        public string BlobId { get; set; }

        private string _mediaId;

        public string MediaId
        {
            get { return _mediaId; }
            set { _mediaId = value; }
        }

        private string _mimeType;

        public string MimeType
        {
            get { return _mimeType; }
            set { _mimeType = value; }
        }

        public NodeBlob(BlobType type = BlobType.Audio)
        {
            Type = type;
        }

        public override void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            // Verify this is a valid type
            if (raw.ContainsKey("type"))
            {
                try
                {
                    Type = (BlobType)Enum.Parse(typeof(BlobType), raw["type"]);
                }
                catch (ArgumentException)
                {
                    // Handle invalid BlobType
                    throw new ArgumentException("Invalid BlobType");
                }
            }
            BlobId = raw.ContainsKey("blob_id") ? raw["blob_id"] : null;
            MediaId = raw.ContainsKey("media_id") ? raw["media_id"] : null;
            MimeType = raw.ContainsKey("mimetype") ? raw["mimetype"] : null;
        }

        public override Dictionary<string, dynamic> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["kind"] = "notes#blob";
            ret["type"] = Type.ToString().ToLower();
            if (!string.IsNullOrEmpty(BlobId))
            {
                ret["blob_id"] = BlobId;
            }
            if (!string.IsNullOrEmpty(MediaId))
            {
                ret["media_id"] = MediaId;
            }
            ret["mimetype"] = MimeType;
            return ret;
        }
    }

    public class NodeAudio : NodeBlob
    {
        private int _length;

        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }

        public NodeAudio()
            : base(BlobType.Audio)
        {
        }

        public override void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            Length = raw.ContainsKey("length") ? raw["length"] : 0;
        }

        public override Dictionary<string, dynamic> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            if (Length > 0)
            {
                ret["length"] = Length;
            }
            return ret;
        }
    }

    public class NodeImage : NodeBlob
    {
        public bool IsUploaded { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ByteSize { get; set; }
        public string ExtractedText { get; set; }
        public string ExtractionStatus { get; set; }

        public NodeImage()
            : base(BlobType.Image)
        {
            IsUploaded = false;
        }

        public override void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            IsUploaded = raw.ContainsKey("is_uploaded") ? raw["is_uploaded"] : false;
            Width = raw.ContainsKey("width") ? raw["width"] : 0;
            Height = raw.ContainsKey("height") ? raw["height"] : 0;
            ByteSize = raw.ContainsKey("byte_size") ? raw["byte_size"] : 0;
            ExtractedText = raw.ContainsKey("extracted_text") ? raw["extracted_text"] : "";
            ExtractionStatus = raw.ContainsKey("extraction_status") ? raw["extraction_status"] : "";
        }

        public override Dictionary<string, dynamic> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["width"] = Width;
            ret["height"] = Height;
            ret["byte_size"] = ByteSize;
            ret["extracted_text"] = ExtractedText;
            ret["extraction_status"] = ExtractionStatus;
            return ret;
        }
    }

    public class NodeDrawing : NodeBlob
    {
        public string ExtractedText { get; set; }
        public string ExtractionStatus { get; set; }
        public NodeDrawingInfo DrawingInfo { get; set; }

        public NodeDrawing()
            : base(BlobType.Drawing)
        {
            DrawingInfo = new NodeDrawingInfo();
        }

        public override void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            ExtractedText = raw.ContainsKey("extracted_text") ? raw["extracted_text"] : "";
            ExtractionStatus = raw.ContainsKey("extraction_status") ? raw["extraction_status"] : "";
            if (raw.ContainsKey("drawingInfo"))
            {
                DrawingInfo.Load(raw["drawingInfo"]);
            }
        }

        public override Dictionary<string, dynamic> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["extracted_text"] = ExtractedText;
            ret["extraction_status"] = ExtractionStatus;
            if (DrawingInfo != null)
            {
                ret["drawingInfo"] = DrawingInfo.Save(clean);
            }
            return ret;
        }
    }

    public class NodeDrawingInfo : Element
    {
        public string DrawingId { get; set; }
        public NodeImage Snapshot { get; set; }
        private string _snapshotFingerprint;
        public DateTime ThumbnailGeneratedTime { get; set; }
        private string _inkHash;
        private string _snapshotProtoFprint;

        public NodeDrawingInfo()
        {
            Snapshot = new NodeImage();
            ThumbnailGeneratedTime = new DateTime(0);
        }

        public override void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            DrawingId = raw["drawingId"];
            Snapshot.Load(raw["snapshotData"]);
            _snapshotFingerprint = raw.ContainsKey("snapshotFingerprint") ? raw["snapshotFingerprint"] : _snapshotFingerprint;
            ThumbnailGeneratedTime = raw.ContainsKey("thumbnailGeneratedTime")
                ? DateTime.Parse(raw["thumbnailGeneratedTime"])
                : new DateTime(0);
            _inkHash = raw.ContainsKey("inkHash") ? raw["inkHash"] : "";
            _snapshotProtoFprint = raw.ContainsKey("snapshotProtoFprint") ? raw["snapshotProtoFprint"] : _snapshotProtoFprint;
        }

        public override Dictionary<string, dynamic> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["drawingId"] = DrawingId;
            ret["snapshotData"] = Snapshot.Save(clean);
            ret["snapshotFingerprint"] = _snapshotFingerprint;
            ret["thumbnailGeneratedTime"] = ThumbnailGeneratedTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            ret["inkHash"] = _inkHash;
            ret["snapshotProtoFprint"] = _snapshotProtoFprint;
            return ret;
        }
    }

    public class Blob : Node
    {
        private static readonly Dictionary<BlobType, Type> _blobTypeMap = new Dictionary<BlobType, Type>
    {
        { BlobType.Audio, typeof(NodeAudio) },
        { BlobType.Image, typeof(NodeImage) },
        { BlobType.Drawing, typeof(NodeDrawing) }
    };

        public Blob(string parentId = null, Dictionary<string, dynamic> kwargs = null)
            : base(NodeType.Blob, parentId, kwargs)
        {
            Blob = null;
        }

        public NodeBlob Blob { get; private set; }

        public static NodeBlob FromJson(Dictionary<string, dynamic> raw)
        {
            if (raw == null)
            {
                return null;
            }

            if (!raw.ContainsKey("type"))
            {
                return null;
            }

            if (!_blobTypeMap.TryGetValue(Enum.TryParse(raw["type"], out BlobType type) ? type : BlobType.Unknown, out var bcls))
            {
                // Handle unknown blob types
                // logger.Warning("Unknown blob type: " + type);
                return null;
            }

            var blob = Activator.CreateInstance(bcls) as NodeBlob;
            blob?.Load(raw);

            return blob;
        }

        public override void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            Blob = FromJson(raw.ContainsKey("blob") ? raw["blob"] : null);
        }

        public override Dictionary<string, dynamic> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            if (Blob != null)
            {
                ret["blob"] = Blob.Save(clean);
            }
            return ret;
        }
    }

    public class Label : Element, ITimestamps
    {
        public Label()
        {
            double createTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            this.Id = this.GenerateId(createTime);
            this._Name = "";
            this.Timestamps = new NodeTimestamps(createTime);
            this._Merged = NodeTimestamps.IntToDateTimeOffset(0);
        }

        public string Id { get; private set; }
        private string _Name { get; set; }
        public NodeTimestamps Timestamps { get; private set; }
        private DateTimeOffset _Merged { get; set; }

        private string GenerateId(double tz)
        {
            return $"tag.{string.Join("", Enumerable.Range(0, 12).Select(_ => "abcdefghijklmnopqrstuvwxyz0123456789"[new Random().Next(36)]))}.{(long)(tz * 1000)}";
        }

        public void Load(Dictionary<string, dynamic> raw)
        {
            base.Load(raw);
            this.Id = raw["mainId"];
            this._Name = raw["name"];
            this.Timestamps.Load(raw["timestamps"]);
            this._Merged = raw.ContainsKey("lastMerged") ? NodeTimestamps.StrToDateTimeOffset(raw["lastMerged"]) : NodeTimestamps.IntToDateTimeOffset(0);
        }

        public new Dictionary<string, dynamic> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["mainId"] = this.Id;
            ret["name"] = this._Name;
            ret["timestamps"] = this.Timestamps.Save(clean);
            ret["lastMerged"] = NodeTimestamps.DateTimeOffsetToStr(this._Merged);
            return ret;
        }

        public string Name
        {
            get => this._Name;
            set
            {
                this._Name = value;
                this.Touch(true);
            }
        }

        public DateTimeOffset Merged
        {
            get => this._Merged;
            set
            {
                this._Merged = value;
                this.Touch();
            }
        }

        public bool Dirty => base.Dirty || this.Timestamps.Dirty;

        public override string ToString()
        {
            return this.Name;
        }
    }
}
