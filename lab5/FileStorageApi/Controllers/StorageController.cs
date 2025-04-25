using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json;

[ApiController]
public class FileStorageController : ControllerBase
{
    private IWebHostEnvironment _environment;
    private ILogger<FileStorageController> _logger;
    private string BaseStoragePath = "Storage";

    public FileStorageController(IWebHostEnvironment environment, ILogger<FileStorageController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpPut("{*filePath}")]
    public async Task<IActionResult> UploadFileWithOverwrite(string filePath)
    {
        try
        {
            if (Request.Headers.TryGetValue("X-Copy-From", out var copyFromHeader))
            {
                var sourcePath = copyFromHeader.ToString();
                sourcePath = sourcePath.TrimStart('/');
                var sourceFullPath = Path.Combine(_environment.ContentRootPath, BaseStoragePath, sourcePath);
                var destFullPath = Path.Combine(_environment.ContentRootPath, BaseStoragePath, filePath);

                if (sourceFullPath == null || destFullPath == null)
                    return BadRequest("Invalid path");

                if (!System.IO.File.Exists(sourceFullPath))
                    return NotFound("Source file not found");
                if (!Directory.Exists(Path.GetDirectoryName(destFullPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(destFullPath));

                System.IO.File.Copy(sourceFullPath, destFullPath, overwrite: true);

                _logger.LogInformation($"File copied from {sourcePath} to {filePath}");
                return Ok($"File copied from {sourcePath} to {filePath}");
            }
            else
            {
                if (Request.Body == null || Request.ContentLength == 0)
                    return BadRequest("No file content received");
                var fullPath = Path.Combine(_environment.ContentRootPath, BaseStoragePath, filePath);
                _logger.LogInformation(fullPath);
                if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                await using (var fileStream = System.IO.File.Create(fullPath))
                {
                    await Request.Body.CopyToAsync(fileStream);
                }
                _logger.LogInformation($"File {filePath} uploaded/overwritten successfully");
                return Ok($"File {filePath} uploaded/overwritten successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading file to {filePath}");
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

  /*  private async Task<IActionResult> CopyFile(string destinationPath, string sourcePath)
    {
        try
        {
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error copying file from {sourcePath} to {destinationPath}");
            return StatusCode(500, ex.Message);
        }
    }
*/

    [HttpHead("{*filePath}")]
    public IActionResult GetFileInfo(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.ContentRootPath, BaseStoragePath, filePath);
            if (fullPath == null || !System.IO.File.Exists(fullPath))
                return NotFound();

            var fileInfo = new FileInfo(fullPath);
            Response.Headers.Append("File-Size", fileInfo.Length.ToString());
            Response.Headers.Append("Last-Modified", fileInfo.LastWriteTimeUtc.ToString("R"));
            Response.Headers.Append("Content-Type", GetContentType(fullPath));

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting file info {filePath}");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{*path}")]
    public IActionResult Delete(string path)
    {
        try
        {
            var fullPath = Path.Combine(_environment.ContentRootPath, BaseStoragePath,path);
            if (fullPath == null)
                return BadRequest("Invalid path");

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                _logger.LogInformation($"File {path} deleted");
                return Ok($"200 File {path} deleted successfully");
            }

            if (Directory.Exists(fullPath))
            {
                if (Directory.GetFileSystemEntries(fullPath).Length > 0)
                    return BadRequest("Directory is not empty");

                Directory.Delete(fullPath);
                _logger.LogInformation($"Directory {path} deleted");
                return Ok($"Directory {path} deleted successfully");
            }

            return NotFound("404 Not Found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting {path}");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("list/{*directoryPath}")]
    public IActionResult ListFiles(string directoryPath = "")
{
    try
    {
        var fullPath = Path.Combine(_environment.ContentRootPath, BaseStoragePath, directoryPath);
        fullPath = Path.GetFullPath(fullPath);
        if (!fullPath.StartsWith(Path.Combine(_environment.ContentRootPath, BaseStoragePath)))
            return BadRequest("Invalid path");

        if (!Directory.Exists(fullPath))
            return NotFound();

        var directoryInfo = new DirectoryInfo(fullPath);
       
        var files = directoryInfo.GetFiles()
            .Select(f => new {
                Name = f.Name,
                Path = Path.Combine(directoryPath, f.Name),
                Size = f.Length,
                LastModified = f.LastWriteTimeUtc,
                IsDirectory = false
            });

        var directories = directoryInfo.GetDirectories()
            .Select(d => new {
                Name = d.Name,
                Path = Path.Combine(directoryPath, d.Name),
                Size = 0L,
                LastModified = d.LastWriteTimeUtc,
                IsDirectory = true
            });

        var result = files.Concat(directories).ToList();
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return Content(
                JsonSerializer.Serialize(result, jsonOptions),
                "application/json; charset=utf-8"
            );
        }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error listing directory {directoryPath}");
        return StatusCode(500, ex.Message);
    }
}

    [HttpGet("{*filePath}")]
    public IActionResult GetFile(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.ContentRootPath, BaseStoragePath, filePath);
            if (fullPath == null || !System.IO.File.Exists(fullPath))
                return NotFound();

            var fileStream = System.IO.File.OpenRead(fullPath);
            return File(fileStream, GetContentType(fullPath), Path.GetFileName(fullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting file {filePath}");
            return StatusCode(500, ex.Message);
        }
    }

    private string GetContentType(string filePath)
    {
        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }

}