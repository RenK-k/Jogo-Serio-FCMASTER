using System;
using System.Globalization;

namespace CoachMaster.Domain
{
    public sealed class GameCalendar
    {
        public static readonly DateTime SeasonStart = new DateTime(2026, 7, 13);

        public DateTime CurrentDate { get; private set; }

        public GameCalendar(DateTime currentDate)
        {
            CurrentDate = currentDate.Date;
        }

        public string WeekdayName
        {
            get
            {
                switch (CurrentDate.DayOfWeek)
                {
                    case DayOfWeek.Monday: return "Segunda-feira";
                    case DayOfWeek.Tuesday: return "Terça-feira";
                    case DayOfWeek.Wednesday: return "Quarta-feira";
                    case DayOfWeek.Thursday: return "Quinta-feira";
                    case DayOfWeek.Friday: return "Sexta-feira";
                    case DayOfWeek.Saturday: return "Sábado";
                    default: return "Domingo";
                }
            }
        }

        public string LongDateText
        {
            get
            {
                var culture = CultureInfo.GetCultureInfo("pt-PT");
                return $"{WeekdayName}, {CurrentDate.ToString("d 'de' MMMM 'de' yyyy", culture)}";
            }
        }

        public string SeasonLabel => $"{CurrentDate.Year}/{CurrentDate.Year + 1}";

        public void AdvanceDays(int days)
        {
            CurrentDate = CurrentDate.AddDays(days);
        }
    }
}
