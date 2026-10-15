using System.Collections.Generic;
using CoachMaster.App;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class TeamSelectController : MonoBehaviour
    {
        void Start()
        {
            var root = transform;
            var session = GameRoot.Require().Session;
            session.PrepareLeague(session.Draft.LeagueId);
            SceneUtil.BindButton(root, "Header/Back", () => GameRoot.Instance.Open(SceneNames.LeagueSelect));

            var clubs = new List<Club>();
            foreach (var club in session.Catalog.ClubsInLeague(session.Draft.LeagueId))
                clubs.Add(club);
            clubs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            var preview = SceneUtil.Find<Image>(root, "Preview/Kit");
            var previewName = SceneUtil.Find<Text>(root, "Preview/ClubName");
            var hint = SceneUtil.Find<Text>(root, "Preview/Hint");
            if (clubs.Count > 0)
                Show(preview, previewName, hint, clubs[0]);

            var content = SceneUtil.FindT(root, "Body/Scroll/Viewport/Content");
            var template = SceneUtil.FindT(root, "Body/Scroll/Viewport/Content/RowTemplate");
            SceneUtil.Rebuild(content, template, clubs.Count, (row, i) =>
            {
                var club = clubs[i];
                KitView.Apply(row.transform.Find("Kit").GetComponent<Image>(), club);
                row.transform.Find("Name").GetComponent<Text>().text = club.Name.ToUpperInvariant();
                row.transform.Find("Meta").GetComponent<Text>().text = club.City + "  ·  " + club.Stadium;
                var select = row.transform.Find("Select").GetComponent<Button>();
                select.onClick.RemoveAllListeners();
                select.onClick.AddListener(() =>
                {
                    Show(preview, previewName, hint, club);
                    session.Draft.ClubId = club.Id;
                    GameRoot.Instance.Open(SceneNames.ClubInfo);
                });
            });
        }

        static void Show(Image preview, Text name, Text hint, Club club)
        {
            KitView.Apply(preview, club, hint);
            if (name != null)
                name.text = club.Name.ToUpperInvariant();
        }
    }
}
