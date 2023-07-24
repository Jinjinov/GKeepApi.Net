using Ardalis.SmartEnum;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// node.py
/// </summary>
namespace GoogleKeep
{
    public static class DictionaryExtensions
    {
        public static T GetValueOrDefault<T>(this Dictionary<string, object> dictionary, string key, T defaultValue)
        {
            return dictionary.TryGetValue(key, out object value) && value is T val ? val : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }

    public interface ISmartEnum { }

    public sealed class NodeType : SmartEnum<NodeType>, ISmartEnum
    {
        public static readonly NodeType Note = new NodeType("NOTE", 1);
        public static readonly NodeType List = new NodeType("LIST", 2);
        public static readonly NodeType ListItem = new NodeType("LIST_ITEM", 3);
        public static readonly NodeType Blob = new NodeType("BLOB", 4);

        private NodeType(string name, int value) : base(name, value)
        {
        }
    }

    public sealed class BlobType : SmartEnum<BlobType>, ISmartEnum
    {
        public static readonly BlobType Audio = new BlobType("AUDIO", 1);
        public static readonly BlobType Image = new BlobType("IMAGE", 2);
        public static readonly BlobType Drawing = new BlobType("DRAWING", 3);

        private BlobType(string name, int value) : base(name, value)
        {
        }
    }

    public sealed class ColorValue : SmartEnum<ColorValue>, ISmartEnum
    {
        public static readonly ColorValue White = new ColorValue("DEFAULT", 1);
        public static readonly ColorValue Red = new ColorValue("RED", 2);
        public static readonly ColorValue Orange = new ColorValue("ORANGE", 3);
        public static readonly ColorValue Yellow = new ColorValue("YELLOW", 4);
        public static readonly ColorValue Green = new ColorValue("GREEN", 5);
        public static readonly ColorValue Teal = new ColorValue("TEAL", 6);
        public static readonly ColorValue Blue = new ColorValue("BLUE", 7);
        public static readonly ColorValue DarkBlue = new ColorValue("CERULEAN", 8);
        public static readonly ColorValue Purple = new ColorValue("PURPLE", 9);
        public static readonly ColorValue Pink = new ColorValue("PINK", 10);
        public static readonly ColorValue Brown = new ColorValue("BROWN", 11);
        public static readonly ColorValue Gray = new ColorValue("GRAY", 12);

        private ColorValue(string name, int value) : base(name, value)
        {
        }
    }

    public sealed class CategoryValue : SmartEnum<CategoryValue>, ISmartEnum
    {
        public static readonly CategoryValue Books = new CategoryValue("BOOKS", 1);
        public static readonly CategoryValue Food = new CategoryValue("FOOD", 2);
        public static readonly CategoryValue Movies = new CategoryValue("MOVIES", 3);
        public static readonly CategoryValue Music = new CategoryValue("MUSIC", 4);
        public static readonly CategoryValue Places = new CategoryValue("PLACES", 5);
        public static readonly CategoryValue Quotes = new CategoryValue("QUOTES", 6);
        public static readonly CategoryValue Travel = new CategoryValue("TRAVEL", 7);
        public static readonly CategoryValue TV = new CategoryValue("TV", 8);

        private CategoryValue(string name, int value) : base(name, value)
        {
        }
    }

    public sealed class SuggestValue : SmartEnum<SuggestValue>, ISmartEnum
    {
        public static readonly SuggestValue GroceryItem = new SuggestValue("GROCERY_ITEM", 1);

        private SuggestValue(string name, int value) : base(name, value)
        {
        }
    }

    public sealed class NewListItemPlacementValue : SmartEnum<NewListItemPlacementValue>, ISmartEnum
    {
        public static readonly NewListItemPlacementValue Top = new NewListItemPlacementValue("TOP", 1);
        public static readonly NewListItemPlacementValue Bottom = new NewListItemPlacementValue("BOTTOM", 2);

        private NewListItemPlacementValue(string name, int value) : base(name, value)
        {
        }
    }

    public sealed class GraveyardStateValue : SmartEnum<GraveyardStateValue>, ISmartEnum
    {
        public static readonly GraveyardStateValue Expanded = new GraveyardStateValue("EXPANDED", 1);
        public static readonly GraveyardStateValue Collapsed = new GraveyardStateValue("COLLAPSED", 2);

        private GraveyardStateValue(string name, int value) : base(name, value)
        {
        }
    }

    public sealed class CheckedListItemsPolicyValue : SmartEnum<CheckedListItemsPolicyValue>, ISmartEnum
    {
        public static readonly CheckedListItemsPolicyValue Default = new CheckedListItemsPolicyValue("DEFAULT", 1);
        public static readonly CheckedListItemsPolicyValue Graveyard = new CheckedListItemsPolicyValue("GRAVEYARD", 2);

        private CheckedListItemsPolicyValue(string name, int value) : base(name, value)
        {
        }
    }

    public sealed class ShareRequestValue : SmartEnum<ShareRequestValue>, ISmartEnum
    {
        public static readonly ShareRequestValue Add = new ShareRequestValue("WR", 1);
        public static readonly ShareRequestValue Remove = new ShareRequestValue("RM", 2);

        private ShareRequestValue(string name, int value) : base(name, value)
        {
        }
    }

    public sealed class RoleValue : SmartEnum<RoleValue>, ISmartEnum
    {
        public static readonly RoleValue Owner = new RoleValue("O", 1);
        public static readonly RoleValue User = new RoleValue("W", 2);

        private RoleValue(string name, int value) : base(name, value)
        {
        }
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

        public virtual void Load(Dictionary<string, object> raw)
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

        public virtual bool Dirty
        {
            get
            {
                return _dirty;
            }
            set
            {
                _dirty = value;
            }
        }
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
            return Guid.NewGuid().ToString();
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

            if (raw["topicCategory"] is Dictionary<string, object> topicCategory)
            {
                _category = (CategoryValue)Enum.Parse(typeof(CategoryValue), topicCategory["category"].ToString());
            }
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

        public CategoryValue CategoryValue
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

            if (raw["taskAssist"] is Dictionary<string, object> taskAssist)
            {
                _suggest = taskAssist["suggestType"].ToString();
            }
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
                foreach (var pair in context)
                {
                    var key = pair.Key;
                    var entry = pair.Value;
                    _entries[key] = NodeAnnotations.FromJson(new Dictionary<string, object> { { key, entry } });
                }
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

        public override bool Dirty => base.Dirty || _entries.Values.Any(annotation => annotation.Dirty);

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
        private readonly Dictionary<string, Annotation> _annotations = new Dictionary<string, Annotation>();

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

        private Category GetCategoryNode()
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
                var node = GetCategoryNode();
                return node?.CategoryValue;
            }
            set
            {
                var node = GetCategoryNode();
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
                    node.CategoryValue = value;
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
            Deleted = raw.ContainsKey("deleted") ? (DateTime?)StrToDt(raw["deleted"].ToString()) : null;
            Trashed = raw.ContainsKey("trashed") ? (DateTime?)StrToDt(raw["trashed"].ToString()) : null;
            Updated = StrToDt(raw["updated"].ToString());
            Edited = raw.ContainsKey("userEdited") ? (DateTime?)StrToDt(raw["userEdited"].ToString()) : null;
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
            NewListItemPlacement = NewListItemPlacementValue.FromName(raw["newListItemPlacement"].ToString());
            GraveyardState = GraveyardStateValue.FromName(raw["graveyardState"].ToString());
            CheckedListItemsPolicy = CheckedListItemsPolicyValue.FromName(raw["checkedListItemsPolicy"].ToString());
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["newListItemPlacement"] = NewListItemPlacement;
            ret["graveyardState"] = GraveyardState;
            ret["checkedListItemsPolicy"] = CheckedListItemsPolicy;
            return ret;
        }
    }

    public class NodeCollaborators : Element
    {
        private readonly Dictionary<string, ISmartEnum> _collaborators = new Dictionary<string, ISmartEnum>();

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
                    _collaborators[email] = RoleValue.FromName(collaborator["role"].ToString());
                }
            }

            foreach (var collaborator in requestsRaw)
            {
                if (collaborator.ContainsKey("email") && collaborator["email"] is string email && collaborator.ContainsKey("type") && collaborator["type"] is string type)
                {
                    _collaborators[email] = ShareRequestValue.FromName(type);
                }
            }
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = new Dictionary<string, object>();
            var collaborators = new List<Dictionary<string, object>>();
            var requests = new List<Dictionary<string, object>>();

            foreach (var pair in _collaborators)
            {
                var email = pair.Key;
                var action = pair.Value;
                if (action is ShareRequestValue requestValue)
                {
                    requests.Add(new Dictionary<string, object>
                    {
                        { "email", email },
                        { "type", requestValue }
                    });
                }
                else if (action is RoleValue roleValue)
                {
                    collaborators.Add(new Dictionary<string, object>
                    {
                        { "email", email },
                        { "role", roleValue },
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
            foreach (var pair in _collaborators)
            {
                var email = pair.Key;
                var action = pair.Value;
                if (action == RoleValue.Owner || action == RoleValue.User || action == ShareRequestValue.Add)
                    collaboratorsList.Add(email);
            }
            return collaboratorsList;
        }
    }

    public class NodeLabels : Element
    {
        private readonly Dictionary<string, Label> _labels = new Dictionary<string, Label>();

        public int Count => _labels.Count;

        public Dictionary<string, Label>.KeyCollection Keys => _labels.Keys;

        protected override void _load(Dictionary<string, object> raw)
        {
            base._load(raw);
            if (raw.Count > 0 && raw.Last().Value is bool dirty)
            {
                _dirty = dirty;
                raw.Remove(raw.Last().Key);
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
            foreach (var pair in _labels)
            {
                var labelId = pair.Key;
                var label = pair.Value;
                var labelDict = label.Save(clean);
                ret[labelId] = labelDict.Count == 0 ? null : labelDict;
            }

            if (!clean)
                ret[_dirty.ToString()] = null;

            return ret;
        }

        public bool Any() => _labels.Any();

        public bool ContainsKey(string id) => _labels.ContainsKey(id);

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
        public override bool Dirty { get; set; }
        public NodeTimestamps Timestamps { get; set; }

        public Node(string id = null, NodeType? type = null, string parentId = null) : base()
        {
            double createTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            this.Parent = null;
            this.Id = id ?? this.GenerateId(createTime);
            this.ServerId = null;
            this.ParentId = parentId;
            this.Type = type;
            this.Sort = new Random().Next(1000000000, int.MaxValue);
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
            byte[] buffer = new byte[8];
            new Random().NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        public override void Load(Dictionary<string, object> raw)
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

        public override Dictionary<string, object> Save(bool clean = true)
        {
            Dictionary<string, object> ret = base.Save(clean);
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
        public virtual string Text { get; set; }
        public string Version { get; set; }
        private readonly Dictionary<string, Node> _children;
        public NodeSettings Settings { get; set; }
        public NodeAnnotations Annotations { get; set; }

        public Node Parent { get; set; }
        public IReadOnlyDictionary<string, Node> Children => _children;

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

        public Node Get(string node_id)
        {
            // Get child node with the given ID.
            return this._children.ContainsKey(node_id) ? this._children[node_id] : null;
        }

        public Node Append(Node node, bool dirty = true)
        {
            // Add a new child node.
            this._children[node.Id] = node;
            node.Parent = this;
            if (dirty)
            {
                this.Touch();
            }

            return node;
        }

        public void Remove(Node node, bool dirty = true)
        {
            // Remove the given child node.
            if (this._children.ContainsKey(node.Id))
            {
                this._children[node.Id].Parent = null;
                this._children.Remove(node.Id);
            }
            if (dirty)
            {
                this.Touch();
            }
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
        protected NodeType _TYPE;
        protected ColorValue _color = ColorValue.White;
        protected bool _archived = false;
        protected bool _pinned = false;
        protected string _title = "";
        public NodeLabels Labels { get; set; }
        public NodeCollaborators Collaborators { get; set; }

        public TopLevelNode(Dictionary<string, object> kwargs, NodeType type) : base(type: type, parentId: Root.ID, id: kwargs.GetValueOrDefault("id"))
        {
            this._color = (ColorValue)kwargs.GetValueOrDefault("color", ColorValue.White);
            this._archived = kwargs.GetValueOrDefault("isArchived", false);
            this._pinned = kwargs.GetValueOrDefault("isPinned", false);
            this._title = kwargs.GetValueOrDefault("title", "");
            this.Labels = new NodeLabels();
            this.Collaborators = new NodeCollaborators();
        }

        public override bool Dirty => base.Dirty || Labels.Dirty || Collaborators.Dirty;

        public override void Load(Dictionary<string, object> raw)
        {
            base.Load(raw);
            this._color = (ColorValue)(raw.ContainsKey("color") ? raw["color"] : ColorValue.White);
            this._archived = raw.ContainsKey("isArchived") ? raw["isArchived"] : false;
            this._pinned = raw.ContainsKey("isPinned") ? raw["isPinned"] : false;
            this._title = raw.ContainsKey("title") ? raw["title"] : "";
            this.Labels.Load(raw.ContainsKey("labelIds") ? raw["labelIds"] : new List<string>());
            this.Collaborators.Load(
                raw.ContainsKey("roleInfo") ? raw["roleInfo"] : new List<string>(),
                raw.ContainsKey("shareRequests") ? raw["shareRequests"] : new List<string>()
            );
            this.Moved = raw.ContainsKey("moved");
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            Dictionary<string, object> ret = base.Save(clean);
            ret["color"] = this._color;
            ret["isArchived"] = this._archived;
            ret["isPinned"] = this._pinned;
            ret["title"] = this._title;
            var labels = this.Labels.Save(clean);
            //var (collaborators, requests) = this.Collaborators.Save(clean);
            var dict = this.Collaborators.Save(clean);
            if (labels.Count > 0)
                ret["labelIds"] = labels;
            ret["collaborators"] = dict.Keys;
            if (dict.Values.Count > 0)
                ret["shareRequests"] = dict.Values;
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

        public List<Blob> Blobs => this.Children.OfType<Blob>().ToList();

        public List<NodeImage> Images => this.Blobs.FindAll(blob => blob.NodeBlob is NodeImage).Select(blob => blob.NodeBlob as NodeImage).ToList();

        public List<NodeDrawing> Drawings => this.Blobs.FindAll(blob => blob.NodeBlob is NodeDrawing).Select(blob => blob.NodeBlob as NodeDrawing).ToList();

        public List<NodeAudio> Audio => this.Blobs.FindAll(blob => blob.NodeBlob is NodeAudio).Select(blob => blob.NodeBlob as NodeAudio).ToList();
    }

    public class Note : TopLevelNode
    {
        public Note(Dictionary<string, object> kwargs = null) : base(kwargs: kwargs, type: NodeType.Note)
        {
            _TYPE = NodeType.Note;
        }

        public ListItem GetTextNode()
        {
            foreach (var child_node in this.Children.Values)
            {
                if (child_node is ListItem listItem)
                {
                    return listItem;
                }
            }

            return null;
        }

        public override string Text
        {
            get
            {
                var node = this.GetTextNode();
                return node != null ? node.Text : base.Text;
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
        public const int SORT_DELTA = 10000;

        public List(Dictionary<string, object> kwargs = null) : base(kwargs: kwargs, type: NodeType.List)
        {
            _TYPE = NodeType.List;
        }

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
                var func = new Func<long, long, long>((a, b) => Math.Max(a, b));
                var delta = SORT_DELTA;
                if (sort == NewListItemPlacementValue.Bottom)
                {
                    func = new Func<long, long, long>((a, b) => Math.Min(a, b));
                    delta *= -1;
                }

                node.Sort = func(items.Max(item => item.Sort), node.Sort) + delta;
            }

            this.Append(node, true);
            this.Touch(true);
            return node;
        }

        public override string Text => string.Join(Environment.NewLine, new List<string> { this.Title }.Concat(this.Items.Select(node => node.ToString())));

        public static List<ListItem> SortedItems(List<ListItem> items)
        {
            static List<ListItem> SortFunc(ListItem x) => x.Items.SelectMany(item =>
            {
                var res = new List<ListItem> { item };
                res.AddRange(SortFunc(item));
                return res;
            }).ToList();

            return SortFunc(items[0]);
        }

        public List<ListItem> Items => SortedItems(this.GetItems());

        public List<ListItem> Checked => SortedItems(this.GetItems(checked_: true));

        public List<ListItem> Unchecked => SortedItems(this.GetItems(checked_: false));

        public void SortItems(Comparison<ListItem> comparison)
        {
            this.GetItems().Sort(comparison);
            var sortValue = new Random().Next(1000000000, int.MaxValue);
            foreach (var node in this.GetItems())
            {
                node.Sort = sortValue;
                sortValue -= SORT_DELTA;
            }
        }

        public override string ToString() => string.Join(Environment.NewLine, new List<string> { this.Title }.Concat(this.Items.Select(item => item.ToString())));

        private List<ListItem> GetItems(bool? checked_ = null) => this.Children.Values
            .Where(node => node is ListItem listItem && (!checked_.HasValue || listItem.Checked == checked_.Value))
            .Cast<ListItem>()
            .ToList();
    }

    public class ListItem : Node
    {
        public ListItem(string parentId = null, string parentServerId = null, string superListItemId = null, Dictionary<string, object> kwargs = null)
            : base(type: NodeType.ListItem, parentId: parentId)
        {
            this.ParentItem = null;
            this.ParentServerId = parentServerId;
            this.SuperListItemId = superListItemId;
            this.PrevSuperListItemId = null;
            this._subitems = new Dictionary<string, ListItem>();
            this._checked = false;
            this.Items = new List<ListItem>();
        }

        public ListItem ParentItem { get; set; }
        public string ParentServerId { get; set; }
        public string SuperListItemId { get; set; }
        public string PrevSuperListItemId { get; set; }
        private Dictionary<string, ListItem> _subitems;
        protected bool _checked;

        public override void Load(Dictionary<string, object> raw)
        {
            base.Load(raw);
            this.PrevSuperListItemId = this.SuperListItemId;
            this.SuperListItemId = raw.GetValueOrDefault("superListItemId");
            this._checked = raw.GetValueOrDefault("checked", false);
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            Dictionary<string, object> ret = base.Save(clean);
            ret["parentServerId"] = this.ParentServerId;
            ret["superListItemId"] = this.SuperListItemId;
            ret["checked"] = this._checked;
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
                    node.Sort = check ? max + List.SORT_DELTA : min - List.SORT_DELTA;
                }
            }

            this.Parent.Append(node, true);
            this.Parent.Touch(true);
            return node;
        }

        public List<ListItem> Subitems => this.GetSortedSubitems().ToList();

        public IEnumerable<ListItem> GetSortedSubitems(bool? check = null)
        {
            foreach (var subitem in this.Children.OfType<ListItem>())
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
            get => this._checked;
            set
            {
                this._checked = value;
                this.Touch(true);
            }
        }

        public void SortItems(Func<ListItem, ListItem, int> comparison)
        {
            this.GetSortedSubitems().ToList().Sort(new ListItemComparer());
        }

        public override string ToString() => $"{(this.Indented ? "  " : "")}{(this.Checked ? "☑" : "☐")} {this.Text}";

        public List<ListItem> Items { get; }

        public void Indent(ListItem node, bool dirty = true)
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

        public void Dedent(ListItem node, bool dirty = true)
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

        public class ListItemComparer : IComparer<ListItem>
        {
            public int Compare(ListItem x, ListItem y)
            {
                static int CompareSubitems(ListItem a, ListItem b)
                {
                    var aSort = a.ParentItem == null ? a.Sort : a.ParentItem.Sort;
                    var bSort = b.ParentItem == null ? b.Sort : b.ParentItem.Sort;
                    if (aSort != bSort)
                    {
                        return aSort.CompareTo(bSort);
                    }

                    var aSubitems = a.Items.Count == 0 ? new List<ListItem> { a } : a.Items;
                    var bSubitems = b.Items.Count == 0 ? new List<ListItem> { b } : b.Items;
                    var cmp = new ListItemComparer();
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

        public NodeBlob(BlobType type)
        {
            Type = type;
        }

        public override void Load(Dictionary<string, object> raw)
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

        public override Dictionary<string, object> Save(bool clean = true)
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

        public override void Load(Dictionary<string, object> raw)
        {
            base.Load(raw);
            Length = raw.ContainsKey("length") ? raw["length"] : 0;
        }

        public override Dictionary<string, object> Save(bool clean = true)
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

        public override void Load(Dictionary<string, object> raw)
        {
            base.Load(raw);
            IsUploaded = raw.ContainsKey("is_uploaded") ? raw["is_uploaded"] : false;
            Width = raw.ContainsKey("width") ? raw["width"] : 0;
            Height = raw.ContainsKey("height") ? raw["height"] : 0;
            ByteSize = raw.ContainsKey("byte_size") ? raw["byte_size"] : 0;
            ExtractedText = raw.ContainsKey("extracted_text") ? raw["extracted_text"] : "";
            ExtractionStatus = raw.ContainsKey("extraction_status") ? raw["extraction_status"] : "";
        }

        public override Dictionary<string, object> Save(bool clean = true)
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

        public override void Load(Dictionary<string, object> raw)
        {
            base.Load(raw);
            ExtractedText = raw.ContainsKey("extracted_text") ? raw["extracted_text"] : "";
            ExtractionStatus = raw.ContainsKey("extraction_status") ? raw["extraction_status"] : "";
            if (raw.ContainsKey("drawingInfo"))
            {
                DrawingInfo.Load(raw["drawingInfo"]);
            }
        }

        public override Dictionary<string, object> Save(bool clean = true)
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

        public override void Load(Dictionary<string, object> raw)
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

        public override Dictionary<string, object> Save(bool clean = true)
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

        public Blob(string parentId = null, Dictionary<string, object> kwargs = null)
            : base(type: NodeType.Blob, parentId: parentId)
        {
            NodeBlob = null;
        }

        public NodeBlob NodeBlob { get; private set; }

        public static NodeBlob FromJson(Dictionary<string, object> raw)
        {
            if (raw == null)
            {
                return null;
            }

            if (!raw.ContainsKey("type"))
            {
                return null;
            }

            Type? bcls = null;

            if (!Enum.TryParse(raw["type"], out BlobType type) || !_blobTypeMap.TryGetValue(type, out bcls))
            {
                // Handle unknown blob types
                // logger.Warning("Unknown blob type: " + type);
                return null;
            }

            var blob = Activator.CreateInstance(bcls) as NodeBlob;
            blob?.Load(raw);

            return blob;
        }

        public override void Load(Dictionary<string, object> raw)
        {
            base.Load(raw);
            NodeBlob = FromJson(raw.ContainsKey("blob") ? raw["blob"] : null);
        }

        public override Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            if (NodeBlob != null)
            {
                ret["blob"] = NodeBlob.Save(clean);
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
            this._name = "";
            this.Timestamps = new NodeTimestamps(createTime);
            this._merged = NodeTimestamps.IntToDt(0);
        }

        public string Id { get; private set; }
        private string _name;
        public NodeTimestamps Timestamps { get; set; }
        private DateTime _merged;

        private string GenerateId(double tz)
        {
            return $"tag.{string.Join("", Enumerable.Range(0, 12).Select(_ => "abcdefghijklmnopqrstuvwxyz0123456789"[new Random().Next(36)]))}.{(long)(tz * 1000)}";
        }

        public override void Load(Dictionary<string, object> raw)
        {
            base.Load(raw);
            this.Id = raw["mainId"];
            this._name = raw["name"];
            this.Timestamps.Load(raw["timestamps"]);
            this._merged = raw.ContainsKey("lastMerged") ? NodeTimestamps.StrToDt(raw["lastMerged"]) : NodeTimestamps.IntToDt(0);
        }

        public new Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["mainId"] = this.Id;
            ret["name"] = this._name;
            ret["timestamps"] = this.Timestamps.Save(clean);
            ret["lastMerged"] = NodeTimestamps.DtToStr(this._merged);
            return ret;
        }

        public string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                this.Touch(true);
            }
        }

        public DateTime Merged
        {
            get => this._merged;
            set
            {
                this._merged = value;
                this.Touch();
            }
        }

        public override bool Dirty => base.Dirty || this.Timestamps.Dirty;

        public override string ToString()
        {
            return this.Name;
        }
    }
}
