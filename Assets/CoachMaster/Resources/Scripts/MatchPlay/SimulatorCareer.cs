using System;
using System.Collections.Generic;
using CoachMaster.Domain;
using CoachMaster.UI;
using FStudio.Data;
using FStudio.Database;
using FStudio.MatchEngine.Tactics;
using UnityEngine;

namespace CoachMaster.MatchPlay
{
    public static class SimulatorCareer
    {
        const string KitPlayers = "Assets/FootballSimulator/Arts/FootballPlayer/PlayerModel/UIKits/KitMask1.png";
        const string KitCloth = "Assets/FootballSimulator/Arts/FootballPlayer/PlayerModel/Textures/KitSchemas/KitMask1.png";

        public static void Prepare(MatchSession match)
        {
            CareerTacticLock.Active = match != null;
            CareerTacticLock.Home = Tactic(match == null ? null : match.HomeTactics);
            CareerTacticLock.Away = Tactic(match == null ? null : match.AwayTactics);
        }

        public static TeamEntry BuildTeam(MatchSession match, bool home)
        {
            var club = home ? match.Home : match.Away;
            var tactics = home ? match.HomeTactics : match.AwayTactics;
            var ids = home ? match.HomeOnField : match.AwayOnField;
            var formation = Formation(tactics == null ? null : tactics.Formation);
            var team = ScriptableObject.CreateInstance<TeamEntry>();
            team.TeamName = club == null || string.IsNullOrEmpty(club.Name) ? (home ? "Casa" : "Fora") : club.Name;
            team.Formation = formation;
            team.HomeKit = Kit(club, false);
            team.AwayKit = Kit(club, true);
            team.TeamLogo = Logo(team.HomeKit.Color1, team.HomeKit.Color2);
            var people = new List<Player>();
            for (var i = 0; i < ids.Count && i < 11; i++)
            {
                if (match.People.TryGetValue(ids[i], out var player) && player != null)
                    people.Add(player);
            }

            var rule = FormationRules.GetTeamFormation(formation);
            var ordered = Order(people, rule.Positions);
            team.Players = new PlayerEntry[11];
            for (var i = 0; i < 11; i++)
            {
                var entry = PlayerEntry(ordered[i], i);
                entry.team = team;
                team.Players[i] = entry;
            }

            return team;
        }

        static PlayerEntry PlayerEntry(Player player, int index)
        {
            var entry = ScriptableObject.CreateInstance<PlayerEntry>();
            entry.Name = player == null || string.IsNullOrEmpty(player.FullName) ? "Jogador " + (index + 1) : ShortName(player.FullName);
            entry.height = player == null || player.HeightCm < 150 ? 180 : Mathf.Clamp(player.HeightCm, 150, 210);
            entry.weight = player == null || player.WeightKg < 45 ? 76 : Mathf.Clamp(player.WeightKg, 45, 100);
            entry.SkinColor = player != null && FaceBook.DarkSkin(player) ? SkinColor.Dark : SkinColor.LightBronze;
            var mix = Mix(entry.Name);
            var hair = new[]
            {
                HairStyles.ShortHair,
                HairStyles.StylishHair,
                HairStyles.ShortItalian,
                HairStyles.Mohawk,
                HairStyles.StylishUnordinaryHair,
                HairStyles.BobMarley
            };
            entry.HairStyles = hair[mix % hair.Length];
            entry.HairColor = entry.SkinColor == SkinColor.Dark || mix % 4 != 0 ? HairColors.Black : HairColors.DarkBrown;
            entry.FacialHairColor = entry.HairColor;
            if (index > 0 && mix % 3 == 0)
                entry.FacialHairStyles = (FacialHairStyles)(1 + ((mix / 3) % 3));
            if (player == null || player.Attributes == null)
                return entry;

            var pace = Skill(player, "Velocidade");
            var strength = Skill(player, "Força");
            var passing = Skill(player, "Passe");
            var finishing = Skill(player, "Finalização");
            var dribbling = Skill(player, "Drible");
            var tackling = Skill(player, "Desarme");
            var crossing = Skill(player, "Cruzamento");
            var positioning = Skill(player, "Posicionamento");
            var composure = Skill(player, "Frieza");
            var work = Skill(player, "Entrega");
            var decisions = Skill(player, "Decisão");
            var reflexes = Skill(player, "Reflexos");
            var handling = Skill(player, "Manejo");
            if (player.HasTrait(PlayerTrait.Rapido))
                pace = Lift(pace, 8);
            if (player.HasTrait(PlayerTrait.Driblador) || player.HasTrait(PlayerTrait.DribladorTecnico) || player.HasTrait(PlayerTrait.DribladorRapido))
                dribbling = Lift(dribbling, 8);
            if (player.HasTrait(PlayerTrait.Visionario) || player.HasTrait(PlayerTrait.CriadorDeJogo))
                passing = Lift(passing, 8);
            if (player.HasTrait(PlayerTrait.PassadorLongo) || player.HasTrait(PlayerTrait.Cruzador) || player.HasTrait(PlayerTrait.LancamentoLongo))
                crossing = Lift(crossing, 10);
            if (player.HasTrait(PlayerTrait.Finalizador) || player.HasTrait(PlayerTrait.RemateEmJeito) || player.HasTrait(PlayerTrait.RemateDeLonge))
                finishing = Lift(finishing, 8);
            if (player.HasTrait(PlayerTrait.Marcador))
                tackling = Lift(tackling, 8);
            if (player.HasTrait(PlayerTrait.Cabeceador))
                strength = Lift(strength, 6);
            if (player.HasTrait(PlayerTrait.ImuneAPressao))
                composure = Lift(composure, 8);
            if (player.HasTrait(PlayerTrait.Trabalhador))
                work = Lift(work, 6);

            entry.topSpeed = pace;
            entry.acceleration = pace;
            entry.dribbleSpeed = dribbling;
            entry.agility = Mean(pace, work);
            entry.strength = strength;
            entry.jump = player.HasTrait(PlayerTrait.Cabeceador) ? Lift(Mean(strength, positioning), 8) : Mean(strength, positioning);
            entry.tackling = tackling;
            entry.ballKeeping = composure;
            entry.passing = passing;
            entry.longBall = crossing;
            entry.shooting = finishing;
            entry.shootPower = player.HasTrait(PlayerTrait.Potente) ? Lift(finishing, 10) : finishing;
            entry.positioning = positioning;
            entry.reaction = player.Position == PlayerPosition.Goalkeeper ? Mean(reflexes, handling) : decisions;
            entry.ballControl = Mean(dribbling, composure);
            return entry;
        }

        static string ShortName(string fullName)
        {
            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? fullName : parts[parts.Length - 1];
        }

        static LogoEntry Logo(Color primary, Color secondary)
        {
            var logo = ScriptableObject.CreateInstance<LogoEntry>();
            logo.TeamLogoColor1 = primary;
            logo.TeamLogoColor2 = secondary;
#if UNITY_EDITOR
            logo.TeamLogoMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>("Assets/FootballSimulator/Arts/UI/TeamLogos/TeamLogo1.psd");
#endif
            return logo;
        }

        static KitEntry Kit(Club club, bool away)
        {
            var kit = ScriptableObject.CreateInstance<KitEntry>();
            var primary = FromHex(club == null ? null : club.JerseyPrimaryHex, new Color(0.1f, 0.25f, 0.55f));
            var secondary = FromHex(club == null ? null : club.JerseySecondaryHex, Color.white);
            if (away)
            {
                var swap = primary;
                primary = secondary;
                secondary = swap;
            }

            kit.Color1 = primary;
            kit.Color2 = secondary;
            kit.TextColor = Contrast(primary);
            kit.GKColor1 = new Color(0.85f, 0.85f, 0.2f);
            kit.GKColor2 = new Color(0.1f, 0.1f, 0.1f);
            kit.GKTextColor = Color.black;
#if UNITY_EDITOR
            var players = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>(KitPlayers);
            var cloth = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>(KitCloth);
            kit.PreviewTexture = players;
            kit.KitMaterial = cloth;
            kit.GKKitMaterial = cloth;
#endif
            return kit;
        }

        static Player[] Order(List<Player> people, Positions[] slots)
        {
            var used = new bool[people.Count];
            var ordered = new Player[11];
            for (var slot = 0; slot < 11; slot++)
            {
                var best = -1;
                var bestScore = -1;
                for (var i = 0; i < people.Count; i++)
                {
                    if (used[i])
                        continue;
                    var score = Fit(people[i].Position, slots[slot]);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = i;
                    }
                }

                if (best >= 0)
                {
                    used[best] = true;
                    ordered[slot] = people[best];
                }
            }

            return ordered;
        }

        static int Fit(PlayerPosition position, Positions slot)
        {
            if (slot == Positions.GK)
                return position == PlayerPosition.Goalkeeper ? 5 : 0;
            if (position == PlayerPosition.Goalkeeper)
                return 0;
            switch (slot)
            {
                case Positions.LB: return position == PlayerPosition.LeftBack ? 5 : position == PlayerPosition.LeftMidfielder || position == PlayerPosition.LeftWinger ? 2 : position == PlayerPosition.CentreBack ? 1 : 0;
                case Positions.RB: return position == PlayerPosition.RightBack ? 5 : position == PlayerPosition.RightMidfielder || position == PlayerPosition.RightWinger ? 2 : position == PlayerPosition.CentreBack ? 1 : 0;
                case Positions.CB:
                case Positions.CB_L:
                case Positions.CB_R: return position == PlayerPosition.CentreBack ? 5 : position == PlayerPosition.LeftBack || position == PlayerPosition.RightBack ? 2 : 0;
                case Positions.DMF:
                case Positions.DMF_L:
                case Positions.DMF_R: return position == PlayerPosition.DefensiveMidfielder ? 5 : position == PlayerPosition.CentralMidfielder ? 3 : 0;
                case Positions.CM:
                case Positions.CM_L:
                case Positions.CM_R: return position == PlayerPosition.CentralMidfielder ? 5 : position == PlayerPosition.DefensiveMidfielder || position == PlayerPosition.AttackingMidfielder ? 3 : 0;
                case Positions.LMF: return position == PlayerPosition.LeftMidfielder ? 5 : position == PlayerPosition.LeftWinger ? 4 : position == PlayerPosition.LeftBack ? 1 : 0;
                case Positions.RMF: return position == PlayerPosition.RightMidfielder ? 5 : position == PlayerPosition.RightWinger ? 4 : position == PlayerPosition.RightBack ? 1 : 0;
                case Positions.AMF:
                case Positions.AMF_L:
                case Positions.AMF_R: return position == PlayerPosition.AttackingMidfielder ? 5 : position == PlayerPosition.CentralMidfielder ? 2 : position == PlayerPosition.Striker ? 1 : 0;
                case Positions.LW: return position == PlayerPosition.LeftWinger ? 5 : position == PlayerPosition.LeftMidfielder ? 3 : 0;
                case Positions.RW: return position == PlayerPosition.RightWinger ? 5 : position == PlayerPosition.RightMidfielder ? 3 : 0;
                case Positions.ST:
                case Positions.ST_L:
                case Positions.ST_R: return position == PlayerPosition.Striker ? 5 : position == PlayerPosition.AttackingMidfielder || position == PlayerPosition.LeftWinger || position == PlayerPosition.RightWinger ? 2 : 0;
                default: return 1;
            }
        }

        public static TacticPresetTypes Tactic(TacticalIdentity tactics)
        {
            var text = ((tactics == null ? "" : tactics.Preset) + " " + (tactics == null ? "" : tactics.Pressing) + " " + (tactics == null ? "" : tactics.AttackIdea) + " " + (tactics == null ? "" : tactics.BuildUp)).ToLowerInvariant();
            if (text.Contains("contra"))
                return TacticPresetTypes.CounterAttack;
            if (text.Contains("ônibus") || text.Contains("onibus") || text.Contains("park"))
                return TacticPresetTypes.ParkTheBus;
            if (text.Contains("press") || (text.Contains("alta") && text.Contains("linha")))
                return TacticPresetTypes.HighPressure;
            if (text.Contains("defens"))
                return TacticPresetTypes.Defensive;
            if (text.Contains("ofens") || text.Contains("ataque"))
                return TacticPresetTypes.Offensive;
            return TacticPresetTypes.Balanced;
        }

        static Formations Formation(string formation)
        {
            var text = formation ?? "";
            if (text.Contains("4-4-2")) return Formations._4_4_2;
            if (text.Contains("4-2-3-1")) return Formations._4_2_3_1_A;
            if (text.Contains("4-1-4-1")) return Formations._4_1_4_1;
            if (text.Contains("4-3-2-1")) return Formations._4_3_2_1;
            if (text.Contains("3-5-2")) return Formations._3_5_2;
            if (text.Contains("3-4-3")) return Formations._3_4_3;
            if (text.Contains("5-4-1")) return Formations._5_4_1;
            if (text.Contains("5-3-2")) return Formations._5_3_2;
            if (text.Contains("5-2-3")) return Formations._5_2_3;
            return Formations._4_3_3;
        }

        static int Mix(string text)
        {
            var hash = 17;
            if (string.IsNullOrEmpty(text))
                return hash;
            for (var i = 0; i < text.Length; i++)
                hash = hash * 31 + text[i];
            return hash & 0x7fffffff;
        }

        static int Skill(Player player, string key)
        {
            return Mathf.Clamp(PlayerAttributes.CurrentValue(player, key) * 5, 5, 99);
        }

        static int Lift(int value, int amount)
        {
            return Mathf.Clamp(value + amount, 5, 99);
        }

        static int Mean(int a, int b)
        {
            return Mathf.Clamp((a + b) / 2, 5, 99);
        }

        static Color FromHex(string hex, Color fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var color))
                return color;
            return fallback;
        }

        static Color Contrast(Color color)
        {
            var light = color.r * 0.3f + color.g * 0.6f + color.b * 0.1f;
            return light > 0.62f ? Color.black : Color.white;
        }
    }
}
