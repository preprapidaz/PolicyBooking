namespace PolicyService.API.Models
{
    /// <summary>
    /// Standard success response wrapper
    /// Provides consistent response structure across all endpoints
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public ResponseMetadata Metadata { get; set; }
        public List<string> Messages { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Messages = message != null ? new List<string> { message } : new List<string>(),
                Metadata = new ResponseMetadata
                {
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0"
                }
            };
        }

        public static ApiResponse<T> SuccessResponseWithMetadata(
            T data,
            int totalCount,
            int page,
            int pageSize)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Messages = new List<string>(),
                Metadata = new ResponseMetadata
                {
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0",
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            };
        }
    }

    public class ResponseMetadata
    {
        public DateTime Timestamp { get; set; }
        public string Version { get; set; }
        public int? TotalCount { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int? TotalPages { get; set; }
        public string CorrelationId { get; set; }
    }
}