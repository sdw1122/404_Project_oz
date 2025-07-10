using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerSaveData
{
    public string userId;
    public string userJob;
    public Vector3 position;
}

public static class SaveSystem
{
    /*public static void SavePlayerPosition(string P_UserId, Vector3 position)
    {
        PlayerSaveData data = new PlayerSaveData
        {
          
        };

        string path = GetFilePath(P_UserId);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log($" 저장됨: {path}");
    }*/
    public static void SavePlayerData(PlayerSaveData data)
    {
        string json = JsonUtility.ToJson(data, true); // pretty print
        string path = GetFilePath(data.userId);
        File.WriteAllText(path, json);
        Debug.Log($"[SaveSystem] 저장 완료 → {path}");
    }
    /* public static Vector3? LoadPlayerPosition(string P_UserId)
     {
         string path = GetFilePath(P_UserId);
         if (!File.Exists(path))
         {
             Debug.LogWarning($" 저장된 위치 없음: {path}");
             return null;
         }

         string json = File.ReadAllText(path);
         PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
         return new Vector3(data.x, data.y, data.z);
     }*/
    public static PlayerSaveData LoadPlayerData(string userId)
    {
        string path = GetFilePath(userId);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
            Debug.Log($"[SaveSystem] 불러오기 완료 → {path}");
            return data;
        }
        else
        {
            Debug.LogWarning($"[SaveSystem] 저장 파일 없음 → {path}");
            return null;
        }
    }
    private static string GetFilePath(string P_UserId)
    {
        return Path.Combine(Application.persistentDataPath, $"player_{P_UserId}.json");
    }
}
