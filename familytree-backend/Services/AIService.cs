using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAI_API;
using OpenAI_API.Chat;

namespace familytree_backend.Services
{
    public class AIService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;
        private readonly OpenAIAPI _openAIClient;

        public AIService(IConfiguration configuration, ILogger<AIService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // 初始化 OpenAI 客戶端
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("OpenAI API Key 未配置，將使用簡單解析模式");
                _openAIClient = null!;
            }
            else
            {
                _openAIClient = new OpenAIAPI(apiKey);
                _logger.LogInformation("OpenAI 客戶端已初始化");
            }
        }

        /// <summary>
        /// 讀取Prompt模板
        /// </summary>
        /// <param name="templateName">模板名稱</param>
        /// <returns>模板內容</returns>
        public async Task<JsonNode?> LoadPromptTemplateAsync(string templateName)
        {
            // 使用 IConfiguration 來獲取應用程式根目錄
            var contentRootPath = _configuration["ContentRoot"] ?? AppDomain.CurrentDomain.BaseDirectory;
            var templatePath = Path.Combine(contentRootPath, "AI", "Templates", $"{templateName}_prompt.json");
            
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {templatePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(templatePath);
            return JsonNode.Parse(jsonContent);
        }

        /// <summary>
        /// 讀取Function Calling schema
        /// </summary>
        /// <param name="functionName">函數名稱</param>
        /// <returns>schema內容</returns>
        public async Task<JsonNode?> LoadFunctionSchemaAsync(string functionName)
        {
            // 使用 IConfiguration 來獲取應用程式根目錄
            var contentRootPath = _configuration["ContentRoot"] ?? AppDomain.CurrentDomain.BaseDirectory;
            var schemaPath = Path.Combine(contentRootPath, "AI", "Templates", $"{functionName}_function.json");
            
            if (!File.Exists(schemaPath))
            {
                throw new FileNotFoundException($"Function schema file not found: {schemaPath}");
            }

            var jsonContent = await File.ReadAllTextAsync(schemaPath);
            return JsonNode.Parse(jsonContent);
        }

        /// <summary>
        /// 使用 OpenAI 4o Mini 模型抽取人名關係
        /// </summary>
        /// <param name="textContent">要分析的文字內容</param>
        /// <returns>人名關係配對列表</returns>
        public async Task<List<NameRelationPair>> ExtractNameRelationsWithAIAsync(string textContent)
        {
            try
            {
                _logger.LogInformation("=== 開始使用 OpenAI 4o Mini 模型解析人名關係 ===");
                _logger.LogInformation("輸入文字內容: {TextContent}", textContent);
                
                if (_openAIClient == null)
                {
                    _logger.LogWarning("OpenAI 客戶端未初始化，回退到簡單解析模式");
                    return await ExtractNameRelationsAsync(textContent);
                }
                
                if (string.IsNullOrEmpty(textContent))
                {
                    _logger.LogInformation("文字內容為空，返回空結果");
                    return new List<NameRelationPair>();
                }
                
                // 載入提示詞模板
                var promptTemplate = await LoadPromptTemplateAsync("extract_name_relation");
                if (promptTemplate == null)
                {
                    _logger.LogError("無法載入提示詞模板");
                    throw new InvalidOperationException("無法載入提示詞模板");
                }
                
                // 載入函數定義
                var functionSchema = await LoadFunctionSchemaAsync("extract_name_relation");
                if (functionSchema == null)
                {
                    _logger.LogError("無法載入函數定義");
                    throw new InvalidOperationException("無法載入函數定義");
                }
                
                // 準備消息
                var systemMessage = promptTemplate["system_message"]?["content"]?.ToString() ?? "";
                var userMessage = promptTemplate["user_message_template"]?["content"]?.ToString()?.Replace("{text_content}", textContent) ?? textContent;
                
                var chatRequest = new ChatRequest
                {
                    Model = "gpt-4o-mini",
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage(ChatMessageRole.System, systemMessage),
                        new ChatMessage(ChatMessageRole.User, userMessage)
                    },
                    MaxTokens = 1000,
                    Temperature = 0.1f
                };
                
                _logger.LogInformation("發送請求到 OpenAI 4o Mini 模型...");
                
                // 調用 OpenAI API
                var response = await _openAIClient.Chat.CreateChatCompletionAsync(chatRequest);
                
                if (response.Choices.Count > 0)
                {
                    var choice = response.Choices[0];
                    var content = choice.Message.Content;
                    
                    _logger.LogInformation("AI 回應內容: {Content}", content);
                    
                    // 嘗試解析 JSON 格式的回應
                    try
                    {
                        var result = JsonSerializer.Deserialize<ExtractNameRelationResult>(content);
                        
                        _logger.LogInformation("AI 解析結果: {ResultCount} 個關係配對", result?.Pairs?.Count ?? 0);
                        foreach (var pair in result?.Pairs ?? new List<NameRelationPair>())
                        {
                            _logger.LogInformation("  - {Name}: {Relation}", pair.Name, pair.Relation);
                        }
                        
                        _logger.LogInformation("=== AI 解析完成 ===");
                        return result?.Pairs ?? new List<NameRelationPair>();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("AI 回應不是有效的 JSON 格式: {Error}", ex.Message);
                        _logger.LogInformation("回退到簡單解析模式");
                        return await ExtractNameRelationsAsync(textContent);
                    }
                }
                
                _logger.LogWarning("AI 模型未返回函數調用結果，回退到簡單解析");
                return await ExtractNameRelationsAsync(textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI 解析人名關係失敗: {TextContent}", textContent);
                _logger.LogInformation("回退到簡單解析模式");
                return await ExtractNameRelationsAsync(textContent);
            }
        }

        /// <summary>
        /// 抽取人名關係 (簡單解析版本 - 回退方案)
        /// </summary>
        /// <param name="textContent">要分析的文字內容</param>
        /// <returns>人名關係配對列表</returns>
        public async Task<List<NameRelationPair>> ExtractNameRelationsAsync(string textContent)
        {
            try
            {
                _logger.LogInformation("=== 開始解析人名關係 (新版本) ===");
                _logger.LogInformation("輸入文字內容: {TextContent}", textContent);
                
                var result = new List<NameRelationPair>();
                
                if (string.IsNullOrEmpty(textContent))
                {
                    _logger.LogInformation("文字內容為空，返回空結果");
                    return result;
                }
                
                _logger.LogInformation("開始分割文字內容...");
                // 處理多行文字，先將 \n 字符替換為真正的換行符
                var normalizedText = textContent.Replace("\\n", "\n");
                _logger.LogInformation("標準化後文字: '{NormalizedText}'", normalizedText);
                
                // 簡單的解析邏輯
                var lines = normalizedText.Split(new[] { '\n', '；', '。' }, StringSplitOptions.RemoveEmptyEntries);
                _logger.LogInformation("分割後得到 {LineCount} 行", lines.Length);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    _logger.LogInformation("處理行: '{TrimmedLine}'", trimmedLine);
                    
                    if (string.IsNullOrEmpty(trimmedLine)) 
                    {
                        _logger.LogInformation("跳過空行");
                        continue;
                    }
                    
                    // 處理家庭關係格式：如 "母：邱還真"
                    if (trimmedLine.Contains('：'))
                    {
                        _logger.LogInformation("檢測到家庭關係格式");
                        var parts = trimmedLine.Split('：');
                        _logger.LogInformation("分割結果: {PartsCount} 部分", parts.Length);
                        
                        if (parts.Length == 2)
                        {
                            var relation = parts[0].Trim();
                            var name = parts[1].Trim();
                            _logger.LogInformation("提取關係: '{Relation}', 名字: '{Name}'", relation, name);
                            
                            if (!string.IsNullOrEmpty(name))
                            {
                                result.Add(new NameRelationPair { Name = name, Relation = relation });
                                _logger.LogInformation("✅ 添加家庭關係: {Name} ({Relation})", name, relation);
                            }
                        }
                    }
                    // 處理家庭關係格式：如 "妹妹，李惠如"
                    else if (trimmedLine.Contains('，') && (trimmedLine.Contains("妹妹") || trimmedLine.Contains("姐姐") || trimmedLine.Contains("哥哥") || trimmedLine.Contains("弟弟") || trimmedLine.Contains("爸爸") || trimmedLine.Contains("媽媽") || trimmedLine.Contains("兒子") || trimmedLine.Contains("女兒")))
                    {
                        _logger.LogInformation("檢測到家庭關係格式");
                        var parts = trimmedLine.Split('，');
                        _logger.LogInformation("分割結果: {PartsCount} 部分", parts.Length);
                        
                        if (parts.Length >= 2)
                        {
                            var relation = parts[0].Trim();
                            var name = parts[1].Trim();
                            _logger.LogInformation("提取關係: '{Relation}', 名字: '{Name}'", relation, name);
                            
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(relation))
                            {
                                result.Add(new NameRelationPair { Name = name, Relation = relation });
                                _logger.LogInformation("✅ 添加家庭關係: {Name} ({Relation})", name, relation);
                            }
                        }
                    }
                    // 處理朋友關係格式：如 "羅亞瑟，淡江大學同學" 或 "周雅，律理法律資訊有限公司"
                    else if (trimmedLine.Contains('，'))
                    {
                        _logger.LogInformation("檢測到朋友關係格式");
                        var parts = trimmedLine.Split('，');
                        _logger.LogInformation("分割結果: {PartsCount} 部分", parts.Length);
                        
                        if (parts.Length >= 2)
                        {
                            var name = parts[0].Trim();
                            var relation = parts[1].Trim();
                            _logger.LogInformation("提取名字: '{Name}', 關係: '{Relation}'", name, relation);
                            
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(relation))
                            {
                                result.Add(new NameRelationPair { Name = name, Relation = relation });
                                _logger.LogInformation("✅ 添加朋友關係: {Name} ({Relation})", name, relation);
                            }
                        }
                    }
                    // 處理簡單的名字列表
                    else
                    {
                        _logger.LogInformation("檢測到簡單名字格式，長度: {Length}", trimmedLine.Length);
                        // 檢查是否為中文名字（2-4個字符）
                        if (trimmedLine.Length >= 2 && trimmedLine.Length <= 4 && 
                            trimmedLine.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                        {
                            result.Add(new NameRelationPair { Name = trimmedLine, Relation = "朋友" });
                            _logger.LogInformation("✅ 添加簡單朋友: {Name}", trimmedLine);
                        }
                        else
                        {
                            _logger.LogInformation("❌ 不符合名字格式，跳過");
                        }
                    }
                }
                
                _logger.LogInformation("解析結果: {ResultCount} 個關係配對", result.Count);
                foreach (var pair in result)
                {
                    _logger.LogInformation("  - {Name}: {Relation}", pair.Name, pair.Relation);
                }
                
                _logger.LogInformation("=== 解析完成 ===");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析人名關係失敗: {TextContent}", textContent);
                _logger.LogError("=== 解析失敗 ===");
                throw;
            }
        }
    }

    /// <summary>
    /// 人名關係配對
    /// </summary>
    public class NameRelationPair
    {
        public string Name { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
    }

    /// <summary>
    /// 抽取人名關係的回應結果
    /// </summary>
    public class ExtractNameRelationResult
    {
        public List<NameRelationPair> Pairs { get; set; } = new List<NameRelationPair>();
    }
} 