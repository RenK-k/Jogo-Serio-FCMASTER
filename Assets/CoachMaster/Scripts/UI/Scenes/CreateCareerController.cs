using CoachMaster.App;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class CreateCareerController : MonoBehaviour
    {
        void Start()
        {
            var root = transform;
            var session = GameRoot.Require().Session;
            var club = session.Catalog.Clubs[session.Draft.ClubId];
            SceneUtil.Find<Text>(root, "Header/Subtitle").text = "Vais assumir o " + club.Name + ".";
            SceneUtil.BindButton(root, "Header/Back", () => GameRoot.Instance.Open(SceneNames.ClubInfo));
            KitView.Apply(SceneUtil.Find<Image>(root, "KitSlot/Kit"), club, SceneUtil.Find<Text>(root, "KitSlot/Hint"));
            var field = SceneUtil.Find<InputField>(root, "CoachName");
            var error = SceneUtil.Find<Text>(root, "Error");
            if (!string.IsNullOrEmpty(session.Draft.CoachName))
                field.text = session.Draft.CoachName;
            SceneUtil.BindButton(root, "Confirm", () =>
            {
                var name = field.text == null ? "" : field.text.Trim();
                if (name.Length < 2)
                {
                    error.text = "Escreve o nome do treinador.";
                    return;
                }

                session.Draft.CoachName = name;
                session.StartCareer();
                GameRoot.Instance.Open(SceneNames.CareerHub);
            });
        }
    }
}
