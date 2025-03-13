using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace api_mmo.Rest;
    public class RestClient : IDisposable
    {
        private readonly List<KeyValuePair<string, string>> _postParams = [];
        private readonly List<KeyValuePair<string, string>> _getParams = [];
        private bool _clearAuth = false;
        private bool _disposedValue;

        private static readonly List<Func<RequestInfo, RestDelegate, Task<RequestResult>>> _staticMiddlewares = [];
        private readonly List<Func<RequestInfo, RestDelegate, Task<RequestResult>>> _instanceMiddlewares = [];


        public JsonSerializerOptions SerializerOptions { get; set; } = new()
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public HttpClient Client { get; private set; }
        public SocketsHttpHandler ClientHandler { get; private set; }

        public CookieContainer CookieContainer { get; private set; }

        /// <summary>
        /// The data used by the RestClient to make the call. Any modifications to the cookies and headers may be saved for the lifetime of this instance.
        /// Checking for existing cookies and headers is recommended before adding a new one
        /// </summary>
        public struct RequestInfo
        {
            public RequestInfo(HttpMethod method, string url, HttpContent? content, HttpRequestHeaders headers, CookieContainer cookieContainer)
            {
                Method = method;
                Url = url;
                Content = content;
                Headers = headers;
                CookieContainer = cookieContainer;
            }

            public HttpMethod Method { get; private set; }
            public string Url { get; set; }
            public HttpContent? Content { get; set; }
            public HttpRequestHeaders Headers { get; private set; }
            public CookieContainer CookieContainer { get; private set; }
        }


        public struct RequestResult(HttpResponseMessage? httpResponse, JsonSerializerOptions serializerOptions, RestStatusCode? errorCode = null)
    {
            private HttpResponseMessage? _httpResponse = httpResponse;
            private readonly RestStatusCode? _errorCode = errorCode;
            public readonly RestStatusCode StatusCode
            {
                get
                {
                    if (_errorCode != null)
                        return (RestStatusCode)_errorCode;

                    if (_httpResponse != null)
                        return (RestStatusCode)Response.StatusCode;

                    return RestStatusCode.ClientTimeout;
                }
            }
            public readonly bool IsSuccessStatusCode => ((int)StatusCode >= 200) && ((int)StatusCode <= 299);
            public HttpResponseMessage Response {
                readonly get
                {
                    return _httpResponse ?? new(HttpStatusCode.RequestTimeout);
                }
                set
                {
                    _httpResponse = value;
                }
            }

        private JsonSerializerOptions SerializerOptions { get; set; } = serializerOptions;

        public async readonly Task<T?> Deserialize<T>()
            {
                if (Response == null)
                    return default;
                return JsonSerializer.Deserialize<T>(await Response.Content.ReadAsStreamAsync(), SerializerOptions);
            }

            public async readonly Task<string> AsString()
            {
                if (Response == null)
                    return "";
                return await Response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Creates a new Rest client that abstracts data serialization and more
        /// </summary>
        /// <param name="connectionTimeout">Optionally set the initial connection timeout (2 seconds by default)</param>
        /// <param name="requestTimeout">Optionally set the overall request timeout (100 seconds by default)</param>
        /// <param name="allowAutoRedirect">Allow the client to automatically follow redirects</param>
        public RestClient(TimeSpan? connectionTimeout, TimeSpan? requestTimeout, bool allowAutoRedirect = true)
        {
            _instanceMiddlewares.AddRange(_staticMiddlewares);
            CookieContainer = new CookieContainer();

            ClientHandler = new SocketsHttpHandler() {
                AllowAutoRedirect= allowAutoRedirect,
                CookieContainer = CookieContainer,
                UseCookies = true,
                ConnectTimeout = connectionTimeout ?? TimeSpan.FromSeconds(2)
            };

            Client = new HttpClient(ClientHandler) { Timeout = requestTimeout ?? TimeSpan.FromSeconds(100) };

            //Defaults
            Client.DefaultRequestHeaders.Add("User-Agent", "RedoDMS Client C# .NET");
        }

        /// <summary>
        /// Creates a new Rest client that abstracts data serialization and more
        /// </summary>
        /// <param name="connectionTimeoutInMs">Optionally set the initial connection timeout (2 seconds by default)</param>
        /// <param name="requestTimeoutInMs">Optionally set the overall request timeout (100 seconds by default)</param>
        /// <param name="allowAutoRedirect">Allow the client to automatically follow redirects</param>
        public RestClient(double connectionTimeoutInMs = 2000, double requestTimeoutInMs = 100000, bool allowAutoRedirect = true)
        {
            _instanceMiddlewares.AddRange(_staticMiddlewares);
            CookieContainer = new CookieContainer();
            ClientHandler = new SocketsHttpHandler()
            {
                AllowAutoRedirect = allowAutoRedirect,
                CookieContainer = CookieContainer,
                UseCookies = true,
                ConnectTimeout = TimeSpan.FromMilliseconds(connectionTimeoutInMs)
            };

            Client = new HttpClient(ClientHandler) { Timeout = TimeSpan.FromMilliseconds(requestTimeoutInMs) };

            //Defaults
            Client.DefaultRequestHeaders.Add("User-Agent", "RedoDMS Client C# .NET");

        }

        /// <summary>
        /// Add a static middleware that executes in the order added
        /// </summary>
        /// <param name="middleware">The middleware function to run</param>
        public static void UseStatic(Func<RequestInfo, RestDelegate, Task<RequestResult>> middleware)
        {
            _staticMiddlewares.Add(middleware);
        }

        /// <summary>
        /// Add a middleware to this instance of RestClient that executes in the order added (After static middlewares)
        /// </summary>
        /// <param name="middleware">The middleware function to run</param>
        public void Use(Func<RequestInfo, RestDelegate, Task<RequestResult>> middleware)
        {
            _instanceMiddlewares.Add(middleware);
        }

        /// <summary>
        /// Add a middleware of type IRestClientMiddleware to the call chain. Static middlewares are used on all requests
        /// </summary>
        /// <typeparam name="TRestMiddleware">The middleware class (must inherit IRestClientMiddleware)</typeparam>
        /// <exception cref="ArgumentException">The class does not inherit from IRestClientMiddleware</exception>
        public static void UseStaticMiddleware<TRestMiddleware>()
        {
            UseStaticMiddleware(typeof(TRestMiddleware));
        }

        /// <summary>
        /// Add a middleware of type IRestClientMiddleware to the call chain of this instance of RestClient
        /// </summary>
        /// <typeparam name="TRestMiddleware">The middleware class (must inherit IRestClientMiddleware)</typeparam>
        /// <exception cref="ArgumentException">The class does not inherit from IRestClientMiddleware</exception>
        public void UseMiddleware<TRestMiddleware>()
        {
            UseMiddleware(typeof(TRestMiddleware));
        }

        /// <summary>
        /// Add a middleware of type IRestClientMiddleware to the call chain. Static middlewares are used on all requests
        /// </summary>
        /// <param name="restMiddleware">The middleware class (must inherit IRestClientMiddleware)</param>
        /// <exception cref="ArgumentException">The class does not inherit from IRestClientMiddleware</exception>
        public static void UseStaticMiddleware(Type restMiddleware)
        {
            // This could be changed later to accept middlewares that are not IRestClientMiddleware and dynamically adapt to the constructor signature
            if (!typeof(IRestClientMiddleware).IsAssignableFrom(restMiddleware))
            {
                throw new ArgumentException($"The type {restMiddleware.Name} does not implement {nameof(IRestClientMiddleware)}");
            }

            UseStatic(async (requestInfo, next) =>
            {
                var constructor = restMiddleware.GetConstructor([typeof(RestDelegate)]) ?? throw new ArgumentException($"The type {restMiddleware.Name} does not implement a constructor with a {nameof(RestDelegate)} param");
                var instance = constructor.Invoke([next]);

                var result = await (instance as IRestClientMiddleware)!.InvokeAsync(requestInfo);

                return result;
            });
        }

        /// <summary>
        /// Add a middleware of type IRestClientMiddleware to the call chain of this instance of RestClient
        /// </summary>
        /// <param name="restMiddleware">The middleware class (must inherit IRestClientMiddleware)</param>
        /// <exception cref="ArgumentException">The class does not inherit from IRestClientMiddleware</exception>
        public void UseMiddleware(Type restMiddleware)
        {
            // This could be changed later to accept middlewares that are not IRestClientMiddleware and dynamically adapt to the constructor signature
            if (!typeof(IRestClientMiddleware).IsAssignableFrom(restMiddleware))
            {
                throw new ArgumentException($"The type {restMiddleware.Name} does not implement {nameof(IRestClientMiddleware)}");
            }

            Use(async (requestInfo, next) =>
            {
                var constructor = restMiddleware.GetConstructor([typeof(RestDelegate)]) ?? throw new ArgumentException($"The type {restMiddleware.Name} does not implement a constructor with a {nameof(RestDelegate)} param");
                var instance = constructor.Invoke([next]);

                var result = await (instance as IRestClientMiddleware)!.InvokeAsync(requestInfo);

                return result;
            });
        }

        /// <summary>
        /// Recursive function that iterates through each middleware and finally executes the actual request before returning the response up the chain
        /// </summary>
        /// <param name="mainFunc">The final function call that executes the request</param>
        /// <param name="requestInfo">The request info passed through each middleware</param>
        /// <param name="iteration">Iteration count for the recursive function</param>
        /// <returns>The final request result</returns>
        private async Task<RequestResult> InvokeWithMiddleWare(Func<RequestInfo, Task<RequestResult>> mainFunc, RequestInfo requestInfo, int iteration = 0)
        {
            if (iteration < _instanceMiddlewares.Count)
            {
                return await _instanceMiddlewares[iteration](requestInfo, (RequestInfo) => InvokeWithMiddleWare(mainFunc, RequestInfo, ++iteration));
            }
            else
            {
                return await mainFunc(requestInfo);
            }

        }

        public void AddPostParameter(string paramName, object paramValue)
        {
            _postParams.Add(new KeyValuePair<string, string>(paramName, paramValue.ToString() ?? ""));
        }

        public void ClearPostParameters()
        {
            _postParams.Clear();
        }

        public void AddGetParameter(string paramName, object paramValue)
        {
            _getParams.Add(new KeyValuePair<string, string>(paramName, paramValue.ToString() ?? ""));
        }
        public void ClearGetParameters()
        {
            _getParams.Clear();
        }

        public void AddBasicAuth(string username, string password, bool clearAfterCall)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            _clearAuth = clearAfterCall;
        }

        public void AddCookie(string url, string key, object value)
        {
            Cookie c = new(key, value.ToString());
            CookieContainer.Add(new Uri(url), c);
        }

        #region Async_Calls

        public async Task<RequestResult> PostAsync(string url, object body, JsonSerializerOptions? serializerOptions = null)
        {
            string jsonBody;
            if (typeof(object).IsPrimitive)
            {
                jsonBody = body.ToString() ?? "";
            }
            else
            {
                jsonBody = JsonSerializer.Serialize(body, serializerOptions ?? SerializerOptions);
            }

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var requestInfo = new RequestInfo(HttpMethod.Post, url, content, Client.DefaultRequestHeaders, CookieContainer);

            try
            {
                return await InvokeWithMiddleWare(async (rq) =>
                {
                    var result = await Client.PostAsync(rq.Url, rq.Content);

                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, serializerOptions ?? SerializerOptions);
                }, requestInfo);
            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }

        public async Task<RequestResult> PostAsync(string url, bool clearParamsAfterResponse = true, JsonSerializerOptions? serializerOptions = null)
        {
            FormUrlEncodedContent content = new(_postParams);

            var requestInfo = new RequestInfo(HttpMethod.Post, url, content, Client.DefaultRequestHeaders, CookieContainer);

            try
            {

                return await InvokeWithMiddleWare(async (rq) =>
                {
                    HttpResponseMessage result = await Client.PostAsync(rq.Url, rq.Content);

                    if (clearParamsAfterResponse)
                    {
                        _postParams.Clear();
                    }

                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, serializerOptions ?? SerializerOptions);
                }, requestInfo);
            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }

        public async Task<RequestResult> PostContentAsync(string url, HttpContent? content, JsonSerializerOptions? serializerOptions = null)
        {
            var requestInfo = new RequestInfo(HttpMethod.Post, url, content, Client.DefaultRequestHeaders, CookieContainer);

            try
            {

                return await InvokeWithMiddleWare(async (rq) =>
                {
                    HttpResponseMessage result = await Client.PostAsync(rq.Url, rq.Content);

                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, serializerOptions ?? SerializerOptions);
                }, requestInfo);
            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }

        public async Task<RequestResult> PutAsync(string url, object body, JsonSerializerOptions? serializerOptions = null)
        {
            string jsonBody;
            if (typeof(object).IsPrimitive)
            {
                jsonBody = body.ToString() ?? "";
            }
            else
            {
                jsonBody = JsonSerializer.Serialize(body, serializerOptions ?? SerializerOptions);
            }

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var requestInfo = new RequestInfo(HttpMethod.Put, url, content, Client.DefaultRequestHeaders, CookieContainer);

            try
            {
                return await InvokeWithMiddleWare(async (rq) =>
                {
                    var result = await Client.PutAsync(rq.Url, rq.Content);

                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, serializerOptions ?? SerializerOptions);
                }, requestInfo);
            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }

        public async Task<RequestResult> PutAsync(string url, bool clearParamsAfterResponse = true, JsonSerializerOptions? serializerOptions = null)
        {
            FormUrlEncodedContent content = new(_postParams);

            var requestInfo = new RequestInfo(HttpMethod.Put, url, content, Client.DefaultRequestHeaders, CookieContainer);

            try
            {
                return await InvokeWithMiddleWare(async (rq) =>
                {
                    HttpResponseMessage result = await Client.PutAsync(rq.Url, rq.Content);

                    if (clearParamsAfterResponse)
                    {
                        _postParams.Clear();
                    }

                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, serializerOptions ?? SerializerOptions);
                }, requestInfo);
            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }

        public async Task<RequestResult> PatchAsync(string url, object body, JsonSerializerOptions? serializerOptions = null)
        {
            string jsonBody;
            if (typeof(object).IsPrimitive)
            {
                jsonBody = body.ToString() ?? "";
            }
            else
            {
                jsonBody = JsonSerializer.Serialize(body, serializerOptions ?? SerializerOptions);
            }

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var requestInfo = new RequestInfo(HttpMethod.Patch, url, content, Client.DefaultRequestHeaders, CookieContainer);

            try
            {
                return await InvokeWithMiddleWare(async (rq) =>
                {
                    var result = await Client.PatchAsync(rq.Url, rq.Content);

                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, serializerOptions ?? SerializerOptions);

                }, requestInfo);
            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }

        public async Task<RequestResult> PatchAsync(string url, bool clearParamsAfterResponse = true, JsonSerializerOptions? serializerOptions = null)
        {
            FormUrlEncodedContent content = new(_postParams);

            var requestInfo = new RequestInfo(HttpMethod.Patch, url, content, Client.DefaultRequestHeaders, CookieContainer);

            try
            {
                return await InvokeWithMiddleWare(async (rq) =>
                {
                    HttpResponseMessage result = await Client.PatchAsync(rq.Url, rq.Content);

                    if (clearParamsAfterResponse)
                    {
                        _postParams.Clear();
                    }

                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, serializerOptions ?? SerializerOptions);
                }, requestInfo);
            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }

        public async Task<RequestResult> GetAsync(string url, bool clearParamsAfterResponse = true, JsonSerializerOptions? serializerOptions = null)
        {
            for (int i = 0; i < _getParams.Count; i++)
            {
                if (i == 0)
                    url += "?";
                url += _getParams[i].Key + "=" + _getParams[i].Value;
                if (i + 1 < _getParams.Count)
                    url += "&";
            }

            var requestInfo = new RequestInfo(HttpMethod.Get, url, null, Client.DefaultRequestHeaders, CookieContainer);

            try
            {
                return await InvokeWithMiddleWare(async (rq) =>
                {
                    HttpResponseMessage result = await Client.GetAsync(rq.Url);

                    if (clearParamsAfterResponse)
                    {
                        _getParams.Clear();
                    }

                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, serializerOptions ?? SerializerOptions);
                }, requestInfo);
            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }

        public async Task<RequestResult> DeleteAsync(string url, bool clearParamsAfterResponse = true)
        {
            for (int i = 0; i < _getParams.Count; i++)
            {
                if (i == 0)
                    url += "?";
                url += _getParams[i].Key + "=" + _getParams[i].Value;
                if (i + 1 < _getParams.Count)
                    url += "&";
            }

            var requestInfo = new RequestInfo(HttpMethod.Delete, url, null, Client.DefaultRequestHeaders, CookieContainer);

            try
            {

                return await InvokeWithMiddleWare(async (rq) =>
                {
                    HttpResponseMessage result = await Client.DeleteAsync(rq.Url);
                    if (clearParamsAfterResponse)
                    {
                        _getParams.Clear();
                    }
                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, SerializerOptions);
                }, requestInfo);

            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }

        public async Task<RequestResult> DeleteAsync(string url, object body, JsonSerializerOptions? serializerOptions = null)
        {
            string jsonBody;
            if (typeof(object).IsPrimitive)
            {
                jsonBody = body.ToString() ?? "";
            }
            else
            {
                jsonBody = JsonSerializer.Serialize(body, serializerOptions ?? SerializerOptions);
            }

            StringContent content = new(jsonBody, Encoding.UTF8, "application/json");

            var requestInfo = new RequestInfo(HttpMethod.Delete, url, content, Client.DefaultRequestHeaders, CookieContainer);

            try
            {
                return await InvokeWithMiddleWare(async (rq) =>
                {
                    HttpResponseMessage result = await Client.SendAsync(new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(rq.Url),
                        Content = rq.Content
                    });

                    if (_clearAuth)
                    {
                        _clearAuth = false;
                        Client.DefaultRequestHeaders.Authorization = null;
                    }

                    return new RequestResult(result, serializerOptions ?? SerializerOptions);
                }, requestInfo);
            }
            catch (TaskCanceledException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ClientTimeout);
            }
            catch (HttpRequestException)
            {
                return new RequestResult(null, serializerOptions ?? SerializerOptions, RestStatusCode.ConnectionRefused);
            }
        }
        #endregion

        #region Sync_Calls

        public RequestResult Post(string url, object body, JsonSerializerOptions? serializerOptions = null)
        {
            return PostAsync(url, body, serializerOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public RequestResult Post(string url, bool clearParamsAfterResponse = true, JsonSerializerOptions? serializerOptions = null)
        {
            return PostAsync(url, clearParamsAfterResponse, serializerOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public RequestResult Put(string url, object body, JsonSerializerOptions? serializerOptions = null)
        {
            return PutAsync(url, body, serializerOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public RequestResult Put(string url, bool clearParamsAfterResponse = true, JsonSerializerOptions? serializerOptions = null)
        {
            return PutAsync(url, clearParamsAfterResponse, serializerOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public RequestResult Patch(string url, object body, JsonSerializerOptions? serializerOptions = null)
        {
            return PatchAsync(url, body, serializerOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public RequestResult Patch(string url, bool clearParamsAfterResponse = true, JsonSerializerOptions? serializerOptions = null)
        {
            return PatchAsync(url, clearParamsAfterResponse, serializerOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public RequestResult Get(string url, bool clearParamsAfterResponse = true, JsonSerializerOptions? serializerOptions = null)
        {
            return GetAsync(url, clearParamsAfterResponse, serializerOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public RequestResult Delete(string url, bool clearParamsAfterResponse = true)
        {
            return DeleteAsync(url, clearParamsAfterResponse).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public RequestResult Delete(string url, object body, JsonSerializerOptions? serializerOptions = null)
        {
            return DeleteAsync(url, body, serializerOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    ClientHandler.Dispose();
                    Client.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }