using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using familytree_backend.Models;
using FamilyTree.Attributes;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 稽核日誌配置控制器
    /// 提供稽核系統的動態配置選項
    /// </summary>
    [Route("api/audit/config")]
    [ApiController]
    [Authorize]
    public class AuditConfigController : ControllerBase
    {
        /// <summary>
        /// 獲取事件類型選項
        /// </summary>
        [HttpGet("event-types")]
        [AuditReaderPermission]
        public IActionResult GetEventTypes()
        {
            var eventTypes = new[]
            {
                new { value = "", label = "全部事件類型" },
                new { value = "LOGIN", label = "登入" },
                new { value = "LOGOUT", label = "登出" },
                new { value = "LOGIN_FAILED", label = "登入失敗" },
                new { value = "DATA_ACCESS", label = "資料存取" },
                new { value = "FILE_UPLOAD", label = "檔案上傳" },
                new { value = "SEARCH", label = "搜尋" },
                new { value = "EXPORT", label = "匯出" },
                new { value = "ADMIN_ACTION", label = "管理員動作" }
            };

            return Ok(new ApiResponse<object[]>
            {
                Success = true,
                Data = eventTypes,
                Message = "成功獲取事件類型選項"
            });
        }

        /// <summary>
        /// 獲取安全等級選項
        /// </summary>
        [HttpGet("security-levels")]
        [AuditReaderPermission]
        public IActionResult GetSecurityLevels()
        {
            var securityLevels = new[]
            {
                new { value = "", label = "全部安全等級" },
                new { value = "LOW", label = "低" },
                new { value = "NORMAL", label = "一般" },
                new { value = "HIGH", label = "高" },
                new { value = "CRITICAL", label = "緊急" }
            };

            return Ok(new ApiResponse<object[]>
            {
                Success = true,
                Data = securityLevels,
                Message = "成功獲取安全等級選項"
            });
        }

        /// <summary>
        /// 獲取操作結果選項
        /// </summary>
        [HttpGet("operation-results")]
        [AuditReaderPermission]
        public IActionResult GetOperationResults()
        {
            var operationResults = new[]
            {
                new { value = "", label = "全部結果" },
                new { value = "true", label = "成功" },
                new { value = "false", label = "失敗" }
            };

            return Ok(new ApiResponse<object[]>
            {
                Success = true,
                Data = operationResults,
                Message = "成功獲取操作結果選項"
            });
        }

        /// <summary>
        /// 獲取資源類型選項
        /// </summary>
        [HttpGet("resource-types")]
        [AuditReaderPermission]
        public IActionResult GetResourceTypes()
        {
            var resourceTypes = new[]
            {
                new { value = "", label = "全部資源類型" },
                new { value = "USER", label = "使用者" },
                new { value = "PROJECT", label = "專案" },
                new { value = "FILE", label = "檔案" },
                new { value = "PERSON", label = "人員資料" },
                new { value = "SYSTEM", label = "系統" }
            };

            return Ok(new ApiResponse<object[]>
            {
                Success = true,
                Data = resourceTypes,
                Message = "成功獲取資源類型選項"
            });
        }

        /// <summary>
        /// 獲取所有配置選項
        /// </summary>
        [HttpGet("all")]
        [AuditReaderPermission]
        public IActionResult GetAllConfigs()
        {
            var configs = new
            {
                eventTypes = new[]
                {
                    new { value = "", label = "全部事件類型" },
                    new { value = "LOGIN", label = "登入" },
                    new { value = "LOGOUT", label = "登出" },
                    new { value = "LOGIN_FAILED", label = "登入失敗" },
                    new { value = "DATA_ACCESS", label = "資料存取" },
                    new { value = "FILE_UPLOAD", label = "檔案上傳" },
                    new { value = "SEARCH", label = "搜尋" },
                    new { value = "EXPORT", label = "匯出" },
                    new { value = "ADMIN_ACTION", label = "管理員動作" }
                },
                securityLevels = new[]
                {
                    new { value = "", label = "全部安全等級" },
                    new { value = "LOW", label = "低" },
                    new { value = "NORMAL", label = "一般" },
                    new { value = "HIGH", label = "高" },
                    new { value = "CRITICAL", label = "緊急" }
                },
                operationResults = new[]
                {
                    new { value = "", label = "全部結果" },
                    new { value = "true", label = "成功" },
                    new { value = "false", label = "失敗" }
                },
                resourceTypes = new[]
                {
                    new { value = "", label = "全部資源類型" },
                    new { value = "USER", label = "使用者" },
                    new { value = "PROJECT", label = "專案" },
                    new { value = "FILE", label = "檔案" },
                    new { value = "PERSON", label = "人員資料" },
                    new { value = "SYSTEM", label = "系統" }
                }
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = configs,
                Message = "成功獲取所有配置選項"
            });
        }
    }
}