﻿using Anet.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Anet.Web.Api;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly HashSet<string> _pathPrefixes;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, HashSet<string> pathPrefixes = null)
    {
        _next = next;
        _logger = logger;
        _pathPrefixes = pathPrefixes;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (_pathPrefixes == null || _pathPrefixes.Count == 0
            || _pathPrefixes.Any(x => httpContext.Request.Path.StartsWithSegments(x)))
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }
        else
        {
            await _next(httpContext);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var message = exception.Message;

        ApiResult result;
        if (exception is BadRequestException)
        {
            result = ApiResult.Error(exception.Message, 400);
        }
        else if (exception is NotFoundException)
        {
            message = string.IsNullOrEmpty(message) ? "Not Found" : message;
            result = ApiResult.Error(message, 404);
        }
        else if (exception is UnauthorizedAccessException)
        {
            message = string.IsNullOrEmpty(message) ? "Unauthorized" : message;
            result = ApiResult.Error(message, 401);
        }
        else if (exception is GatewayException ex)
        {
            message = string.IsNullOrEmpty(message) ? "Call API Error" : message;
            _logger.LogError("============= Error Info Begin =============");
            _logger.LogError(exception, message);
            _logger.LogError("Url:{0}", ex.Url);
            _logger.LogError("Response:{0}", ex.Response);
            _logger.LogError("============= Error Info End   =============");
            result = ApiResult.Error(message, 500);
        }
        else
        {
            message = string.IsNullOrEmpty(message) ? "Internal Server Error" : message;
            _logger.LogError(exception, message);
            result = ApiResult.Error(message, 500);
        }
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        return context.Response.WriteAsync(JsonUtility.SerializeCamelCase(result));
    }
}