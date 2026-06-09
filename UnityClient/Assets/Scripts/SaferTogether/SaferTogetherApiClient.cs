using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SaferTogether.UnityClient
{
    // little coroutine api client for talking to the gateway
    public sealed class SaferTogetherApiClient
    {
        private string accessToken;

        public string GatewayBaseUrl;

        // set up the client with whatever gateway url we got
        public SaferTogetherApiClient(string gatewayBaseUrl)
        {
            GatewayBaseUrl = string.IsNullOrEmpty(gatewayBaseUrl)
                ? "http://localhost:5173"
                : TrimTrailingSlash(gatewayBaseUrl);
        }

        // sign up a user + save the token we get back
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

        // log in + save the token we get back
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

        // update my avatar
        public IEnumerator UpdateAvatar(string avatar, string avatarImage, Action<UserProfile> onSuccess, Action<string> onError)
        {
            var body = new AvatarUpdateRequest
            {
                avatar = avatar,
                avatarImage = avatarImage
            };

            yield return SendJson<ProfileResponse>("/api/auth/profile", "PATCH", body, response =>
            {
                onSuccess?.Invoke(response.profile);
            }, onError);
        }

        // log out and wipe the local token
        public IEnumerator Logout(Action onSuccess, Action<string> onError)
        {
            yield return SendJson<LogoutResponse>("/api/auth/logout", "POST", null, _ =>
            {
                accessToken = "";
                onSuccess?.Invoke();
            }, onError);
        }

        // stash the bearer token if it's not empty
        private void SaveAccessToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                accessToken = token;
            }
        }

        // POST/PATCH json to the gateway and parse the json back
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

        // slap the bearer token onto the request if we have one
        private void ApplyAuthHeaders(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            }
        }

        // did the request fail?
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

        // pull out a nice error message from the response
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

        // glue the base url and the api path together
        private static string CombineUrl(string baseUrl, string path)
        {
            return TrimTrailingSlash(baseUrl) + "/" + path.TrimStart('/');
        }

        // chop trailing slashes off a url
        private static string TrimTrailingSlash(string value)
        {
            return value.TrimEnd('/');
        }
    }
}
