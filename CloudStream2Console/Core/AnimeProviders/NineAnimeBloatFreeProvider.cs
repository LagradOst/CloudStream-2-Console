using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Linq;
using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
    public class NineAnimeBloatFreeProvider : BloatFreeBaseAnimeProvider
    {
        public NineAnimeBloatFreeProvider(CloudStreamCore _core) : base(_core) { }

        public override string Name => "9Anime";

        [System.Serializable]
        public struct NineAnimeEpisodeData
        {
            public int ep;
            public string href;
            public string id;
        }

        public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData,bool isDub)
        {
            string request = episodeLink.Replace("https://9anime.to/watch/", "") + "/"; // /xrrj358";
            string url = "https://9anime.to/watch/" + request;
            string d = DownloadString(url);
            string key = core.GetMcloudKey(url);

            string dataTs = FindHTML(d, "data-ts=\"", "\"");
            string _id = FindHTML(request, ".", "/");
            // string _endId = FindHTML(request + "|||", "/", "|||");
            string _under = rng.Next(100, 999).ToString();
            // print(_id + "|" + _endId);

            string requestServer = $"https://9anime.to/ajax/film/servers?id={_id}&ts={dataTs}&_={_under}"; // &episode={_endId}
            string serverResponse = DownloadString(requestServer).Replace("\\", "").Replace("  ", "");
            //   print(serverResponse);
            string real = FindHTML(serverResponse, "{\"html\":\"", "\"}");
            // real = real.Substring(0, real.Length - 2);
            print(real);


            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(real);
            var data = doc.QuerySelectorAll("div.server");

            List<NineAnimeEpisodeData> Streamtape = new List<NineAnimeEpisodeData>();
            List<NineAnimeEpisodeData> Mp4upload = new List<NineAnimeEpisodeData>();
            List<NineAnimeEpisodeData> MyCloud = new List<NineAnimeEpisodeData>();
            const string myCloudId = "28";
            const string streamtapeId = "40";
            const string mp4uploadId = "35";


            foreach (var subData in data) {

                string name = subData.GetAttributeValue("ndata-id", "");

                foreach (var selectors in subData.QuerySelectorAll("ul > li > a")) {
                    string subId = selectors.GetAttributeValue("ndata-id", "");
                    string subEp = selectors.GetAttributeValue("ndata-base", "");
                    string href = selectors.GetAttributeValue("nnhref", "");
                    int realEp = int.Parse(selectors.InnerText);
                    var storeData = new NineAnimeEpisodeData() {
                        ep = realEp,
                        href = href,
                        id = subId,
                    };

                    if (name == myCloudId) {
                        MyCloud.Add(storeData);
                    }
                    else if (name == streamtapeId) {
                        Streamtape.Add(storeData);
                    }
                    else if (name == mp4uploadId) {
                        Mp4upload.Add(storeData);
                    }

                }
            }

            try {
                string target = core.GetTarget(Mp4upload[normalEpisode].id, key, dataTs, "https://9anime.to", url);
                AddMp4(FindHTML(target, "embed-", "."), normalEpisode, tempThred);
            }
            catch (Exception) {

            }

            try {
                core.AddStreamTape(Streamtape[normalEpisode].id, key, dataTs, "https://9anime.to", normalEpisode, url);
            }
            catch (Exception) {

            }

            try {
                core.AddMCloud(MyCloud[normalEpisode].id, key, dataTs, "https://9anime.to", normalEpisode, url); 
            }
            catch (Exception) {

            }

        }

        public override object StoreData(string year, TempThread tempThred, MALData malData)
        {
            return Search(malData.engName);
        }

        public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
        {
            List<NineAnimeDataSearch> data = (List<NineAnimeDataSearch>)storedData;
            print("START:::: " + data.FString());
            NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
            foreach (var subData in data) {
                if (subData.names.Split(' ').Contains(ms.japName)) {
                    bool isDub = subData.isDub;
                    if (isDub && !setData.dubExists) {
                        for (int i = 1; i <= subData.maxEp; i++) {
                            setData.dubEpisodes.Add(subData.href);
                        }
                    }
                    else if (!setData.subExists) {
                        for (int i = 1; i <= subData.maxEp; i++) {
                            setData.subEpisodes.Add(subData.href);
                        }
                    }
                }
            }
            return setData;
        }

        [Serializable]
        public struct NineAnimeDataSearch
        {
            public string title;
            public string names;
            public bool isDub;
            public string href;
            public int maxEp;
        }

        List<NineAnimeDataSearch> Search(string search)
        {
            List<NineAnimeDataSearch> searchData = new List<NineAnimeDataSearch>();

            string url = "https://9anime.to/search?keyword=" + search;
            string d = DownloadString(url);
            print(d);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(d);
            var data = doc.QuerySelector("div.film-list");
            var items = data.QuerySelectorAll("div.item > div.inner > a");
            foreach (var item in items) {
                string href = item.GetAttributeValue("href", "");
                string dataTip = item.GetAttributeValue("data-tip", "");

                if (dataTip != "") {
                    string _d = DownloadString("https://9anime.to" + dataTip, referer: url);
                    int.TryParse(FindHTML(_d, "Episode ", "/"), out int maxEp);
                    if (maxEp != 0) { // IF NOT MOVIE 
                        string otherNames = FindHTML(_d, "<label>Other names:</label>\n            <span>", "<").Replace("  ", "").Replace("\n", "").Replace(";", "");
                        string title = FindHTML(_d, "data-jtitle=\"", "\"");
                        print(maxEp + "|" + title + "|" + otherNames.FString());
                        bool isDub = title.Contains("(Dub)");
                        searchData.Add(new NineAnimeDataSearch() { isDub = isDub, maxEp = maxEp, href = href, names = otherNames, title = title.Replace(" (Dub)", "").Replace("  ", "") });
                    }
                }
            }
            return searchData;
        }
    }
}
