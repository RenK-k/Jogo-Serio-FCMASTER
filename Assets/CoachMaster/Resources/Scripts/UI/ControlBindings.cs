using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace CoachMaster.UI
{
    public static class ControlBindings
    {
        const string GameKey = "cm_binding_overrides";
        const string UiKey = "cm_ui_overrides";
        static InputActionAsset _game;

        public static InputActionAsset GameAsset()
        {
            if (_game != null)
                return _game;
            var live = FindNamed("engine");
            _game = live != null ? live : new Engine().asset;
            var json = PlayerPrefs.GetString(GameKey, "");
            if (!string.IsNullOrEmpty(json))
                _game.LoadBindingOverridesFromJson(json);
            return _game;
        }

        public static void Apply()
        {
            var gameJson = PlayerPrefs.GetString(GameKey, "");
            if (!string.IsNullOrEmpty(gameJson))
            {
                var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
                for (var i = 0; i < assets.Length; i++)
                {
                    var asset = assets[i];
                    if (asset != null && asset.name == "engine")
                        asset.LoadBindingOverridesFromJson(gameJson);
                }
                var players = PlayerInput.all;
                for (var i = 0; i < players.Count; i++)
                {
                    var input = players[i];
                    if (input != null && input.actions != null && input.actions.name == "engine")
                        input.actions.LoadBindingOverridesFromJson(gameJson);
                }
            }

            var uiJson = PlayerPrefs.GetString(UiKey, "");
            if (string.IsNullOrEmpty(uiJson))
                return;
            var modules = Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None);
            for (var i = 0; i < modules.Length; i++)
            {
                var module = modules[i];
                if (module != null && module.actionsAsset != null)
                    module.actionsAsset.LoadBindingOverridesFromJson(uiJson);
            }
        }

        public static void SaveGame(InputActionAsset asset)
        {
            if (asset == null)
                return;
            PlayerPrefs.SetString(GameKey, asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
            Apply();
        }

        public static void SaveUi(InputActionAsset asset)
        {
            if (asset == null)
                return;
            PlayerPrefs.SetString(UiKey, asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
            Apply();
        }

        public static void ResetAll()
        {
            var game = GameAsset();
            if (game != null)
                game.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(GameKey);
            PlayerPrefs.DeleteKey(UiKey);
            PlayerPrefs.Save();
            var modules = Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None);
            for (var i = 0; i < modules.Length; i++)
            {
                var module = modules[i];
                if (module != null && module.actionsAsset != null)
                    module.actionsAsset.RemoveAllBindingOverrides();
            }
            var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            for (var i = 0; i < assets.Length; i++)
            {
                var asset = assets[i];
                if (asset != null && asset.name == "engine")
                    asset.RemoveAllBindingOverrides();
            }
        }

        public static List<BindingRow> Rows()
        {
            var rows = new List<BindingRow>();
            Collect(rows, GameAsset(), "MatchEngine", true);
            var module = Object.FindAnyObjectByType<InputSystemUIInputModule>();
            if (module != null && module.actionsAsset != null)
                Collect(rows, module.actionsAsset, null, false);
            return rows;
        }

        static void Collect(List<BindingRow> rows, InputActionAsset asset, string mapName, bool gameplay)
        {
            if (asset == null)
                return;
            for (var m = 0; m < asset.actionMaps.Count; m++)
            {
                var map = asset.actionMaps[m];
                if (mapName != null && map.name != mapName)
                    continue;
                for (var a = 0; a < map.actions.Count; a++)
                {
                    var action = map.actions[a];
                    if (!gameplay && !MenuAction(action.name))
                        continue;
                    for (var b = 0; b < action.bindings.Count; b++)
                    {
                        var binding = action.bindings[b];
                        if (binding.isComposite)
                            continue;
                        var path = binding.effectivePath ?? "";
                        var keyboard = path.IndexOf("<Keyboard>", System.StringComparison.Ordinal) >= 0;
                        var pad = path.IndexOf("<Gamepad>", System.StringComparison.Ordinal) >= 0
                            || path.IndexOf("<XInputController>", System.StringComparison.Ordinal) >= 0;
                        if (!keyboard && !pad)
                            continue;
                        rows.Add(new BindingRow
                        {
                            Gameplay = gameplay,
                            Asset = asset,
                            Action = action,
                            BindingIndex = b,
                            Keyboard = keyboard,
                            Part = binding.name ?? "",
                            Title = Name(action.name, binding.name)
                        });
                    }
                }
            }
        }

        static bool MenuAction(string name)
        {
            return name == "Navigate" || name == "Submit" || name == "Cancel" || name == "Left" || name == "Right" || name == "Approve";
        }

        public static List<ControlPair> Pairs()
        {
            var rows = Rows();
            var map = new Dictionary<string, ControlPair>();
            var order = new List<ControlPair>();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var key = (row.Gameplay ? "g:" : "m:") + (row.Action == null ? "" : row.Action.name) + ":" + row.Part;
                ControlPair pair;
                if (!map.TryGetValue(key, out pair))
                {
                    pair = new ControlPair
                    {
                        Title = row.Title,
                        Tab = row.Gameplay && row.Action != null && row.Action.name == "Move" ? "move" : row.Gameplay ? "match" : "menu"
                    };
                    map[key] = pair;
                    order.Add(pair);
                }
                if (row.Keyboard)
                    pair.Keys.Add(row);
                else if (pair.Pad == null)
                    pair.Pad = row;
                else if (IsGenericPad(row) && !IsGenericPad(pair.Pad))
                {
                    pair.Pads.Add(pair.Pad);
                    pair.Pad = row;
                }
                else
                    pair.Pads.Add(row);
            }
            return order;
        }

        public static void MirrorPad(BindingRow source, List<BindingRow> mirrors)
        {
            if (source == null || source.Keyboard || source.Action == null || mirrors == null)
                return;
            var path = source.Action.bindings[source.BindingIndex].effectivePath ?? "";
            var slash = path.LastIndexOf('/');
            if (slash < 0)
                return;
            var control = path.Substring(slash);
            for (var i = 0; i < mirrors.Count; i++)
            {
                var extra = mirrors[i];
                if (extra == null || extra.Keyboard || extra.Action == null)
                    continue;
                var current = extra.Action.bindings[extra.BindingIndex].effectivePath ?? "";
                var end = current.IndexOf('>');
                if (end < 0)
                    continue;
                extra.Action.ApplyBindingOverride(extra.BindingIndex, current.Substring(0, end + 1) + control);
            }
        }

        static bool IsGenericPad(BindingRow row)
        {
            if (row == null || row.Action == null)
                return false;
            var path = row.Action.bindings[row.BindingIndex].effectivePath ?? "";
            return path.IndexOf("<Gamepad>", System.StringComparison.Ordinal) >= 0;
        }

        static string Name(string action, string part)
        {
            string name;
            switch (action)
            {
                case "Move": name = "Mover " + Direction(part); break;
                case "Pass": name = "Passe"; break;
                case "Shoot": name = "Remate"; break;
                case "Tackle": name = "Desarme"; break;
                case "Cross": name = "Cruzamento"; break;
                case "ThroughtPass": name = "Passe em profundidade"; break;
                case "ChangePlayer": name = "Mudar de jogador"; break;
                case "Pause": name = "Pausa"; break;
                case "ChangeTacticHigh": name = "Tática ofensiva"; break;
                case "ChangeTacticLow": name = "Tática defensiva"; break;
                case "ForceShoot": name = "Remate forte"; break;
                case "SuperSlow": name = "Câmara lenta"; break;
                case "Navigate": name = "Menu " + Direction(part); break;
                case "Submit": name = "Confirmar"; break;
                case "Cancel": name = "Voltar"; break;
                case "Left": name = "Menu esquerda"; break;
                case "Right": name = "Menu direita"; break;
                case "Approve": name = "Aprovar"; break;
                default: name = action; break;
            }
            return name;
        }

        static string Direction(string part)
        {
            if (string.IsNullOrEmpty(part))
                return "";
            switch (part.ToLowerInvariant())
            {
                case "up": return "cima";
                case "down": return "baixo";
                case "left": return "esquerda";
                case "right": return "direita";
                default: return part;
            }
        }

        static InputActionAsset FindNamed(string name)
        {
            var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            for (var i = 0; i < assets.Length; i++)
            {
                var asset = assets[i];
                if (asset != null && asset.name == name)
                    return asset;
            }
            return null;
        }

        public sealed class BindingRow
        {
            public bool Gameplay;
            public InputActionAsset Asset;
            public InputAction Action;
            public int BindingIndex;
            public bool Keyboard;
            public string Part;
            public string Title;
        }

        public sealed class ControlPair
        {
            public string Title;
            public string Tab;
            public BindingRow Pad;
            public readonly List<BindingRow> Pads = new List<BindingRow>();
            public readonly List<BindingRow> Keys = new List<BindingRow>();
        }
    }
}
