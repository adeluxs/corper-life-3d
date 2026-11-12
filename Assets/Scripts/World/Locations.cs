using System.Collections.Generic;

namespace CorperLife
{
    /// <summary>Location ids. Blender exports an empty named "LOC_&lt;id&gt;" for each one.</summary>
    public static class Locations
    {
        public const string Hostel = "hostel";
        public const string Worship = "worship";
        public const string Parade = "parade";
        public const string Kitchen = "kitchen";
        public const string Market = "market";
        public const string Hall = "hall";
        public const string Saed = "saed";
        public const string Field = "field";
        public const string Clinic = "clinic";
        public const string Gate = "gate";
        public const string Office = "office";

        public static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { Hostel, "Hostel" },
            { Worship, "Mosque and chapel" },
            { Parade, "Parade ground" },
            { Kitchen, "Camp kitchen" },
            { Market, "Mammy Market" },
            { Hall, "Multipurpose hall" },
            { Saed, "SAED tents" },
            { Field, "Sports field" },
            { Clinic, "Camp clinic" },
            { Gate, "Camp gate" },
            { Office, "Camp office" },
        };

        public static string NameOf(string id) =>
            id != null && Names.TryGetValue(id, out var n) ? n : "Camp";
    }
}
