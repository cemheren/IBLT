
namespace LiveGraph.Prototype
{

    using Newtonsoft.Json.Linq;

    public class Index
    {
        public string KeyType { get; set; } = string.Empty;

        public string ValueType { get; set; } = string.Empty;
    }

    public class Input
    {
        public string Type { get; set; } = string.Empty;

        public Func<JObject, string> Match { get; set; } = (x) => "";
    }

    public class Return 
    {
        public string Type { get; set; } = string.Empty;

        public Func<JObject, bool> Filter { get; set; } = (x) => true;
    }

    public class Query
    {
        public Input[] TypesToReactTo { get; set; } = [];

        public Dictionary<string, Index> IndexesToUse { get; set; } = new();

        public Return[] Returns { get; set; } = []; 
    }
}