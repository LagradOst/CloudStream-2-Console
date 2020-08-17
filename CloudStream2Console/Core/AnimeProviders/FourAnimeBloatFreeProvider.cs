using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
    class FourAnimeBloatFreeProvider : BloatFreeBaseAnimeProvider
    {
        public FourAnimeBloatFreeProvider(CloudStreamCore _core) : base(_core) { }
        //public override bool HasDub => false; // some are dub
        public override string Name => "4Anime";

        public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
        {
            if (!episodeLink.IsClean()) return;
            string d = DownloadString(episodeLink, tempThred);
            string url = FindHTML(d, "source src=\"", "\"");
            AddPotentialLink(normalEpisode, url, "4Anime GoogleVideo", 10);
        }

        [System.Serializable]
        struct FourAnimeQuickSearchItem
        {
            public string href;
            public string title;
            public string year;
        }

        public override object StoreData(string year, TempThread tempThred, MALData malData)
        {
            try {
                string search = activeMovie.title.name;
                string data = $"action=ajaxsearchlite_search&aslp={search}&asid=1&options=qtranslate_lang%3D0%26set_intitle%3DNone%26customset%255B%255D%3Danime";

                string d = core.PostRequest("https://4anime.to/wp-admin/admin-ajax.php", "https://4anime.to/", data);
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(d);
                var items = doc.QuerySelectorAll("div.info");
                List<FourAnimeQuickSearchItem> searchItems = new List<FourAnimeQuickSearchItem>();

                foreach (var item in items) {
                    try {
                        var link = item.QuerySelector("> a");
                        var localYear = item.QuerySelectorAll("> div > span > span > a")[1];
                        searchItems.Add(new FourAnimeQuickSearchItem() {
                            href = link.GetAttributeValue("href", ""),
                            title = link.InnerHtml,
                            year = localYear.InnerText,
                        });
                    }
                    catch (Exception _ex) {
                        error("Error EX parsing item in 4Anime: " + _ex);
                    }
                }
                return searchItems;
            }
            catch (Exception _ex) {
                error("Fatal EX in 4Anime: " + _ex);
                return new List<FourAnimeQuickSearchItem>();
            }
        }

        struct FourAnimeEpisode
        {
            public int episode;
            public string href;
        }

        public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
        {
            try {
                List<FourAnimeQuickSearchItem> data = (List<FourAnimeQuickSearchItem>)storedData;
                NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
                foreach (var subData in data) {
                    var _title = ToLowerAndReplace(subData.title);
                    if ((ms.Year == -1 || (subData.year == ms.Year.ToString())) && (
                        _title == ToLowerAndReplace(ms.engName) ||
                        _title == ToLowerAndReplace(ms.name) ||
                        ms.synonyms.Select(t => ToLowerAndReplace(t)).Contains(_title))) { // CHECK

                        string url = subData.href;
                        string d = DownloadString(url, tempThread);
                        if (!d.IsClean()) continue;

                        bool isSub = d.Contains("/language/subbed"); // There is some dubbed anime

                        if ((isSub && !setData.subExists) || (!isSub && !setData.dubExists)) { // To prevent duplicates, first result if often right
                            var doc = new HtmlAgilityPack.HtmlDocument();
                            doc.LoadHtml(d);
                            var episodes = doc.QuerySelectorAll("ul.episodes.range.active > li > a");
                            List<FourAnimeEpisode> localEpisodes = new List<FourAnimeEpisode>();

                            foreach (var f in episodes) { // This is to really make sure no offset happends, if for some reason some episode is misson
                                int.TryParse(f.InnerText, out int ep);
                                if (ep > 0) {
                                    localEpisodes.Add(new FourAnimeEpisode() {
                                        episode = ep,
                                        href = f.GetAttributeValue("href", "")
                                    });
                                }
                            }

                            if (localEpisodes.Count == 0) continue;
                            int maxEp = localEpisodes.OrderBy(t => -t.episode).First().episode;
                            var list = new string[maxEp].ToList();
                            foreach (var item in localEpisodes) {
                                list[item.episode - 1] = item.href;
                            }

                            if (isSub) {
                                setData.subEpisodes = list;
                            }
                            else {
                                setData.dubEpisodes = list;
                            }
                        }
                    }
                }
                return setData;
            }
            catch (Exception _ex) {
                error("Fatal Ex in 4Anime Getseason: " + _ex);
                return new NonBloatSeasonData();
            }
        }
    }
}
