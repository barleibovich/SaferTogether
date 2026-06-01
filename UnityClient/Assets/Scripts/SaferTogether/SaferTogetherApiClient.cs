using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SaferTogether.UnityClient
{
    /// <summary>
    /// Small coroutine-based API client for the SaferTogether gateway.
    /// </summary>
    public sealed class SaferTogetherApiClient
    {
        private string accessToken;

        public string GatewayBaseUrl;

        /// <summary>
        /// This function creates an API client with the selected gateway URL.
        /// </summary>
        public SaferTogetherApiClient(string gatewayBaseUrl)
        {
            GatewayBaseUrl = string.IsNullOrEmpty(gatewayBaseUrl)
                ? "http://localhost:5173"
                : TrimTrailingSlash(gatewayBaseUrl);
        }

        /// <summary>
        /// This function signs up a user and stores the returned bearer token.
        /// </summary>
        public IEnumerator SignUp(string username, string password, string role, string avatar, Action<UserProfile> onSuccess, Action<string> onError)
        {
            var body = new SignUpRequest
            {
                username = username,
                password = password,
                role = role,
                avatar = avatar
            };

            yield return SendJson<AuthResponse>("/api/auth/signup", "POST", body, response =>
            {
                SaveAccessToken(response.accessToken);
                onSuccess?.Invoke(response.profile);
            }, onError);
        }

        /// <summary>
        /// This function logs in a user and stores the returned bearer token.
        /// </summary>
        public IEnumerator Login(string username, string password, Action<UserProfile> onSuccess, Action<string> onError)
        {
            var body = new LoginRequest
            {
                username = username,
                password = password
            };

            yield return SendJson<AuthResponse>("/api/auth/login", "POST", body, response =>
            {
                SaveAccessToken(response.accessToken);
                onSuccess?.Invoke(response.profile);
            }, onError);
        }

        /// <summary>
        /// This function updates the current user's avatar.
        /// </summary>
        public IEnumerator UpdateAvatar(string avatar, Action<UserProfile> onSuccess, Action<string> onError)
        {
            var body = new AvatarUpdateRequest
            {
                avatar = avatar
            };

            yield return SendJson<ProfileResponse>("/api/auth/profile", "PATCH", body, response =>
            {
                onSuccess?.Invoke(response.profile);
            }, onError);
        }

        /// <summary>
        /// This function logs out and clears local auth state.
        /// </summary>
        public IEnumerator Logout(Action onSuccess, Action<string> onError)
        {
            yield return SendJson<LogoutResponse>("/api/auth/logout", "POST", null, _ =>
            {
                accessToken = "";
                onSuccess?.Invoke();
            }, onError);
        }

        /// <summary>
        /// This function stores a bearer token returned by the gateway.
        /// </summary>
        private void SaveAccessToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                accessToken = token;
            }
        }

        /// <summary>
        /// This function sends JSON to the gateway and parses the JSON response.
        /// </summary>
        private IEnumerator SendJson<TResponse>(string path, string method, object body, Action<TResponse> onSuccess, Action<string> onError)
        {
            var request = new UnityWebRequest(CombineUrl(GatewayBaseUrl, path), method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (body != null)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(UnityEngine.JsonUtility.ToJson(body));
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            ApplyAuthHeaders(request);
            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (IsFailure(request))
            {
                onError?.Invoke(GetErrorMessage(responseText, request.error));
                request.Dispose();
                yield break;
            }

            TResponse response = string.IsNullOrEmpty(responseText)
                ? default(TResponse)
                : UnityEngine.JsonUtility.FromJson<TResponse>(responseText);

            onSuccess?.Invoke(response);
            request.Dispose();
        }

        /// <summary>
        /// This function applies bearer auth headers to a Unity request.
        /// </summary>
        private void ApplyAuthHeaders(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            }
        }

        /// <summary>
        /// This function checks whether a Unity request failed.
        /// </summary>
        private static bool IsFailure(UnityWebRequest request)
        {
#if UNITY_2020_1_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.ProtocolError
                || request.result == UnityWebRequest.Result.DataProcessingError;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

        /// <summary>
        /// This function extracts a readable gateway error message.
        /// </summary>
        private static string GetErrorMessage(string responseText, string fallback)
        {
            if (!string.IsNullOrEmpty(responseText))
            {
                ErrorResponse response = UnityEngine.JsonUtility.FromJson<ErrorResponse>(responseText);

                if (!string.IsNullOrEmpty(response.error))
                {
                    return response.error;
                }
            }

            return string.IsNullOrEmpty(fallback) ? "Request failed" : fallback;
        }

        /// <summary>
        /// This function combines the gateway base URL with an API path.
        /// </summary>
        private static string CombineUrl(string baseUrl, string path)
        {
            return TrimTrailingSlash(baseUrl) + "/" + path.TrimStart('/');
        }

        /// <summary>
        /// This function removes trailing slashes from a URL.
        /// </summary>
        private static string TrimTrailingSlash(string value)
        {
            return value.TrimEnd('/');
        }
    }
}
