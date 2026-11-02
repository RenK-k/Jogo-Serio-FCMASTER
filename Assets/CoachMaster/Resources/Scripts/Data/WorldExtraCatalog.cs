using System;
using CoachMaster.Domain;
using CoachMaster.UI;
using UnityEngine;

namespace CoachMaster.Data
{
    public static class WorldExtraCatalog
    {
        static readonly string[] Sponsors =
        {
            "Northstar Mobile", "Asteron Energy", "Crownline Finance", "Bluewell Telecom",
            "Meridian Athletic", "Harbourlight Bank", "Velora Motors", "Pinnacle Works",
            "Rivermark Insurance", "Summit Wireless", "Redwood Travel", "Fieldstone Group"
        };

        public static void EnsureSponsors(WorldState world)
        {
            if (world?.Clubs == null) return;
            foreach (var club in world.Clubs.Values)
            {
                if (club == null) continue;
                if (club.Sponsors == null) club.Sponsors = new System.Collections.Generic.List<SponsorContract>();
                if (club.Sponsors.Count > 0) continue;
                var seed = StableHash(club.Id ?? club.Name);
                var mainName = Sponsors[seed % Sponsors.Length];
                var localName = LocalSponsor(club.City, seed);
                club.Sponsors.Add(new SponsorContract
                {
                    Id = "sponsor:main:" + club.Id,
                    Name = mainName,
                    WeeklyIncome = WeeklyDeal(club.Reputation, 1f),
                    LogoKey = Initials(mainName),
                    LogoHex = SponsorColor(seed),
                    IsActive = true
                });
                club.Sponsors.Add(new SponsorContract
                {
                    Id = "sponsor:local:" + club.Id,
                    Name = localName,
                    WeeklyIncome = WeeklyDeal(club.Reputation, 0.28f),
                    LogoKey = Initials(localName),
                    LogoHex = SponsorColor(seed + 5),
                    IsActive = true
                });
            }
        }

        public static int WeeklyDeal(int reputation, float scale)
        {
            var baseline = Math.Max(0, reputation) * Math.Max(0, reputation) * 8;
            return Mathf.Clamp(Mathf.RoundToInt(baseline * scale), 0, 10000000);
        }

        static string LocalSponsor(string city, int seed)
        {
            var roots = new[] { "Foundry", "Harbour", "Civic", "Union", "Beacon", "Works", "Exchange", "Park" };
            return (string.IsNullOrWhiteSpace(city) ? "City" : city.Trim()) + " " + roots[(seed / 7) % roots.Length];
        }

        static string Initials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "SP";
            var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpperInvariant();
            return (words[0][0].ToString() + words[1][0]).ToUpperInvariant();
        }

        static string SponsorColor(int seed)
        {
            var colors = new[] { "#20D8F4", "#34D27C", "#F0B43C", "#EF5C68", "#7D8CFF", "#F18B48" };
            return colors[Math.Abs(seed) % colors.Length];
        }

        static int StableHash(string value)
        {
            unchecked
            {
                var hash = 23;
                foreach (var c in value ?? "") hash = hash * 31 + c;
                return hash & 0x7fffffff;
            }
        }
    }

    public static class FreeAgentCatalog
    {
        static readonly PlayerPosition[] Positions =
        {
            PlayerPosition.Goalkeeper, PlayerPosition.RightBack, PlayerPosition.CentreBack,
            PlayerPosition.LeftBack, PlayerPosition.DefensiveMidfielder, PlayerPosition.CentralMidfielder,
            PlayerPosition.AttackingMidfielder, PlayerPosition.RightWinger, PlayerPosition.LeftWinger,
            PlayerPosition.Striker
        };

        static readonly string[] Nations =
        {
            "Inglaterra", "Portugal", "Espanha", "França", "Alemanha", "Países Baixos",
            "Brasil", "Argentina", "Itália", "Nigéria", "Gana", "Japão"
        };

        public static void Ensure(WorldState world)
        {
            if (world == null) return;
            const int count = 36;
            for (var i = 0; i < count; i++)
            {
                var id = "free-agent:" + i.ToString("000");
                if (world.Players.ContainsKey(id)) continue;
                var seed = (i + 37) * 7919;
                var age = 18 + (seed % 16);
                var position = Positions[i % Positions.Length];
                var nationality = Nations[(seed / 11) % Nations.Length];
                var reputation = 36 + seed % 32;
                var player = new Player
                {
                    Id = id,
                    ClubId = "",
                    FullName = FaceBook.PersonName(nationality, seed),
                    Age = age,
                    SquadNumber = 0,
                    Position = position,
                    PreferredFoot = (seed & 1) == 0 ? PreferredFoot.Right : PreferredFoot.Left,
                    Nationality = nationality,
                    Morale = 55 + seed % 26,
                    Fatigue = 0,
                    Fitness = 76 + seed % 19,
                    Form = 48 + seed % 27,
                    Motivation = 55 + seed % 30,
                    HeightCm = position == PlayerPosition.Goalkeeper || position == PlayerPosition.CentreBack ? 181 + seed % 16 : 168 + seed % 20,
                    Potential = Mathf.Clamp(reputation + (age <= 21 ? 8 + seed % 16 : seed % 7), 42, 91),
                    Reputation = reputation,
                    WeeklyWage = Mathf.Max(500, 1700 + reputation * 45),
                    ContractEndTicks = 0,
                    TrainingRating = 60 + seed % 25,
                    Attributes = PlayerAttributes.Roll(seed, position, reputation),
                    Injury = new InjuryState(),
                    Duty = IndividualOrders.For(position, seed)
                };
                player.WeightKg = Mathf.Clamp(player.HeightCm - 101 + seed % 9, 52, 110);
                PlayerPositionProfile.Assign(player, seed);
                PlayerTraits.Assign(player, seed);
                world.Players[id] = player;
            }
        }
    }
}
