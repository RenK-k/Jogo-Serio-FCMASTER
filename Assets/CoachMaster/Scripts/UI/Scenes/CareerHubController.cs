using System.Collections.Generic;
using System.Globalization;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class CareerHubController : MonoBehaviour
    {
        Transform _root;
        GameObject[] _pages;
        Button[] _replies;
        CareerState _career;

        void Start()
        {
            _root = transform;
            _career = GameRoot.Require().Session.Career;
            if (_career.Current == null && _career.Upcoming.Count == 0)
                CareerMoments.Begin(_career);
            var club = _career.Club;
            KitView.Apply(SceneUtil.Find<Image>(_root, "Header/Kit"), club);
            SceneUtil.Find<Text>(_root, "Header/ClubName").text = club.Name.ToUpperInvariant();

            _pages = new[]
            {
                SceneUtil.FindT(_root, "Pages/Home").gameObject,
                SceneUtil.FindT(_root, "Pages/News").gameObject,
                SceneUtil.FindT(_root, "Pages/Team").gameObject,
                SceneUtil.FindT(_root, "Pages/Staff").gameObject,
                SceneUtil.FindT(_root, "Pages/Calendar").gameObject
            };

            WireTab("Tabs/Inicio", 0);
            WireTab("Tabs/Noticias", 1);
            WireTab("Tabs/Equipa", 2);
            WireTab("Tabs/Staff", 3);
            WireTab("Tabs/Calendario", 4);
            Show(0);

            BuildReplies(SceneUtil.FindT(_root, "Pages/Home"));
            SceneUtil.Find<Text>(_root, "Pages/Team/Body").text = SquadText(_career, club);
            SceneUtil.Find<Text>(_root, "Pages/Staff/Body").text = StaffText(_career, club);
            FillCalendar(_career);

            var play = SceneUtil.Find<Button>(_root, "PlayMatch");
            play.onClick.RemoveAllListeners();
            play.onClick.AddListener(OnContinue);
            Refresh();
        }

        void Update()
        {
            if (_career == null || _career.Current == null || _career.Current.Kind != MomentKind.Talk || _career.Current.Answered)
                return;
            if (ControlChrome.Digit(1)) Answer(0);
            else if (ControlChrome.Digit(2)) Answer(1);
            else if (ControlChrome.Digit(3)) Answer(2);
        }

        void OnContinue()
        {
            var moment = _career.Current;
            if (moment != null && moment.Kind == MomentKind.Match && moment.Fixture != null && !moment.Fixture.Played)
            {
                SeasonSchedule.SimulateOthers(_career, moment.Fixture.Date, moment.Fixture.Id);
                GameRoot.Instance.LiveMatch = MatchSession.Create(_career, moment.Fixture);
                GameRoot.Instance.Open(SceneNames.MatchDay);
                return;
            }

            CareerMoments.Continue(_career);
            Refresh();
        }

        void Answer(int choice)
        {
            CareerMoments.Answer(_career, choice);
            Refresh();
        }

        void Refresh()
        {
            var moment = _career.Current;
            SceneUtil.Find<Text>(_root, "Header/Details").text =
                $"{_career.Calendar.LongDateText}   ·   {_career.League.Name} {_career.Calendar.SeasonLabel}   ·   Mister {_career.Coach.Name}";
            var home = moment == null
                ? "A época ficou em silêncio."
                : moment.Title.ToUpperInvariant() + "\n\n" + moment.Body + "\n\nPróximo jogo: " + NextMatchText(_career);
            SceneUtil.Find<Text>(_root, "Pages/Home/Body").text = home;
            SceneUtil.Find<Text>(_root, "Pages/News/Body").text = NewsText();
            var play = SceneUtil.Find<Button>(_root, "PlayMatch");
            var label = play.GetComponentInChildren<Text>();
            if (moment != null && moment.Kind == MomentKind.Match)
                label.text = "JOGAR  " + MatchLabel(_career, moment.Fixture);
            else
                label.text = "CONTINUAR";

            var talking = moment != null && moment.Kind == MomentKind.Talk && !moment.Answered;
            for (var i = 0; i < _replies.Length; i++)
            {
                _replies[i].gameObject.SetActive(talking);
                if (!talking)
                    continue;
                _replies[i].GetComponentInChildren<Text>().text = (i + 1) + "  " + moment.Replies[i];
            }
        }

        void BuildReplies(Transform home)
        {
            _replies = new Button[3];
            for (var i = 0; i < 3; i++)
            {
                var go = new GameObject("Reply" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(home, false);
                rect.anchorMin = new Vector2(0.03f, 0.04f + i * 0.1f);
                rect.anchorMax = new Vector2(0.97f, 0.13f + i * 0.1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = UiTheme.AccentDark;
                var button = go.GetComponent<Button>();
                var choice = i;
                button.onClick.AddListener(() => Answer(choice));
                var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.SetParent(rect, false);
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(12, 4);
                textRect.offsetMax = new Vector2(-12, -4);
                var text = textGo.GetComponent<Text>();
                text.font = UiTheme.Font;
                text.fontSize = 16;
                text.color = UiTheme.Text;
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                _replies[i] = button;
                go.SetActive(false);
            }
        }

        string NewsText()
        {
            if (_career.Notebook.Count == 0)
                return _career.Calendar.LongDateText + ": chegas ao " + _career.Club.Name + ".\n\n" + _career.Club.PublicCoachVacancyNote;
            return string.Join("\n", _career.Notebook);
        }

        void WireTab(string path, int index)
        {
            SceneUtil.BindButton(_root, path, () => Show(index));
        }

        void Show(int index)
        {
            for (var i = 0; i < _pages.Length; i++)
                _pages[i].SetActive(i == index);
        }

        void FillCalendar(CareerState career)
        {
            var content = SceneUtil.FindT(_root, "Pages/Calendar/Scroll/Viewport/Content");
            var template = SceneUtil.FindT(_root, "Pages/Calendar/Scroll/Viewport/Content/RowTemplate");
            var rows = new List<Fixture>();
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.Involves(career.Coach.ClubId))
                    rows.Add(fixture);
            }

            var culture = CultureInfo.GetCultureInfo("pt-PT");
            SceneUtil.Rebuild(content, template, rows.Count, (row, i) =>
            {
                var fixture = rows[i];
                var opp = career.World.Clubs[fixture.OpponentId(career.Coach.ClubId)];
                var venue = fixture.IsHome(career.Coach.ClubId) ? "CASA" : "FORA";
                var score = fixture.Played ? $"{fixture.HomeGoals}-{fixture.AwayGoals}" : "vs";
                row.transform.Find("Name").GetComponent<Text>().text =
                    $"{fixture.Date.ToString("dd MMM", culture)}   {venue}   {opp.Name}   {score}";
            });
        }

        static string NextMatchText(CareerState career)
        {
            var next = career.NextUserFixture();
            return next == null ? "Época sem jogos por jogar." : MatchLabel(career, next);
        }

        static string MatchLabel(CareerState career, Fixture fixture)
        {
            var opp = career.World.Clubs[fixture.OpponentId(career.Coach.ClubId)];
            var venue = fixture.IsHome(career.Coach.ClubId) ? "Casa" : "Fora";
            return $"{venue} vs {opp.Name}";
        }

        static string SquadText(CareerState career, Club club)
        {
            var players = new List<Player>();
            foreach (var id in club.PlayerIds)
                players.Add(career.World.Players[id]);
            players.Sort((a, b) => a.SquadNumber.CompareTo(b.SquadNumber));
            var lines = new List<string>();
            foreach (var player in players)
            {
                lines.Add($"{player.SquadNumber:00}  {PresentationLabels.Position(player.Position)}  {player.FullName}");
                lines.Add("    " + CareerMoments.ClueMood(player));
            }

            return "Não há barras de humor nem de cansaço. O que vês é o que eles deixam transparecer.\n\n" + string.Join("\n", lines);
        }

        static string StaffText(CareerState career, Club club)
        {
            var lines = new List<string>();
            foreach (var member in club.Staff)
                lines.Add(PresentationLabels.StaffRole(member.Role) + " — " + member.FullName);
            lines.Add("");
            lines.Add(career.Club.DressingRoomAtmosphere);
            lines.Add("Eles falam por observações, não por números.");
            return string.Join("\n", lines);
        }
    }
}
