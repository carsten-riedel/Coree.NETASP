﻿using Polly;

namespace Coree.NETASP.Extensions.HttpResponsex
{
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// Adds security headers to the HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        public static async Task WriteDefaultStatusCodeAnswer(this HttpResponse response,int StatusCode)
        {
            response.StatusCode = StatusCode;
            response.ContentType = "text/html";

            // Adding additional headers
            //response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            //response.Headers.Add("Content-Security-Policy", "default-src 'self'; script-src 'self'; object-src 'none';");
            //response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
            //response.Headers.Add("X-Content-Type-Options", "nosniff");
            //response.Headers.Add("Referrer-Policy", "no-referrer");
            //response.Headers.Add("Permissions-Policy", "accelerometer=(), autoplay=(), camera=(), encrypted-media=(), fullscreen=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), midi=(), payment=(), picture-in-picture=(), sync-xhr=(), usb=(), vr=(), xr-spatial-tracking=()");
            //response.Headers.Add("Expect-CT", "max-age=86400, enforce");
            //response.Headers.Add("Feature-Policy", "geolocation 'none'; microphone 'none'; camera 'none'");

            string responseMessage = $"<html><body><h1><p>{response.StatusCode} - {GetStatusCodeDescription(response.StatusCode)}</p></h1></body></html>";
            await response.WriteAsync(responseMessage);
        }

        private static string GetStatusCodeDescription(int statusCode)
        {
            return statusCode switch
            {
                100 => "Continue",
                101 => "Switching Protocols",
                102 => "Processing",
                103 => "Early Hints",
                200 => "OK",
                201 => "Created",
                202 => "Accepted",
                203 => "Non-Authoritative Information",
                204 => "No Content",
                205 => "Reset Content",
                206 => "Partial Content",
                207 => "Multi-Status",
                208 => "Already Reported",
                226 => "IM Used",
                300 => "Multiple Choices",
                301 => "Moved Permanently",
                302 => "Found",
                303 => "See Other",
                304 => "Not Modified",
                307 => "Temporary Redirect",
                308 => "Permanent Redirect",
                400 => "Bad Request",
                401 => "Unauthorized",
                402 => "Payment Required",
                403 => "Forbidden",
                404 => "Not Found",
                405 => "Method Not Allowed",
                406 => "Not Acceptable",
                407 => "Proxy Authentication Required",
                408 => "Request Timeout",
                409 => "Conflict",
                410 => "Gone",
                411 => "Length Required",
                412 => "Precondition Failed",
                413 => "Payload Too Large",
                414 => "URI Too Long",
                415 => "Unsupported Media Type",
                416 => "Range Not Satisfiable",
                417 => "Expectation Failed",
                418 => "I'm a teapot",
                421 => "Misdirected Request",
                422 => "Unprocessable Entity",
                423 => "Locked",
                424 => "Failed Dependency",
                425 => "Too Early",
                426 => "Upgrade Required",
                428 => "Precondition Required",
                429 => "Too Many Requests",
                431 => "Request Header Fields Too Large",
                451 => "Unavailable For Legal Reasons",
                500 => "Internal Server Error",
                501 => "Not Implemented",
                502 => "Bad Gateway",
                503 => "Service Unavailable",
                504 => "Gateway Timeout",
                505 => "HTTP Version Not Supported",
                506 => "Variant Also Negotiates",
                507 => "Insufficient Storage",
                508 => "Loop Detected",
                510 => "Not Extended",
                511 => "Network Authentication Required",
                _ => "Unknown Status Code"
            };
        }
    }
}
