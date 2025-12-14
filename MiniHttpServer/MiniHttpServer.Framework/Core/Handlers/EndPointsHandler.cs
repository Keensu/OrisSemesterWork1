using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.FrameWork.Core.Handlers
{
    class EndPointsHandler : Handler
    {
        public override void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var segments = request.Url.AbsolutePath.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();
                var endpointName = segments.Length > 0 ? segments[0] : "";

                var assembly = Assembly.GetEntryAssembly();
                var endpoint = assembly.GetTypes()
                    .FirstOrDefault(t => t.GetCustomAttribute<EndpointAttribute>() != null &&
                                         IsCheckedNameEndpoint(t.Name, endpointName));
                if (endpoint == null)
                {
                    // Если эндпоинт не найден — отправляем 404
                    context.Response.StatusCode = 404;
                    WriteResponse(context.Response, "Endpoint not found");
                    return;
                }

                MethodInfo matchedMethod = null;
                Dictionary<string, string> routeParams = null;

                foreach (var methodInfo in endpoint.GetMethods())
                {
                    var httpAttr = methodInfo.GetCustomAttributes(true)
                        .FirstOrDefault(attr => attr.GetType().Name.StartsWith($"Http{request.HttpMethod}", StringComparison.OrdinalIgnoreCase));

                    if (httpAttr == null) continue;

                    var routeProp = httpAttr.GetType().GetProperty("Route");
                    if (routeProp?.GetValue(httpAttr) is string routePattern && !string.IsNullOrEmpty(routePattern))
                    {
                        if (TryMatchRoute(routePattern, request.Url.AbsolutePath, out var capturedParams))
                        {
                            matchedMethod = methodInfo;
                            routeParams = capturedParams;
                            break; 
                        }
                    }
                }


                if (matchedMethod == null)
                {
                    matchedMethod = endpoint.GetMethods()
                        .FirstOrDefault(m => m.GetCustomAttributes(true)
                            .Any(attr => attr.GetType().Name.StartsWith($"Http{request.HttpMethod}", StringComparison.OrdinalIgnoreCase)));
                }

                string body = "";
                if (request.HttpMethod == "POST" || request.HttpMethod == "PUT")
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }
                }

                var postParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(body))
                {
                    // Поддержка application/x-www-form-urlencoded
                    foreach (var pair in body.Split('&'))
                    {
                        if (string.IsNullOrEmpty(pair)) continue;
                        var kv = pair.Split('=', 2);
                        if (kv.Length == 2)
                        {
                            var key = WebUtility.UrlDecode(kv[0]);
                            var value = WebUtility.UrlDecode(kv[1]);
                            postParams[key] = value;
                        }
                    }
                }

                var parameters = matchedMethod.GetParameters().Select(param =>
                {
                    var paramType = param.ParameterType;

                    
                    if (routeParams != null && routeParams.TryGetValue(param.Name, out var routeValue))
                    {
                        return ConvertValueToType(routeValue, paramType);
                    }

                    
                    if (postParams.TryGetValue(param.Name, out var postValue))
                    {
                        return ConvertValueToType(postValue, paramType);
                    }

                    
                    return GetDefaultValue(paramType);
                }).ToArray();

                var instance = Activator.CreateInstance(endpoint);
                if (typeof(EndpointBase).IsAssignableFrom(endpoint))
                {
                    (instance as EndpointBase)?.SetContext(context);
                }

                var ret = matchedMethod.Invoke(instance, parameters);

                if (ret is IHttpResult httpResult)
                {
                    var content = httpResult.Execute(context);
                    WriteResponse(context.Response, content);
                }
                else if (ret is string strResult)
                {
                    WriteResponse(context.Response, strResult);
                }
                else if (ret != null)
                {
                    WriteResponse(context.Response, ret.ToString());
                }
                else
                {
                    context.Response.StatusCode = 200;
                    WriteResponse(context.Response, "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"STACK: {ex.StackTrace}");
                context.Response.StatusCode = 500;
                WriteResponse(context.Response, "Internal Server Error. Check logs.");
            }
        }
        private bool TryMatchRoute(string routeTemplate, string actualPath, out Dictionary<string, string> parameters)
        {
            parameters = new Dictionary<string, string>();

            var templateSegments = routeTemplate.Trim('/').Split('/');
            var actualSegments = actualPath.Trim('/').Split('/');

            if (templateSegments.Length != actualSegments.Length)
                return false;

            for (int i = 0; i < templateSegments.Length; i++)
            {
                var templatePart = templateSegments[i];
                var actualPart = actualSegments[i];

                if (templatePart.StartsWith("{") && templatePart.EndsWith("}"))
                {
                    var paramName = templatePart.Substring(1, templatePart.Length - 2);
                    parameters[paramName] = actualPart;
                }
                else if (!string.Equals(templatePart, actualPart, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsCheckedNameEndpoint(string endpointName, string className) =>
            endpointName.Equals(className, StringComparison.OrdinalIgnoreCase) ||
            endpointName.Equals($"{className}EndPoint", StringComparison.OrdinalIgnoreCase);

        private static void WriteResponse(HttpListenerResponse response, string content)
        {
            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            if (string.IsNullOrEmpty(response.ContentType))
            {
                response.ContentType = "text/html; charset=utf-8";
            }
            response.Close(buffer, false);
        }
        private object ConvertValueToType(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return GetDefaultValue(targetType);

            if (targetType == typeof(string))
                return value;
            if (targetType == typeof(int))
                return int.Parse(value);
            if (targetType == typeof(long))
                return long.Parse(value);
            if (targetType == typeof(bool))
                return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       value.Equals("1", StringComparison.OrdinalIgnoreCase);
            if (targetType == typeof(decimal))
                return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(DateTime))
            {
                if (DateTime.TryParse(value, out var dt))
                    return dt;
                else
                    throw new FormatException("Invalid DateTime format");
            }

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, ignoreCase: true);

            return value;
        }

        private object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
