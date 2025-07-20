// 檔案上傳控制器 - 提供檔案上傳、列表查詢、刪除等 API 端點
using Microsoft.AspNetCore.Mvc;
using familytree_backend.Models;
using familytree_backend.Services;

namespace familytree_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly FileUploadService _fileUploadService;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(FileUploadService fileUploadService, ILogger<FileUploadController> logger)
        {
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "請選擇要上傳的檔案" });
                }

                var result = await _fileUploadService.UploadFileAsync(file);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案上傳控制器錯誤");
                return StatusCode(500, new { success = false, message = "檔案上傳過程中發生錯誤" });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetFileList()
        {
            try
            {
                var result = await _fileUploadService.GetFileListAsync();

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得檔案列表控制器錯誤");
                return StatusCode(500, new { success = false, message = "取得檔案列表過程中發生錯誤" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            try
            {
                var result = await _fileUploadService.DeleteFileAsync(id);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案刪除控制器錯誤: {FileId}", id);
                return StatusCode(500, new { success = false, message = "檔案刪除過程中發生錯誤" });
            }
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { success = true, message = "檔案上傳服務正常運作", timestamp = DateTime.UtcNow });
        }
    }
} 