using UnityEngine;

using TMPro;

namespace FStudio.UI {
    public class SquadPosition : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI positionText;

        public void SetPosition (FStudio.Data.Positions position) {
            if (position == FStudio.Data.Positions.ParametersCount) {
                positionText.text = FStudio.MatchEngine.Tactics.CareerTacticLock.Portuguese ? "TODOS" : "ALL";
                return;
            }

            var code = position.ToString();
            if (FStudio.MatchEngine.Tactics.CareerTacticLock.Portuguese && PortuguesePositions.TryGetValue(code, out var translated))
                code = translated;
            positionText.text = code;
        }

        static readonly System.Collections.Generic.Dictionary<string, string> PortuguesePositions = new System.Collections.Generic.Dictionary<string, string> {
            { "GK", "GR" },
            { "LB", "LE" }, { "RB", "LD" }, { "CB", "DC" }, { "CB_L", "DC" }, { "CB_R", "DC" },
            { "DMF", "MDC" }, { "DMF_L", "MDC" }, { "DMF_R", "MDC" },
            { "CM", "MC" }, { "CM_L", "MC" }, { "CM_R", "MC" },
            { "AMF", "MO" }, { "AMF_L", "MO" }, { "AMF_R", "MO" },
            { "LMF", "ME" }, { "RMF", "MD" },
            { "LW", "EE" }, { "RW", "ED" },
            { "ST", "AV" }, { "ST_L", "AV" }, { "ST_R", "AV" }
        };
    }
}
