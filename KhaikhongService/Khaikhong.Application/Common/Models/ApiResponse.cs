namespace Khaikhong.Application.Common.Models;

public sealed class ApiResponse<T>
{
    public int Status { get; }

    public string Message { get; }

    public T Data { get; }

    private ApiResponse(int status, string message, T data)
    {
        Status = status;
        Message = message;
        Data = data;
    }

    public static ApiResponse<T> Success(int status, string message, T data) =>
        new(status, message, data);

    public static ApiResponse<T> Fail(int status, string message, object? data = null)
    {
        T payload;

        if (data is null)
        {
            payload = typeof(T).IsValueType ? Activator.CreateInstance<T>() : default!;
        }
        else if (data is T typedData)
        {
            payload = typedData;
        }
        else if (typeof(T).IsAssignableFrom(data.GetType()))
        {
            payload = (T)data;
        }
        else
        {
            throw new InvalidCastException($"Cannot convert data of type '{data.GetType()}' to '{typeof(T)}'.");
        }

        return new ApiResponse<T>(status, message, payload);
    }
}
