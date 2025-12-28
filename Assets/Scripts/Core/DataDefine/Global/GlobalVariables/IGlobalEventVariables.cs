using System;

public interface IGlobalEventVariables
{
    bool HasKey(GlobalEventKey key);
    void Remove(GlobalEventKey key);

    void SetInt(GlobalEventKey key, int value);
    int GetInt(GlobalEventKey key, int defaultValue = 0);

    void SetFloat(GlobalEventKey key, float value);
    float GetFloat(GlobalEventKey key, float defaultValue = 0f);

    void SetString(GlobalEventKey key, string value);
    string GetString(GlobalEventKey key, string defaultValue = "");

    void SetBool(GlobalEventKey key, bool value);
    bool GetBool(GlobalEventKey key, bool defaultValue = false);
}
