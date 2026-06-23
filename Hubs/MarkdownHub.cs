using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MarkdownEditor.Hubs {
    public interface IMarkdownClient {
        Task ReceiveContent(string content);
        Task ReceiveChange(string change);
        Task UserJoined(string connectionId, string username);
        Task UserLeft(string connectionId, string username);
        Task UpdateCursor(string connectionId, int position);
    }

    public class MarkdownHub : Hub<IMarkdownClient> {
        private static readonly ConcurrentDictionary<string, string> _documents = new();
        private static readonly ConcurrentDictionary<string, HashSet<string>> _documentGroups = new();
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();
        private static readonly ConcurrentDictionary<string, string> _connectionDocuments = new();
        private static readonly ConcurrentDictionary<string, int> _userCursors = new();

        public async Task JoinDocument(string documentId, string username) {
            if (string.IsNullOrEmpty(documentId))
                return;

            var connectionId = Context.ConnectionId;

            // Добавляем пользователя в группу документа
            await Groups.AddToGroupAsync(connectionId, documentId);

            // Сохраняем связь пользователя с документом
            _connectionDocuments[connectionId] = documentId;
            _userConnections[connectionId] = username;

            // Добавляем в список участников группы
            if (!_documentGroups.ContainsKey(documentId))
                _documentGroups[documentId] = new HashSet<string>();
            _documentGroups[documentId].Add(connectionId);

            // Отправляем текущее содержимое документа
            if (_documents.TryGetValue(documentId, out var content)) {
                await Clients.Caller.ReceiveContent(content);
            }

            // Уведомляем других пользователей о подключении
            await Clients.OthersInGroup(documentId).UserJoined(connectionId, username);
        }

        public async Task LeaveDocument(string documentId) {
            var connectionId = Context.ConnectionId;

            if (_userConnections.TryGetValue(connectionId, out var username)) {
                await Groups.RemoveFromGroupAsync(connectionId, documentId);
                _documentGroups[documentId]?.Remove(connectionId);

                // Уведомляем других пользователей
                await Clients.OthersInGroup(documentId).UserLeft(connectionId, username);
            }

            _connectionDocuments.TryRemove(connectionId, out _);
            _userConnections.TryRemove(connectionId, out _);
            _userCursors.TryRemove(connectionId, out _);
        }

        //public async Task UpdateContent(string documentId, string change) {
        //    var connectionId = Context.ConnectionId;

        //    if (!_documents.ContainsKey(documentId))
        //        _documents[documentId] = "";

        //    // Применяем изменение к документу
        //    var currentContent = _documents[documentId];
        //    var newContent = ApplyChange(currentContent, change);

        //    // Обновляем документ в базе данных (здесь должна быть логика сохранения в БД)
        //    _documents[documentId] = newContent;

        //    // Отправляем изменение всем клиентам в группе, кроме отправителя
        //    await Clients.OthersInGroup(documentId).ReceiveChange(change);
        //}
        public async Task UpdateContent(string documentId, string newContent) {
            var connectionId = Context.ConnectionId;

            if (!_documents.ContainsKey(documentId))
                _documents[documentId] = "";

            // Обновляем документ в базе данных (здесь должна быть логика сохранения в БД)
            _documents[documentId] = newContent;

            // Отправляем изменение всем клиентам в группе, кроме отправителя
            await Clients.OthersInGroup(documentId).ReceiveContent(newContent);
        }

        public async Task UpdateCursor(string documentId, int position) {
            var connectionId = Context.ConnectionId;
            _userCursors[connectionId] = position;

            await Clients.OthersInGroup(documentId).UpdateCursor(connectionId, position);
        }

        private string ApplyChange(string currentContent, string change) {
            // Формат изменения: "position|character" или "position|backspace" или "position|undo" или "position|redo"
            var parts = change.Split('|');
            if (parts.Length != 2) return currentContent;

            var position = int.Parse(parts[0]);
            var action = parts[1];

            switch (action) {
                case "backspace":
                    if (position > 0 && position <= currentContent.Length)
                        return currentContent.Remove(position - 1, 1);
                    break;
                case "undo":
                    // TODO: Реализовать историю изменений
                    return currentContent;
                case "redo":
                    // TODO: Реализовать историю изменений
                    return currentContent;
                default:
                    // Вставка символа
                    if (position >= 0 && position <= currentContent.Length)
                        return currentContent.Insert(position, action);
                    break;
            }

            return currentContent;
        }

        public override async Task OnDisconnectedAsync(Exception exception) {
            var connectionId = Context.ConnectionId;

            if (_connectionDocuments.TryGetValue(connectionId, out var documentId)) {
                if (_userConnections.TryGetValue(connectionId, out var username)) {
                    await Clients.OthersInGroup(documentId).UserLeft(connectionId, username);
                }

                _documentGroups[documentId]?.Remove(connectionId);
                _connectionDocuments.TryRemove(connectionId, out _);
                _userConnections.TryRemove(connectionId, out _);
                _userCursors.TryRemove(connectionId, out _);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
