using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// node.py
/// </summary>
namespace GoogleKeep
{
    public static class EnumEx
    {
        public static TEnum Parse<TEnum>(string value) where TEnum : struct
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value);
        }
    }

    /// <summary>
    /// Valid note types.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NodeType
    {
        /// <summary>
        /// A Note
        /// </summary>
        [JsonPropertyName("NOTE")]
        Note,

        /// <summary>
        /// A List
        /// </summary>
        [JsonPropertyName("LIST")]
        List,

        /// <summary>
        /// A List item
        /// </summary>
        [JsonPropertyName("LIST_ITEM")]
        ListItem,

        /// <summary>
        /// A blob (attachment)
        /// </summary>
        [JsonPropertyName("BLOB")]
        Blob
    }

    /// <summary>
    /// Valid blob types.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BlobType
    {
        /// <summary>
        /// Audio
        /// </summary>
        [JsonPropertyName("AUDIO")]
        Audio,

        /// <summary>
        /// Image
        /// </summary>
        [JsonPropertyName("IMAGE")]
        Image,

        /// <summary>
        /// Drawing
        /// </summary>
        [JsonPropertyName("DRAWING")]
        Drawing
    }

    /// <summary>
    /// Valid note colors.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ColorValue
    {
        /// <summary>
        /// White
        /// </summary>
        [JsonPropertyName("DEFAULT")]
        White,

        /// <summary>
        /// Red
        /// </summary>
        [JsonPropertyName("RED")]
        Red,

        /// <summary>
        /// Orange
        /// </summary>
        [JsonPropertyName("ORANGE")]
        Orange,

        /// <summary>
        /// Yellow
        /// </summary>
        [JsonPropertyName("YELLOW")]
        Yellow,

        /// <summary>
        /// Green
        /// </summary>
        [JsonPropertyName("GREEN")]
        Green,

        /// <summary>
        /// Teal
        /// </summary>
        [JsonPropertyName("TEAL")]
        Teal,

        /// <summary>
        /// Blue
        /// </summary>
        [JsonPropertyName("BLUE")]
        Blue,

        /// <summary>
        /// Dark blue
        /// </summary>
        [JsonPropertyName("CERULEAN")]
        DarkBlue,

        /// <summary>
        /// Purple
        /// </summary>
        [JsonPropertyName("PURPLE")]
        Purple,

        /// <summary>
        /// Pink
        /// </summary>
        [JsonPropertyName("PINK")]
        Pink,

        /// <summary>
        /// Brown
        /// </summary>
        [JsonPropertyName("BROWN")]
        Brown,

        /// <summary>
        /// Gray
        /// </summary>
        [JsonPropertyName("GRAY")]
        Gray
    }

    /// <summary>
    /// Valid note categories.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CategoryValue
    {
        /// <summary>
        /// Books
        /// </summary>
        [JsonPropertyName("BOOKS")]
        Books,

        /// <summary>
        /// Food
        /// </summary>
        [JsonPropertyName("FOOD")]
        Food,

        /// <summary>
        /// Movies
        /// </summary>
        [JsonPropertyName("MOVIES")]
        Movies,

        /// <summary>
        /// Music
        /// </summary>
        [JsonPropertyName("MUSIC")]
        Music,

        /// <summary>
        /// Places
        /// </summary>
        [JsonPropertyName("PLACES")]
        Places,

        /// <summary>
        /// Quotes
        /// </summary>
        [JsonPropertyName("QUOTES")]
        Quotes,

        /// <summary>
        /// Travel
        /// </summary>
        [JsonPropertyName("TRAVEL")]
        Travel,

        /// <summary>
        /// TV
        /// </summary>
        [JsonPropertyName("TV")]
        TV
    }

    /// <summary>
    /// Valid task suggestion categories.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SuggestValue
    {
        /// <summary>
        /// Grocery item
        /// </summary>
        [JsonPropertyName("GROCERY_ITEM")]
        GroceryItem
    }

    /// <summary>
    /// Target location to put new list items.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NewListItemPlacementValue
    {
        /// <summary>
        /// Top
        /// </summary>
        [JsonPropertyName("TOP")]
        Top,

        /// <summary>
        /// Bottom
        /// </summary>
        [JsonPropertyName("BOTTOM")]
        Bottom
    }

    /// <summary>
    /// Visibility setting for the graveyard.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GraveyardStateValue
    {
        /// <summary>
        /// Expanded
        /// </summary>
        [JsonPropertyName("EXPANDED")]
        Expanded,

        /// <summary>
        /// Collapsed
        /// </summary>
        [JsonPropertyName("COLLAPSED")]
        Collapsed
    }

    /// <summary>
    /// Movement setting for checked list items.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CheckedListItemsPolicyValue
    {
        /// <summary>
        /// Default
        /// </summary>
        [JsonPropertyName("DEFAULT")]
        Default,

        /// <summary>
        /// Graveyard
        /// </summary>
        [JsonPropertyName("GRAVEYARD")]
        Graveyard
    }

    /// <summary>
    /// Collaborator change type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ShareRequestValue
    {
        /// <summary>
        /// Grant access.
        /// </summary>
        [JsonPropertyName("WR")]
        Add,

        /// <summary>
        /// Remove access.
        /// </summary>
        [JsonPropertyName("RM")]
        Remove
    }

    /// <summary>
    /// Collaborator role type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleValue
    {
        /// <summary>
        /// Note owner.
        /// </summary>
        [JsonPropertyName("O")]
        Owner,

        /// <summary>
        /// Note collaborator.
        /// </summary>
        [JsonPropertyName("W")]
        User
    }

    public class Element
    {
        /// <summary>
        /// Interface for elements that can be serialized and deserialized.
        /// </summary>
        protected bool _dirty;

        /// <summary>
        /// Find discrepancies between the raw representation and the serialized version.
        /// </summary>
        /// <param name="raw">Raw representation.</param>
        private void _find_discrepancies(JsonElement raw)
        {
            Dictionary<string, object> s_raw = Save(false);

            if (raw.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in raw.EnumerateObject())
                {
                    string key = prop.Name;
                    if (key == "parentServerId" || key == "lastSavedSessionId")
                        continue;

                    if (!s_raw.ContainsKey(key))
                    {
                        Console.WriteLine($"Missing key for {GetType()} key {key}");
                        continue;
                    }

                    var val = prop.Value;
                    if (val.ValueKind == JsonValueKind.Array || val.ValueKind == JsonValueKind.Object)
                        continue;

                    object val_a = DeserializeJsonElementToObject(raw[key]);
                    object val_b = s_raw[key];

                    if (val_a is string val_aStr && val_b is string val_bStr)
                    {
                        try
                        {
                            DateTime tval_a = StrToDateTime(val_aStr);
                            DateTime tval_b = StrToDateTime(val_bStr);
                            val_a = tval_a;
                            val_b = tval_b;
                        }
                        catch (Exception)
                        {
                            // Ignore the error, continue comparing.
                        }
                    }

                    if (!val_a.Equals(val_b))
                    {
                        Console.WriteLine($"Different value for {GetType()} key {key}: {val} != {s_raw[key]}");
                    }
                }
            }
            else if (raw.ValueKind == JsonValueKind.Array)
            {
                if (raw.GetArrayLength() != s_raw.Count)
                {
                    Console.WriteLine($"Different length for {GetType()}: {raw.GetArrayLength()} != {s_raw.Count}");
                }
            }
        }

        /// <summary>
        /// Unserialize from raw representation. (Wrapper)
        /// </summary>
        /// <param name="raw">Raw representation.</param>
        public void Load(JsonElement raw)
        {
            try
            {
                _load(raw);
            }
            catch (Exception e)
            {
                throw new ParseException($"Parse error in {GetType()}", raw, e);
            }
        }

        /// <summary>
        /// Unserialize from raw representation. (Implementation logic)
        /// </summary>
        /// <param name="raw">Raw representation.</param>
        private void _load(JsonElement raw)
        {
            _dirty = raw.GetProperty("_dirty").GetBoolean();
        }

        /// <summary>
        /// Serialize into raw representation. Clears the dirty bit by default.
        /// </summary>
        /// <param name="clean">Whether to clear the dirty bit.</param>
        /// <returns>Raw representation.</returns>
        public Dictionary<string, object> Save(bool clean = true)
        {
            var ret = new Dictionary<string, object>();
            if (clean)
                _dirty = false;
            else
                ret["_dirty"] = _dirty;
            return ret;
        }

        /// <summary>
        /// Get dirty state.
        /// </summary>
        public bool Dirty
        {
            get { return _dirty; }
        }

        /// <summary>
        /// Deserialize a JSON element to its corresponding C# object representation.
        /// </summary>
        /// <param name="element">JSON element to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        private static object DeserializeJsonElementToObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = DeserializeJsonElementToObject(prop.Value);
                    }
                    return dict;

                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(DeserializeJsonElementToObject(item));
                    }
                    return list;

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert a string to a DateTime object.
        /// </summary>
        /// <param name="dateTimeStr">String representation of DateTime.</param>
        /// <returns>The DateTime object.</returns>
        private static DateTime StrToDateTime(string dateTimeStr)
        {
            return DateTime.ParseExact(dateTimeStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
    }

    public class Annotation : Element
    {
        /// <summary>
        /// Note annotations base class.
        /// </summary>
        public Annotation()
        {
            base._dirty = false;
            this.Id = GenerateAnnotationId();
        }

        public string Id { get; private set; }

        public new void Load(JsonElement raw)
        {
            base.Load(raw);
            _load(raw);
        }

        public new Dictionary<string, object> Save(bool clean = true)
        {
            var ret = new Dictionary<string, object>();
            if (Id != null)
            {
                ret = base.Save(clean);
            }
            if (Id != null)
            {
                ret["id"] = Id;
            }
            return ret;
        }

        private new void _load(JsonElement raw)
        {
            Id = raw.GetProperty("id").GetString();
        }

        private static string GenerateAnnotationId()
        {
            return string.Format(
                "{0}-{1}-{2}-{3}-{4}",
                Guid.NewGuid().ToString("N").Substring(0, 8),
                Guid.NewGuid().ToString("N").Substring(0, 4),
                Guid.NewGuid().ToString("N").Substring(0, 4),
                Guid.NewGuid().ToString("N").Substring(0, 4),
                Guid.NewGuid().ToString("N").Substring(0, 12)
            );
        }
    }

    public class WebLink : Annotation
    {
        /// <summary>
        /// Represents a link annotation on a TopLevelNode.
        /// </summary>
        public WebLink()
        {
            base._dirty = false;
            _title = "";
            _url = "";
            _image_url = null;
            _provenance_url = "";
            _description = "";
        }

        private string _title;
        private string _url;
        private string _image_url;
        private string _provenance_url;
        private string _description;

        public new void Load(JsonElement raw)
        {
            base.Load(raw);
            _load(raw);
        }

        public new Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["webLink"] = new Dictionary<string, object>
            {
                { "title", _title },
                { "url", _url },
                { "imageUrl", _image_url },
                { "provenanceUrl", _provenance_url },
                { "description", _description }
            };
            return ret;
        }

        private new void _load(JsonElement raw)
        {
            JsonElement webLink = raw.GetProperty("webLink");
            _title = webLink.GetProperty("title").GetString();
            _url = webLink.GetProperty("url").GetString();
            _image_url = webLink.TryGetProperty("imageUrl", out JsonElement imageUrl) ? imageUrl.GetString() : null;
            _provenance_url = webLink.GetProperty("provenanceUrl").GetString();
            _description = webLink.GetProperty("description").GetString();
        }

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                _dirty = true;
            }
        }

        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                _dirty = true;
            }
        }

        public string ImageUrl
        {
            get { return _image_url; }
            set
            {
                _image_url = value;
                _dirty = true;
            }
        }

        public string ProvenanceUrl
        {
            get { return _provenance_url; }
            set
            {
                _provenance_url = value;
                _dirty = true;
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                _dirty = true;
            }
        }
    }

    public class Category : Annotation
    {
        /// <summary>
        /// Represents a category annotation on a TopLevelNode.
        /// </summary>
        public Category()
        {
            base._dirty = false;
            _category = CategoryValue.Books;
        }

        private CategoryValue _category;

        public new void Load(JsonElement raw)
        {
            base.Load(raw);
            _load(raw);
        }

        public new Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["topicCategory"] = new Dictionary<string, object>
            {
                { "category", _category.ToString() }
            };
            return ret;
        }

        private new void _load(JsonElement raw)
        {
            string category = raw.GetProperty("topicCategory").GetProperty("category").GetString();
            _category = (CategoryValue)Enum.Parse(typeof(CategoryValue), category);
        }

        public CategoryValue category
        {
            get { return _category; }
            set
            {
                _category = value;
                _dirty = true;
            }
        }
    }

    public class TaskAssist : Annotation
    {
        /// <summary>
        /// Represents an unknown task assist annotation.
        /// </summary>
        public TaskAssist()
        {
            base._dirty = false;
            _suggest = null;
        }

        private string _suggest;

        public new void Load(JsonElement raw)
        {
            base.Load(raw);
            _load(raw);
        }

        public new Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["taskAssist"] = new Dictionary<string, object>
            {
                { "suggestType", _suggest }
            };
            return ret;
        }

        private new void _load(JsonElement raw)
        {
            _suggest = raw.GetProperty("taskAssist").GetProperty("suggestType").GetString();
        }

        public string Suggest
        {
            get { return _suggest; }
            set
            {
                _suggest = value;
                _dirty = true;
            }
        }
    }

    public class Context : Annotation
    {
        /// <summary>
        /// Represents a context annotation, which may contain other annotations.
        /// </summary>
        public Context()
        {
            base._dirty = false;
            _entries = new Dictionary<string, Annotation>();
        }

        private Dictionary<string, Annotation> _entries;

        public new void Load(JsonElement raw)
        {
            base.Load(raw);
            _load(raw);
        }

        public new Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            var context = new Dictionary<string, object>();
            foreach (var entry in _entries)
            {
                var entrySave = entry.Value.Save(clean);
                context.Add(entry.Key, entrySave.ContainsKey(entry.Key) ? entrySave[entry.Key] : entrySave);
            }
            ret["context"] = context;
            return ret;
        }

        private new void _load(JsonElement raw)
        {
            _entries.Clear();
            var context = raw.GetProperty("context");
            foreach (var prop in context.EnumerateObject())
            {
                _entries[prop.Name] = NodeAnnotations.FromJson(new JsonDocument(prop.Value).RootElement);
            }
        }

        public List<Annotation> All()
        {
            return new List<Annotation>(_entries.Values);
        }

        public new bool Dirty
        {
            get { return base.Dirty || _entries.Values.Exists(annotation => annotation.Dirty); }
        }
    }

    public class NodeAnnotations : Element
    {
        /// <summary>
        /// Represents the annotation container on a TopLevelNode.
        /// </summary>
        public NodeAnnotations()
        {
            base._dirty = false;
            _annotations = new Dictionary<string, Annotation>();
        }

        private Dictionary<string, Annotation> _annotations;

        public int Count
        {
            get { return _annotations.Count; }
        }

        public static Annotation FromJson(JsonElement raw)
        {
            if (raw.TryGetProperty("webLink", out JsonElement webLink))
            {
                return new WebLink { Suggest = webLink.GetString() };
            }
            else if (raw.TryGetProperty("topicCategory", out JsonElement topicCategory))
            {
                return new Category { Category = (CategoryValue)Enum.Parse(typeof(CategoryValue), topicCategory.GetString()) };
            }
            else if (raw.TryGetProperty("taskAssist", out JsonElement taskAssist))
            {
                return new TaskAssist { Suggest = taskAssist.GetString() };
            }
            else if (raw.TryGetProperty("context", out JsonElement context))
            {
                return new Context();
            }

            Console.WriteLine("Unknown annotation type: " + raw);
            return null;
        }

        public List<Annotation> All()
        {
            return new List<Annotation>(_annotations.Values);
        }

        public new void Load(JsonElement raw)
        {
            base.Load(raw);
            _load(raw);
        }

        public new Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["kind"] = "notes#annotationsGroup";
            if (_annotations.Count > 0)
            {
                var annotations = new List<Dictionary<string, object>>();
                foreach (var annotation in _annotations.Values)
                {
                    annotations.Add(annotation.Save(clean));
                }
                ret["annotations"] = annotations;
            }
            return ret;
        }

        private new void _load(JsonElement raw)
        {
            _annotations.Clear();
            if (raw.TryGetProperty("annotations", out JsonElement annotations))
            {
                foreach (var annotation in annotations.EnumerateArray())
                {
                    var loadedAnnotation = FromJson(annotation);
                    _annotations[loadedAnnotation.Id] = loadedAnnotation;
                }
            }
        }

        private Category _getCategoryNode()
        {
            foreach (var annotation in _annotations.Values)
            {
                if (annotation is Category categoryAnnotation)
                {
                    return categoryAnnotation;
                }
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
                    {
                        _annotations.Remove(node.Id);
                    }
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
                    {
                        links.Add(webLink);
                    }
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

        public new bool Dirty
        {
            get
            {
                return base.Dirty || _annotations.Values.Exists(annotation => annotation.Dirty);
            }
        }
    }

    public class NodeTimestamps : Element
    {
        private static string TZ_FMT = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public NodeTimestamps(double createTime = 0)
        {
            base._dirty = false;
            if (createTime == 0)
            {
                createTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            _created = IntToDt(createTime);
            _deleted = null;
            _trashed = null;
            _updated = IntToDt(createTime);
            _edited = null;
        }

        private DateTimeOffset _created;
        private DateTimeOffset? _deleted;
        private DateTimeOffset? _trashed;
        private DateTimeOffset _updated;
        private DateTimeOffset? _edited;

        public new void Load(JsonElement raw)
        {
            base.Load(raw);
            _load(raw);
        }

        public new Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["kind"] = "notes#timestamps";
            ret["created"] = DtToStr(_created);
            if (_deleted.HasValue)
            {
                ret["deleted"] = DtToStr(_deleted.Value);
            }
            if (_trashed.HasValue)
            {
                ret["trashed"] = DtToStr(_trashed.Value);
            }
            ret["updated"] = DtToStr(_updated);
            if (_edited.HasValue)
            {
                ret["userEdited"] = DtToStr(_edited.Value);
            }
            return ret;
        }

        private new void _load(JsonElement raw)
        {
            _created = StrToDt(raw.GetProperty("created").GetString());
            _deleted = raw.TryGetProperty("deleted", out JsonElement deleted) ? (DateTimeOffset?)StrToDt(deleted.GetString()) : null;
            _trashed = raw.TryGetProperty("trashed", out JsonElement trashed) ? (DateTimeOffset?)StrToDt(trashed.GetString()) : null;
            _updated = StrToDt(raw.GetProperty("updated").GetString());
            _edited = raw.TryGetProperty("userEdited", out JsonElement edited) ? (DateTimeOffset?)StrToDt(edited.GetString()) : null;
        }

        private DateTimeOffset StrToDt(string tzs)
        {
            return DateTimeOffset.ParseExact(tzs, TZ_FMT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        private DateTimeOffset IntToDt(double tz)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long)(tz * 1000));
        }

        private string DtToStr(DateTimeOffset dt)
        {
            return dt.ToUniversalTime().ToString(TZ_FMT, CultureInfo.InvariantCulture);
        }

        public DateTimeOffset Created
        {
            get { return _created; }
            set
            {
                _created = value;
                _dirty = true;
            }
        }

        public DateTimeOffset? Deleted
        {
            get { return _deleted; }
            set
            {
                _deleted = value;
                _dirty = true;
            }
        }

        public DateTimeOffset? Trashed
        {
            get { return _trashed; }
            set
            {
                _trashed = value;
                _dirty = true;
            }
        }

        public DateTimeOffset Updated
        {
            get { return _updated; }
            set
            {
                _updated = value;
                _dirty = true;
            }
        }

        public DateTimeOffset? Edited
        {
            get { return _edited; }
            set
            {
                _edited = value;
                _dirty = true;
            }
        }
    }

    public class NodeSettings : Element
    {
        public NodeSettings()
        {
            base._dirty = false;
            _new_listitem_placement = NewListItemPlacementValue.Bottom;
            _graveyard_state = GraveyardStateValue.Collapsed;
            _checked_listitems_policy = CheckedListItemsPolicyValue.Graveyard;
        }

        private NewListItemPlacementValue _new_listitem_placement;
        private GraveyardStateValue _graveyard_state;
        private CheckedListItemsPolicyValue _checked_listitems_policy;

        public new void Load(JsonElement raw)
        {
            base.Load(raw);
            _load(raw);
        }

        public new Dictionary<string, object> Save(bool clean = true)
        {
            var ret = base.Save(clean);
            ret["newListItemPlacement"] = _new_listitem_placement;
            ret["graveyardState"] = _graveyard_state;
            ret["checkedListItemsPolicy"] = _checked_listitems_policy;
            return ret;
        }

        private new void _load(JsonElement raw)
        {
            _new_listitem_placement = EnumEx.Parse<NewListItemPlacementValue>(raw.GetProperty("newListItemPlacement").GetString());
            _graveyard_state = EnumEx.Parse<GraveyardStateValue>(raw.GetProperty("graveyardState").GetString());
            _checked_listitems_policy = EnumEx.Parse<CheckedListItemsPolicyValue>(raw.GetProperty("checkedListItemsPolicy").GetString());
        }

        public NewListItemPlacementValue NewListItemPlacement
        {
            get { return _new_listitem_placement; }
            set
            {
                _new_listitem_placement = value;
                _dirty = true;
            }
        }

        public GraveyardStateValue GraveyardState
        {
            get { return _graveyard_state; }
            set
            {
                _graveyard_state = value;
                _dirty = true;
            }
        }

        public CheckedListItemsPolicyValue CheckedListItemsPolicy
        {
            get { return _checked_listitems_policy; }
            set
            {
                _checked_listitems_policy = value;
                _dirty = true;
            }
        }
    }

    public class NodeCollaborators : Element
    {
        public NodeCollaborators()
        {
            base._dirty = false;
            _collaborators = new Dictionary<string, ShareRequestValue>();
        }

        private Dictionary<string, ShareRequestValue> _collaborators;

        public void Load(List<JsonElement> collaboratorsRaw, List<JsonElement> requestsRaw)
        {
            if (requestsRaw.Count > 0 && requestsRaw[requestsRaw.Count - 1].ValueKind == JsonValueKind.True)
            {
                _dirty = true;
                requestsRaw.RemoveAt(requestsRaw.Count - 1);
            }
            else
            {
                _dirty = false;
            }

            _collaborators = new Dictionary<string, ShareRequestValue>();
            foreach (var collaborator in collaboratorsRaw)
            {
                _collaborators[collaborator.GetProperty("email").GetString()] = (ShareRequestValue)Enum.Parse(typeof(ShareRequestValue), collaborator.GetProperty("role").GetString());
            }

            foreach (var collaborator in requestsRaw)
            {
                _collaborators[collaborator.GetProperty("email").GetString()] = (ShareRequestValue)Enum.Parse(typeof(ShareRequestValue), collaborator.GetProperty("type").GetString());
            }
        }

        public Tuple<List<JsonElement>, List<JsonElement>> Save(bool clean = true)
        {
            var collaborators = new List<JsonElement>();
            var requests = new List<JsonElement>();
            foreach (var collaborator in _collaborators)
            {
                var email = collaborator.Key;
                var action = collaborator.Value;
                if (action != ShareRequestValue.Add)
                {
                    collaborators.Add(JsonDocument.Parse($"{{\"email\": \"{email}\", \"role\": \"{action.ToString().ToUpper()}\", \"auxiliary_type\": \"None\"}}").RootElement);
                }
                else
                {
                    requests.Add(JsonDocument.Parse($"{{\"email\": \"{email}\", \"type\": \"{action.ToString().ToUpper()}\", \"auxiliary_type\": \"None\"}}").RootElement);
                }
            }

            if (!clean)
            {
                requests.Add(JsonDocument.Parse($"{{\"email\": \"\", \"type\": \"\", \"auxiliary_type\": \"None\"}}").RootElement);
            }

            return new Tuple<List<JsonElement>, List<JsonElement>>(collaborators, requests);
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
                {
                    _collaborators.Remove(email);
                }
                else
                {
                    _collaborators[email] = ShareRequestValue.Remove;
                }
            }
            _dirty = true;
        }

        public List<string> All()
        {
            var collaborators = new List<string>();
            foreach (var collaborator in _collaborators)
            {
                if (collaborator.Value == ShareRequestValue.Add || collaborator.Value == ShareRequestValue.Owner || collaborator.Value == ShareRequestValue.User)
                {
                    collaborators.Add(collaborator.Key);
                }
            }
            return collaborators;
        }
    }

    public class NodeLabels : Element
    {
        public NodeLabels()
        {
            base._dirty = false;
            _labels = new Dictionary<string, Label>();
        }

        private Dictionary<string, Label> _labels;

        public void Load(List<JsonElement> raw)
        {
            if (raw.Count > 0 && raw[raw.Count - 1].ValueKind == JsonValueKind.True)
            {
                _dirty = true;
                raw.RemoveAt(raw.Count - 1);
            }
            else
            {
                _dirty = false;
            }

            _labels = new Dictionary<string, Label>();
            foreach (var rawLabel in raw)
            {
                _labels[rawLabel.GetProperty("labelId").GetString()] = null;
            }
        }

        public List<JsonElement> Save(bool clean = true)
        {
            var ret = new List<JsonElement>();
            foreach (var label in _labels)
            {
                var labelId = label.Key;
                var labelObject = label.Value;
                if (labelObject == null)
                {
                    ret.Add(JsonDocument.Parse($"{{\"labelId\": \"{labelId}\", \"deleted\": \"{NodeTimestamps.DtToStr(DateTime.UtcNow)}\"}}").RootElement);
                }
                else
                {
                    ret.Add(JsonDocument.Parse($"{{\"labelId\": \"{labelId}\", \"deleted\": \"{NodeTimestamps.IntToStr(0)}\"}}").RootElement);
                }
            }

            if (!clean)
            {
                ret.Add(JsonDocument.Parse("{\"labelId\": \"\", \"deleted\": \"\"}").RootElement);
            }

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
            {
                _labels[label.Id] = null;
            }
            _dirty = true;
        }

        public Label Get(string labelId)
        {
            _labels.TryGetValue(labelId, out var label);
            return label;
        }

        public List<Label> All()
        {
            var labels = new List<Label>();
            foreach (var label in _labels)
            {
                if (label.Value != null)
                {
                    labels.Add(label.Value);
                }
            }
            return labels;
        }
    }

    public class TimestampsMixin
    {
        public Element Element { get; set; }

        public TimestampsMixin(Element element)
        {
            Element = element;
        }

        public void Touch(bool edited = false)
        {
            Element._dirty = true;
            DateTime dt = DateTime.UtcNow;
            Element.Timestamps.Updated = dt;
            if (edited)
            {
                Element.Timestamps.Edited = dt;
            }
        }

        public bool Trashed
        {
            get
            {
                return Element.Timestamps.Trashed != null && Element.Timestamps.Trashed > NodeTimestamps.IntToDt(0);
            }
        }

        public void Trash()
        {
            Element.Timestamps.Trashed = DateTime.UtcNow;
        }

        public void Untrash()
        {
            Element.Timestamps.Trashed = null;
        }

        public bool Deleted
        {
            get
            {
                return Element.Timestamps.Deleted != null && Element.Timestamps.Deleted > NodeTimestamps.IntToDt(0);
            }
        }

        public void Delete()
        {
            Element.Timestamps.Deleted = DateTime.UtcNow;
        }

        public void Undelete()
        {
            Element.Timestamps.Deleted = null;
        }
    }
}
