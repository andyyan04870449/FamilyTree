// 日誌控制器：提供查看分析日誌的 API 端點
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace familytree_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogController : ControllerBase
    {
        private readonly ILogger<LogController> _logger;
        private readonly IWebHostEnvironment _environment;

        public LogController(ILogger<LogController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
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
                    return NotFound(new { 
                        success = false, 
                        message = $"找不到日誌檔案: {logFileName}" 
                    });
                }

                var logLines = System.IO.File.ReadAllLines(logFilePath);
                var recentLines = logLines.TakeLast(lines).ToArray();
                
                return Ok(new { 
                    success = true, 
                    data = new {
                        fileName = logFileName,
                        totalLines = logLines.Length,
                        requestedLines = lines,
                        actualLines = recentLines.Length,
                        logs = recentLines
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取分析日誌失敗");
                return StatusCode(500, new { 
                    success = false, 
                    message = "讀取日誌時發生錯誤" 
                });
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
                
                return Ok(new { 
                    success = true, 
                    data = new { files = logFiles }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取日誌檔案列表失敗");
                return StatusCode(500, new { 
                    success = false, 
                    message = "獲取日誌檔案列表時發生錯誤" 
                });
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
                    return NotFound(new { 
                        success = false, 
                        message = "找不到日誌檔案" 
                    });
                }

                var logLines = System.IO.File.ReadAllLines(logFilePath);
                var recentLines = logLines.TakeLast(lines).ToArray();
                
                return Ok(new { 
                    success = true, 
                    data = new {
                        totalLines = logLines.Length,
                        recentLines = recentLines
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取日誌尾部失敗");
                return StatusCode(500, new { 
                    success = false, 
                    message = "讀取日誌時發生錯誤" 
                });
            }
        }
    }
} 