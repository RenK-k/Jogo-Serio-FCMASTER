using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace CoachMaster.UI
{
    public sealed class MusicTrackInfo
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public bool Enabled { get; internal set; }
        public bool IsUserTrack { get; internal set; }
    }

    public static class GameAudio
    {
        const string ResourceFolder = "Audio";
        const string LibraryResource = "GameAudioLibrary";

        static GameAudioLibrary _library;
        static AudioSource _effects;
        static AudioSource _menu;
        static AudioSource _music;
        static AudioSource _crowd;
        static AudioSource _reaction;
        static string _reactionKey;
        static float _reactionLock;
        static bool _filled;
        static int _track = -1;
        static float _musicVolume = 0.5f;
        static float _effectVolume = 0.58f;
        static float _crowdVolume = 0.42f;
        static float _menuVolume = 0.5f;
        static float _masterVolume = 0.5f;
        static bool _muteAll;
        static bool _prefsDirty;
        static bool _customLoadStarted;
        static readonly List<UserTrack> UserTracks = new List<UserTrack>();
        static float _savePrefsAt;
        static float _reactUntil;
        static float _reactPeak;

        public static float MusicVolume => _musicVolume;
        public static float EffectVolume => _effectVolume;
        public static float CrowdVolume => _crowdVolume;
        public static float MenuVolume => _menuVolume;
        public static float MasterVolume => _masterVolume;
        public static bool MuteAll => _muteAll;
        public static int TrackIndex => _track;

        public static void Bind(GameAudioLibrary library)
        {
            if (library != null)
                _library = UnityEngine.Object.Instantiate(library);
            _filled = false;
            FillGaps();
            ReloadPreferences();
            EnsureCustomMusicLoaded();
        }

        public static void Click() => PlayMenuClip(Library?.Click, "click");
        public static void Goal() => PlayClip(Library?.Goal, "golo", 1.35f);
        static int _whistleLeft;
        static float _nextWhistle;

        public static void Whistle() => PlayClip(Library?.Whistle, "whistle", 1.18f);

        public static void Whistles(int count)
        {
            if (count < 1)
                return;
            Whistle();
            _whistleLeft = count - 1;
            _nextWhistle = Time.unscaledTime + 0.48f;
        }
        public static void Foul() => PlayClip(Library?.Foul, "foul");
        public static void Injury() => PlayClip(Library?.Injury, "injury");
        public static void YellowCard() => PlayClip(Library?.YellowCard, "yellow");
        public static void RedCard() => PlayClip(Library?.RedCard, "red");
        public static void Corner() => PlayClip(Library?.Corner, "corner");
        public static void ThrowIn() => PlayClip(Library?.ThrowIn, "throwin");
        public static void Kickoff() => PlayClip(Library?.Kickoff ?? Library?.Whistle, "whistle");
        public static void CrowdGoal(bool home) => React(home ? "home-goal" : "away-goal", home ? Library?.HomeGoal ?? Library?.Goal : Library?.AwayGoal);
        public static void Bicycle() => React("bicycle", Library?.BicycleKick);
        public static void ShotWide() => React("wide", Library?.ShotWide);
        public static void ShotPost() => React("post", Library?.ShotPost);
        public static void BigSave() => React("save", Library?.BigSave);
        public static void Penalty() => React("penalty", Library?.Penalty);

        public static void Play(string resourceName)
        {
            PlayClip(null, resourceName);
        }

        public static void ForFlash(string flashText)
        {
            if (string.IsNullOrEmpty(flashText))
                return;
            var upper = flashText.ToUpperInvariant();
            if (upper.Contains("GOLO") || upper.Contains("GOAL") || upper.Contains("PEN"))
                return;
            if (upper.Contains("VERMELHO") || upper.Contains("RED CARD"))
                RedCard();
            else if (upper.Contains("AMARELO") || upper.Contains("YELLOW CARD"))
                YellowCard();
            else if (upper.Contains("LES") || upper.Contains("INJUR"))
                Injury();
            else if (upper.Contains("FALTA") || upper.Contains("FOUL"))
                Foul();
            else if (upper.Contains("CANTO") || upper.Contains("CORNER"))
                Corner();
            else if (upper.Contains("LANÇAMENTO") || upper.Contains("LANCAMENTO"))
                ThrowIn();
        }

        public static void Music(int trackIndex = -1)
        {
            FillGaps();
            var tracks = Tracks();
            if (tracks.Count == 0)
            {
                if (AllTrackEntries().Count == 0)
                    StartMusic(LoadResource("music"));
                else
                {
                    _track = -1;
                    StopMusic();
                }
                return;
            }

            if (trackIndex < 0)
                trackIndex = _track >= 0 ? _track : 0;
            _track = (trackIndex % tracks.Count + tracks.Count) % tracks.Count;
            StartMusic(tracks[_track]);
        }

        public static void NextTrack()
        {
            var tracks = Tracks();
            var count = tracks.Count;
            if (count <= 1)
            {
                if (count == 1 && (_track < 0 || _music == null || _music.clip != tracks[0]))
                    Music(0);
                else if (Repeat)
                    Music(0);
                return;
            }
            Music(_track + 1);
        }

        public static void PreviousTrack()
        {
            var count = Tracks().Count;
            Music(_track < 0 && count > 0 ? 0 : _track - 1);
        }

        public static string CurrentTrackName()
        {
            var tracks = Tracks();
            if (_track < 0 || _track >= tracks.Count || tracks[_track] == null)
                return "Sem faixa";
            return Clean(tracks[_track].name);
        }

        public static List<string> TrackNames()
        {
            var names = new List<string>();
            var tracks = Tracks();
            for (var i = 0; i < tracks.Count; i++)
                names.Add(Clean(tracks[i].name));
            return names;
        }

        public static List<MusicTrackInfo> MusicLibrary()
        {
            var result = new List<MusicTrackInfo>();
            foreach (var entry in AllTrackEntries())
            {
                result.Add(new MusicTrackInfo
                {
                    Id = entry.Id,
                    Name = Clean(entry.Clip.name),
                    Enabled = IsTrackEnabled(entry.Id),
                    IsUserTrack = entry.IsUserTrack
                });
            }
            return result;
        }

        public static void SetMusicTrackEnabled(string id, bool enabled)
        {
            if (string.IsNullOrEmpty(id))
                return;
            PlayerPrefs.SetInt(TrackPreferenceKey(id), enabled ? 1 : 0);
            MarkPreferencesDirty();
            if (!enabled && _music != null && _music.clip != null && TrackId(_music.clip) == id)
            {
                var enabledTracks = Tracks();
                if (enabledTracks.Count == 0)
                {
                    _track = -1;
                    StopMusic();
                }
                else
                    Music(Mathf.Clamp(_track, 0, enabledTracks.Count - 1));
            }
        }

        public static void ImportUserMusic(MonoBehaviour host, string sourcePath, Action<bool, string> completed)
        {
            if (host == null)
            {
                completed?.Invoke(false, "The music import could not be started.");
                return;
            }
            host.StartCoroutine(ImportUserMusicRoutine(sourcePath, completed));
        }

        public static bool RemoveUserMusic(string id)
        {
            var index = UserTracks.FindIndex(track => track.Id == id);
            if (index < 0)
                return false;
            var track = UserTracks[index];
            try
            {
                if (File.Exists(track.Path))
                    File.Delete(track.Path);
            }
            catch (Exception)
            {
                return false;
            }
            var wasPlaying = _music != null && _music.clip == track.Clip;
            UserTracks.RemoveAt(index);
            if (_music != null && _music.clip == track.Clip)
                StopMusic();
            if (track.Clip != null)
                UnityEngine.Object.Destroy(track.Clip);
            PlayerPrefs.DeleteKey(TrackPreferenceKey(id));
            MarkPreferencesDirty();
            if (wasPlaying && Tracks().Count > 0)
                Music(0);
            return true;
        }

        static bool _unfocused;
        static float _grace;
        static bool _repeat;
        public static bool Repeat
        {
            get => _repeat;
            set
            {
                _repeat = value;
                PlayerPrefs.SetInt("cm_audio_repeat", value ? 1 : 0);
                MarkPreferencesDirty();
                if (_music != null)
                    _music.loop = value;
            }
        }

        public static void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("cm_audio_music", _musicVolume);
            MarkPreferencesDirty();
            ApplyVolumes();
        }

        public static void SetEffectVolume(float value)
        {
            _effectVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("cm_audio_effects", _effectVolume);
            MarkPreferencesDirty();
            ApplyVolumes();
        }

        public static void SetCrowdVolume(float value)
        {
            _crowdVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("cm_audio_crowd", _crowdVolume);
            MarkPreferencesDirty();
            ApplyVolumes();
        }

        public static void SetMenuVolume(float value)
        {
            _menuVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("cm_audio_menu", _menuVolume);
            MarkPreferencesDirty();
            ApplyVolumes();
        }

        public static void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("cm_audio_master", _masterVolume);
            MarkPreferencesDirty();
            ApplyVolumes();
        }

        public static void SetMuteAll(bool muted)
        {
            _muteAll = muted;
            PlayerPrefs.SetInt("cm_audio_mute", muted ? 1 : 0);
            MarkPreferencesDirty();
            ApplyVolumes();
        }

        public static void Tick()
        {
            if (_prefsDirty && Time.unscaledTime >= _savePrefsAt)
            {
                PlayerPrefs.Save();
                _prefsDirty = false;
            }

            if (!Application.isFocused)
            {
                if (!_unfocused)
                    ApplyVolumes();
                _unfocused = true;
                return;
            }

            if (_unfocused)
            {
                _unfocused = false;
                _grace = Time.unscaledTime + 1.2f;
                ApplyVolumes();
                if (_music != null && _music.clip != null && !_music.isPlaying)
                    _music.Play();
                return;
            }

            if (_reaction != null && _reaction.isPlaying)
            {
                var remain = _reactUntil - Time.unscaledTime;
                if (remain <= 0f)
                    _reaction.Stop();
                else
                {
                    _reactPeak = ReactionPeak();
                    _reaction.volume = _reactPeak * (remain < 0.7f ? remain / 0.7f : 1f);
                }
            }

            if (Time.unscaledTime < _grace || _music == null || _music.clip == null)
                return;
            _music.loop = Repeat;
            if (Repeat)
                return;
            var left = _music.clip.length - _music.time;
            if (_music.time > 1f && left >= 0f && left < 0.2f)
                NextTrack();
        }

        public static void StartCrowd()
        {
            FillGaps();
            var clip = Library == null ? null : Library.Crowd;
            if (clip == null)
                clip = LoadResource("crowd");
            if (clip == null)
                return;
            Ensure();
            _crowd.volume = _crowdVolume;
            ApplyVolumes();
            if (_crowd.clip == clip && _crowd.isPlaying)
                return;
            _crowd.clip = clip;
            _crowd.loop = true;
            _crowd.Play();
        }

        public static void StopMusic()
        {
            if (_music != null)
                _music.Stop();
        }

        static GameAudioLibrary Library
        {
            get
            {
                if (_library == null)
                {
                    var loaded = Resources.Load<GameAudioLibrary>(LibraryResource);
                    if (loaded == null)
                        loaded = Resources.Load<GameAudioLibrary>(ResourceFolder + "/GameAudioLibrary");
                    _library = loaded != null ? UnityEngine.Object.Instantiate(loaded) : ScriptableObject.CreateInstance<GameAudioLibrary>();
                }
                if (!_filled)
                    FillGaps();
                return _library;
            }
        }

        static void FillGaps()
        {
            var library = _library;
            if (library == null)
            {
                var loaded = Resources.Load<GameAudioLibrary>(LibraryResource);
                if (loaded == null)
                    loaded = Resources.Load<GameAudioLibrary>(ResourceFolder + "/GameAudioLibrary");
                library = loaded != null ? UnityEngine.Object.Instantiate(loaded) : ScriptableObject.CreateInstance<GameAudioLibrary>();
                _library = library;
            }

            if (library.MusicTracks == null || library.MusicTracks.Length < 10)
            {
                var sized = new AudioClip[10];
                if (library.MusicTracks != null)
                {
                    for (var i = 0; i < library.MusicTracks.Length && i < sized.Length; i++)
                        sized[i] = library.MusicTracks[i];
                }

                library.MusicTracks = sized;
            }

            var spare = new List<AudioClip>();
            foreach (var clip in Resources.LoadAll<AudioClip>(ResourceFolder))
            {
                if (clip == null)
                    continue;
                var name = clip.name.ToLowerInvariant();
                if (Contains(name, "golo", "goal"))
                    library.Goal = library.Goal ?? clip;
                else if (Contains(name, "whistle", "apito"))
                {
                    library.Whistle = library.Whistle ?? clip;
                    library.Kickoff = library.Kickoff ?? clip;
                }
                if (Contains(name, "bicicleta", "chilena", "bicycle"))
                    library.BicycleKick = library.BicycleKick ?? clip;
                else if (Contains(name, "trave", "poste", "post"))
                    library.ShotPost = library.ShotPost ?? clip;
                else if (Contains(name, "ao lado", "wide", "raspando"))
                    library.ShotWide = library.ShotWide ?? clip;
                else if (Contains(name, "grande defesa", "big save", "defesa"))
                    library.BigSave = library.BigSave ?? clip;
                else if (Contains(name, "penalti", "penalty", "penálti"))
                    library.Penalty = library.Penalty ?? clip;
                else if (Contains(name, "vaia", "boo", "golo fora", "away goal"))
                    library.AwayGoal = library.AwayGoal ?? clip;
                else if (Contains(name, "golo casa", "home goal"))
                    library.HomeGoal = library.HomeGoal ?? clip;
                else if (Contains(name, "golo", "goal"))
                    library.Crowd = library.Crowd ?? clip;
                else if (Contains(name, "foul", "falta"))
                    library.Foul = library.Foul ?? clip;
                else if (Contains(name, "injur", "lesao", "lesão"))
                    library.Injury = library.Injury ?? clip;
                else if (Contains(name, "yellow", "amarelo"))
                    library.YellowCard = library.YellowCard ?? clip;
                else if (Contains(name, "vermelho") || name.Contains("red card") || name.StartsWith("red"))
                    library.RedCard = library.RedCard ?? clip;
                else if (Contains(name, "corner", "canto"))
                    library.Corner = library.Corner ?? clip;
                else if (Contains(name, "throw", "lancamento", "lançamento"))
                    library.ThrowIn = library.ThrowIn ?? clip;
                else if (name.Contains("click"))
                    library.Click = library.Click ?? clip;
                else
                    spare.Add(clip);
            }

            var slot = 0;
            foreach (var clip in spare)
            {
                while (slot < library.MusicTracks.Length && library.MusicTracks[slot] != null)
                    slot++;
                if (slot >= library.MusicTracks.Length)
                    break;
                library.MusicTracks[slot] = clip;
                slot++;
            }

            _filled = true;
        }

        static bool Contains(string name, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (name.Contains(key))
                    return true;
            }

            return false;
        }

        static List<AudioClip> Tracks()
        {
            FillGaps();
            var list = new List<AudioClip>();
            foreach (var entry in AllTrackEntries())
            {
                if (IsTrackEnabled(entry.Id))
                    list.Add(entry.Clip);
            }

            return list;
        }

        static List<UserTrack> AllTrackEntries()
        {
            FillGaps();
            var result = new List<UserTrack>();
            var seen = new HashSet<AudioClip>();
            var builtIn = Library == null ? null : Library.MusicTracks;
            if (builtIn != null)
            {
                foreach (var clip in builtIn)
                {
                    if (clip == null || !seen.Add(clip))
                        continue;
                    result.Add(new UserTrack
                    {
                        Id = "builtin:" + clip.name,
                        Clip = clip,
                        IsUserTrack = false
                    });
                }
            }
            foreach (var track in UserTracks)
                if (track != null && track.Clip != null && seen.Add(track.Clip))
                    result.Add(track);
            return result;
        }

        static bool IsTrackEnabled(string id) => PlayerPrefs.GetInt(TrackPreferenceKey(id), 1) != 0;

        static string TrackPreferenceKey(string id)
        {
            unchecked
            {
                var hash = 2166136261u;
                for (var i = 0; i < id.Length; i++)
                    hash = (hash ^ id[i]) * 16777619u;
                return "cm_music_enabled_" + hash.ToString("x8");
            }
        }

        static string TrackId(AudioClip clip)
        {
            if (clip == null)
                return "";
            foreach (var track in UserTracks)
                if (track.Clip == clip)
                    return track.Id;
            return "builtin:" + clip.name;
        }

        static string UserMusicDirectory => Path.Combine(Application.persistentDataPath, "CoachMaster", "Music");

        static void EnsureCustomMusicLoaded()
        {
            if (_customLoadStarted)
                return;
            _customLoadStarted = true;
            Ensure();
            var host = _effects.gameObject.GetComponent<GameAudioCoroutineHost>();
            if (host == null)
                host = _effects.gameObject.AddComponent<GameAudioCoroutineHost>();
            host.StartCoroutine(LoadCustomMusicRoutine());
        }

        static IEnumerator LoadCustomMusicRoutine()
        {
            string[] files;
            try
            {
                if (!Directory.Exists(UserMusicDirectory))
                    yield break;
                files = Directory.GetFiles(UserMusicDirectory);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                yield break;
            }

            foreach (var file in files)
            {
                if (!SupportedMusicExtension(Path.GetExtension(file)))
                    continue;
                using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri, AudioTypeFor(file)))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                        continue;
                    var clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip == null)
                        continue;
                    clip.name = Path.GetFileNameWithoutExtension(file);
                    UserTracks.Add(new UserTrack { Id = "custom:" + Path.GetFileName(file), Clip = clip, Path = file, IsUserTrack = true });
                }
            }
            if (_music != null && !_music.isPlaying && Tracks().Count > 0)
                Music(0);
        }

        static IEnumerator ImportUserMusicRoutine(string sourcePath, Action<bool, string> completed)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath) || !SupportedMusicExtension(Path.GetExtension(sourcePath)))
            {
                completed?.Invoke(false, "Choose an existing MP3, WAV, OGG or AIFF music file.");
                yield break;
            }

            string destination;
            try
            {
                Directory.CreateDirectory(UserMusicDirectory);
                var name = Path.GetFileName(sourcePath);
                destination = Path.Combine(UserMusicDirectory, name);
                var suffix = 2;
                while (File.Exists(destination))
                {
                    var stem = Path.GetFileNameWithoutExtension(name);
                    var ext = Path.GetExtension(name);
                    destination = Path.Combine(UserMusicDirectory, stem + " (" + suffix++ + ")" + ext);
                }
                File.Copy(sourcePath, destination);
            }
            catch (Exception)
            {
                completed?.Invoke(false, "The selected music file could not be copied.");
                yield break;
            }

            using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(destination).AbsoluteUri, AudioTypeFor(destination)))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    try { File.Delete(destination); } catch (Exception) { }
                    completed?.Invoke(false, "This music file could not be decoded by the game.");
                    yield break;
                }
                var clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    try { File.Delete(destination); } catch (Exception) { }
                    completed?.Invoke(false, "This music file could not be decoded by the game.");
                    yield break;
                }
                clip.name = Path.GetFileNameWithoutExtension(destination);
                UserTracks.Add(new UserTrack
                {
                    Id = "custom:" + Path.GetFileName(destination),
                    Clip = clip,
                    Path = destination,
                    IsUserTrack = true
                });
                if (_music != null && !_music.isPlaying && Tracks().Count > 0)
                    Music(0);
                completed?.Invoke(true, Clean(clip.name));
            }
        }

        static bool SupportedMusicExtension(string extension)
        {
            return string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".aiff", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".aif", StringComparison.OrdinalIgnoreCase);
        }

        static AudioType AudioTypeFor(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".mp3": return AudioType.MPEG;
                case ".wav": return AudioType.WAV;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".aiff":
                case ".aif": return AudioType.AIFF;
                default: return AudioType.UNKNOWN;
            }
        }

        sealed class UserTrack
        {
            public string Id;
            public AudioClip Clip;
            public string Path;
            public bool IsUserTrack;
        }

        static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Faixa";
            var cut = name;
            var dash = cut.LastIndexOf('-');
            if (dash > 8 && dash < cut.Length - 1 && cut.Length > 40)
                cut = cut.Substring(0, dash);
            cut = cut.Replace('_', ' ').Replace('-', ' ');
            if (cut.Length > 42)
                cut = cut.Substring(0, 42);
            return cut;
        }

        static void PlayClip(AudioClip assigned, string resourceFallback, float gain = 1f)
        {
            var clip = assigned != null ? assigned : LoadResource(resourceFallback);
            if (clip == null)
                return;
            Ensure();
            _effects.PlayOneShot(clip, Mathf.Clamp01(_effectVolume * gain));
        }

        static void PlayMenuClip(AudioClip assigned, string resourceFallback)
        {
            if (_muteAll || (SettingsPrefs.MuteWhenBackground && !Application.isFocused))
                return;
            var clip = assigned != null ? assigned : LoadResource(resourceFallback);
            if (clip == null)
                return;
            Ensure();
            _menu.PlayOneShot(clip, _menuVolume);
        }

        static void React(string key, AudioClip clip)
        {
            if (clip == null || string.IsNullOrEmpty(key))
                return;
            if (_reaction != null && _reaction.isPlaying)
                return;
            if (key == _reactionKey && Time.unscaledTime < _reactionLock)
                return;
            Ensure();
            if (_reaction == null)
                return;
            _reactionKey = key;
            var window = key.Contains("goal") ? 2.3f : 1.5f;
            _reactionLock = Time.unscaledTime + window;
            _reactUntil = Time.unscaledTime + window;
            _reactPeak = ReactionPeak(key);
            _reaction.volume = _reactPeak;
            _reaction.clip = clip;
            _reaction.Play();
        }

        public static void StopCrowd()
        {
            if (_crowd != null)
                _crowd.Stop();
            if (_reaction != null)
                _reaction.Stop();
            _reactUntil = 0f;
        }

        static void StartMusic(AudioClip clip)
        {
            if (clip == null)
                return;
            Ensure();
            _music.volume = EffectiveGain * _musicVolume;
            if (_music.clip == clip && _music.isPlaying)
                return;
            _music.clip = clip;
            _music.loop = Repeat;
            _music.Play();
        }

        static void ApplyVolumes()
        {
            var gain = EffectiveGain;
            if (_music != null)
                _music.volume = _musicVolume * gain;
            if (_effects != null)
                _effects.volume = gain;
            if (_menu != null)
                _menu.volume = gain;
            if (_crowd != null)
                _crowd.volume = _crowdVolume * gain;
            if (_reaction != null)
            {
                if (_reaction.isPlaying)
                {
                    _reactPeak = ReactionPeak();
                    _reaction.volume = _reactPeak;
                }
                else
                    _reaction.volume = 1f;
            }
        }

        static float EffectiveGain => _muteAll || (SettingsPrefs.MuteWhenBackground && !Application.isFocused) ? 0f : _masterVolume;

        static float ReactionPeak(string key = null)
        {
            var eventKey = key ?? _reactionKey;
            var group = eventKey != null && eventKey.Contains("goal")
                ? Mathf.Clamp01(Mathf.Max(_crowdVolume * 1.45f, _musicVolume * 1.25f))
                : Mathf.Clamp01(_crowdVolume * 0.78f);
            return EffectiveGain * group;
        }

        static void MarkPreferencesDirty()
        {
            _prefsDirty = true;
            _savePrefsAt = Time.unscaledTime + 0.5f;
        }

        public static void ReloadPreferences()
        {
            _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("cm_audio_master", 0.5f));
            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("cm_audio_music", 0.5f));
            _effectVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("cm_audio_effects", 0.58f));
            _crowdVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("cm_audio_crowd", 0.42f));
            _menuVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("cm_audio_menu", 0.5f));
            _muteAll = PlayerPrefs.GetInt("cm_audio_mute", 0) != 0;
            _repeat = PlayerPrefs.GetInt("cm_audio_repeat", 0) != 0;
            ApplyVolumes();
        }

        public static void ResetDefaults()
        {
            _masterVolume = 0.5f;
            _musicVolume = 0.5f;
            _effectVolume = 0.58f;
            _crowdVolume = 0.42f;
            _menuVolume = 0.5f;
            _muteAll = false;
            _repeat = false;
            PlayerPrefs.SetFloat("cm_audio_master", _masterVolume);
            PlayerPrefs.SetFloat("cm_audio_music", _musicVolume);
            PlayerPrefs.SetFloat("cm_audio_effects", _effectVolume);
            PlayerPrefs.SetFloat("cm_audio_crowd", _crowdVolume);
            PlayerPrefs.SetFloat("cm_audio_menu", _menuVolume);
            PlayerPrefs.SetInt("cm_audio_mute", 0);
            PlayerPrefs.SetInt("cm_audio_repeat", 0);
            MarkPreferencesDirty();
            ApplyVolumes();
        }

        static AudioClip LoadResource(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
                return null;
            return Resources.Load<AudioClip>(ResourceFolder + "/" + clipName);
        }

        static void Ensure()
        {
            if (_effects != null)
                return;
            var host = new GameObject("GameAudio");
            UnityEngine.Object.DontDestroyOnLoad(host);
            _effects = host.AddComponent<AudioSource>();
            _effects.playOnAwake = false;
            _menu = host.AddComponent<AudioSource>();
            _menu.playOnAwake = false;
            _music = host.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = _musicVolume;
            _crowd = host.AddComponent<AudioSource>();
            _crowd.playOnAwake = false;
            _crowd.loop = true;
            _crowd.volume = _crowdVolume;
            _reaction = host.AddComponent<AudioSource>();
            _reaction.playOnAwake = false;
            _reaction.loop = false;
            if (UnityEngine.Object.FindAnyObjectByType<AudioListener>() == null)
                host.AddComponent<AudioListener>();
        }
    }

    sealed class GameAudioCoroutineHost : MonoBehaviour { }
}
