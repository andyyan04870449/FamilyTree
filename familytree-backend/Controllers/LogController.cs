// 日誌控制器：提供查看分析日誌的 API 端點
using Microsoft.AspNetCore.Mvc;
using System.IO;
using familytree_backend.Services;
using familytree_backend.Models;

namespace familytree_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogController : BaseController
    {
        private readonly IWebHostEnvironment _environment;

        public LogController(
            ILogger<LogController> logger,
            IConfigurationService configurationService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService,
            IWebHostEnvironment environment)
            : base(logger, configurationService, validationService, accessControlService, loggingService)
        {
            _environment = environment;
        }

        [HttpGet("analysis")]
        public IActionResult GetAnalysisLogs([FromQuery] int lines = 100, [FromQuery] string? date = null)
        {
            try
            {
                var logDirectory = Path.Combine(_environment.ContentRootPath, "logs");
                var logFileName = string.IsNullOrEmpty(date) 
                    ? "familytree-analysis-.log" 
                    : $"familytree-analysis-{date}.log";
                
                var logFilePath = Path.Combine(logDirectory, logFileName);
                
                if (!System.IO.File.Exists(logFilePath))
                {
                    return CreateNotFoundResponse("日誌檔案", logFileName);
                }

                var logLines = System.IO.File.ReadAllLines(logFilePath);
                var recentLines = logLines.TakeLast(lines).ToArray();
                
                return CreateSuccessResponse(new {
                    fileName = logFileName,
                    totalLines = logLines.Length,
                    requestedLines = lines,
                    actualLines = recentLines.Length,
                    logs = recentLines
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "讀取分析日誌失敗");
                return StatusCode(500, ApiResponse.ErrorResult(
                    "讀取日誌時發生錯誤",
                    requestId: HttpContext.TraceIdentifier
                ));
            }
        }

        [HttpGet("files")]
        public IActionResult GetLogFiles()
        {
            try
            {
                var logDirectory = Path.Combine(_environment.ContentRootPath, "logs");
                
                if (!Directory.Exists(logDirectory))
                {
                    return Ok(new { 
                        success = true, 
                        data = new { files = new string[0] }
                    });
                }

                var logFiles = Directory.GetFiles(logDirectory, "familytree-analysis-*.log")
                    .Select(Path.GetFileName)
                    .OrderByDescending(f => f)
                    .ToArray();
                
                return CreateSuccessResponse(new { files = logFiles });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "獲取日誌檔案列表失敗");
                return StatusCode(500, ApiResponse.ErrorResult(
                    "獲取日誌檔案列表時發生錯誤",
                    requestId: HttpContext.TraceIdentifier
                ));
            }
        }

        [HttpGet("tail")]
        public IActionResult TailLogs([FromQuery] int lines = 50)
        {
            try
            {
                var logDirectory = Path.Combine(_environment.ContentRootPath, "logs");
                var logFilePath = Path.Combine(logDirectory, "familytree-analysis-.log");
                
                if (!System.IO.File.Exists(logFilePath))
                {
                    return CreateNotFoundResponse("日誌檔案");
                }

                var logLines = System.IO.File.ReadAllLines(logFilePath);
                var recentLines = logLines.TakeLast(lines).ToArray();
                
                return CreateSuccessResponse(new {
                    totalLines = logLines.Length,
                    recentLines = recentLines
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "讀取日誌尾部失敗");
                return StatusCode(500, ApiResponse.ErrorResult(
                    "讀取日誌時發生錯誤",
                    requestId: HttpContext.TraceIdentifier
                ));
            }
        }
    }
} 