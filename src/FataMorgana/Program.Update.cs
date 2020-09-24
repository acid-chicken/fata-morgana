#nullable enable

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AcidChicken.FataMorgana
{
    partial class Program
    {
        public static async Task<Uri?> RequestArtifactUrl()
        {
            try
            {
                var name = typeof(Program).Assembly.GetName();

                using var http = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                })
                {
                };

                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Format("{0}{1}{2}f{3}f{4}{5}c{6}d{7}{5}{5}f{5}bb{8}{5}{9}dd{10}d{3}{5}{5}{11}{1}",
                    0x00000000,
                    A,
                    0x00000007,
                    0x00000002,
                    0x0000001e,
                    E,
                    0x00000004,
                    0x000016b1,
                    0x00000008,
                    0x00099e85,
                    0x00000051,
                    0x00000009));

                http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(name.Name, name.Version?.ToString()));

                var response = JsonSerializer.Deserialize<ArtifactCollection>(await http.GetByteArrayAsync("https://api.github.com/repos/acid-chicken/fata-morgana/actions/artifacts"));
                var artifact = response.Artifacts
                    ?.Where(x => !x.Expired)
                    .Where(x => x.Name?.Contains(
#if RI_WIN_X64
                        "-win-x64-"
#elif RI_OSX_X64
                        "-osx-x64-"
#elif RI_LINUX_X64
                        "-linux-x64-"
#elif RI_LINUX_MUSL_X64
                        "-linux-musl-x64-"
#elif RI_LINUX_ARM
                        "-linux-arm-"
#else
                        ""
#endif
                    ) ?? false)
                    .Where(x => long.TryParse(x.Name?.Split('-').LastOrDefault(), out var number) && number > RunNumber)
                    .FirstOrDefault();

                var redirector = await http.GetAsync(artifact?.ArchiveDownloadUrl);

                return redirector.Headers.Location;
            }
            catch
            {
                return null;
            }
        }

        public static string? AuthorizeKey(string key)
        {
            try
            {
                var t = key[3..].Split('-');
                var p = int.Parse(key[..3]);
                var q = int.Parse(t[0]);
                var r = int.Parse(t[1]);
                var s = int.Parse(t[2]);
                var u = Convert.ToBase64String(_woff2.Where((_, i) => i % p == _woff2[r] || i % p == q).Reverse().Take(_woff2[_woff2[s]]).ToArray());

                return u.Length == 0x120 ? u : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class ArtifactCollection
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("artifacts")]
        public Artifact[]? Artifacts { get; set; }
    }

    public class Artifact
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("node_id")]
        public string? NodeId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("size_in_bytes")]
        public long SizeInBytes { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("archive_download_url")]
        public string? ArchiveDownloadUrl { get; set; }

        [JsonPropertyName("expired")]
        public bool Expired { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
