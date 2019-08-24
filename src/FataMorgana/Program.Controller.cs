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
        static readonly Uri _audiosUri = new Uri(_baseUri, "/chocoh/audios/");
        static readonly Uri _imagesUri = new Uri(_baseUri, "/chocoh/images/");
        static readonly Uri _fontsUri = new Uri(_baseUri, "/chocoh/fonts/");
        static readonly HttpClientHandler _handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip
        };
        static readonly HttpClient _http = new HttpClient(_handler)
        {
            DefaultRequestVersion = new Version(2, 0)
        };
        static readonly ProxyServer _server = new ProxyServer
        {
            ForwardToUpstreamGateway = true
        };
        static readonly ExplicitProxyEndPoint _endpoint = new ExplicitProxyEndPoint(IPAddress.Any, 8334);

        static void ServerStart()
        {
            _server.CertificateManager.RootCertificateIssuerName = "Izumi Ohishi";

            _server.CertificateManager.RootCertificateName = "Fata Morgana Root Certificate Authority";

            _server.CertificateManager.SaveFakeCertificates = true;

            _server.CertificateManager.CreateRootCertificate(true);

            _server.CertificateManager.TrustRootCertificate(true);

            _server.BeforeResponse += async (sender, e) =>
            {
                try
                {
                    bool match(Uri uri) => e.HttpClient.Request.RequestUri.AbsoluteUri.StartsWith(uri.AbsoluteUri);

                    if (match(_audiosUri))
                    {
                        var body = await HandleAudioAsync(e.HttpClient.Request.RequestUri);

                        if (body is null)
                        {
                            e.GenericResponse(null, HttpStatusCode.NotFound);
                        }
                        else
                        {
                            e.Ok(body, new Dictionary<string, HttpHeader>
                            {
                                ["Cache-Control"] = new HttpHeader("Cache-Control", "public, max-age=31536000"),
                                ["Content-Type"] = new HttpHeader("Content-Type", "media/mp4")
                            });
                        }
                    }
                    else if (match(_imagesUri))
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

        static void ServerStop()
        {
            _server.Stop();
        }

        static async Task<byte[]?> HandleAudioAsync(Uri uri)
        {
            var parts = _audiosUri.MakeRelativeUri(uri).ToString().Split('?');

            var filename = parts[0];

            var version = parts.Length > 1 && parts[1].Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]).TryGetValue("v", out var value) ? value : "0";

            var directory = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FataMorgana", "audios", version));

            var fullpath = Path.Combine(directory.FullName, filename);

            if (directory.GetFiles(filename).Any())
            {
                return await File.ReadAllBytesAsync(fullpath);
            }

            using var response = await _http.GetAsync($"https://shinycolors.enza.fun/assets/{filename}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var raw = await response.Content.ReadAsStreamAsync();

            using var memory = new MemoryStream();

            await raw.CopyToAsync(memory);

            memory.Seek(0, SeekOrigin.Begin);

            var body = memory.ToArray();

            using var file = File.OpenWrite(fullpath);

            await file.WriteAsync(body);

            file.Close();

            return body;
        }

        static async Task<(string? contentType, byte[] body)> HandleImageAsync(string status, string id)
        {
            using var response = await _http.GetAsync($"https://shinycolors.enza.fun/assets/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return (null, Array.Empty<byte>());
            }

            var contentType = response.Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault() ?? "";

            using var body = await response.Content.ReadAsStreamAsync();

            using var memory = new MemoryStream();

            await body.CopyToAsync(memory);

            body.Close();

            /* */ var bytes = memory.ToArray();

            memory.Close();

            using var bitmap = SKBitmap.Decode(bytes);

            if (bitmap is null)
            {
                return (contentType, bytes);
            }

            using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));

            if (surface is null)
            {
                return (contentType, bytes);
            }

            using var canvas = surface.Canvas;

            if ( // Background
                bitmap.Width == 1136 &&
                bitmap.Height == 640)
            {
                if ( // JFIF (JPEG)
                    bytes.AsSpan().StartsWith(stackalloc byte[] { 0xff, 0xd8, 0xff, 0xe0 }))
                {
                    var background = status switch
                    {
                        "red" => new SKColor(255, 0, 0, 255),
                        "green" => new SKColor(0, 255, 0, 255),
                        "blue" => new SKColor(0, 0, 255, 255),
                        "cyan" => new SKColor(0, 255, 255, 255),
                        "magenta" => new SKColor(255, 0, 255, 255),
                        "yellow" => new SKColor(255, 255, 0, 255),
                        "black" => new SKColor(0, 0, 0, 255),
                        "white" => new SKColor(255, 255, 255, 255),
                        "amai" => new SKColor(255, 255, 255, 255),
                        _ => SKColor.Empty
                    };

                    if (background == SKColor.Empty)
                    {
                        return (contentType, bytes);
                    }

                    canvas.Clear(background);
                }
                else
                {
                    canvas.Clear();
                }
            }
            else if ( // Spine Texture
                status == "amai" && ( // amai mode only
                bitmap.Width == 1024 &&
                bitmap.Height == 1024 ||
                bitmap.Width == 512 &&
                bitmap.Width == 512))
            {
                var pixels = bitmap.Pixels; // Attention: deep copy

                for (var i = 0; i < pixels.Length; i++)
                {
                    var pixel = pixels[i];

                    pixels[i] = new SKColor(0, 0, 0, pixel.Alpha);
                }

                bitmap.Pixels = pixels; // return (see: L235)

                using var map = bitmap.PeekPixels();

                using var data = map.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 0.0f));

                return ("image/webp", data.ToArray());
            }
            else if ( // Bubble
                bitmap.Width == 329 &&
                bitmap.Height == 116 ||
                bitmap.Width == 840 &&
                bitmap.Height == 162)
            {
                canvas.Clear();
            }
            else if ( // Button
                bitmap.Width == 800 &&
                bitmap.Height == 800)
            {
                canvas.Clear();
            }
            else if ( // Transition
                bitmap.Width == 1235 &&
                bitmap.Height == 1235)
            {
                canvas.Clear();
            }
            else if ( // Room Background
                bitmap.Width == 866 &&
                bitmap.Height == 353)
            {
                canvas.Clear();
            }
            else if ( // Fes Right Slot Character Icon
                bitmap.Width == 110 &&
                bitmap.Height == 64)
            {
                canvas.Clear();
            }
            else if ( // Fes Bottom Slot Character Icon
                bitmap.Width == 150 &&
                bitmap.Height == 96)
            {
                canvas.Clear();
            }
            else if ( // Fes Right Slot Character Icon Alternative
                bitmap.Width == 184 &&
                bitmap.Height == 96)
            {
                canvas.Clear();
            }
            else if ( // Fes Background Assets
                bitmap.Width == 908 &&
                bitmap.Height == 908)
            {
                canvas.Clear();
            }
            else if ( // Fes Number Assets
                bitmap.Width == 506 &&
                bitmap.Height == 506)
            {
                canvas.Clear();
            }
            else if ( // Fes Order Assets
                bitmap.Width == 163 &&
                bitmap.Height == 163)
            {
                canvas.Clear();
            }
            else if ( // Fes Effects
                bitmap.Width == 525 &&
                bitmap.Height == 155 ||
                bitmap.Width == 128 &&
                bitmap.Height == 128 ||
                bitmap.Width == 500 &&
                bitmap.Height == 250 ||
                bitmap.Width == 352 &&
                bitmap.Height == 888 ||
                bitmap.Width == 500 &&
                bitmap.Height == 204 ||
                bitmap.Width == 511 &&
                bitmap.Height == 102 ||
                bitmap.Width == 320 &&
                bitmap.Height == 460 ||
                bitmap.Width == 480 &&
                bitmap.Height == 480 ||
                bitmap.Width == 500 &&
                bitmap.Height == 100 ||
                bitmap.Width == 480 &&
                bitmap.Height == 172 ||
                bitmap.Width == 520 &&
                bitmap.Height == 100 ||
                bitmap.Width == 510 &&
                bitmap.Height == 510 ||
                bitmap.Width == 212 &&
                bitmap.Height == 212 ||
                bitmap.Width == 454 &&
                bitmap.Height == 454 ||
                bitmap.Width == 510 &&
                bitmap.Height == 510 ||
                bitmap.Width == 525 &&
                bitmap.Height == 155)
            {
                canvas.Clear();
            }
            else
            {
                return (contentType, bytes);
            }

            {
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
}
