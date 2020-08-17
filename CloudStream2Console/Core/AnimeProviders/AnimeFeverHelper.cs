using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudStreamForms.Core.AnimeProviders
{
    public class AnimeFeverHelper
    {
        public class AnimeFeverSearchPoster
        {
            public string source { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class AnimeFeverSearchPoster2
        {
            public int id { get; set; }
            public string disk_name { get; set; }
            public int file_size { get; set; }
            public string content_type { get; set; }
            public object title { get; set; }
            public object description { get; set; }
            public string field { get; set; }
            public int sort_order { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
            public string path { get; set; }
            public string extension { get; set; }
        }

        public class AnimeFeverSearchLogo
        {
            public int id { get; set; }
            public string disk_name { get; set; }
            public int file_size { get; set; }
            public string content_type { get; set; }
            public object title { get; set; }
            public object description { get; set; }
            public string field { get; set; }
            public int sort_order { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
            public string path { get; set; }
            public string extension { get; set; }
        }

        public class AnimeFeverSearchDatum
        {
            public int id { get; set; }
            public string uid { get; set; }
            public object anilist_id { get; set; }
            public string name { get; set; }
            public string alt_name { get; set; }
            public string description { get; set; }
            public object trailer { get; set; }
            public string status { get; set; }
            public string type { get; set; }
            public int episode_count { get; set; }
            public string parental_rating { get; set; }
            public string release_date { get; set; }
            public string end_date { get; set; }
            public string broadcast { get; set; }
            public string premiered { get; set; }
            public string episode_at { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
            public int episodes_count { get; set; }
            public string slug { get; set; }
            public bool is_collected { get; set; }
            public object collection_status { get; set; }
            public List<AnimeFeverSearchPoster> posters { get; set; }
            public object backgrounds { get; set; }
            public AnimeFeverSearchPoster2 poster { get; set; }
            public AnimeFeverSearchLogo logo { get; set; }
            public object background { get; set; }
        }

        public class AnimeFeverSearchInfo
        {
            public int current_page { get; set; }
            public List<AnimeFeverSearchDatum> data { get; set; }
            public string first_page_url { get; set; }
            public int from { get; set; }
            public int last_page { get; set; }
            public string last_page_url { get; set; }
            public object next_page_url { get; set; }
            public string path { get; set; }
            public int per_page { get; set; }
            public object prev_page_url { get; set; }
            public int to { get; set; }
            public int total { get; set; }
        }
         
        public class AnimeFeverEpisodeImage
        {
            public int id { get; set; }
            public string disk_name { get; set; }
            public int file_size { get; set; }
            public string content_type { get; set; }
            public object title { get; set; }
            public object description { get; set; }
            public string field { get; set; }
            public int sort_order { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
            public string path { get; set; }
            public string extension { get; set; }
        }

        public class AnimeFeverEpisodeVideoMeta
        {
            public List<string> audio_languages { get; set; }
            public string status { get; set; }
            public long download_size { get; set; }
        }

        public class AnimeFeverEpisodeDatum
        {
            public int id { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string number { get; set; }
            public int duration { get; set; }
            public AnimeFeverEpisodeImage image { get; set; }
            public AnimeFeverEpisodeVideoMeta video_meta { get; set; }
            public int is_filler { get; set; }
            public int is_recap { get; set; }
            public bool watched { get; set; }
            public object progress { get; set; }
        }

        public class AnimeFeverEpisodeLinks
        {
            public string first { get; set; }
            public string last { get; set; }
            public object prev { get; set; }
            public object next { get; set; }
        }

        public class AnimeFeverEpisodeMeta
        {
            public int current_page { get; set; }
            public int from { get; set; }
            public int last_page { get; set; }
            public string path { get; set; }
            public int per_page { get; set; }
            public int to { get; set; }
            public int total { get; set; }
        }

        public class AnimeFeverEpisodeInfo
        {
            public List<AnimeFeverEpisodeDatum> data { get; set; }
            public AnimeFeverEpisodeLinks links { get; set; }
            public AnimeFeverEpisodeMeta meta { get; set; }
        }

        static readonly string[] headerValue = new string[] { "animefever", "cloudflare" };
        static readonly string[] headerName = new string[] { "AF-Access-API", "server-provider" };
        public AnimeFeverSearchInfo GetSearchResults(string search, bool isMovie)
        {
            /*   webRequest.Headers.Add("AF-Access-API", "animefever");
            webRequest.Headers.Add("server-provider", "cloudflare");*/
            string qry = $"https://www.animefever.tv/api/anime/shows?search={search}&sortBy=name+asc&type[]={(isMovie ? "Movie" : "TV")}&hasVideos=true&hasMultiAudio=false&page=1";
            string d = core.DownloadString(qry, referer: "https://www.animefever.tv/series",
                headerName: headerName, headerValue: headerValue);
            if (d == "") {
                return null;
            }
            return JsonConvert.DeserializeObject<AnimeFeverSearchInfo>(d);
        }

        public AnimeFeverEpisodeInfo GetAnimeFeverEpisodeInfo(int id, string slug)
        {
            string qry = $"https://www.animefever.tv/api/anime/details/episodes?id={id}-{slug}";
            string d = core.DownloadString(qry, referer: "https://www.animefever.tv/series",
                headerName: headerName, headerValue: headerValue);
            if (d == "") {
                return null;
            }
            return JsonConvert.DeserializeObject<AnimeFeverEpisodeInfo>(d);
        }

        public string GetAnimeFeverEpisodeStream(int id)
        {
            string qry = $"  https://www.animefever.tv/video/{id}/stream.m3u8";
            string d = core.DownloadString(qry, referer: "https://www.animefever.tv/series",
                headerName: headerName, headerValue: headerValue);

            return d;
        }

        CloudStreamCore core;
        public AnimeFeverHelper(CloudStreamCore _core)
        {
            core = _core;
        }

    }

}
