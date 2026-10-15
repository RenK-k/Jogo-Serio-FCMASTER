using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.MatchPlay;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class MatchDayController : MonoBehaviour
    {
        MatchSession _match;
        MatchEngine _engine;
        Transform _pitch;
        RectTransform _ball;
        Image[] _homeTokens;
        Image[] _awayTokens;
        Text _score;
        Text _clock;
        Text _eventText;
        GameObject _pause;
        GameObject _halfTime;
        GameObject _fullTime;
        bool _paused;

        void Start()
        {
            var root = transform;
            _match = GameRoot.Require().LiveMatch;
            if (_match == null)
            {
                GameRoot.Instance.Open(SceneNames.CareerHub);
                return;
            }

            _engine = new MatchEngine(_match);
            _pitch = SceneUtil.FindT(root, "Pitch");
            _ball = SceneUtil.Find<RectTransform>(root, "Pitch/Ball");
            _score = SceneUtil.Find<Text>(root, "Hud/Score");
            _clock = SceneUtil.Find<Text>(root, "Hud/Clock");
            _eventText = SceneUtil.Find<Text>(root, "Hud/Event");
            _pause = SceneUtil.FindT(root, "PausePanel").gameObject;
            _halfTime = SceneUtil.FindT(root, "HalfTimePanel").gameObject;
            _fullTime = SceneUtil.FindT(root, "FullTimePanel").gameObject;
            _pause.SetActive(false);
            _halfTime.SetActive(false);
            _fullTime.SetActive(false);

            SceneUtil.Find<Text>(root, "Hud/HomeName").text = _match.Home.ShortName.ToUpperInvariant();
            SceneUtil.Find<Text>(root, "Hud/AwayName").text = _match.Away.ShortName.ToUpperInvariant();
            _homeTokens = Tokens("HomePlayer", true);
            _awayTokens = Tokens("AwayPlayer", false);

            SceneUtil.BindButton(root, "PauseButton", TogglePause);
            SceneUtil.BindButton(root, "PausePanel/Resume", TogglePause);
            SceneUtil.BindButton(root, "PausePanel/CycleFormation", () => Cycle("formation"));
            SceneUtil.BindButton(root, "PausePanel/CyclePressing", () => Cycle("pressing"));
            SceneUtil.BindButton(root, "PausePanel/Sub", DoSub);
            SceneUtil.BindButton(root, "HalfTimePanel/TalkCalm", () =>
            {
                _match.DefenseBoost += 0.06f;
                Talk("Mantém a calma. Fechamos os espaços e saímos com critério.");
            });
            SceneUtil.BindButton(root, "HalfTimePanel/TalkDemand", () =>
            {
                _match.AttackBoost += 0.04f;
                _match.DefenseBoost += 0.03f;
                Talk("Exijo mais intensidade. Quero cada segunda bola.");
            });
            SceneUtil.BindButton(root, "HalfTimePanel/TalkAttack", () =>
            {
                _match.HomeTactics.Pressing = "Pressão alta";
                _match.HomeTactics.BuildUp = "Construção curta";
                _match.HomeTactics.DefensiveLine = "Linha defensiva alta";
                _match.AttackBoost += 0.08f;
                Talk("Vamos subir as linhas e atacar mais.");
            });
            SceneUtil.BindButton(root, "FullTimePanel/Continue", FinishMatch);
        }

        void Update()
        {
            if (_match == null)
                return;
            if (_match.Phase == MatchPhase.HalfTime)
            {
                _halfTime.SetActive(true);
                RefreshHud();
                return;
            }

            if (_match.Phase == MatchPhase.FullTime)
            {
                _fullTime.SetActive(true);
                SceneUtil.Find<Text>(transform, "FullTimePanel/Result").text =
                    $"{_match.Home.ShortName} {_match.HomeGoals} - {_match.AwayGoals} {_match.Away.ShortName}";
                RefreshHud();
                return;
            }

            if (!_paused)
                _engine.Tick(Time.deltaTime, 1.35f);

            PlaceTokens();
            PlaceBall();
            RefreshHud();
        }

        void TogglePause()
        {
            _paused = !_paused;
            _pause.SetActive(_paused);
            RefreshPause();
        }

        void Cycle(string what)
        {
            var t = _match.HomeTactics;
            if (what == "formation")
            {
                var options = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
                t.Formation = Next(options, t.Formation);
            }
            else
            {
                var options = new[] { "Pressão alta", "Pressão média", "Pressão baixa" };
                t.Pressing = Next(options, t.Pressing);
            }

            t.Summary = $"{t.Formation} · {t.Pressing} · {t.BuildUp}";
            RefreshPause();
        }

        int _subOff = 10;
        int _subOn;

        void DoSub()
        {
            if (_match.HomeBench.Count == 0 || _match.HomeSubsLeft <= 0)
                return;
            _subOff = (_subOff + 1) % _match.HomeOnField.Count;
            if (_subOff == 0)
                _subOff = 1;
            _subOn %= _match.HomeBench.Count;
            if (!_engine.TrySub(true, _subOff, _subOn))
                return;
            _subOn = (_subOn + 1) % Mathf.Max(1, _match.HomeBench.Count);
            RefreshPause();
        }

        void Talk(string line)
        {
            _engine.ApplyTeamTalk(line);
            _halfTime.SetActive(false);
            _engine.StartSecondHalf();
            _paused = false;
        }

        void FinishMatch()
        {
            var career = GameRoot.Instance.Session.Career;
            _match.Fixture.HomeGoals = _match.HomeGoals;
            _match.Fixture.AwayGoals = _match.AwayGoals;
            SeasonSchedule.ApplyResult(career, _match.Fixture);
            SeasonSchedule.SimulateOthers(career, _match.Fixture.Date, _match.Fixture.Id);
            var userHome = _match.Home.Id == career.Coach.ClubId;
            CareerMoments.AfterMatch(career, _match.HomeGoals, _match.AwayGoals, userHome);
            GameRoot.Instance.LiveMatch = null;
            GameRoot.Instance.Open(SceneNames.CareerHub);
        }

        void RefreshHud()
        {
            _score.text = $"{_match.HomeGoals}  -  {_match.AwayGoals}";
            _clock.text = Mathf.Min(90, Mathf.FloorToInt(_match.Minute)).ToString("00") + "'";
            _eventText.text = _match.LastEvent;
        }

        void RefreshPause()
        {
            var off = NameAt(_match.HomeOnField, _subOff);
            var on = _match.HomeBench.Count == 0 ? "—" : NameAt(_match.HomeBench, _subOn % _match.HomeBench.Count);
            SceneUtil.Find<Text>(transform, "PausePanel/Tactics").text =
                $"Tática: {_match.HomeTactics.Formation}  ·  {_match.HomeTactics.Pressing}\nSubstituições: {_match.HomeSubsLeft}\nPróxima troca: sai {off}, entra {on}";
        }

        void PlaceBall()
        {
            _ball.anchorMin = _engine.Ball;
            _ball.anchorMax = _engine.Ball;
            _ball.anchoredPosition = Vector2.zero;
        }

        void PlaceTokens()
        {
            for (var i = 0; i < 11; i++)
            {
                var homeSlot = MatchEngine.Slot(true, i, _match.HomeTactics.Formation);
                var awaySlot = MatchEngine.Slot(false, i, _match.AwayTactics.Formation);
                if (_match.HomeHasBall && i == _match.BallHolder)
                    homeSlot = Vector2.Lerp(homeSlot, _engine.Ball, 0.35f);
                if (!_match.HomeHasBall && i == _match.BallHolder)
                    awaySlot = Vector2.Lerp(awaySlot, _engine.Ball, 0.35f);
                Move(_homeTokens[i], homeSlot, _match.HomeHasBall && i == _match.BallHolder);
                Move(_awayTokens[i], awaySlot, !_match.HomeHasBall && i == _match.BallHolder);
            }
        }

        static void Move(Image token, Vector2 anchor, bool hasBall)
        {
            var rect = token.rectTransform;
            var current = rect.anchorMin;
            var next = Vector2.Lerp(current, anchor, 1f - Mathf.Exp(-Time.deltaTime * 5f));
            rect.anchorMin = next;
            rect.anchorMax = next;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one * (hasBall ? 1.35f : 1f);
        }

        string NameAt(System.Collections.Generic.List<string> ids, int index)
        {
            if (ids == null || ids.Count == 0)
                return "—";
            index = Mathf.Clamp(index, 0, ids.Count - 1);
            return _match.Names.TryGetValue(ids[index], out var name) ? name : "Jogador";
        }

        Image[] Tokens(string prefix, bool home)
        {
            var list = new Image[11];
            ColorUtility.TryParseHtmlString(home ? _match.Home.JerseyPrimaryHex : _match.Away.JerseyPrimaryHex, out var color);
            for (var i = 0; i < 11; i++)
            {
                list[i] = SceneUtil.Find<Image>(transform, "Pitch/" + prefix + i);
                list[i].color = color;
            }

            return list;
        }

        static string Next(string[] options, string current)
        {
            var index = System.Array.IndexOf(options, current);
            return options[(index + 1 + options.Length) % options.Length];
        }
    }
}
