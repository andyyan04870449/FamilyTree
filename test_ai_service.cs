using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class TestAIService
{
    public static async Task Main()
    {
        // 模擬修復後的 AI 服務邏輯
        string testText = "女：黃心田\\n子：王先生";
        
        Console.WriteLine($"原始文字: '{testText}'");
        
        // 處理多行文字，先將 \n 字符替換為真正的換行符
        var normalizedText = testText.Replace("\\n", "\n");
        Console.WriteLine($"標準化後文字: '{normalizedText}'");
        
        // 分割文字
        var lines = normalizedText.Split(new[] { '\n', '；', '。' }, StringSplitOptions.RemoveEmptyEntries);
        Console.WriteLine($"分割後得到 {lines.Length} 行");
        
        var result = new List<NameRelationPair>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            Console.WriteLine($"處理行: '{trimmedLine}'");
            
            if (string.IsNullOrEmpty(trimmedLine)) 
            {
                Console.WriteLine("跳過空行");
                continue;
            }
            
            // 處理家庭關係格式：如 "母：邱還真"
            if (trimmedLine.Contains('：'))
            {
                Console.WriteLine("檢測到家庭關係格式");
                var parts = trimmedLine.Split('：');
                Console.WriteLine($"分割結果: {parts.Length} 部分");
                
                if (parts.Length == 2)
                {
                    var relation = parts[0].Trim();
                    var name = parts[1].Trim();
                    Console.WriteLine($"提取關係: '{relation}', 名字: '{name}'");
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        result.Add(new NameRelationPair { Name = name, Relation = relation });
                        Console.WriteLine($"✅ 添加家庭關係: {name} ({relation})");
                    }
                }
            }
        }
        
        Console.WriteLine($"解析結果: {result.Count} 個關係配對");
        foreach (var pair in result)
        {
            Console.WriteLine($"  - {pair.Name}: {pair.Relation}");
        }
    }
}

public class NameRelationPair
{
    public string Name { get; set; } = string.Empty;
    public string Relation { get; set; } = string.Empty;
} 