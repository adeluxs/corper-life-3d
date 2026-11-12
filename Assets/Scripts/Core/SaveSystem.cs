using System;
using System.IO;
using UnityEngine;

namespace CorperLife
{
    /// <summary>Saves the game as JSON in the app's private storage. Fully offline.</summary>
    public static class SaveSystem
    {
        const string FileName = "corperlife_save_{0}.json";

        static string PathFor(int slot) =>
            Path.Combine(Application.persistentDataPath, string.Format(FileName, slot));

        public static bool HasSave(int slot = 0) => File.Exists(PathFor(slot));

        public static void Save(GameState state, int slot = 0)
        {
            try
            {
                string json = JsonUtility.ToJson(state);
                string tmp = PathFor(slot) + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(PathFor(slot))) File.Delete(PathFor(slot));
                File.Move(tmp, PathFor(slot));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save failed: " + e.Message);
            }
        }

        public static GameState Load(int slot = 0)
        {
            try
            {
                if (!HasSave(slot)) return null;
                var state = JsonUtility.FromJson<GameState>(File.ReadAllText(PathFor(slot)));
                state?.needs.ClampAll();
                return state;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Load failed: " + e.Message);
                return null;
            }
        }

        public static void Delete(int slot = 0)
        {
            if (HasSave(slot)) File.Delete(PathFor(slot));
        }
    }
}
