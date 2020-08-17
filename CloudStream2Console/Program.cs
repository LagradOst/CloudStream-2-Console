using Jint;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
//using Android.Util;
//using Android.Content;
using System.Threading.Tasks;
using HtmlAgilityPack.CssSelectors;
using HtmlAgilityPack.CssSelectors.NetCore;
using CloudStreamForms.Core;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms
{

    public static class App
    {
        public static int ConvertDPtoPx(int dp)
        {
            return dp * 4;
        }

        public static void ShowToast(string toast)
        {

        }

        public static int GetSizeOfJumpOnSystem()
        {
            return 1024;
        }

        static string GetKeyPath(string folder, string name = "")
        {
            string _s = ":" + folder + "-";
            if (name != "") {
                _s += name + ":";
            }
            return _s;
        }
        static Dictionary<string, object> Properties = new Dictionary<string, object>();
        public static void SetKey(string folder, string name, object value)
        {
            string path = GetKeyPath(folder, name);
            if (Properties.ContainsKey(path)) {
                Properties[path] = value;
            }
            else {
                Properties.Add(path, value);
            }
        }

        public static T GetKey<T>(string folder, string name, T defVal)
        {
            string path = GetKeyPath(folder, name);
            return GetKey<T>(path, defVal);
        }

        public static void RemoveFolder(string folder)
        {
            List<string> keys = App.GetKeysPath(folder);
            for (int i = 0; i < keys.Count; i++) {
                RemoveKey(keys[i]);
            }
        }

        public static T GetKey<T>(string path, T defVal)
        {
            if (Properties.ContainsKey(path)) {
                return (T)Properties[path];
            }
            else {
                return defVal;
            }
        }

        public static List<T> GetKeys<T>(string folder)
        {
            List<string> keyNames = GetKeysPath(folder);

            List<T> allKeys = new List<T>();
            foreach (var key in keyNames) {
                allKeys.Add((T)Properties[key]);
            }

            return allKeys;
        }

        public static int GetKeyCount(string folder)
        {
            return GetKeysPath(folder).Count;
        }
        public static List<string> GetKeysPath(string folder)
        {
            List<string> keyNames = Properties.Keys.Where(t => t.StartsWith(GetKeyPath(folder))).ToList();
            return keyNames;
        }

        public static bool KeyExists(string folder, string name)
        {
            string path = GetKeyPath(folder, name);
            return KeyExists(path);
        }
        public static bool KeyExists(string path)
        {
            return (Properties.ContainsKey(path));
        }
        public static void RemoveKey(string folder, string name)
        {
            string path = GetKeyPath(folder, name);
            RemoveKey(path);
        }
        public static void RemoveKey(string path)
        {
            if (Properties.ContainsKey(path)) {
                Properties.Remove(path);
            }
        }
    }

    public static class Settings
    {
        public static string NativeSubShortName = "eng";

        public static bool SubtitlesEnabled = false;
        public static bool DefaultDub = true;
        public static bool CacheImdb = true;
        public static bool CacheMAL = true;
        public static bool IgnoreSSLCert = true;
        public static bool UseAniList = false;
        public static bool IsProviderActive(string name)
        {
            return true;
        }
    }
}

namespace CloudStream2Console
{
    class Program
    {
        const string SEARCH_FOR_PREFIX = "Search: ";
        const bool LIVE_SEARCH = false;
        public static CloudStreamCore core = new CloudStreamCore();

        /// <summary>
        /// 0 = Search, 1 = EPView, 2 = Links
        /// </summary>
        static int currentView = 0;
        static string currentSearch = "";
        static List<Poster> searchPosters = new List<Poster>();
        static List<Episode> currentEpisodes = new List<Episode>();
        static int currentSeason = 1;
        static int epSelect = -1;
        static bool isDub = true;
        static bool dubExists = false;
        static bool subExists = false;
        static int maxEpisodes = -1;
        static int loadLinkEpisodeSelected = -1;
        static int currentMaxEpisodes { get { if (currentMovie.title.movieType == MovieType.Anime) { return Math.Min(maxEpisodes, currentEpisodes.Count); } else { return currentEpisodes.Count; } } }

        static Movie currentMovie = new Movie();
        static int selected = -1;

        static void PrintSearch()
        {
            Console.Clear();

            string s(int sel)
            {
                return (selected == sel ? "> " : "");
            }

            Console.WriteLine(s(-1) + SEARCH_FOR_PREFIX + currentSearch);
            for (int i = 0; i < searchPosters.Count; i++) {
                Console.WriteLine(s(i) + searchPosters[i].name + " (" + (searchPosters[i].year) + ")");
            }
        }


        static bool IsEnglishLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }
        static bool IsEnglishNumber(char c)
        {
            return (c >= '0' && c <= '9');
        }

        static List<Link> currentLinks = new List<Link>();

        static void Main(string[] args)
        {

            string __d = ShareMovieCode("dd");
            print(__d);
            Console.ReadKey();
            Console.Title = "CloudStream 2 Console";
            PrintSearch();

            core.searchLoaded += (o, e) => {
                if (e != searchPosters) {
                    if (currentView == 0) {
                        searchPosters = e;
                        selected = -1;
                        PrintSearch();
                    }
                }
            };

            void UpdateTitle()
            {
                if (currentView == 0) {
                    Console.Title = "Search: " + currentSearch;
                }
                else if (currentView == 1) {
                    if (currentMovie.title.name.IsClean()) {
                        Console.Title = currentMovie.title.name + " (" + currentMovie.title.year + ")";
                    }
                    else {
                        Console.Title = "Loading Title";
                    }
                }
                else if (currentView == 2) {
                    Console.Title = (!currentMovie.title.IsMovie ? $"S{currentSeason}:E{loadLinkEpisodeSelected + 1} - " : "") + currentEpisodes[loadLinkEpisodeSelected].name;
                }
            }

            void PrintCurrentTitle()
            {
                UpdateTitle();
                Console.Clear();
                Console.WriteLine(currentMovie.title.name + " IMDb:" + currentMovie.title.rating + " (" + currentMovie.title.year + ")");
            }

            int progressFish = 100;

            fishProgressLoaded += (o, e) => {
                if (e.currentProgress != 0) {
                    progressFish = (int)(100 * e.currentProgress / e.maxProgress);

                    print("PROGRES::: " + progressFish);
                    RenderEveryTitle();
                }
            };


            void RenderEpisodes()
            {
                if (progressFish != 100 && currentMovie.title.movieType == MovieType.Anime) {
                    int max = 10;
                    Console.WriteLine("Loading [" + CloudStreamCore.MultiplyString("=", progressFish / max) + CloudStreamCore.MultiplyString("-", max - progressFish / max) + "]");
                }
                else if (currentEpisodes.Count != 0) {
                    if (currentMovie.title.movieType == MovieType.Anime) {
                        Console.WriteLine((epSelect == -2 ? "< " : "") + (isDub ? "Dub" : "Sub") + (epSelect == -2 ? " >" : ""));
                    }
                    if (!currentMovie.title.IsMovie) {
                        Console.WriteLine((epSelect == -1 ? "< " : "") + "Season " + currentSeason + (epSelect == -1 ? " >" : ""));
                    }
                }

                for (int i = 0; i < currentMaxEpisodes; i++) {
                    var ep = currentEpisodes[i];
                    Console.WriteLine((epSelect == i ? "> " : "") + (currentMovie.title.IsMovie ? "" : (i + 1) + ". ") + ep.name + (currentMovie.title.IsMovie ? "" : " (" + ep.rating + ")"));
                };
            }

            void RenderEveryTitle()
            {
                if (currentView == 1) {
                    if (currentMovie.title.id == null) {
                        Console.Clear();
                        Console.WriteLine("Loading ");
                    }
                    else {
                        PrintCurrentTitle();
                        RenderEpisodes();
                    }
                }
                else {
                    core.PurgeThreads(-1);
                }
            }

            core.titleLoaded += (o, e) => {
                currentEpisodes = new List<Episode>();
                currentMovie = e;
                if (currentView == 1) {
                    PrintCurrentTitle();
                    currentSeason = 1;
                    core.GetImdbEpisodes();
                }
            };

            core.linkAdded += (o, e) => {
                if (currentView == 2) {
                    var links = CloudStreamCore.GetCachedLink(e);
                    if (links != null) { 
                        foreach (var link in links.Value.links.Where(t => t.referer != "")) {
                            Console.WriteLine(link.baseUrl + "\n");
                        }
                    }
                }
            };

            static void SetDubSub()
            {
                maxEpisodes = core.GetMaxEpisodesInAnimeSeason(currentSeason, isDub);
            }

            core.episodeLoaded += (o, e) => {
                currentMovie = core.activeMovie;
                //  currentMovie.episodes = e;
                currentEpisodes = e;

                // DUB SUB LOGIC
                if (currentMovie.title.movieType == MovieType.Anime) {
                    core.GetSubDub(currentSeason, out bool subExists, out bool dubExists);

                    isDub = dubExists;
                    SetDubSub();
                    print("MAXEPISDES:" + maxEpisodes + "| DUBEX:" + dubExists + "| SUBEX" + subExists);
                }
                RenderEveryTitle();
            };

            void Search()
            {
                core.QuickSearch(currentSearch);
                selected = -1;
                searchPosters = new List<Poster>();
            }
            void SwitchDubState()
            {
                if (isDub && subExists) {
                    isDub = false;
                }
                else if (!isDub && dubExists) {
                    isDub = true;
                }
            }

            while (true) {
                var input = Console.ReadKey();
                char f = input.KeyChar;

                int epSelectfloor = -2;
                if (currentView == 1) {

                    if (currentMovie.title.IsMovie) {
                        epSelectfloor = 0;
                    }
                    else if (currentMovie.title.movieType != MovieType.Anime) {
                        epSelectfloor = -1;
                    }
                }

                switch (input.Key) {
                    case ConsoleKey.Escape:
                        currentView--;
                        if (currentView < 0) currentView = 0;
                        if (currentView == 0) { selected = -1; core.PurgeThreads(-1); }
                        //  if(currentView == 1) { epSelect = -1; }
                        break;
                    case ConsoleKey.DownArrow:
                        if (currentView == 0) {
                            selected++;
                            if (selected >= searchPosters.Count) {
                                selected = -1;
                            }
                        }
                        else if (currentView == 1) {
                            epSelect++;
                            if (epSelect >= currentMaxEpisodes) {
                                epSelect = epSelectfloor;
                            }
                        }
                        // handle left arrow
                        break;
                    case ConsoleKey.UpArrow:
                        if (currentView == 0) {
                            selected--;
                            if (selected < -1) {
                                selected = searchPosters.Count - 1;
                            }
                        }
                        else if (currentView == 1) {
                            epSelect--;
                            if (epSelect < epSelectfloor) {
                                epSelect = currentMaxEpisodes - 1;
                            }
                        }
                        // handle right arrow
                        break;
                    case ConsoleKey.Enter:
                        if (currentView == 0) {
                            if (selected != -1) {
                                currentView = 1;
                                epSelect = -1;
                                progressFish = 0;
                                currentMovie = new Movie();
                                core.GetImdbTitle(searchPosters[selected], autoSearchTrailer: false);
                                Console.Clear();
                                Console.WriteLine("Loading");
                            }
                            else {
                                Search();
                            }
                        }
                        else if (currentView == 1) {
                            if (epSelect == -1) {
                                currentEpisodes = new List<Episode>();
                                core.GetImdbEpisodes(currentSeason);
                            }
                            else if (epSelect == -2) {
                                SetDubSub();
                            }
                            else {
                                Console.Clear();
                                currentView = 2;
                                loadLinkEpisodeSelected = epSelect;
                                currentLinks = new List<Link>();
                                core.GetEpisodeLink(currentMovie.title.IsMovie ? -1 : epSelect + 1, currentSeason, isDub: isDub);
                            }
                        }

                        // handle right arrow
                        break;
                    case ConsoleKey.RightArrow:
                        if (currentView == 1) {
                            if (epSelect == -1) {
                                currentSeason++;
                                if (currentSeason > currentMovie.title.seasons) {
                                    currentSeason = 1;
                                }
                            }
                            else if (epSelect == -2) {
                                SwitchDubState();
                            }
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (currentView == 1) {
                            if (epSelect == -1) {
                                currentSeason--;
                                if (currentSeason <= 0) {
                                    currentSeason = currentMovie.title.seasons;
                                }
                            }
                            else if (epSelect == -2) {
                                SwitchDubState();
                            }
                        }
                        break;
                }
                if (currentView == 0) {
                    bool search = false;
                    
                    if (input.Key == ConsoleKey.Delete || input.Key == ConsoleKey.Backspace) { // DELETE
                        search = true;
                        if (currentSearch.Length >= 1) {
                            currentSearch = currentSearch.Substring(0, currentSearch.Length - 1);
                        }
                    }
                    else if (IsEnglishLetter(f) || f == ' ' || IsEnglishNumber(f)) {
                        currentSearch += f;
                        search = true;
                    }
                    if (search) {
                        if (LIVE_SEARCH) {
                            Search();
                        }
                    }
                    PrintSearch();
                }
                else if (currentView == 1) {
                    RenderEveryTitle();
                    print("sel:" + epSelect);
                }
                UpdateTitle();
            }
        }
    } 
}
