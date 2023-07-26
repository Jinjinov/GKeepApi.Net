using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GKeepApi.Net
{
    public interface IKeep
    {
        void Add(Node node);
        IEnumerable<TopLevelNode> All();
        Label CreateLabel(string name);
        List CreateList(string title = null, List<(string, bool)> items = null);
        Note CreateNote(string title = null, string text = null);
        void DeleteLabel(string labelId);
        Dictionary<string, object> Dump();
        IEnumerable<Node> Find(object query = null, Func<Node, bool> func = null, List<object> labels = null, List<ColorValue> colors = null, bool? pinned = null, bool? archived = null, bool trashed = false);
        Label FindLabel(object query, bool create = false);
        Node Get(string nodeId);
        Label GetLabel(string labelId);
        string GetMasterToken();
        Task<string> GetMediaLink(Blob blob);
        IEnumerable<Label> Labels();
        Task<bool> Login(string email = null, string password = null, Dictionary<string, object> state = null, bool sync = true, string deviceId = null);
        Task<bool> Resume(string email = null, string masterToken = null, Dictionary<string, object> state = null, bool sync = true, string deviceId = null);
        Task Sync(bool resync = false);
    }
}