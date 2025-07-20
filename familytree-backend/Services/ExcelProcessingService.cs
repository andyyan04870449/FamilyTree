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

        public async Task<ExcelProcessingResult> ProcessExcelFileAsync(string filePath, string fileMd5)
        {
            try
            {
                _logger.LogInformation("開始處理Excel檔案: {FilePath}", filePath);

                // 讀取欄位對應
                var fieldMappings = await GetFieldMappingsAsync();
                
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
                var result = await ProcessExcelData(excelData, fieldMappings, fileMd5);
                
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

        private async Task<Dictionary<string, string>> GetFieldMappingsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT excel_field_name as ExcelFieldName, db_field_name as DbFieldName FROM field_mapping";
            var mappings = await connection.QueryAsync<FieldMappingModel>(sql);

            _logger.LogInformation("從資料庫取得 {Count} 個欄位對應", mappings.Count());

            var result = new Dictionary<string, string>();
            var processedKeys = new List<string>();
            var duplicateKeys = new List<string>();
            
            foreach (var mapping in mappings)
            {
                var key = mapping.ExcelFieldName?.Trim();
                _logger.LogDebug("處理欄位對應: '{ExcelField}' -> '{DbField}'", mapping.ExcelFieldName, mapping.DbFieldName);
                
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogWarning("發現空白的Excel欄位名稱，跳過");
                    continue;
                }
                
                if (result.ContainsKey(key))
                {
                    _logger.LogWarning("發現重複的Excel欄位名稱: '{Key}'", key);
                    duplicateKeys.Add(key);
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

        private async Task<ExcelProcessingResult> ProcessExcelData(DataTable excelData, Dictionary<string, string> fieldMappings, string fileMd5)
        {
            _logger.LogInformation("=== 開始處理Excel資料 ===");
            _logger.LogInformation("FileMd5: {FileMd5}, 資料行數: {RowCount}", fileMd5, excelData.Rows.Count);

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
                if (!fieldMappings.ContainsKey(column.ColumnName))
                {
                    unmappedFields.Add(column.ColumnName);
                    _logger.LogWarning("發現未對應的Excel欄位: '{ColumnName}'", column.ColumnName);
                }
                else
                {
                    _logger.LogInformation("欄位對應成功: '{ExcelField}' -> '{DbField}'", 
                        column.ColumnName, fieldMappings[column.ColumnName]);
                }
            }
            result.UnmappedFields = unmappedFields.ToList();

            _logger.LogInformation("欄位對應統計: 成功對應 {MappedCount} 個欄位, 未對應 {UnmappedCount} 個欄位", 
                fieldMappings.Count, unmappedFields.Count);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (DataRow row in excelData.Rows)
            {
                var rowIndex = excelData.Rows.IndexOf(row) + 1;
                _logger.LogInformation("=== 處理第 {RowIndex} 行資料 ===", rowIndex);
                
                try
                {
                    var personData = new PersonDataModel
                    {
                        FileMd5 = fileMd5,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // 對應欄位資料
                    foreach (DataColumn column in excelData.Columns)
                    {
                        if (fieldMappings.TryGetValue(column.ColumnName, out var dbFieldName))
                        {
                            var value = row[column.ColumnName]?.ToString() ?? "";
                            _logger.LogInformation("設定欄位: '{ExcelField}' -> '{DbField}' = '{Value}'", 
                                column.ColumnName, dbFieldName, value.Length > 100 ? value.Substring(0, 100) + "..." : value);
                            
                            SetPersonDataField(personData, dbFieldName, value);
                        }
                    }

                    // 檢查必要欄位
                    if (string.IsNullOrWhiteSpace(personData.Name))
                    {
                        _logger.LogWarning("第 {RowIndex} 行跳過: 姓名欄位為空", rowIndex);
                        result.FailedRows++;
                        continue;
                    }

                    _logger.LogInformation("第 {RowIndex} 行人員資料準備完成: {Name}", rowIndex, personData.Name);
                    _logger.LogInformation("完整人員資料:");
                    _logger.LogInformation("  - 姓名: {Name}", personData.Name ?? "無");
                    _logger.LogInformation("  - 性別: {Gender}", personData.Gender ?? "無");
                    _logger.LogInformation("  - 生日: {Birthday}", personData.Birthday?.ToString("yyyy-MM-dd") ?? "無");
                    _logger.LogInformation("  - 國籍: {Nationality}", personData.Nationality ?? "無");
                    _logger.LogInformation("  - 民族: {Ethnicity}", personData.Ethnicity ?? "無");
                    _logger.LogInformation("  - 電話: {Phone}", personData.Phone ?? "無");
                    _logger.LogInformation("  - 手機: {Mobile}", personData.Mobile ?? "無");
                    _logger.LogInformation("  - 信箱: {Email}", personData.Email ?? "無");
                    _logger.LogInformation("  - 現居地址: {CurrentAddress}", personData.CurrentAddress ?? "無");
                    _logger.LogInformation("  - 現職單位: {CurrentWorkplace}", personData.CurrentWorkplace ?? "無");
                    _logger.LogInformation("  - 親屬關係: {FamilyRelationships}", personData.FamilyRelationships ?? "無");
                    _logger.LogInformation("  - 重要友人: {ImportantFriends}", personData.ImportantFriends ?? "無");
                    _logger.LogInformation("  - 工作經歷: {Experience}", personData.Experience ?? "無");
                    _logger.LogInformation("  - 學歷: {Education}", personData.Education ?? "無");
                    _logger.LogInformation("  - 網路帳號: {OnlineAccounts}", personData.OnlineAccounts ?? "無");
                    _logger.LogInformation("  - 著作: {Publications}", personData.Publications ?? "無");
                    _logger.LogInformation("  - 參與活動: {Activities}", personData.Activities ?? "無");
                    _logger.LogInformation("  - 經常出入場所: {FrequentPlaces}", personData.FrequentPlaces ?? "無");
                    _logger.LogInformation("  - 出國紀錄: {TravelRecords}", personData.TravelRecords ?? "無");
                    _logger.LogInformation("  - 備註: {Notes}", personData.Notes ?? "無");

                    // 儲存到資料庫
                    _logger.LogInformation("開始儲存第 {RowIndex} 行資料到資料庫...", rowIndex);
                    await SavePersonDataAsync(connection, personData);
                    _logger.LogInformation("✅ 第 {RowIndex} 行資料儲存成功", rowIndex);
                    result.SuccessRows++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 處理Excel第 {RowIndex} 行資料失敗", rowIndex);
                    _logger.LogError("錯誤詳情: {ErrorMessage}", ex.Message);
                    result.FailedRows++;
                }
            }

            _logger.LogInformation("=== Excel資料處理完成 ===");
            _logger.LogInformation("成功處理: {SuccessRows} 行, 失敗: {FailedRows} 行", result.SuccessRows, result.FailedRows);
            return result;
        }

        private void SetPersonDataField(PersonDataModel personData, string dbFieldName, string value)
        {
            _logger.LogDebug("SetPersonDataField: {DbFieldName} = '{Value}'", dbFieldName, value);
            
            switch (dbFieldName.ToLower())
            {
                case "name":
                    personData.Name = value;
                    _logger.LogDebug("設定 Name: '{Value}'", value);
                    break;
                case "photo":
                    personData.Photo = value;
                    _logger.LogDebug("設定 Photo: '{Value}'", value);
                    break;
                case "discovery_process":
                    personData.DiscoveryProcess = value;
                    _logger.LogDebug("設定 DiscoveryProcess: '{Value}'", value);
                    break;
                case "gender":
                    personData.Gender = value;
                    _logger.LogDebug("設定 Gender: '{Value}'", value);
                    break;
                case "birthday":
                    if (DateTime.TryParse(value, out var birthday))
                    {
                        personData.Birthday = birthday;
                        _logger.LogDebug("設定 Birthday: '{Value}' -> {ParsedDate}", value, birthday);
                    }
                    else
                    {
                        _logger.LogWarning("無法解析生日格式: '{Value}'", value);
                    }
                    break;
                case "birthplace":
                    personData.Birthplace = value;
                    _logger.LogDebug("設定 Birthplace: '{Value}'", value);
                    break;
                case "nationality":
                    personData.Nationality = value;
                    _logger.LogDebug("設定 Nationality: '{Value}'", value);
                    break;
                case "ethnicity":
                    personData.Ethnicity = value;
                    _logger.LogDebug("設定 Ethnicity: '{Value}'", value);
                    break;
                case "ancestral_home":
                    personData.AncestralHome = value;
                    _logger.LogDebug("設定 AncestralHome: '{Value}'", value);
                    break;
                case "political_party":
                    personData.PoliticalParty = value;
                    _logger.LogDebug("設定 PoliticalParty: '{Value}'", value);
                    break;
                case "id_number":
                    personData.IdNumber = value;
                    _logger.LogDebug("設定 IdNumber: '{Value}'", value);
                    break;
                case "passport_number":
                    personData.PassportNumber = value;
                    _logger.LogDebug("設定 PassportNumber: '{Value}'", value);
                    break;
                case "phone":
                    personData.Phone = value;
                    _logger.LogDebug("設定 Phone: '{Value}'", value);
                    break;
                case "mobile":
                    personData.Mobile = value;
                    _logger.LogDebug("設定 Mobile: '{Value}'", value);
                    break;
                case "email":
                    personData.Email = value;
                    _logger.LogDebug("設定 Email: '{Value}'", value);
                    break;
                case "current_workplace":
                    personData.CurrentWorkplace = value;
                    _logger.LogDebug("設定 CurrentWorkplace: '{Value}'", value);
                    break;
                case "current_address":
                    personData.CurrentAddress = value;
                    _logger.LogDebug("設定 CurrentAddress: '{Value}'", value);
                    break;
                case "mailing_address":
                    personData.MailingAddress = value;
                    _logger.LogDebug("設定 MailingAddress: '{Value}'", value);
                    break;
                case "family_relationships":
                    personData.FamilyRelationships = value;
                    _logger.LogInformation("🔥 設定 FamilyRelationships: '{Value}'", value);
                    break;
                case "experience":
                    personData.Experience = value;
                    _logger.LogInformation("🔥 設定 Experience: '{Value}'", value);
                    break;
                case "education":
                    personData.Education = value;
                    _logger.LogDebug("設定 Education: '{Value}'", value);
                    break;
                case "online_accounts":
                    personData.OnlineAccounts = value;
                    _logger.LogInformation("🔥 設定 OnlineAccounts: '{Value}'", value);
                    break;
                case "publications":
                    personData.Publications = value;
                    _logger.LogInformation("🔥 設定 Publications: '{Value}'", value);
                    break;
                case "activities":
                    personData.Activities = value;
                    _logger.LogInformation("🔥 設定 Activities: '{Value}'", value);
                    break;
                case "important_friends":
                    personData.ImportantFriends = value;
                    _logger.LogInformation("🔥 設定 ImportantFriends: '{Value}'", value);
                    break;
                case "frequent_places":
                    personData.FrequentPlaces = value;
                    _logger.LogInformation("🔥 設定 FrequentPlaces: '{Value}'", value);
                    break;
                case "travel_records":
                    personData.TravelRecords = value;
                    _logger.LogInformation("🔥 設定 TravelRecords: '{Value}'", value);
                    break;
                case "notes":
                    personData.Notes = value;
                    _logger.LogDebug("設定 Notes: '{Value}'", value);
                    break;
                default:
                    _logger.LogWarning("未知的資料庫欄位名稱: '{DbFieldName}'", dbFieldName);
                    break;
            }
        }

        private async Task SavePersonDataAsync(NpgsqlConnection connection, PersonDataModel personData)
        {
                            var sql = @"INSERT INTO person_profile (
                file_md5, photo_index, name, discovery_process, gender, birthday, birthplace, 
                nationality, ethnicity, ancestral_origin, political_party, id_number, 
                passport_number, phone, mobile, email, current_employer, address, 
                mailing_address, family_relationships, experience, education, online_accounts, 
                publications, activities, friends, frequent_locations, travel_history, 
                remarks, created_at, updated_at
            ) VALUES (
                @FileMd5, @Photo, @Name, @DiscoveryProcess, @Gender, @Birthday, @Birthplace,
                @Nationality, @Ethnicity, @AncestralHome, @PoliticalParty, @IdNumber,
                @PassportNumber, @Phone, @Mobile, @Email, @CurrentWorkplace, @CurrentAddress,
                @MailingAddress, @FamilyRelationships, @Experience, @Education, @OnlineAccounts,
                @Publications, @Activities, @ImportantFriends, @FrequentPlaces, @TravelRecords,
                @Notes, @CreatedAt, @UpdatedAt
            )";

            await connection.ExecuteAsync(sql, personData);
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
    }
} 