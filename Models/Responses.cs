namespace MarkdownEditor.Models
{
    public abstract class BaseResponse
    {
        public int Status { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class ApiError : BaseResponse
    {
        public string Message { get; set; }
        public string? Details { get; set; }

        public ApiError(string message, int status=500, string? details=null)
        {
            Status = status;
            Message = message;
            Details = details;
        }
    }

    public class ApiResponse<T> : BaseResponse
    {
        public bool Success { get; set; } = true;
        public T Data { get; set; }

        public ApiResponse(T data, int status=200, bool success=true)
        {
            Data = data;
            Status = status;
            Success = success;
        }
    }
}
