using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Screens
{
    public sealed class CreateCareerScreen : GameScreen
    {
        protected override void Build(ScreenRouter router)
        {
            UiFactory.FullScreen(Root.transform, "Bg", UiTheme.Canvas);
            var club = router.Session.Catalog.Clubs[router.Session.Draft.ClubId];
            ScreenChrome.Page(
                Root.transform,
                "Criar carreira",
                $"Vais assumir o {club.Name}. Indica o nome com que queres ser conhecido no clube.",
                () => router.Show("club"),
                out var body);

            var preview = KitView.Create(body.transform, club, new Vector2(160, 200), false);
            UiFactory.LayoutElement(preview, 200);
            preview.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;

            var field = UiFactory.Input(body.transform, "Nome do treinador");
            UiFactory.LayoutElement(field, 52);
            field.text = router.Session.Draft.CoachName;

            var error = UiFactory.Label(body.transform, "Error", "", 18, new Color(0.85f, 0.45f, 0.4f));
            UiFactory.LayoutElement(error, 28);

            var confirm = UiFactory.Button(body.transform, "ASSUMIR O CLUBE", () =>
            {
                var name = field.text == null ? "" : field.text.Trim();
                if (name.Length < 2)
                {
                    error.text = "Escreve o nome do treinador.";
                    return;
                }

                router.Session.Draft.CoachName = name;
                router.Session.StartCareer();
                router.Show("day");
            }, UiTheme.Accent);
            UiFactory.LayoutElement(confirm, 52);
        }
    }
}
