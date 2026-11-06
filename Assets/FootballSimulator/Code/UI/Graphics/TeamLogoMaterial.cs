using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using FStudio.Scriptables;
using FStudio.Database;

namespace FStudio.UI.Graphics {
    public class TeamLogoMaterial : SerializedSingletonScriptable<TeamLogoMaterial> {
        private const string MASK_TEXTURE = "_MaskTexture";
        private const string MASK_COLOR_1 = "_FirstColor";
        private const string MASK_COLOR_2 = "_SecondColor";

        [SerializeField] private AssetReferenceT<Material> ScoreboardMaterial = default;

        [SerializeField] private Material mainMaterial;

        public Material GetColoredMaterial(LogoEntry logoResponse) {
            if (logoResponse == null || mainMaterial == null)
                return mainMaterial;
            return GetColoredMaterial(logoResponse.TeamLogoMaterial, logoResponse.TeamLogoColor1, logoResponse.TeamLogoColor2);
        }

        private Material GetColoredMaterial (Texture texture, Color color1, Color color2) {
            var logoMaterial = new Material(mainMaterial);
            logoMaterial.SetTexture(MASK_TEXTURE, texture);

            logoMaterial.SetColor(MASK_COLOR_1, color1);
            logoMaterial.SetColor(MASK_COLOR_2, color2);

            return logoMaterial;
        }

        public async Task<Material> GetScoreboardMaterial(Color color1, Color color2) {
            Material boardMaterial = null;
#if UNITY_EDITOR
            FStudio.Loaders.EditorContent.TryLoad(ScoreboardMaterial, out boardMaterial);
#endif
            if (boardMaterial == null) {
                var board = Addressables.LoadAssetAsync<Material>(ScoreboardMaterial);
                await board.Task;
                boardMaterial = board.Result;
            }

            var colored = new Material(boardMaterial);
            colored.SetColor(MASK_COLOR_1, color1);
            colored.SetColor(MASK_COLOR_2, color2);

            return colored;
        }
    }
}
