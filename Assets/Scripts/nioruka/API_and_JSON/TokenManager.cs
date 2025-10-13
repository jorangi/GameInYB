using UnityEngine;

public static class TokenManager
{
    private const string TokenKey = "auth_token";

    public static void SaveToken(string token)
    {
        PlayerPrefs.SetString(TokenKey, token);
        PlayerPrefs.Save();
    }

    public static string LoadToken()
    {
        return PlayerPrefs.GetString(TokenKey, "");
    }

    public static void ClearToken()
    {
        PlayerPrefs.DeleteKey(TokenKey);
    }
}
