using UnityEngine;

using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace FStudio.MatchEngine.Graphics {
    public class InGameKitMaterial : KitMaterial<InGameKitMaterial> {
        [SerializeField] private AssetReferenceT<Material> RefereeMaterial = default;

        public async Task<Material> GetRefereeMaterial () {
#if UNITY_EDITOR
            if (FStudio.Loaders.EditorContent.TryLoad(RefereeMaterial, out Material direct))
                return direct;
#endif
            var loadSkin = Addressables.LoadAssetAsync<Material>(RefereeMaterial);

            await loadSkin.Task;

            return loadSkin.Result;
        }
    }
}
