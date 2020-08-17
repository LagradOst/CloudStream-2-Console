using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Linq;
using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
    public class AnimeVibeBloatFreeProvider : BloatFreeBaseAnimeProvider
    {
        public AnimeVibeBloatFreeProvider(CloudStreamCore _core) : base(_core) { }

        public override string Name => "AnimeVibe";

        public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
        {
            string page = DownloadString(episodeLink,tempThred);
            if (!page.IsClean()) return;
            string iframe = FindHTML(page, "<iframe src=\"", "\"");
            if (iframe != "") {
                string d = DownloadString("https://animevibe.tv" + iframe);
                AddEpisodesFromMirrors(tempThred, d, normalEpisode, "", "");
            }
        }

        public override object StoreData(string year, TempThread tempThred, MALData malData)
        {
            return Search(malData.engName);
        }

        public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
        {
            List<AnimeVibeData> data = (List<AnimeVibeData>)storedData;
            NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
            foreach (var subData in data) {
                if (subData.title.Contains(ms.japName)) {
                    bool isDub = subData.isDub;
                    if (isDub && !setData.dubExists) {
                        for (int i = 1; i <= subData.maxEp; i++) {
                            setData.dubEpisodes.Add(subData.href + i);
                        }
                    }
                    else if (!setData.subExists) {
                        for (int i = 1; i <= subData.maxEp; i++) {
                            setData.subEpisodes.Add(subData.href + i);
                        }
                    }
                }
            }
            return setData;
        }


        [Serializable]
        public struct AnimeVibeData
        {
            public string href;
            public string title;
            public bool isDub;
            public int maxEp;
        }

        private List<AnimeVibeData> Search(string search)
        {
            string searchResults = DownloadString($"https://animevibe.tv/?s={search}");
            //print(search_results);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(searchResults);
            var data = doc.QuerySelectorAll("div.blogShort");
            var list = new List<AnimeVibeData>();
            for (int i = 0; i < data.Count; i++) {
                var realName = data[i].QuerySelectorAll("div.search-ex > h6");
                var link = data[i].QuerySelectorAll("a");

                bool isDub = (link[0].InnerText.Contains("(Dub)"));
                string href = (link[0].GetAttributeValue("href", ""));
                string title = (realName[0].InnerText);
                int maxEp = (int.Parse(FindHTML(realName[3].InnerText, ":", "Episode(s)")));
                list.Add(new AnimeVibeData() {
                    isDub = isDub,
                    href = href,
                    title = title,
                    maxEp = maxEp
                });
            }
            return list;
        }
    }
}
