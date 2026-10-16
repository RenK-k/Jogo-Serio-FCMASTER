using System.Collections.Generic;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class GameAudio
    {
        const string ResourceFolder = "Audio";
        const string LibraryResource = "GameAudioLibrary";

        static GameAudioLibrary _library;
        static AudioSource _effects;
        static AudioSource _music;
        static AudioSource _crowd;
        static bool _filled;
        static int _track = -1;
        static float _musicVolume = 0.42f;
        static float _effectVolume = 1f;
        static float _crowdVolume = 0.35f;

        public static float MusicVolume => _musicVolume;
        public static float EffectVolume => _effectVolume;
        public static float CrowdVolume => _crowdVolume;
        public static int TrackIndex => _track;

        public static void Bind(GameAudioLibrary library)
        {
            if (library != null)
                _library = UnityEngine.Object.Instantiate(library);
            _filled = false;
            FillGaps();
        }

        public static void Click() => PlayClip(Library?.Click, "click");
        public static void Goal() => PlayClip(Library?.Goal, "golo");
        public static void Whistle() => PlayClip(Library?.Whistle, "whistle");
        public static void Foul() => PlayClip(Library?.Foul, "foul");
        public static void Injury() => PlayClip(Library?.Injury, "injury");
        public static void YellowCard() => PlayClip(Library?.YellowCard, "yellow");
        public static void RedCard() => PlayClip(Library?.RedCard, "red");
        public static void Corner() => PlayClip(Library?.Corner, "corner");
        public static void ThrowIn() => PlayClip(Library?.ThrowIn, "throwin");
        public static void Kickoff() => PlayClip(Library?.Kickoff ?? Library?.Whistle, "whistle");

        public static void Play(string resourceName)
        {
            PlayClip(null, resourceName);
        }

        public static void ForFlash(string flashText)
        {
            if (string.IsNullOrEmpty(flashText))
                return;
            var upper = flashText.ToUpperInvariant();
            if (upper.Contains("GOLO"))
                Goal();
            else if (upper.Contains("VERMELHO"))
                RedCard();
            else if (upper.Contains("AMARELO"))
                YellowCard();
            else if (upper.Contains("LES"))
                Injury();
            else if (upper.Contains("PEN"))
                Whistle();
            else if (upper.Contains("FALTA"))
                Foul();
            else if (upper.Contains("CANTO"))
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
                StartMusic(LoadResource("music"));
                return;
            }

            if (trackIndex < 0)
                trackIndex = _track >= 0 ? _track : 0;
            _track = (trackIndex % tracks.Count + tracks.Count) % tracks.Count;
            StartMusic(tracks[_track]);
        }

        public static void NextTrack()
        {
            var count = Tracks().Count;
            if (count <= 1)
            {
                if (Repeat)
                    Music(0);
                return;
            }
            Music(_track + 1);
        }

        public static void PreviousTrack()
        {
            Music(_track - 1);
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

        static bool _unfocused;
        static float _grace;
        public static bool Repeat { get; set; }

        public static void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        public static void SetEffectVolume(float value)
        {
            _effectVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        public static void SetCrowdVolume(float value)
        {
            _crowdVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        public static void Tick()
        {
            if (!Application.isFocused)
            {
                _unfocused = true;
                return;
            }

            if (_unfocused)
            {
                _unfocused = false;
                _grace = Time.unscaledTime + 1.2f;
                if (_music != null && _music.clip != null && !_music.isPlaying)
                    _music.Play();
                return;
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
            if (_crowd.clip == clip && _crowd.isPlaying)
                return;
            _crowd.clip = clip;
            _crowd.loop = true;
            _crowd.volume = _crowdVolume;
            _crowd.Play();
        }

        public static void StopCrowd()
        {
            if (_crowd != null)
                _crowd.Stop();
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
                else if (Contains(name, "crowd", "torcida", "publico", "público", "fas", "fans"))
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
            var tracks = Library == null ? null : Library.MusicTracks;
            if (tracks == null)
                return list;
            foreach (var clip in tracks)
            {
                if (clip != null)
                    list.Add(clip);
            }

            return list;
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

        static void PlayClip(AudioClip assigned, string resourceFallback)
        {
            var clip = assigned != null ? assigned : LoadResource(resourceFallback);
            if (clip == null)
                return;
            Ensure();
            _effects.PlayOneShot(clip, _effectVolume);
        }

        static void StartMusic(AudioClip clip)
        {
            if (clip == null)
                return;
            Ensure();
            _music.volume = _musicVolume;
            if (_music.clip == clip && _music.isPlaying)
                return;
            _music.clip = clip;
            _music.loop = Repeat;
            _music.Play();
        }

        static void ApplyVolumes()
        {
            if (_music != null)
                _music.volume = _musicVolume;
            if (_crowd != null)
                _crowd.volume = _crowdVolume;
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
            Object.DontDestroyOnLoad(host);
            _effects = host.AddComponent<AudioSource>();
            _effects.playOnAwake = false;
            _music = host.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = _musicVolume;
            _crowd = host.AddComponent<AudioSource>();
            _crowd.playOnAwake = false;
            _crowd.loop = true;
            _crowd.volume = _crowdVolume;
            if (Object.FindAnyObjectByType<AudioListener>() == null)
                host.AddComponent<AudioListener>();
        }
    }
}
