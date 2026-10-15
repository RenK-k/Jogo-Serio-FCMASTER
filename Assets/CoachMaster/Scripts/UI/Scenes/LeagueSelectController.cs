using System.Collections.Generic;
using CoachMaster.App;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class LeagueSelectController : MonoBehaviour
    {
        void Start()
        {
            var root = transform;
            var session = GameRoot.Require().Session;
            SceneUtil.BindButton(root, "Header/Back", () => GameRoot.Instance.Open(SceneNames.MainMenu));
            var content = SceneUtil.FindT(root, "Body/Scroll/Viewport/Content");
            var template = SceneUtil.FindT(root, "Body/Scroll/Viewport/Content/RowTemplate");
            var leagues = new List<League>(session.Catalog.Leagues);
            SceneUtil.Rebuild(content, template, leagues.Count, (row, i) =>
            {
                var league = leagues[i];
                Set(row.transform, "Name", league.Name.ToUpperInvariant());
                Set(row.transform, "Desc", league.Description);
                Set(row.transform, "Meta", league.ClubCount + " clubes  ·  " + league.ShortName);
                var button = row.transform.Find("Select").GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    session.Draft.LeagueId = league.Id;
                    session.Draft.ClubId = null;
                    session.PrepareLeague(league.Id);
                    GameRoot.Instance.Open(SceneNames.TeamSelect);
                });
            });
        }

        static void Set(Transform row, string child, string value)
        {
            var text = row.Find(child).GetComponent<Text>();
            text.text = value;
        }
    }
}
