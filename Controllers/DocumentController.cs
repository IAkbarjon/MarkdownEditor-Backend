using MarkdownEditor.Models;
using MarkdownEditor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MarkdownEditor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase {
        private readonly ApplicationContext _context;
        private readonly IJwtService _jwtService;

        public DocumentController(ApplicationContext context, IJwtService jwtService) {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetDocuments() {
            if (Request.Cookies.TryGetValue("session", out var session)) {
                int? userId = _jwtService.GetUserIdFromToken(session);
                if (userId == null) {
                    return BadRequest(new ApiError("Неправильный формат токена", 400));
                }

                var documents = await _context.Documents
                    .Include(doc => doc.DocumentAccesses)
                        .ThenInclude(acc => acc.User)
                    .Where(doc => doc.OwnerId == userId)
                    .ToListAsync();
                
                return Ok(new ApiResponse<List<Document>>(documents));
            }

            return Unauthorized(new ApiError("Пользователь не авторизован", 401));
        }

        [HttpPost]
        public async Task<IActionResult> CreateDocument([FromBody] Document newDocument) {
            if (Request.Cookies.TryGetValue("session", out var session)) {
                int? userId = _jwtService.GetUserIdFromToken(session);
                if (userId == null) {
                    return BadRequest(new ApiError("Неправильный формат токена", 400));
                }

                var user = await GetUser(u => u.Id == userId);

                if (user == null) {
                    return NotFound(new ApiError("Пользователя не существует", 404));
                }

                if (user.Documents?.Any(d => d.Title.Trim() ==  newDocument.Title.Trim()) ?? false) {
                    return BadRequest(new ApiError("Документ с таким названием уже существует", 401));
                }

                _context.Documents?.Add(newDocument);

                await _context.SaveChangesAsync();

                Document? createdDocument = await GetDocument(d => d.Id == newDocument.Id);

                return Created(Request.Path.Value, new ApiResponse<Document>(createdDocument!, 201));
            }

            return Unauthorized(new ApiError("Пользователь не авторизован", 401));
        }

        [HttpPatch("rename/{id}/{title}")]
        public async Task<IActionResult> RenameDocument(int id, string title) {
            if (string.IsNullOrEmpty(title)) {
                return BadRequest(new ApiError("Получено неправильное имя для документа", 400));
            }

            var document = await _context.Documents.FirstOrDefaultAsync(doc => doc.Id == id);
            if (document == null) {
                return BadRequest(new ApiError("Такого документа не существует", 400));
            }

            document.Title = title;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<Document>(document));
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteDocument(int id) {
            var document = await _context.Documents.FindAsync(id);

            if (document == null) {
                return BadRequest(new ApiError("Ошибка запроса", 400, "Отсутствует параметр 'id' в запросе"));
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("access")]
        public async Task<IActionResult> GetAccesses() {
            if (Request.Cookies.TryGetValue("session", out var session)) {
                int? userId = _jwtService.GetUserIdFromToken(session);
                if (userId == null) {
                    return BadRequest(new ApiError("Неправильный формат токена", 400));
                }

                var user = await GetUser(u => u.Id == userId);

                if (user == null) {
                    return NotFound(new ApiError("Пользователя не существует", 404));
                }

                var accesses = await _context.DocumentAccesses
                    .Include(acc => acc.Document)
                        .ThenInclude(doc => doc.Owner)
                    .Where(acc => acc.UserId == userId)
                    .Select(acc => new {
                        acc.Id,
                        acc.DocumentId,
                        acc.UserId,
                        Document = new {
                            acc.Document.Id,
                            acc.Document.Title,
                            acc.Document.LastUpdated
                        },
                        Owner = new {
                            acc.Document.Owner.Username,
                            acc.Document.Owner.FirstName,
                            acc.Document.Owner.LastName,
                            acc.Document.Owner.Email
                        },
                        acc.AccessLevel
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>(accesses));
            }

            return Unauthorized(new ApiError("Пользователь не авторизован", 401));
        }

        [HttpPost("access")]
        public async Task<IActionResult> CreateAccess([FromBody] DocumentAccess access) {
            if (access == null) {
                return BadRequest(new ApiError("Нет тела запроса", 400));
            }

            if (_context.DocumentAccesses.Any(da => da.UserId == access.UserId && da.DocumentId == access.DocumentId)) {
                return Ok(new ApiResponse<object>(new { message = "Данный пользователь уже имеет доступ к документу" }));
            }

            await _context.DocumentAccesses.AddAsync(access);
            await _context.SaveChangesAsync();

            var newAccess = await _context.DocumentAccesses
                .Include(acc => acc.User)
                .FirstAsync(da => da.UserId == access.UserId && da.DocumentId == access.DocumentId);

            return Created(Request.Path, new ApiResponse<object>(newAccess));
        }

        [HttpDelete("access/{accessId}")]
        public async Task<IActionResult> RemoveAccess(int accessId) {
            var access = await _context.DocumentAccesses.FindAsync(accessId);

            if (access == null) {
                return BadRequest(new ApiError("Ошибка запроса", 400, "Не хватает параметра в запросе"));
            }

            _context.DocumentAccesses.Remove(access);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<User?> GetUser(Expression<Func<User, bool>> func) {
            User? user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Documents)!
                    .ThenInclude(d => d.DocumentAccesses)
                .Include(u => u.Documents)!
                    .ThenInclude(d => d.DocumentVersions)
                .Include(u => u.AccessToDocuments)!
                    .ThenInclude(a => a.Document)
                .FirstOrDefaultAsync(func);

            return user;
        }

        private async Task<Document?> GetDocument(Expression<Func<Document, bool>> func) {
            Document? doc = await _context.Documents
                .AsNoTracking()
                .Include(d => d.Owner)!
                .Include(d => d.DocumentAccesses)!
                .Include(d => d.DocumentVersions)!
                .FirstOrDefaultAsync(func);

            return doc;
        }
    }
}
