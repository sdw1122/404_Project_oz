using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FlagScenePair
{
    public string label;
    public string scene;
}

[System.Serializable]
public class FlagDatabaseWrapper
{
    public List<FlagScenePair> flags;
}
public static class FlagDataManager
{
    private static Dictionary<string, string> flagToSceneMap;
    public static void Load()
    {
        if (flagToSceneMap != null) return;
        TextAsset jsonFile = Resources.Load<TextAsset>("FlagData");
        if(jsonFile==null)
        {
            Debug.LogError("[FlagDataManager] Json파일이 없습니다.");
            return;
        }
        FlagDatabaseWrapper wrapper = JsonUtility.FromJson<FlagDatabaseWrapper>(jsonFile.text);
        flagToSceneMap=new Dictionary<string, string>();
        foreach(var pair in wrapper.flags)
        {
            flagToSceneMap[pair.label]=pair.scene;
        }
        Debug.Log("[FlagDataManager] 깃발 Data 로드 완료.");
        
    }
    public static string GetSceneByFlag(string label)
    {
        if (flagToSceneMap == null)
            Load();
        if(flagToSceneMap.TryGetValue(label,out string scene))
        {
            return scene;
        }
        else
        {
            Debug.LogWarning($"[FlagDataManager] 해당 라벨을 가진 깃발이 없습니다.");
            return null;
        }
    }
}
