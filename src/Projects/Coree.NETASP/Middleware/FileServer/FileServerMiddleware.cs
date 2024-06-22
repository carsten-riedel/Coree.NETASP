using System.Diagnostics;
using System.Text;

using Microsoft.AspNetCore.StaticFiles;

namespace Coree.NETASP.Middleware.FileServer
{
    public class FileServerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _rootPath;
        private readonly FileExtensionContentTypeProvider provider;
        private readonly DefaultFilesOptions options;


        //app.UseFileServerMiddleware(app.Environment.WebRootPath);
        public FileServerMiddleware(RequestDelegate next, string rootPath)
        {
            _next = next;
            _rootPath = rootPath;
            this.provider = new FileExtensionContentTypeProvider();
            this.options = new DefaultFilesOptions();
            this.provider.Mappings.Add(".php", "application/x-httpd-php");
        }


        public async Task InvokeAsync(HttpContext context)
        {
            var rawUrl = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestFeature>()?.RawTarget;

            var uri = FileServerMiddlewareExtensions.GetFullRequestUri(context);
            var path = Path.Combine(_rootPath, uri.LocalPath.TrimStart('/'));

            FileInfo fileInfo = new FileInfo(path);
            DirectoryInfo dirInfo = new DirectoryInfo(path);

            if (dirInfo.Exists)
            {
                foreach (var item in options.DefaultFileNames)
                {
                    if (System.IO.File.Exists(System.IO.Path.Combine(dirInfo.FullName, item)))
                    {
                        fileInfo = new FileInfo(System.IO.Path.Combine(dirInfo.FullName, item));
                    }
                }
            }

            if (fileInfo.Exists)
            {
                if (provider.Mappings.TryGetValue(fileInfo.Extension.ToLowerInvariant(), out string? contentType))
                {
                    if (fileInfo.Extension.ToLowerInvariant() == ".php")
                    {
                        var phpCgiExecutable = @"C:\temp\php-8.3.8-nts-Win32-vs16-x64\php-cgi.exe";

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = phpCgiExecutable,
                            Arguments = "",  // Arguments are now set via environment variables
                            RedirectStandardOutput = true,
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            Environment =
                            {
                                ["PHP_INI_DIR"] = "C:\\temp\\php-8.3.8-nts-Win32-vs16-x64",
                                ["REDIRECT_STATUS"] = "200",
                                ["SCRIPT_FILENAME"] = fileInfo.FullName,
                                ["DOCUMENT_ROOT"] = Path.GetDirectoryName(fileInfo.FullName),
                                ["REQUEST_METHOD"] = context.Request.Method,
                                ["QUERY_STRING"] = context.Request.QueryString.ToString(),
                                ["CONTENT_LENGTH"] = context.Request.ContentLength.ToString(),
                                ["CONTENT_TYPE"] = context.Request.ContentType,
                                ["REMOTE_ADDR"] = context.Connection.RemoteIpAddress?.ToString(),
                                ["SERVER_NAME"] = context.Request.Host.Host,
                                ["SERVER_PORT"] = context.Request.Host.Port?.ToString(),
                                ["SERVER_PROTOCOL"] = context.Request.Protocol,
                                ["SERVER_SOFTWARE"] = "Kestrel",
                                ["PATH_INFO"] = context.Request.Path,
                                ["PATH_TRANSLATED"] = Path.Combine(Path.GetDirectoryName(fileInfo.FullName), context.Request.Path),
                                ["HTTP_COOKIE"] = context.Request.Headers["Cookie"].ToString(),
                                ["AUTH_TYPE"] = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(' ')?.First(),
                                ["REMOTE_USER"] = context.User.Identity.Name
                            }
                        };
                        var process = new Process
                        {
                            StartInfo = startInfo
                        };

                        process.Start();
                        // Writing request body to php-cgi if there is a request body
                        if (context.Request.ContentLength > 0)
                        {
                            using var streamWriter = new StreamWriter(process.StandardInput.BaseStream);
                            using var requestStream = context.Request.Body;
                            await requestStream.CopyToAsync(streamWriter.BaseStream);
                            await streamWriter.BaseStream.FlushAsync();
                        }

                        string output = await process.StandardOutput.ReadToEndAsync();
                        process.WaitForExit();

                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync(output);
                    }
                    else
                    {
                        context.Response.ContentType = contentType;
                        await context.Response.SendFileAsync(fileInfo.FullName);
                    }
                }

                else
                {
                    // Return a simple plain text response or handle as necessary
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("No Mapping.");
                    await context.Response.Body.FlushAsync();
                    await context.Response.CompleteAsync();
                }
            }
            else
            {
                // Return a simple plain text response or handle as necessary
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Resource not found.");
                await context.Response.Body.FlushAsync();
                await context.Response.CompleteAsync();
            }
        }
    }

    public static class FileServerMiddlewareExtensions
    {

        /// <summary>
        /// Parses the Accept header from the provided HttpContext and returns a list of MIME types sorted by their quality factor.
        /// </summary>
        /// <param name="context">The HttpContext from which to extract the Accept header.</param>
        /// <returns>A list of MIME types sorted by quality factor in descending order.</returns>
        public static List<string> ParseAcceptHeader(HttpContext context)
        {
            var acceptHeader = context.Request.Headers["Accept"].ToString();
            var acceptedTypes = new List<(double Quality, string Type)>();

            if (!string.IsNullOrEmpty(acceptHeader))
            {
                // Split the header into individual MIME type strings
                var types = acceptHeader.Split(',');

                // Parse each MIME type and optional quality factor
                foreach (var type in types)
                {
                    var parts = type.Split(';');
                    var mimeType = parts[0].Trim();
                    double quality = 1.0; // Default quality

                    if (parts.Length > 1 && parts[1].Trim().StartsWith("q="))
                    {
                        string qualityPart = parts[1].Trim().Substring(2);
                        if (double.TryParse(qualityPart, out double q))
                        {
                            quality = q;
                        }
                    }

                    // Add to list with quality factor
                    acceptedTypes.Add((Quality: quality, Type: mimeType));
                }

                // Order by quality factor descending and return only MIME types
                return acceptedTypes.OrderByDescending(x => x.Quality).Select(x => x.Type).ToList();
            }

            return new List<string>(); // Return an empty list if no Accept header is present
        }

        public static Uri GetFullRequestUri(HttpContext context)
        {
            var request = context.Request;

            // Build the full URI
            var uriBuilder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = request.Host.Host,
                Port = request.Host.Port ?? -1, // Keep default port handling
                Path = request.PathBase.Add(request.Path).ToString(),
                Query = request.QueryString.ToString()
            };

            return uriBuilder.Uri;
        }

        public static IApplicationBuilder UseFileServerMiddleware(this IApplicationBuilder builder, string rootPath)
        {
            return builder.UseMiddleware<FileServerMiddleware>(rootPath);
        }
    }

    public static class PhpCgiExecutor
    {
        public static async Task ExecutePhpCgi(HttpContext context, string phpCgiExecutablePath, string file)
        {
            // Extract the request body
            context.Request.EnableBuffering();
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;  // Reset the position for other potential middleware

            // Prepare the process start information
            var startInfo = new ProcessStartInfo
            {
                FileName = phpCgiExecutablePath,
                Arguments = $"-f \"{file}\"",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,

            };

            // Execute the process
            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            // Write the request body to the PHP process's standard input if there's content to write
            if (!string.IsNullOrEmpty(requestBody))
            {
                using (var streamWriter = new StreamWriter(process.StandardInput.BaseStream, Encoding.UTF8))
                {
                    await streamWriter.WriteAsync(requestBody);
                }
            }

            // Read the output from PHP CGI
            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            // Output the result to the HTTP response
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(output);
        }
    }
}
