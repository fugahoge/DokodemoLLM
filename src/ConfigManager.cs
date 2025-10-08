using System;
using System.IO;
using System.Text.Json;

namespace DokodemoLLM
{
  public static class ConfigManager
  {
    private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private static AppConfig _config;

    public static AppConfig Config
    {
      get
      {
        if (_config == null)
        {
          LoadConfig();
        }
        return _config;
      }
    }

    public static void LoadConfig()
    {
      try
      {
        if (File.Exists(ConfigFilePath))
        {
          string jsonString = File.ReadAllText(ConfigFilePath);
          _config = JsonSerializer.Deserialize<AppConfig>(jsonString);
        }
        else
        {
          // 設定ファイルが存在しない場合はデフォルト設定を作成
          _config = new AppConfig();
          SaveConfig();
        }
      }
      catch (Exception ex)
      {
        // 設定ファイルの読み込みに失敗した場合はデフォルト設定を使用
        _config = new AppConfig();
        Console.WriteLine($"設定ファイルの読み込みに失敗しました: {ex.Message}");
      }
    }

    public static void SaveConfig()
    {
      try
      {
        var options = new JsonSerializerOptions
        {
          WriteIndented = true,
          Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        string jsonString = JsonSerializer.Serialize(_config, options);
        File.WriteAllText(ConfigFilePath, jsonString);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"設定ファイルの保存に失敗しました: {ex.Message}");
      }
    }
  }
}
