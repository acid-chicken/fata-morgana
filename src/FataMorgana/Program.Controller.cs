using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace AcidChicken.FataMorgana
{
    partial class Program
    {
        static readonly Uri _baseUri = new Uri("https://shinycolors.enza.fun/");
        static readonly Uri _imagesUri = new Uri(_baseUri, "/chocoh/images/");
        static readonly Uri _fontsUri = new Uri(_baseUri, "/chocoh/fonts/");
        static readonly HttpClient _http = new HttpClient();
        static readonly ProxyServer _server = new ProxyServer
        {
            ForwardToUpstreamGateway = true
        };
        static readonly ExplicitProxyEndPoint _endpoint = new ExplicitProxyEndPoint(IPAddress.Any, 8334);

        static void Start()
        {
            _server.CertificateManager.TrustRootCertificate(true);

            _server.CertificateManager.SaveFakeCertificates = true;

            _server.BeforeResponse += async (sender, e) =>
            {
                try
                {
                    bool match(Uri uri) => e.HttpClient.Request.RequestUri.AbsoluteUri.StartsWith(uri.AbsoluteUri);

                    if (match(_imagesUri))
                    {
                        var segments = _imagesUri.MakeRelativeUri(e.HttpClient.Request.RequestUri).ToString().Split('/');

                        var (type, body) = await HandleImageAsync(segments[0], segments[1]);

                        if (type is null)
                        {
                            e.GenericResponse(null, HttpStatusCode.NotFound);
                        }
                        else
                        {
                            e.Ok(body, new Dictionary<string, HttpHeader>
                            {
                                ["Cache-Control"] = new HttpHeader("Cache-Control", "public, max-age=31536000"),
                                ["Content-Type"] = new HttpHeader("Content-Type", type)
                            });
                        }
                    }
                    else if (match(_fontsUri))
                    {
                        e.Ok(_woff2, new Dictionary<string, HttpHeader>
                        {
                            ["Cache-Control"] = new HttpHeader("Cache-Control", "public, max-age=31536000"),
                            ["Content-Type"] = new HttpHeader("Content-Type", "font/woff2")
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };

            _endpoint.BeforeTunnelConnectRequest += (sender, e) =>
            {
                if (e.HttpClient.Request.RequestUri.DnsSafeHost != _baseUri.DnsSafeHost)
                {
                    e.DecryptSsl = false;
                }

                return Task.CompletedTask;
            };

            _server.AddEndPoint(_endpoint);
            _server.Start();
        }

        static async Task<(string? contentType, byte[] body)> HandleImageAsync(string status, string id)
        {
            using var response = await _http.GetAsync($"https://shinycolors.enza.fun/assets/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return (null, new byte[0]);
            }

            using var body = await response.Content.ReadAsStreamAsync();

            using var memory = new MemoryStream();

            await body.CopyToAsync(memory);

            body.Close();

            /* */ var bytes = memory.ToArray();

            memory.Close();

            using var bitmap = SKBitmap.Decode(bytes);

            if (bitmap is null)
            {
                return (response.Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault() ?? "", bytes);
            }

            using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));

            if (surface is null)
            {
                return (response.Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault() ?? "", bytes);
            }

            using var canvas = surface.Canvas;

            if ( // Background
                bitmap.Width == 1136 &&
                bitmap.Height == 640)
            {
                var background = status switch
                {
                    "red" => new SKColor(255, 0, 0, 255),
                    "green" => new SKColor(0, 255, 0, 255),
                    "blue" => new SKColor(0, 0, 255, 255),
                    _ => SKColor.Empty
                };

                if (background == SKColor.Empty)
                {
                    return (response.Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault() ?? "", bytes);
                }

                canvas.Clear(background);
            }
            else if ( // Bubble
                bitmap.Width == 840 &&
                bitmap.Height == 162)
            {
                canvas.Clear();
            }
            else if ( // Button
                bitmap.Width == 695 &&
                bitmap.Height == 695)
            {
                canvas.Clear();
            }
            else
            {
                return (response.Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault() ?? "", bytes);
            }

            canvas.DrawRect(0, 0, 0, 0, new SKPaint
            {
            });

            canvas.Flush();

            using var map = surface.PeekPixels();

            using var data = map.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 0.0f));

            return ("image/webp", data.ToArray());
        }
    }
}
