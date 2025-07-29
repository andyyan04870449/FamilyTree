// Excel處理服務 - 處理Excel檔案讀取、欄位對應和資料匯入
using System.Data;
using System.Text;
using familytree_backend.Models;
using Dapper;
using Npgsql;
using OfficeOpenXml;

namespace familytree_backend.Services
{
    public class ExcelProcessingService
    {
        private readonly string _connectionString;
        private readonly ILogger<ExcelProcessingService> _logger;

        public ExcelProcessingService(IConfiguration configuration, ILogger<ExcelProcessingService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
            
            // 設定EPPlus授權
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<ExcelProcessingResult> ProcessExcelFileAsync(string filePath, string fileMd5, string? projectId = null)
        {
            try
            {
                _logger.LogInformation("開始處理Excel檔案: {FilePath}", filePath);

                // 讀取欄位對應
                var fieldMappings = await GetFieldMappingsAsync(projectId);
                
                // 讀取Excel檔案
                var excelData = await ReadExcelFileAsync(filePath);
                
                if (excelData.Rows.Count == 0)
                {
                    return new ExcelProcessingResult
                    {
                        Success = false,
                        Message = "Excel檔案中沒有資料",
                        FileMd5 = fileMd5
                    };
                }

                // 處理每一行資料
                var result = await ProcessExcelData(excelData, fieldMappings, fileMd5, projectId);
                
                // 更新檔案狀態
                await UpdateFileStatusAsync(fileMd5, "merged");
                
                _logger.LogInformation("Excel檔案處理完成: {FilePath}, 成功處理 {SuccessRows} 行", 
                    filePath, result.SuccessRows);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理Excel檔案失敗: {FilePath}", filePath);
                return new ExcelProcessingResult
                {
                    Success = false,
                    Message = $"處理Excel檔案失敗: {ex.Message}",
                    FileMd5 = fileMd5
                };
            }
        }

        private async Task<DataTable> ReadExcelFileAsync(string filePath)
        {
            _logger.LogInformation("開始讀取Excel檔案: {FilePath}", filePath);
            var dataTable = new DataTable();

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
            {
                throw new InvalidOperationException("Excel檔案中沒有工作表");
            }

            _logger.LogInformation("Excel檔案基本資訊: 工作表名稱={WorksheetName}, 行數={RowCount}, 欄數={ColumnCount}", 
                worksheet.Name, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column);

            // 讀取標題行並確保唯一性
            var headerRow = worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column];
            var columnNames = new HashSet<string>();
            var originalHeaders = new List<string>();
            
            foreach (var cell in headerRow)
            {
                var originalColumnName = cell.Value?.ToString() ?? "";
                originalHeaders.Add($"欄位{cell.Start.Column}: '{originalColumnName}'");
                
                var columnName = originalColumnName.Trim();
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    columnName = $"Column{cell.Start.Column}";
                    _logger.LogWarning("欄位 {ColumnIndex} 名稱為空，使用預設名稱: {DefaultName}", cell.Start.Column, columnName);
                }
                
                // 確保欄位名稱唯一
                var finalColumnName = columnName;
                var counter = 1;
                while (columnNames.Contains(finalColumnName))
                {
                    finalColumnName = $"{columnName}_{counter}";
                    counter++;
                    _logger.LogWarning("欄位名稱重複，重新命名: '{OriginalName}' -> '{FinalName}'", columnName, finalColumnName);
                }
                
                columnNames.Add(finalColumnName);
                dataTable.Columns.Add(finalColumnName);
            }

            _logger.LogInformation("Excel標題行詳情: {Headers}", string.Join(", ", originalHeaders));
            _logger.LogInformation("處理後的欄位名稱: {ProcessedHeaders}", string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => $"'{c.ColumnName}'")));

            // 讀取資料行
            var dataRowCount = 0;
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var dataRow = dataTable.NewRow();
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? "";
                    dataRow[col - 1] = cellValue;
                }
                dataTable.Rows.Add(dataRow);
                dataRowCount++;
            }

            _logger.LogInformation("成功讀取Excel檔案: 標題欄位數={ColumnCount}, 資料行數={DataRowCount}", 
                dataTable.Columns.Count, dataRowCount);

            return dataTable;
        }

        private async Task<Dictionary<string, string>> GetFieldMappingsAsync(string? projectId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // field_mapping 是通用常數表，不需要專案過濾
            string sql = "SELECT excel_field_name as ExcelFieldName, db_field_name as DbFieldName FROM field_mapping";
            var mappings = await connection.QueryAsync<FieldMappingModel>(sql);
            _logger.LogInformation("查詢通用欄位對應表");

            _logger.LogInformation("從資料庫取得 {Count} 個欄位對應", mappings.Count());

            // 詳細記錄所有資料庫中的欄位對應
            _logger.LogInformation("=== 資料庫中的欄位對應清單 ===");
            foreach (var mapping in mappings)
            {
                _logger.LogInformation("資料庫對應: '{ExcelField}' -> '{DbField}'", mapping.ExcelFieldName, mapping.DbFieldName);
            }
            _logger.LogInformation("=== 欄位對應清單結束 ===");

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var processedKeys = new List<string>();
            var duplicateKeys = new List<string>();
            
            foreach (var mapping in mappings)
            {
                var key = mapping.ExcelFieldName?.Trim();
                _logger.LogInformation("處理欄位對應: '{ExcelField}' -> '{DbField}'", mapping.ExcelFieldName, mapping.DbFieldName);
                
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogWarning("發現空白的Excel欄位名稱，跳過");
                    continue;
                }
                
                // 如果已經存在這個鍵，記錄下來但不報錯
                if (result.ContainsKey(key))
                {
                    if (!duplicateKeys.Contains(key))
                    {
                        _logger.LogWarning("發現重複的Excel欄位名稱: '{Key}' -> '{DbField1}' 和 '{DbField2}'", 
                            key, result[key], mapping.DbFieldName);
                        duplicateKeys.Add(key);
                    }
                    continue;
                }
                
                result[key] = mapping.DbFieldName;
                processedKeys.Add(key);
            }
            
            _logger.LogInformation("成功處理 {ProcessedCount} 個欄位對應", processedKeys.Count);
            
            if (duplicateKeys.Any())
            {
                _logger.LogWarning("發現 {DuplicateCount} 個重複的欄位名稱: {DuplicateKeys}", 
                    duplicateKeys.Count, string.Join(", ", duplicateKeys));
            }
            
            return result;
        }

        private async Task<ExcelProcessingResult> ProcessExcelData(DataTable excelData, Dictionary<string, string> fieldMappings, string fileMd5, string? projectId = null)
        {
            _logger.LogInformation("=== 開始處理Excel資料 ===");
            _logger.LogInformation("FileMd5: {FileMd5}, 資料行數: {RowCount}", fileMd5, excelData.Rows.Count);

            // 顯示所有可用的欄位對應
            _logger.LogInformation("所有可用的欄位對應:");
            foreach (var mapping in fieldMappings)
            {
                _logger.LogInformation("Excel欄位: '{ExcelField}' -> DB欄位: '{DbField}'", mapping.Key, mapping.Value);
            }

            // 顯示Excel中的所有欄位
            _logger.LogInformation("Excel檔案中的所有欄位:");
            foreach (DataColumn column in excelData.Columns)
            {
                _logger.LogInformation("Excel欄位名稱: '{ColumnName}'", column.ColumnName);
            }

            var result = new ExcelProcessingResult
            {
                SuccessRows = 0,
                FailedRows = 0,
                UnmappedFields = new List<string>()
            };

            // 收集未對應的欄位
            var unmappedFields = new HashSet<string>();
            foreach (DataColumn column in excelData.Columns)
            {
                var columnName = column.ColumnName.Trim();
                if (!fieldMappings.ContainsKey(columnName))
                {
                    unmappedFields.Add(columnName);
                    _logger.LogWarning("發現未對應的Excel欄位: '{ColumnName}'", columnName);
                }
                else
                {
                    _logger.LogInformation("欄位對應成功: '{ExcelField}' -> '{DbField}'", 
                        columnName, fieldMappings[columnName]);
                }
            }
            result.UnmappedFields = unmappedFields.ToList();

            _logger.LogInformation("欄位對應統計: 成功對應 {MappedCount} 個欄位, 未對應 {UnmappedCount} 個欄位", 
                fieldMappings.Count, unmappedFields.Count);

            // 詳細列出所有未對應的欄位和建議的SQL
            if (unmappedFields.Any())
            {
                _logger.LogWarning("=== 未對應的Excel欄位詳細列表 ===");
                var fieldList = unmappedFields.ToList();
                for (int i = 0; i < fieldList.Count; i++)
                {
                    _logger.LogWarning("未對應欄位 {Index}: '{FieldName}'", i + 1, fieldList[i]);
                }
                _logger.LogWarning("=== 建議將以下欄位加入 field_mapping 表 ===");
                foreach (var field in fieldList)
                {
                    var suggestedDbField = SuggestDbFieldName(field);
                    _logger.LogWarning("INSERT INTO field_mapping (excel_field_name, db_field_name, project_id) VALUES ('{ExcelField}', '{SuggestedDbField}', '{ProjectId}');", 
                        field, suggestedDbField, projectId ?? "YOUR_PROJECT_ID");
                }
                _logger.LogWarning("=== 建議SQL結束 ===");
            }

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (DataRow row in excelData.Rows)
            {
                var rowIndex = excelData.Rows.IndexOf(row) + 1;
                _logger.LogInformation("=== 處理第 {RowIndex} 行資料 ===", rowIndex);
                
                PersonDataModel? personData = null;
                try
                {
                    personData = new PersonDataModel
                    {
                        FileMd5 = fileMd5,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // 處理每個欄位
                    var processedFieldsCount = 0;
                    var skippedFieldsCount = 0;
                    
                    foreach (DataColumn column in excelData.Columns)
                    {
                        var columnName = column.ColumnName.Trim();
                        var cellValue = row[column]?.ToString()?.Trim() ?? "";
                        
                        if (fieldMappings.TryGetValue(columnName, out var dbFieldName))
                        {
                            if (!string.IsNullOrWhiteSpace(cellValue))
                            {
                                _logger.LogDebug("第 {RowIndex} 行 - 處理欄位: '{ExcelField}' -> '{DbField}' = '{Value}'", 
                                    rowIndex, columnName, dbFieldName, cellValue);
                                SetPersonDataField(personData, dbFieldName, cellValue);
                                processedFieldsCount++;
                            }
                            else
                            {
                                _logger.LogDebug("第 {RowIndex} 行 - 跳過空值欄位: '{ExcelField}' -> '{DbField}'", 
                                    rowIndex, columnName, dbFieldName);
                                skippedFieldsCount++;
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(cellValue))
                            {
                                _logger.LogDebug("第 {RowIndex} 行 - 未對應欄位有值: '{ExcelField}' = '{Value}'", 
                                    rowIndex, columnName, cellValue);
                            }
                        }
                    }
                    
                    _logger.LogInformation("第 {RowIndex} 行處理統計: 成功處理 {ProcessedCount} 個欄位, 跳過空值 {SkippedCount} 個欄位", 
                        rowIndex, processedFieldsCount, skippedFieldsCount);

                    // 檢查必填欄位
                    if (string.IsNullOrWhiteSpace(personData.Name))
                    {
                        _logger.LogWarning("第 {RowIndex} 行跳過: 姓名欄位為空", rowIndex);
                        result.FailedRows++;
                        continue;
                    }

                    // 儲存資料
                    await SavePersonDataAsync(connection, personData, projectId);
                    result.SuccessRows++;
                    _logger.LogInformation("第 {RowIndex} 行資料處理成功: Name='{Name}'", rowIndex, personData.Name);
                }
                catch (Exception ex)
                {
                    result.FailedRows++;
                    _logger.LogError(ex, "處理第 {RowIndex} 行資料時發生錯誤: Name='{Name}', Error={ErrorMessage}", 
                        rowIndex, personData?.Name ?? "未知", ex.Message);
                }
            }

            _logger.LogInformation("=== Excel資料處理完成 ===");
            _logger.LogInformation("處理結果: 成功={SuccessRows}, 失敗={FailedRows}, 未對應欄位數={UnmappedCount}", 
                result.SuccessRows, result.FailedRows, result.UnmappedFields.Count);

            return result;
        }

        private void SetPersonDataField(PersonDataModel personData, string dbFieldName, string value)
        {
            _logger.LogDebug("設定欄位: {DbFieldName} = {Value}", dbFieldName, value);
            
            switch (dbFieldName.ToLower())
            {
                case "name":
                    personData.Name = value;
                    break;
                case "gender":
                    personData.Gender = value;
                    break;
                case "birthday":
                    if (DateTime.TryParse(value, out var birthday))
                    {
                        personData.Birthday = birthday.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        personData.Birthday = value; // 保留原始字串如果無法轉換
                    }
                    break;
                case "nationality":
                    personData.Nationality = value;
                    break;
                case "birthplace":
                    personData.Birthplace = value;
                    break;
                case "ethnicity":
                    personData.Ethnicity = value;
                    break;
                case "ancestral_origin":
                    personData.AncestralHome = value;
                    break;
                case "political_party":
                    personData.PoliticalParty = value;
                    break;
                case "id_number":
                    personData.IdNumber = value;
                    break;
                case "passport_number":
                    personData.PassportNumber = value;
                    break;
                case "phone":
                    personData.Phone = value;
                    break;
                case "mobile":
                    personData.Mobile = value;
                    break;
                case "email":
                    personData.Email = value;
                    break;
                case "current_employer":
                    personData.CurrentWorkplace = value;
                    break;
                case "address":
                    personData.CurrentAddress = value;
                    break;
                case "mailing_address":
                    personData.MailingAddress = value;
                    break;
                case "family_relationships":
                    personData.FamilyRelationships = value;
                    break;
                case "experience":
                    personData.Experience = value;
                    break;
                case "education":
                    personData.Education = value;
                    break;
                case "online_accounts":
                    personData.OnlineAccounts = value;
                    break;
                case "publications":
                    personData.Publications = value;
                    break;
                case "activities":
                    personData.Activities = value;
                    break;
                case "important_friends":  // 修正：使用正確的欄位名稱
                    personData.ImportantFriends = value;
                    break;
                case "frequent_locations":
                    personData.FrequentPlaces = value;
                    break;
                case "travel_history":
                    personData.TravelRecords = value;
                    break;
                case "remarks":
                    personData.Notes = value;
                    break;
                case "discovery_process":  // 修正：使用正確的欄位名稱
                    personData.DiscoveryProcess = value;
                    break;
                case "photo_index":  // 修正：使用正確的欄位名稱
                    personData.Photo = value;
                    break;
                default:
                    _logger.LogWarning("未知的資料庫欄位名稱: {DbFieldName}", dbFieldName);
                    break;
            }
        }

        private async Task SavePersonDataAsync(NpgsqlConnection connection, PersonDataModel personData, string? projectId = null)
        {
            _logger.LogInformation("開始保存人員資料: Name='{Name}', FileMd5='{FileMd5}'", personData.Name, personData.FileMd5);

            var sql = @"
                INSERT INTO person_profile (
                    file_md5, photo_index, name, discovery_process, gender, birthday, birthplace,
                    nationality, ethnicity, ancestral_origin, political_party, id_number, passport_number,
                    phone, mobile, email, current_employer, address, mailing_address,
                    family_relationships, experience, education, online_accounts, publications,
                    activities, important_friends, frequent_locations, travel_history, remarks,
                    project_id, created_at, updated_at
                ) VALUES (
                    @FileMd5, @Photo, @Name, @DiscoveryProcess, @Gender, @Birthday, @Birthplace,
                    @Nationality, @Ethnicity, @AncestralHome, @PoliticalParty, @IdNumber, @PassportNumber,
                    @Phone, @Mobile, @Email, @CurrentWorkplace, @CurrentAddress, @MailingAddress,
                    @FamilyRelationships, @Experience, @Education, @OnlineAccounts, @Publications,
                    @Activities, @ImportantFriends, @FrequentPlaces, @TravelRecords, @Notes,
                    @ProjectId, @CreatedAt, @UpdatedAt
                )";

            try
            {
                var parameters = new
                {
                    personData.FileMd5,
                    personData.Photo,
                    personData.Name,
                    personData.DiscoveryProcess,
                    personData.Gender,
                    personData.Birthday,
                    personData.Birthplace,
                    personData.Nationality,
                    personData.Ethnicity,
                    personData.AncestralHome,
                    personData.PoliticalParty,
                    personData.IdNumber,
                    personData.PassportNumber,
                    personData.Phone,
                    personData.Mobile,
                    personData.Email,
                    personData.CurrentWorkplace,
                    personData.CurrentAddress,
                    personData.MailingAddress,
                    personData.FamilyRelationships,
                    personData.Experience,
                    personData.Education,
                    personData.OnlineAccounts,
                    personData.Publications,
                    personData.Activities,
                    personData.ImportantFriends,
                    personData.FrequentPlaces,
                    personData.TravelRecords,
                    personData.Notes,
                    ProjectId = projectId,
                    personData.CreatedAt,
                    personData.UpdatedAt
                };

                await connection.ExecuteAsync(sql, parameters);
                _logger.LogInformation("人員資料保存成功: Name='{Name}'", personData.Name);
            }
            catch (PostgresException pgEx)
            {
                _logger.LogError(pgEx, "保存人員資料時發生PostgreSQL錯誤: Code={ErrorCode}, Message={Message}", 
                    pgEx.SqlState, pgEx.MessageText);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存人員資料時發生未知錯誤");
                throw;
            }
        }

        private async Task UpdateFileStatusAsync(string fileMd5, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"UPDATE user_update_file 
                       SET status = @status, is_merged = true, merge_time = @mergeTime 
                       WHERE md5_hash = @fileMd5";

            await connection.ExecuteAsync(sql, new { status, mergeTime = DateTime.UtcNow, fileMd5 });
        }

        // 輔助方法：根據Excel欄位名稱建議資料庫欄位名稱
        private string SuggestDbFieldName(string excelFieldName)
        {
            if (string.IsNullOrEmpty(excelFieldName)) return "unknown_field";

            // 常見對應建議
            var suggestions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"項次", "sequence_number"},
                {"照片", "photo_path"},
                {"發掘經過", "discovery_process"},
                {"出生地", "birth_place"},
                {"民族", "ethnicity"},
                {"籍貫", "ancestral_origin"},
                {"黨派", "political_party"},
                {"通訊地址", "mailing_address"},
                {"親屬關係", "family_relationship"},
                {"經歷", "work_experience"},
                {"網路帳號", "social_accounts"},
                {"著作", "publications"},
                {"參與活動", "activities"},
                {"重要友人", "important_contacts"},
                {"經常出入場所", "frequent_locations"},
                {"出國紀錄", "travel_records"},
                {"建檔時間", "record_created_time"},
                {"建檔人", "record_creator"},
                {"最後更新時間", "record_updated_time"},
                {"最後更新人", "record_updater"}
            };

            // 移除特殊字符和括號內容
            var cleanField = excelFieldName.Split('(')[0].Split('\n')[0].Trim();
            
            if (suggestions.ContainsKey(cleanField))
            {
                return suggestions[cleanField];
            }

            // 轉換為英文欄位名稱的通用規則
            return cleanField.ToLower().Replace(" ", "_").Replace("　", "_");
        }
    }
} 