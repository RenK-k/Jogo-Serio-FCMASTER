using System.Collections.Generic;
using CoachMaster.Domain;
using CoachMaster.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class MomentRoll : MonoBehaviour
    {
        Image _face;
        Image _crest;
        Text _who;
        Text _line;
        Player _to;
        Sprite _fromFace;
        Sprite _toFace;
        string _landing;
        float _time;
        bool _playing;
        readonly List<string> _reel = new List<string>();

        public static void Play(Transform canvas, Player player, string flash, string[] steps, Sprite crest = null)
        {
            if (!Application.isPlaying || canvas == null || string.IsNullOrEmpty(flash))
                return;
            var host = canvas.Find("MomentRoll");
            MomentRoll roll;
            if (host == null)
            {
                var go = new GameObject("MomentRoll", typeof(RectTransform));
                go.transform.SetParent(canvas, false);
                BroadcastUi.Stretch(go.GetComponent<RectTransform>());
                var shade = BroadcastUi.Solid(go.transform, "Shade", new Color(0.02f, 0.015f, 0.04f, 0.55f));
                BroadcastUi.Stretch(shade.rectTransform);
                var card = BroadcastUi.Panel(go.transform, "Card", new Color(0.07f, 0.04f, 0.12f, 0.94f), false);
                BroadcastUi.Place(card.rectTransform, 0.28f, 0.22f, 0.72f, 0.78f);
                roll = go.AddComponent<MomentRoll>();
                var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
                faceGo.transform.SetParent(card.transform, false);
                roll._face = faceGo.GetComponent<Image>();
                roll._face.preserveAspect = true;
                BroadcastUi.Place(roll._face.rectTransform, 0.28f, 0.42f, 0.72f, 0.9f);
                roll._who = BroadcastUi.Label(card.transform, "Who", "", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(roll._who.rectTransform, 0.06f, 0.28f, 0.94f, 0.42f);
                roll._line = BroadcastUi.Label(card.transform, "Line", "", 26, new Color(0.95f, 0.86f, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(roll._line.rectTransform, 0.05f, 0.05f, 0.95f, 0.3f);
                roll._line.horizontalOverflow = HorizontalWrapMode.Wrap;
                roll._line.verticalOverflow = VerticalWrapMode.Overflow;
                var crestGo = new GameObject("Crest", typeof(RectTransform), typeof(Image));
                crestGo.transform.SetParent(card.transform, false);
                roll._crest = crestGo.GetComponent<Image>();
                roll._crest.preserveAspect = true;
                roll._crest.raycastTarget = false;
                BroadcastUi.Place(roll._crest.rectTransform, 0.03f, 0.82f, 0.16f, 0.97f);
            }
            else
                roll = host.GetComponent<MomentRoll>();

            var dim = roll.transform.Find("Shade")?.GetComponent<Image>();
            if (dim != null)
            {
                dim.color = new Color(0f, 0f, 0f, 0f);
                dim.raycastTarget = false;
            }
            var cardRect = roll.transform.Find("Card") as RectTransform;
            if (cardRect != null)
            {
                BroadcastUi.Place(cardRect, 0.16f, 0.78f, 0.84f, 0.985f);
                var faceRect = cardRect.Find("Face") as RectTransform;
                if (faceRect != null)
                    BroadcastUi.Place(faceRect, 0.02f, 0.12f, 0.12f, 0.88f);
                var whoRect = cardRect.Find("Who") as RectTransform;
                if (whoRect != null)
                    BroadcastUi.Place(whoRect, 0.14f, 0.5f, 0.96f, 0.94f);
                var lineRect = cardRect.Find("Line") as RectTransform;
                if (lineRect != null)
                    BroadcastUi.Place(lineRect, 0.14f, 0.06f, 0.96f, 0.5f);
                var crestRect = cardRect.Find("Crest") as RectTransform;
                if (crestRect != null)
                    BroadcastUi.Place(crestRect, 0.84f, 0.55f, 0.97f, 0.96f);
            }
            roll._to = player;
            roll._toFace = player == null ? null : FaceBook.For(player);
            roll._fromFace = roll._toFace;
            roll._landing = Landing(flash);
            roll._time = 0f;
            roll._playing = true;
            roll.gameObject.SetActive(true);
            roll.transform.SetAsLastSibling();
            roll.BuildReel(steps);
            if (roll._face != null)
            {
                roll._face.sprite = roll._fromFace != null ? roll._fromFace : roll._toFace;
                roll._face.color = Color.white;
                roll._face.gameObject.SetActive(roll._face.sprite != null);
            }
            if (roll._who != null)
                roll._who.text = player == null ? "" : player.FullName.ToUpperInvariant();
            if (roll._crest != null)
            {
                roll._crest.sprite = crest;
                roll._crest.gameObject.SetActive(crest != null);
            }
        }

        void BuildReel(string[] steps)
        {
            _reel.Clear();
            if (steps != null)
            {
                for (var i = 0; i < steps.Length; i++)
                {
                    if (!string.IsNullOrEmpty(steps[i]))
                        _reel.Add(steps[i].ToUpperInvariant());
                }
            }
            if (_reel.Count == 0)
                _reel.Add("A JOGADA CHEGA À ÁREA");
            _reel.Add(_landing);
        }

        void Update()
        {
            if (!_playing)
                return;
            _time += Time.unscaledDeltaTime;
            var step = 0.95f;
            var index = Mathf.Clamp(Mathf.FloorToInt(_time / step), 0, _reel.Count - 1);
            var onResult = index >= _reel.Count - 1;
            if (_line != null)
            {
                _line.text = onResult ? _landing : _reel[index];
                var color = _line.color;
                color.a = onResult ? 1f : 0.42f;
                _line.color = color;
            }
            if (_face != null && _toFace != null)
            {
                var blend = onResult ? 1f : 0f;
                _face.sprite = blend < 0.5f && _fromFace != null ? _fromFace : _toFace;
                if (onResult)
                {
                    var punch = 1f + Mathf.Sin(Mathf.Clamp01((_time - (_reel.Count - 1) * step) / 0.35f) * Mathf.PI) * 0.08f;
                    _face.rectTransform.localScale = new Vector3(punch, punch, 1f);
                }
            }
            if (onResult && _time > (_reel.Count - 1) * step + 1.7f)
            {
                _playing = false;
                gameObject.SetActive(false);
            }
        }

        static string Landing(string flash)
        {
            var text = flash.ToUpperInvariant();
            if (text.Contains("CHILENA") || text.Contains("BICYCLE"))
                return "PONTAPÉ DE BICICLETA\nGOLOOOOOOOOOOOOOOOOOOOOOOOOO";
            if (text.Contains("PENÁLTI") || text.Contains("PENALTY"))
                return text.Contains("GOLO") || text.Contains("GOAL")
                    ? "É PENÁLTI\nGOLOOOOOOOOOOOOOOOOOOOOOOOOO"
                    : "É PENÁLTI";
            if (text.Contains("GOLO") || text.Contains("GOAL"))
                return "VAI À BALIZA\nGOLOOOOOOOOOOOOOOOOOOOOOOOOO";
            if (text.Contains("VERMELHO") || text.Contains("RED"))
                return "CARTÃO VERMELHO";
            if (text.Contains("LES") || text.Contains("INJUR"))
                return "FICA NO CHÃO";
            if (text.Contains("POSTE") || text.Contains("TRAVE"))
                return "FOI AO POSTE";
            if (text.Contains("FALHADO") || text.Contains("FALHA") || text.Contains("LADO"))
                return "REMATE FALHADO";
            if (text.Contains("DEFESA") || text.Contains("SAVE"))
                return "GRANDE DEFESA";
            if (text.Contains("CRUZAMENTO") || text.Contains("CRUZ"))
                return "REMATE FALHADO";
            return text;
        }
    }
}
