using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;
using UnivFI.Infrastructure.Services;

namespace UnivFI.WebUI.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        private readonly S3Helper _s3Helper;
        private readonly IFileAttachRepository _fileAttachRepository;
        private readonly ILogger<FileController> _logger;

        public FileController(
            S3Helper s3Helper,
            IFileAttachRepository fileAttachRepository,
            ILogger<FileController> logger)
        {
            _s3Helper = s3Helper;
            _fileAttachRepository = fileAttachRepository;
            _logger = logger;
        }

        /// <summary>
        /// 파일 관리 메인 화면을 표시합니다.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var files = await _fileAttachRepository.GetAllFileAttachesAsync();
                return View(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "파일 목록 조회 중 오류가 발생했습니다.");
                TempData["ErrorMessage"] = "파일 목록을 불러오는 중 오류가 발생했습니다.";
                return View(new List<FileAttachEntity>());
            }
        }

        /// <summary>
        /// 파일 업로드 화면을 표시합니다.
        /// </summary>
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        /// <summary>
        /// 파일을 업로드합니다.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    ModelState.AddModelError("", "파일이 전송되지 않았습니다.");
                    return View();
                }

                // 파일 확장자 확인
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                // 허용된 파일 타입
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };

                if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
                {
                    ModelState.AddModelError("", "허용되지 않는 파일 형식입니다.");
                    return View();
                }

                // 파일 크기 제한 (10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "파일 크기는 10MB 이하여야 합니다.");
                    return View();
                }

                // 원본 파일명 저장
                string originalFileName = file.FileName;

                // GUID 생성 및 S3에 저장할 파일명 설정 (GUID.확장자)
                var fileGuid = Guid.NewGuid().ToString();
                string s3FileName = $"{fileGuid}{fileExtension}";

                // 폴더 구조: uploads/[year]/[month]/[day]/
                var folder = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}";

                // 스트림으로 업로드
                using var stream = file.OpenReadStream();

                // S3에 GUID 파일명으로 업로드
                var fileUrl = await _s3Helper.UploadStreamAsync(s3FileName, stream, folder);

                // DB에 파일 정보 저장 (원본 파일명 포함)
                var fileAttach = new FileAttachEntity
                {
                    FilePath = folder,
                    FileName = originalFileName,
                    FileType = fileExtension,
                    FileLength = file.Length,
                    FileGUID = fileGuid + fileExtension, // S3에 저장된 파일명 (GUID.확장자)                    
                };

                await _fileAttachRepository.SaveFileAttachAsync(fileAttach);

                TempData["SuccessMessage"] = "파일이 성공적으로 업로드되었습니다.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "파일 업로드 중 오류 발생: {FileName}", file?.FileName);
                ModelState.AddModelError("", "파일 업로드 중 오류가 발생했습니다.");
                return View();
            }
        }

        /// <summary>
        /// 파일 상세 정보를 표시합니다.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(long id)
        {
            try
            {
                var fileAttach = await _fileAttachRepository.GetFileAttachAsync(id);
                if (fileAttach == null)
                {
                    return NotFound();
                }

                return View(fileAttach);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "파일 상세 정보 조회 중 오류 발생: {FileAttachId}", id);
                TempData["ErrorMessage"] = "파일 정보를 불러오는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 파일을 다운로드합니다.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Download(long id)
        {
            try
            {
                var fileAttach = await _fileAttachRepository.GetFileAttachAsync(id);
                if (fileAttach == null)
                {
                    return NotFound();
                }

                // S3에서 파일 다운로드 - folder와 GUID 파일명으로 다운로드
                // BuildKey 함수는 내부적으로 folder와 fileName을 조합하므로 여기서는 분리해서 전달
                var fileBytes = await _s3Helper.DownloadBytesAsync(fileAttach.FileGUID, fileAttach.FilePath);

                // 파일 이름에서 MIME 타입 추정
                var contentType = GetContentType(fileAttach.FileName);

                // 원본 파일명으로 다운로드 제공
                return File(fileBytes, contentType, fileAttach.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "파일 다운로드 중 오류 발생: {FileAttachId}", id);
                TempData["ErrorMessage"] = "파일 다운로드 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 파일 삭제 확인 화면을 표시합니다.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var fileAttach = await _fileAttachRepository.GetFileAttachAsync(id);
                if (fileAttach == null)
                {
                    return NotFound();
                }

                return View(fileAttach);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "파일 삭제 화면 로드 중 오류 발생: {FileAttachId}", id);
                TempData["ErrorMessage"] = "파일 정보를 불러오는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 파일을 삭제합니다.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var fileAttach = await _fileAttachRepository.GetFileAttachAsync(id);
                if (fileAttach == null)
                {
                    return NotFound();
                }

                // S3에서 파일 삭제
                var s3Result = await _s3Helper.DeleteFileAsync(fileAttach.FileGUID, fileAttach.FilePath);

                // DB에서 파일 정보 삭제
                var dbResult = await _fileAttachRepository.DeleteFileAttachAsync(id);

                if (s3Result && dbResult)
                {
                    TempData["SuccessMessage"] = "파일이 성공적으로 삭제되었습니다.";
                }
                else
                {
                    TempData["WarningMessage"] = "파일 삭제 중 일부 작업이 실패했습니다.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "파일 삭제 중 오류 발생: {FileAttachId}", id);
                TempData["ErrorMessage"] = "파일 삭제 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 파일의 공유 URL을 생성하고 표시하는 화면을 표시합니다.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Share(long id)
        {
            try
            {
                var fileAttach = await _fileAttachRepository.GetFileAttachAsync(id);
                if (fileAttach == null)
                {
                    return NotFound();
                }

                // 미리 서명된 URL 생성 (기본 60분)
                var url = _s3Helper.GetPreSignedUrl(fileAttach.FileGUID, 60, fileAttach.FilePath);

                ViewBag.ShareUrl = url;
                ViewBag.ExpiryMinutes = 60;

                return View(fileAttach);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "파일 공유 URL 생성 중 오류 발생: {FileAttachId}", id);
                TempData["ErrorMessage"] = "파일 공유 URL을 생성하는 중 오류가 발생했습니다.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// 파일 확장자를 기반으로 MIME 타입을 반환합니다.
        /// </summary>
        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}