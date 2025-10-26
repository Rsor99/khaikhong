using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Services.Models;

namespace Khaikhong.Application.Features.Authentication;

internal static class AuthResultExtensions
{
    public static ApiResponse<object> ToApiResponse(this AuthResult result)
    {
        bool isSuccess = result.Status is >= 200 and < 400;
        object data = BuildData(result.Data);

        return isSuccess
            ? ApiResponse<object>.Success(result.Status, result.Message, data)
            : ApiResponse<object>.Fail(result.Status, result.Message, errors: data);
    }

    private static object BuildData(AuthResultData data)
    {
        if (!string.IsNullOrWhiteSpace(data.AccessToken) && !string.IsNullOrWhiteSpace(data.RefreshToken))
        {
            return new
            {
                accessToken = data.AccessToken,
                refreshToken = data.RefreshToken
            };
        }

        if (!string.IsNullOrWhiteSpace(data.Message))
        {
            return new
            {
                message = data.Message
            };
        }

        return new { };
    }
}
