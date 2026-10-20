using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class StaffReport
    {
        public static int Read(StaffMember reader, string subjectId, string stat, int truth, int min, int max)
        {
            var error = MaxError(reader == null ? 8 : reader.Judgement);
            var wobble = Stable(reader, subjectId, stat) % (error * 2 + 1) - error;
            var value = truth + wobble;
            if (value < min)
                value = min;
            if (value > max)
                value = max;
            return value;
        }

        public static string Confidence(StaffMember reader)
        {
            var judgement = reader == null ? 8 : reader.Judgement;
            if (judgement >= 16)
                return "fala com convicção. Mesmo assim, não é infalível.";
            if (judgement >= 12)
                return "acerta o essencial e perde o detalhe.";
            return "anda muitas vezes ao lado. Trata a leitura como um palpite.";
        }

        public static string Quality(StaffMember reader)
        {
            var judgement = reader == null ? 8 : reader.Judgement;
            if (judgement >= 16)
                return "Lê o que se passa com precisão.";
            if (judgement >= 12)
                return "Vê o que salta à vista e falha o resto.";
            return "A leitura chega atrasada e, por vezes, errada.";
        }

        public static StaffMember Find(Club club, StaffRole role)
        {
            if (club == null || club.Staff == null)
                return null;
            foreach (var member in club.Staff)
            {
                if (member.Role == role)
                    return member;
            }

            return club.Staff.Count > 0 ? club.Staff[0] : null;
        }

        public static StaffMember Offer(Club club, StaffRole role)
        {
            var current = Find(club, role);
            var seed = Stable(current, club.Id, role + ":" + club.DressingRoomTension);
            var name = Names[seed % Names.Length];
            var guard = 0;
            while (Taken(club, name) && guard < Names.Length)
            {
                guard++;
                name = Names[(seed + guard) % Names.Length];
            }

            var judgement = 15 + seed % 5;
            if (current != null && judgement <= current.Judgement)
                judgement = current.Judgement + 1;
            if (judgement > 19)
                judgement = 19;

            return new StaffMember
            {
                Id = (current == null ? club.Id : current.Id) + ":" + name,
                ClubId = club.Id,
                FullName = name,
                Role = role,
                Judgement = judgement
            };
        }

        static bool Taken(Club club, string name)
        {
            foreach (var member in club.Staff)
            {
                if (member.FullName == name)
                    return true;
            }

            return false;
        }

        static int MaxError(int judgement)
        {
            if (judgement >= 16)
                return 2;
            if (judgement >= 12)
                return 4;
            return 6;
        }

        static int Stable(StaffMember reader, string subjectId, string stat)
        {
            var text = (reader == null ? "ninguem" : reader.Id) + "|" + subjectId + "|" + stat;
            unchecked
            {
                var hash = 23;
                foreach (var c in text)
                    hash = hash * 31 + c;
                return hash & 0x7fffffff;
            }
        }

        static readonly string[] Names =
        {
            "Duarte Belém", "Rita Sado", "Nuno Vouga", "Helena Tejo", "Paulo Mira",
            "Inês Laurada", "Rui Cértima", "Marta Azibo", "João Lena", "Sara Dão",
            "Carlos Minho", "Beatriz Tua", "Miguel Arda", "Ana Zêzere", "Vítor Cávado"
        };
    }
}
