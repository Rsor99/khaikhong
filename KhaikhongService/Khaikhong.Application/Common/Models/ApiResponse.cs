using System.Text.Json.Serialization;

namespace Khaikhong.Application.Common.Models;

public sealed class ApiResponse<T>
{
    public int Status { get; }

    public string Message { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Errors { get; }

    public bool IsSuccess { get; }

    private ApiResponse(int status, string message, bool isSuccess, T? data, object? errors)
    {
        Status = status;
        Message = message;
        IsSuccess = isSuccess;
        Data = data;
        Errors = errors;
    }

    public static ApiResponse<T> Success(int status, string message, T data) =>
        new(status, message, true, data, null);

    public static ApiResponse<T> Fail(int status, string message, object? errors = null, T? data = default) =>
        new(status, message, false, data, errors);
}
