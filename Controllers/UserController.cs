using MarkdownEditor.Models;
using MarkdownEditor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MarkdownEditor.Controllers {
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase {
        private readonly ApplicationContext _context;
        private readonly IJwtService _jwtService;

        public UserController(ApplicationContext context, IJwtService jwtService) {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentUsers() {
            if (Request.Cookies.TryGetValue("session", out var session)) {
                int? userId = _jwtService.GetUserIdFromToken(session);
                if (userId == null || !_context.Users.Any(u => u.Id == userId)) {
                    return Unauthorized(new ApiError("Пользователь не авторизован", 401));
                }

                User? existUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (existUser == null) {
                    return BadRequest(new ApiError("Токен неправильного формата", 400));
                }

                var currentUserId = existUser.Id;

                // Пользователи, которым текущий дал доступ
                var usersICollaboratedWith = await _context.DocumentAccesses
                    .Where(da => da.Document.OwnerId == currentUserId)
                    .Select(da => da.User)
                    .Where(u => u != null && u.Id != currentUserId)
                    .Distinct()
                    .ToListAsync();

                // Пользователи, которые дали доступ к текущему
                var usersWhoCollaboratedWithMe = await _context.DocumentAccesses
                    .Where(da => da.UserId == currentUserId)
                    .Select(da => da.Document.Owner)
                    .Where(u => u != null && u.Id != currentUserId)
                    .Distinct()
                    .ToListAsync();

                // Объединение двух списков с удаением дубликатов
                var allRecentUsers = usersICollaboratedWith
                    .Union(usersWhoCollaboratedWithMe)
                    .GroupBy(u => u.Id)
                    .Select(g => g.First())
                    .OrderByDescending(u => u.JoinDate)
                    .Take(10)
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.JoinDate
                    } as object)
                    .ToList();

                return Ok(new ApiResponse<List<object>>(allRecentUsers));

            }

            return Unauthorized(new ApiError("Пользователь не авторизован", 401));

        }

        [HttpGet("search/{query}")]
        public async Task<IActionResult> SearchUser(string query) {
            int limit = 10;

            if (string.IsNullOrEmpty(query)) {
                return BadRequest(new ApiError("Запрос пустой", 400));
            }

            if (Request.Cookies.TryGetValue("session", out var session)) {
                int? userId = _jwtService.GetUserIdFromToken(session);
                if (userId == null || !_context.Users.Any(u => u.Id == userId)) {
                    return Unauthorized(new ApiError("Пользователь не авторизован", 401));
                }

                User? existUser =await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (existUser == null) {
                    return BadRequest(new ApiError("Токен неправильного формата", 400));
                }

                var currentUserId = existUser.Id;

                var users = await _context.Users
                .Where(u => u.Id != currentUserId) // Исключаем текущего пользователя
                .Where(u =>
                    // Поиск по username
                    u.Username != null && u.Username.Contains(query) ||
                    // Поиск по email
                    u.Email.Contains(query) ||
                    // Поиск по имени
                    (u.FirstName != null && u.FirstName.Contains(query)) ||
                    // Поиск по фамилии
                    (u.LastName != null && u.LastName.Contains(query)) ||
                    // Поиск по полному имени (Имя + Фамилия)
                    (u.FirstName != null && u.LastName != null &&
                     (u.FirstName + " " + u.LastName).Contains(query))
                )
                .OrderBy(u => u.Username) // Сортируем по username
                .Take(limit)
                .Select(u => new {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    JoinDate = u.JoinDate
                } as object)
                .ToListAsync();

                return Ok(new ApiResponse<object>(users));
            }

            return Unauthorized(new ApiError("Пользователь не авторизован", 401));
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserData(string username) {
            if (string.IsNullOrWhiteSpace(username)) {
                return BadRequest(new { error = "Ник пользователя объязателен" });
            }

            var userData = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            if (userData == null) {
                return NotFound(new { error = $"Пользователя '{username}' не существует" });
            }

            userData.Password = null;

            return Ok(new ApiResponse<User>(userData));
        }

        [HttpGet("check-access/{documentId}")]
        public async Task<IActionResult> CheckDocumentAccess(int documentId)
        {
            try
            {
                // Проверяем авторизацию
                if (!Request.Cookies.TryGetValue("session", out var session))
                {
                    return Unauthorized(new ApiError("Пользователь не авторизован", 401));
                }

                var userId = _jwtService.GetUserIdFromToken(session);
                if (userId == null)
                {
                    return Unauthorized(new ApiError("Недействительный токен", 401));
                }

                // Проверяем существование документа
                var document = await _context.Documents
                    .Include(d => d.Owner)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return NotFound(new ApiError("Документ не найден", 404));
                }

                // Проверяем права доступа
                var hasAccess = false;
                var accessLevel = 2;

                // Проверяем, является ли пользователь владельцем
                if (document.OwnerId == userId)
                {
                    hasAccess = true;
                    accessLevel = 1;
                }
                else
                {
                    // Проверяем, есть ли у пользователя доступ к документу
                    var access = await _context.DocumentAccesses
                        .FirstOrDefaultAsync(a => a.DocumentId == documentId && a.UserId == userId);

                    if (access != null)
                    {
                        hasAccess = true;
                        accessLevel = access.AccessLevel;
                    }
                }

                if (!hasAccess)
                {
                    // Возвращаем 403 статус без использования Forbidden()
                    return StatusCode(403, new ApiError("У вас нет доступа к этому документу", 403));
                }

                // Возвращаем информацию о документе и правах
                return Ok(new ApiResponse<object>(new
                {
                    documentId = document.Id,
                    title = document.Title,
                    content = document.Content ?? "",
                    lastUpdated = document.LastUpdated,
                    createdAt = document.CreatedAt,
                    owner = new
                    {
                        document.Owner.Id,
                        document.Owner.Username,
                        document.Owner.FirstName,
                        document.Owner.LastName,
                        document.Owner.Email
                    },
                    accessLevel = accessLevel,
                    isOwner = document.OwnerId == userId
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiError($"Ошибка при проверке доступа: {ex.Message}", 500));
            }
        }

        private async Task<User?> GetUser(Expression<Func<User, bool>> func) {
            User? user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(func);

            return user;
        }
    }
}
