using System;
using System.Collections.Generic;

namespace DokodemoLLM
{
  public class AppConfig
  {
    public string Endpoint { get; set; } = "http://127.0.0.1:1234/v1/";
    public string ApiKey { get; set; } = "dummy";
    public string Model { get; set; } = "google/gemma-3-4b";
    public float Temperature { get; set; } = 0.7f;
    public int MaxOutputTokenCount { get; set; } = 4096;
    public List<string> Prompts { get; set; } = new List<string>
    {
      "要約してください：",
      "次の文章を翻訳してください：",
      "次のトピックについて200文字程度で説明してください："
    };
  }
}
