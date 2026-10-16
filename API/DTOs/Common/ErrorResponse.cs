namespace API.DTOs.Common
{
    public class ErrorResponse
    {
        public required ErrorBody Error { get; set; }
    }

    public class ErrorBody
    {
        public required string Code { get; set; }
        public required string Message { get; set; }
    }
}
