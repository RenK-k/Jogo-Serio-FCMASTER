using System;
using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public enum OnlineWait
    {
        None,
        OtherMatch,
        Clock,
        Partner
    }

    public sealed class OnlineSeat
    {
        public string Name;
        public string ClubId;
        public string Color;
        public bool Local;
        public bool ContinueReady;
        public bool Connected;
    }

    public struct OnlineGate
    {
        public OnlineWait Wait;
        public string Message;
        public bool Stop => Wait != OnlineWait.None;
    }

    public sealed class CareerOnline
    {
        public const int DefaultPort = 7777;
        public static string PendingClub;

        public string Mode = "tempo-real";
        public int KickoffHour = 20;
        public int KickoffMinute;
        public int Port = DefaultPort;
        public string RoomCode = "";
        public readonly List<OnlineSeat> Seats = new List<OnlineSeat>();

        public bool Active => Seats.Count >= 2;

        public static readonly string[] SeatColors =
        {
            "#F4F1E8", "#E07A3D", "#3D8BE0", "#3DDE8B", "#E03D8B", "#E0D23D", "#8B3DE0", "#3DE0D2"
        };

        public string Pack()
        {
            var seats = "";
            for (var i = 0; i < Seats.Count; i++)
            {
                var seat = Seats[i];
                if (seat == null || string.IsNullOrEmpty(seat.ClubId))
                    continue;
                if (seats.Length > 0)
                    seats += ";";
                seats += Clean(seat.Name) + "," + seat.ClubId + "," + (string.IsNullOrEmpty(seat.Color) ? SeatColors[0] : seat.Color);
            }
            var mode = Mode == "hora" ? "hora" : "tempo-real";
            return mode + "|" + KickoffHour + "|" + KickoffMinute + "|" + Port + "|" + Clean(RoomCode) + "|" + seats;
        }

        public static CareerOnline Parse(string payload)
        {
            var online = new CareerOnline();
            if (string.IsNullOrEmpty(payload))
                return online;
            var bits = payload.Split('|');
            if (bits.Length > 0)
                online.Mode = bits[0] == "hora" ? "hora" : "tempo-real";
            if (bits.Length > 1)
                int.TryParse(bits[1], out online.KickoffHour);
            if (bits.Length > 2)
                int.TryParse(bits[2], out online.KickoffMinute);
            if (bits.Length > 3)
                int.TryParse(bits[3], out online.Port);
            if (online.Port <= 0 || online.Port > 65535)
                online.Port = DefaultPort;
            online.KickoffHour = Math.Max(0, Math.Min(23, online.KickoffHour));
            online.KickoffMinute = Math.Max(0, Math.Min(59, online.KickoffMinute));
            if (bits.Length > 4)
                online.RoomCode = bits[4];
            if (bits.Length > 5 && !string.IsNullOrEmpty(bits[5]))
            {
                var chunks = bits[5].Split(';');
                for (var i = 0; i < chunks.Length; i++)
                {
                    var seatBits = chunks[i].Split(',');
                    if (seatBits.Length < 2 || string.IsNullOrEmpty(seatBits[1]))
                        continue;
                    var color = seatBits.Length > 2 ? seatBits[2] : SeatColors[Math.Min(i, SeatColors.Length - 1)];
                    if (string.IsNullOrEmpty(color) || color[0] != '#')
                        color = SeatColors[Math.Min(i, SeatColors.Length - 1)];
                    online.Seats.Add(new OnlineSeat { Name = seatBits[0], ClubId = seatBits[1], Color = color });
                }
            }
            return online;
        }

        public static void BindMachine(CareerState career)
        {
            if (career?.Online == null || career.Coach == null)
                return;
            var wanted = PendingClub;
            if (string.IsNullOrEmpty(wanted) || !HasClub(career, wanted))
                wanted = career.Coach.ClubId;
            OnlineSeat mine = null;
            for (var i = 0; i < career.Online.Seats.Count; i++)
            {
                var seat = career.Online.Seats[i];
                if (seat == null)
                    continue;
                seat.Local = seat.ClubId == wanted;
                seat.ContinueReady = false;
                if (seat.Local && mine == null)
                    mine = seat;
            }
            if (mine == null && career.Online.Seats.Count > 0 && career.Online.Seats[0] != null)
            {
                career.Online.Seats[0].Local = true;
                mine = career.Online.Seats[0];
            }
            if (mine != null && mine.ClubId != career.Coach.ClubId && career.World != null && career.World.Clubs.ContainsKey(mine.ClubId))
                career.Coach.ClubId = mine.ClubId;
            LanRoom.MarkConnected(career);
        }

        public static bool HasClub(CareerState career, string clubId)
        {
            if (career?.Online == null || string.IsNullOrEmpty(clubId))
                return false;
            for (var i = 0; i < career.Online.Seats.Count; i++)
            {
                var seat = career.Online.Seats[i];
                if (seat != null && seat.ClubId == clubId)
                    return true;
            }
            return false;
        }

        public static bool OtherHuman(CareerState career, Fixture fixture)
        {
            if (career?.Online == null || fixture == null || career.Coach == null || !career.Online.Active)
                return false;
            for (var i = 0; i < career.Online.Seats.Count; i++)
            {
                var seat = career.Online.Seats[i];
                if (seat == null || string.IsNullOrEmpty(seat.ClubId) || seat.ClubId == career.Coach.ClubId)
                    continue;
                if (fixture.Involves(seat.ClubId))
                    return true;
            }
            return false;
        }

        public static bool OwnsUpcoming(CareerState career)
        {
            var earliest = Earliest(career);
            if (earliest == null || career?.Coach == null || career.Calendar == null)
                return false;
            if (earliest.Date.Date > career.Calendar.CurrentDate.Date)
                return false;
            return earliest.Involves(career.Coach.ClubId);
        }

        public static OnlineSeat LocalSeat(CareerState career)
        {
            if (career?.Online == null)
                return null;
            for (var i = 0; i < career.Online.Seats.Count; i++)
            {
                var seat = career.Online.Seats[i];
                if (seat != null && seat.Local)
                    return seat;
            }
            return null;
        }

        public static bool EveryoneReady(CareerState career)
        {
            if (career?.Online == null || !career.Online.Active)
                return false;
            for (var i = 0; i < career.Online.Seats.Count; i++)
            {
                var seat = career.Online.Seats[i];
                if (seat != null && !seat.ContinueReady)
                    return false;
            }
            return true;
        }

        public static void ClearReady(CareerState career)
        {
            if (career?.Online == null)
                return;
            for (var i = 0; i < career.Online.Seats.Count; i++)
            {
                if (career.Online.Seats[i] != null)
                    career.Online.Seats[i].ContinueReady = false;
            }
        }

        public static OnlineGate Check(CareerState career, bool startingMatch)
        {
            var gate = new OnlineGate();
            var online = career?.Online;
            if (online == null || !online.Active || career.Coach == null || career.Calendar == null)
                return gate;
            var earliest = Earliest(career);
            if (earliest != null && earliest.Date.Date <= career.Calendar.CurrentDate.Date)
            {
                if (!earliest.Involves(career.Coach.ClubId))
                {
                    gate.Wait = OnlineWait.OtherMatch;
                    gate.Message = "À espera do jogo de " + NameOn(career, earliest) + ". Esse encontro é primeiro.";
                    return gate;
                }
                if (startingMatch && online.Mode == "hora")
                {
                    var now = DateTime.Now;
                    var kick = new DateTime(now.Year, now.Month, now.Day, online.KickoffHour, online.KickoffMinute, 0);
                    if (now < kick)
                    {
                        gate.Wait = OnlineWait.Clock;
                        gate.Message = "O jogo está marcado para as " + kick.ToString("HH:mm") + ".";
                        return gate;
                    }
                }
                return gate;
            }

            var local = LocalSeat(career);
            if (local != null)
                local.ContinueReady = true;
            for (var i = 0; i < online.Seats.Count; i++)
            {
                var seat = online.Seats[i];
                if (seat == null || seat.ContinueReady)
                    continue;
                gate.Wait = OnlineWait.Partner;
                gate.Message = "À espera que " + (string.IsNullOrEmpty(seat.Name) ? "o outro treinador" : seat.Name) + " carregue em continuar.";
                return gate;
            }
            ClearReady(career);
            return gate;
        }

        public static Fixture Earliest(CareerState career)
        {
            if (career?.Online == null || !career.Online.Active)
                return null;
            Fixture best = null;
            foreach (var fixture in career.Fixtures)
            {
                if (fixture == null || fixture.Played || fixture.Walkover)
                    continue;
                if (!InvolvesSeat(career, fixture))
                    continue;
                if (best == null || fixture.Date < best.Date || (fixture.Date == best.Date && string.CompareOrdinal(fixture.Id, best.Id) < 0))
                    best = fixture;
            }
            return best;
        }

        static bool InvolvesSeat(CareerState career, Fixture fixture)
        {
            for (var i = 0; i < career.Online.Seats.Count; i++)
            {
                var seat = career.Online.Seats[i];
                if (seat != null && fixture.Involves(seat.ClubId))
                    return true;
            }
            return false;
        }

        static string NameOn(CareerState career, Fixture fixture)
        {
            for (var i = 0; i < career.Online.Seats.Count; i++)
            {
                var seat = career.Online.Seats[i];
                if (seat == null || seat.ClubId == career.Coach.ClubId || !fixture.Involves(seat.ClubId))
                    continue;
                if (!string.IsNullOrEmpty(seat.Name))
                    return seat.Name;
            }
            return "outro treinador";
        }

        public static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("|", " ").Replace(",", " ").Replace(";", " ").Replace("\n", " ").Replace("\r", " ");
        }

        public static string MakeCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var ticks = DateTime.UtcNow.Ticks;
            var code = "";
            for (var i = 0; i < 4; i++)
            {
                code += alphabet[(int)(ticks % alphabet.Length)];
                ticks /= alphabet.Length;
            }
            return code;
        }
    }
}
