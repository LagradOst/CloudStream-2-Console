using Jint;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
    class TwistMoeBloatFreeProvider : BloatFreeBaseAnimeProvider
    {
        static string HTMLGet(string uri, string referer, bool br = false, List<Cookie> cookies = null, List<string> keys = null, List<string> values = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            request.Method = "GET";
            request.ContentType = "text/html; charset=UTF-8";
            request.UserAgent = USERAGENT;
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            if (values != null) {
                for (int i = 0; i < values.Count; i++) {
                    request.Headers.Add(keys[i], values[i]);//"x-access-token", "1rj2vRtegS8Y60B3w3qNZm5T2Q0TN2NR");
                }
            }
            request.Referer = referer;
            request.CookieContainer = new CookieContainer();

            if (cookies != null) {
                for (int i = 0; i < cookies.Count; i++) {
                    request.CookieContainer.Add(new Uri(referer), cookies[i]);
                }
            }

            request.Headers.Add("TE", "Trailers");

            try {
                using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (br) {
                    return "";
                }
                else {
                    print(response.ResponseUri);
                    using Stream stream = response.GetResponseStream();
                    // print("res" + response.StatusCode);
                    foreach (string e in response.Headers) {
                        // print("Head: " + e);
                    }
                    // print("LINK:" + response.GetResponseHeader("Set-Cookie"));
                    using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    string result = reader.ReadToEnd();
                    return result;
                }
            }
            catch (Exception) {
                return "";
            }
        }

        static string FetchMoeUrlFromSalted(string _salted)
        {
            static byte[] CreateMD5Byte(byte[] input)
            {
                using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] hashBytes = md5.ComputeHash(input);
                return hashBytes;
            }

            static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
            {
                if (cipherText == null || cipherText.Length <= 0)
                    throw new ArgumentNullException("cipherText");
                if (Key == null || Key.Length <= 0)
                    throw new ArgumentNullException("Key");
                if (IV == null || IV.Length <= 0)
                    throw new ArgumentNullException("IV");
                string plaintext = null;
                using (Aes aesAlg = Aes.Create()) {
                    aesAlg.Key = Key;
                    aesAlg.IV = IV;
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.PKCS7;
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using MemoryStream msDecrypt = new MemoryStream(cipherText);
                    using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                    using StreamReader srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8);
                    plaintext = srDecrypt.ReadToEnd();
                }
                return plaintext;
            }

            static byte[] SubArray(byte[] data, int index, int length)
            {
                byte[] result = new byte[length];
                Array.Copy(data, index, result, 0, length);
                return result;
            }

            static byte[] Combine(params byte[][] arrays)
            {
                byte[] rv = new byte[arrays.Sum(a => a.Length)];
                int offset = 0;
                foreach (byte[] array in arrays) {
                    System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                    offset += array.Length;
                }
                return rv;
            }

            static byte[] bytes_to_key(byte[] data, byte[] _salt, int output = 48)
            {
                data = Combine(data, _salt);
                byte[] _key = CreateMD5Byte(data);
                List<byte> final_key = _key.ToList();
                while (final_key.Count < output) {
                    _key = CreateMD5Byte(Combine(_key, data));
                    final_key.AddRange(_key);
                }
                return SubArray(final_key.ToArray(), 0, output);
            }

            const string KEY = "LXgIVP&PorO68Rq7dTx8N^lP!Fa5sGJ^*XK";
            var f = System.Convert.FromBase64String(_salted);
            var salt = SubArray(f, 8, 8);
            var bytes = System.Text.Encoding.ASCII.GetBytes(KEY);
            byte[] key_iv = bytes_to_key(bytes, salt, 32 + 16);
            byte[] key = SubArray(key_iv, 0, 32);

            byte[] iv = SubArray(key_iv, 32, 16);
            return FindHTML(DecryptStringFromBytes_Aes(SubArray(f, 16, f.Length - 16), key, iv) + "|", "/", "|").Replace(" ", "%20");
        }

        public static string GetHTMLF(string url, bool en = true, string overrideReferer = null)
        {
            string html = string.Empty;

            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                // List<string> heads = new List<string>(); // HEADERS
                /*
                heads = HeadAdd("");
                for (int i = 0; i < heads.Count; i++) {
                    try {
                        request.Headers.Add(HeadToRes(heads[i], 0), HeadToRes(heads[i], 1));
                        print("PRO:" + HeadToRes(heads[i], 0) + ": " + HeadToRes(heads[i], 1));

                    }
                    catch (Exception) {

                    }
                }
                */
                WebHeaderCollection myWebHeaderCollection = request.Headers;
                if (en) {
                    myWebHeaderCollection.Add("Accept-Language", "en;q=0.8");
                }
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.UserAgent = USERAGENT;
                request.Referer = overrideReferer ?? url;
                //request.AddRange(1212416);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())

                using (StreamReader reader = new StreamReader(stream)) {
                    print(">>>>>>> " + response.ResponseUri);
                    html = reader.ReadToEnd();
                }
                return html;
            }
            catch (Exception) {
                return "";
            }

        }

        static string tokenCook = "";
        static string openToken = "";
        static bool isFetching = false;
        static bool hasLoaded = false;
        delegate char rType(uint id);

        void Setup()
        {
            if (isFetching) return;
            if (openToken != "" && tokenCook != "") return;
            if (hasLoaded) return;
            if (!Settings.IsProviderActive(Name)) return;

            isFetching = true;
            try {

                string d = GetHTMLF("https://twist.moe/");
                string code = FindHTML(d, "<script>", "</script>").Replace("e(r);", ""); // alert(r);
                string token = "";
                string token2 = "";
                tokenCook = "";
                //code = code.Replace("String.fromCharCode", "tocharf");

                var engine = new Engine();

                engine.Execute(code);
                // engine.EvaluateExpression(new Jint.Parser.ParserException())
                /*engine.SetValue("alert", new Action<char>((a) => {
                    print(a);
                    token = a.ToString(); 
                }));*///.SetValue("log", new Action<string>((a) => { token2 = a; })); ;
                token = engine.Execute(code).GetValue("r").ToString(); ;
                char fChar = token[0];
                token = token.Replace("location.reload();", $"alert({fChar}); alertCook(doccookie);");
                string find = "+ \';path=";
                token = token.Replace(find + "" + FindHTML(token, find, "\'") + "\'", "").Replace("document.cookie=", "var doccookie=");

                print("TWISTTOKEN; " + token);
                var engine2 = new Engine()
                         .SetValue("alert", new Action<string>((a) => { token2 = a; })).SetValue("alertCook", new Action<string>((a) => { tokenCook = a; }));
                engine2.Execute(token);

                string _d = HTMLGet("https://twist.moe/", "https://twist.moe/", cookies: new List<Cookie>() { new Cookie() { Name = FindHTML("|" + tokenCook, "|", "="), Value = FindHTML(tokenCook + "|", "=", "|"), Expires = DateTime.Now.AddSeconds(1000) } });
                openToken = "";
                string lookFor = "<link href=\"/_nuxt/";
                while (_d.Contains(lookFor)) {
                    if (openToken == "") {
                        string ___d = FindHTML(_d, lookFor, "\"");
                        if (___d.EndsWith(".js")) {
                            string dKey = DownloadString("https://twist.moe/_nuxt/" + ___d);
                            openToken = FindHTML(dKey, "x-access-token\":\"", "\"");
                            //x-access-token":"
                        }
                    }
                    _d = RemoveOne(_d, lookFor);
                }

                string allD = HTMLGet("https://twist.moe/api/anime", "https://twist.moe/", cookies: new List<Cookie>() { new Cookie() { Name = FindHTML("|" + tokenCook, "|", "="), Value = FindHTML(tokenCook + "|", "=", "|"), Expires = DateTime.Now.AddSeconds(1000) } }, keys: new List<string>() { "x-access-token" }, values: new List<string>() { openToken });
                //  print("ALLD: " + allD);
                MoeItem[] allItems = JsonConvert.DeserializeObject<MoeItem[]>(allD);
                foreach (var item in allItems) {
                    if (item.mal_id != null) {
                        var slug = item?.slug?.slug;
                        //print("MALID: " + item.mal_id + "|" + slug);
                        if (slug != null) {
                            twistMoeSearch[(int)item.mal_id] = slug;
                        }
                    }
                }
                hasLoaded = true;
            }
            catch (Exception _ex) {
                error("TWIST.MOE ERROR: " + _ex);
            }
            finally {
                isFetching = false;
            }
        }

        public class MoeSlug
        {
            //public int id { get; set; }
            public string? slug { get; set; }
            //public int anime_id { get; set; }
            //public string created_at { get; set; }
            //public string updated_at { get; set; }
        }

        public class MoeItem
        {
            // public int id { get; set; }
            // public string title { get; set; }
            // public string alt_title { get; set; }
            // public int season { get; set; }
            // public int ongoing { get; set; }
            // public int hb_id { get; set; }
            // public string created_at { get; set; }
            // public string updated_at { get; set; }
            // public int hidden { get; set; }
            public int? mal_id { get; set; }
            public MoeSlug? slug { get; set; }
        }

        public class TwistMoeRoot
        {
            public List<MoeItem> MyArray { get; set; }
        }

        /// <summary>
        /// GIVEN MAL ID, RETURN SLUG
        /// </summary>
        static Dictionary<int, string> twistMoeSearch = new Dictionary<int, string>();


        public class MoeSource
        {
            // public int id { get; set; }
            public string source { get; set; }
            // public int number { get; set; }
            // public int anime_id { get; set; }
            // public string created_at { get; set; }
            // public string updated_at { get; set; }
        }

        List<MoeSource> GetSources(string slug)
        {
            if (openToken == "" || tokenCook == "") { Setup(); }
            string d = HTMLGet($"https://twist.moe/api/anime/{slug}/sources", $"https://twist.moe/a/{slug}/1", cookies: new List<Cookie>() { new Cookie() { Name = FindHTML("|" + tokenCook, "|", "="), Value = FindHTML(tokenCook + "|", "=", "|"), Expires = DateTime.Now.AddSeconds(1000) } }, keys: new List<string>() { "x-access-token" }, values: new List<string>() { openToken });
            if (d.IsClean()) {
                return JsonConvert.DeserializeObject<List<MoeSource>>(d);
            }
            return null;
        }

        public TwistMoeBloatFreeProvider(CloudStreamCore _core) : base(_core)
        {
            if (isFetching) return;
            if (openToken != "" && tokenCook != "") return;
            Thread t = new Thread(() => {
                Thread.Sleep(5000);
                Setup();
            });
            t.Start();
        }

        public override string Name => "Twist";
        public override bool HasDub => false;
        public override object StoreData(string year, TempThread tempThred, MALData malData)
        {
            return null;
        }

        public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
        {
            int id = ms.MalId;
            if (twistMoeSearch.ContainsKey(id)) {
                string slug = twistMoeSearch[id];
                var sources = GetSources(slug);
                return new NonBloatSeasonData() {
                    subEpisodes = sources.Select(t => t.source).ToList(),
                    extraData = $"https://twist.moe/a/{slug}/1"
                };
            }
            return new NonBloatSeasonData();
        }

        public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
        {
            string source = FetchMoeUrlFromSalted(episodeLink);
            if (source == "") return;
            string url = "https://twist.moe/" + source;

            AddPotentialLink(normalEpisode, new BasicLink() {
                referer = extraData.ToString(),
                originSite = Name,
                isAdvancedLink = true,
                name = "Twist.Moe",
                priority = 10,
                canNotRunInVideoplayer = true,
                baseUrl = "https://twist.moe/" + source // "https://twistcdn.bunny.sh/" + source
            });
        }
    }
}
