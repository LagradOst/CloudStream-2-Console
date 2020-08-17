using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
    class AnimeFeverBloatFreeProvider : BloatFreeBaseAnimeProvider
    {
        AnimeFeverHelper helper;
        public AnimeFeverBloatFreeProvider(CloudStreamCore _core) : base(_core)
        {
            helper = new AnimeFeverHelper(_core);
        }

        public override string Name => "AnimeFever";

        public override object StoreData(string year, TempThread tempThred, MALData malData)
        {
            return helper.GetSearchResults(activeMovie.title.name, false);
        }

        struct AnimbeFeverVideo
        {
            public string mainUrl;
            public string name;
            public List<AdvancedAudioStream> audioStreams;
        }

        public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
        {
            try {
                if (episodeLink != "") {
                    int id = int.Parse(episodeLink);
                    string d = helper.GetAnimeFeverEpisodeStream(id);
                    if (d == "") return;

                    string[] dSplit = d.Split('\n');
                    bool nextIsVideoUrl = false;
                    string videoData = "";

                    Dictionary<string, AnimbeFeverVideo> streams = new Dictionary<string, AnimbeFeverVideo>();

                    foreach (var _line in dSplit) {
                        var line = _line.Replace(" ", "");
                        if (nextIsVideoUrl) {
                            nextIsVideoUrl = false;
                            string[] data = CoreHelpers.GetStringRegex("BANDWIDTH=?, RESOLUTION=?, AUDIO=\"?\"", videoData);
                            if (data == null) continue;
                            string key = data[2];
                            if (data != null) {
                                if (streams.ContainsKey(key)) {
                                    var _stream = streams[key];
                                    _stream.mainUrl = line;
                                    _stream.name = data[1];
                                    streams[key] = _stream;
                                }
                                else {
                                    error("FATAL EX MISSMATCH IN ANIMEFEBEER:: " + d);
                                }
                            }
                            print("VIDEOURL: " + data[0]);
                        }
                        else {
                            if (line.StartsWith("#EXT-X-STREAM-INF:BANDWIDTH")) { // VIDEO
                                nextIsVideoUrl = true;
                                videoData = line;
                            }
                            else if (line.StartsWith("#EXT-X-MEDIA:TYPE=AUDIO")) { // AUDIO
                                string[] data = CoreHelpers.GetStringRegex("GROUP-ID=\"?\" NAME=\"?\" LANGUAGE=\"?\" URI=\"?\"", line);
                                if (data != null) {
                                    string key = data[0];
                                    string name = data[1];
                                    string url = data[3];
                                    int prio = 0;
                                    if ((isDub && name.ToLower().StartsWith("eng")) || (!isDub && (name.ToLower().StartsWith("jap") || name.ToLower().StartsWith("jpn")))) {
                                        prio = 10;
                                    }

                                    if (streams.ContainsKey(key)) {
                                        streams[key].audioStreams.Add(new AdvancedAudioStream() {
                                            label = name,
                                            url = url,
                                            prio = prio,
                                        });
                                    }
                                    else {
                                        streams.Add(key, new AnimbeFeverVideo() {
                                            audioStreams = new List<AdvancedAudioStream>() {
                                                new AdvancedAudioStream() {
                                                    label = name,
                                                    url = url,
                                                    prio = prio,
                                                }
                                            }
                                        });
                                    }
                                }
                                print("AUDIO: " + data[3] + " | " + data[2]);
                            }
                        }
                    }

                    int videoPrio = 2;
                    foreach (var key in streams.Keys) {
                        videoPrio++;
                        var _stream = streams[key];
                        BasicLink basicLink = new BasicLink() {
                            isAdvancedLink = true,
                            originSite = Name,
                            mirror = 0,
                            name = Name,
                            label = _stream.name,
                            typeName = "m3u8",
                            baseUrl = _stream.mainUrl,
                            priority = videoPrio,
                            audioStreams = _stream.audioStreams,
                            referer = ((string[])extraData)[normalEpisode]
                        };
                        AddPotentialLink(normalEpisode, basicLink);
                    }
                }
            }
            catch (Exception) { }
        }

        public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
        {
            AnimeFeverHelper.AnimeFeverSearchInfo data = (AnimeFeverHelper.AnimeFeverSearchInfo)storedData;
            NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
            foreach (var subData in data.data) {
                if (subData.name == ms.engName || subData.alt_name == ms.engName) {
                    try {
                        var mainInfo = helper.GetAnimeFeverEpisodeInfo(subData.id, subData.slug);
                        if (mainInfo == null) continue;
                        
                        var emtyList = new string[mainInfo.data.Count].ToList();
                        var referers = new string[mainInfo.data.Count];
                        int index = 0;
                        setData.dubEpisodes = emtyList;
                        setData.subEpisodes = emtyList;
                        foreach (var epInfo in mainInfo.data) {
                            var langs = epInfo.video_meta.audio_languages;
                            if (langs.Contains("eng")) {
                                setData.dubEpisodes[index] = (epInfo.id.ToString());
                            }
                            if (langs.Contains("jap")) {
                                setData.subEpisodes[index] = (epInfo.id.ToString());
                            }
                            referers[index] = $"https://www.animefever.tv/series/{subData.id}-{subData.slug}/episode/{epInfo.id}-episode-{index+1}-{epInfo.slug}";
                            print("ANIMEFEVER REFERER: " + referers[index]);
                            index++;
                        }
                        setData.extraData = referers;

                    }
                    catch (Exception) { }
                } 
            }
            return setData;
        }
    }
}
