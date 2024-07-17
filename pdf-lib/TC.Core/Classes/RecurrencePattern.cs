using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TC.Attributes;
using TC.Functions;
using static TC.Classes.RecurrencePattern;

namespace TC.Classes
{
    #region TESTING

    public static class TEST_RecurrencePattern
    {
        #region TESTING

        public static void TEST()
        {
            // declarations
            DateTime startDate = new DateTime(2024, 1, 1); // Sunday
            DateTime dateToCheck;
            RecurrenceTypeEnum type;
            int counter;
            RecurrencePattern pattern;
            string title;

            try
            {
                #region Daily

                // set type
                type = RecurrenceTypeEnum.Daily;

                // every day
                title = "every day";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                while (counter < 3)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every 2nd day
                title = "every 2nd day";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 2);
                while (counter < 3)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                #endregion

                #region Weekly

                // set type
                type = RecurrenceTypeEnum.Weekly;

                // every work-day
                title = "every work-day";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.WeeklyDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
                while (counter < 7)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every week on monday
                title = "every week on monday";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.WeeklyDays = [DayOfWeek.Monday];
                while (counter < 3)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every 2nd week on tuesday
                title = "every 2nd week on tuesday";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 2);
                pattern.WeeklyDays = [DayOfWeek.Tuesday];
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every week on saturday + sunday
                title = "every week on saturday + sunday";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.WeeklyDays = [DayOfWeek.Saturday, DayOfWeek.Sunday];
                while (counter < 4)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                #endregion

                #region Monthly

                // set type
                type = RecurrenceTypeEnum.Monthly;

                // every 1. day of every month
                title = "every 1. day of every month";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.DayOfMonth = 1;
                while (counter < 3)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every 15. day of every 2nd month
                title = "every 15. day of every 2nd month";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 2);
                pattern.DayOfMonth = 15;
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every first monday of every month
                title = "every first monday of every month";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.DayInterval = DayIntervalEnum.First;
                pattern.Day = DayEnum.Monday;
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every last sunday of every 2nd month
                title = "every last sunday of every 2nd month";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 2);
                pattern.DayInterval = DayIntervalEnum.Last;
                pattern.Day = DayEnum.Sunday;
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every first work-day of every month
                title = "every first work-day of every month";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.DayInterval = DayIntervalEnum.First;
                pattern.Day = DayEnum.WorkDay;
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every last work-day of every month
                title = "every last work-day of every month";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.DayInterval = DayIntervalEnum.Last;
                pattern.Day = DayEnum.WorkDay;
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every first day of every month
                title = "every first day of every month";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.DayInterval = DayIntervalEnum.First;
                pattern.Day = DayEnum.Day;
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every last day of every month
                title = "every last day of every month";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.DayInterval = DayIntervalEnum.Last;
                pattern.Day = DayEnum.Day;
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                #endregion

                #region Yearly

                // set type
                type = RecurrenceTypeEnum.Yearly;

                // every year on 6th of january
                title = "every year on 6th of january";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.DayOfMonth = 6;
                pattern.MonthOfYear = 1;
                while (counter < 3)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every year on first monday of january
                title = "every year on first monday of january";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.DayInterval = DayIntervalEnum.First;
                pattern.Day = DayEnum.Monday;
                pattern.MonthOfYear = 1;
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                // every year on last sunday of january
                title = "every year on last sunday of january";
                Console.WriteLine(title + ":");
                dateToCheck = startDate;
                counter = 0;
                pattern = new RecurrencePattern(type, 1);
                pattern.DayInterval = DayIntervalEnum.Last;
                pattern.Day = DayEnum.Sunday;
                pattern.MonthOfYear = 1;
                while (counter < 5)
                {
                    if (pattern.IsDateMatching(dateToCheck, startDate))
                    {
                        counter++;
                        Console.Write($"'{dateToCheck.ToString("ddd, dd.MM.yyyy")}', ");
                    }
                    dateToCheck = dateToCheck.AddDays(1);
                }
                Console.WriteLine(); Console.WriteLine();

                #endregion
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }

    #endregion

    // Recurrence Pattern (14.12.2023, SME)
    public class RecurrencePattern
    {
        #region SHARED

        // Get Day of Week by Day-Enum (14.12.2023, SME)
        public static DayOfWeek? GetDayOfWeek(DayEnum day)
        {
            switch (day)
            {
                case DayEnum.Monday: return DayOfWeek.Monday;
                case DayEnum.Tuesday: return DayOfWeek.Tuesday;
                case DayEnum.Wednesday: return DayOfWeek.Wednesday;
                case DayEnum.Thursday: return DayOfWeek.Thursday;
                case DayEnum.Friday: return DayOfWeek.Friday;
                case DayEnum.Saturday: return DayOfWeek.Saturday;
                case DayEnum.Sunday: return DayOfWeek.Sunday;
                default: return null;
            }
        }

        // Get next Work-Day (14.12.2023, SME)
        public static DateTime GetNextWorkDay(DateTime date, bool returnDateIfWorkDay = false)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday: return date.AddDays(1);
                case DayOfWeek.Saturday: return date.AddDays(2);
                default:
                    if (returnDateIfWorkDay) return date;
                    else return date.AddDays(1);
            }
        }

        // Get previous Work-Day (14.12.2023, SME)
        public static DateTime GetPreviousWorkDay(DateTime date, bool returnDateIfWorkDay = false)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday: return date.AddDays(-2);
                case DayOfWeek.Saturday: return date.AddDays(-1);
                default:
                    if (returnDateIfWorkDay) return date;
                    else return date.AddDays(-1);
            }
        }

        // Get Day of Month (14.12.2023, SME)
        public static DateTime GetDayOfMonth(int year, int month, DayIntervalEnum dayInterval, DayEnum day)
        {
            try
            {
                // error-handling
                if (year < 0 || year > 9999) throw new ArgumentOutOfRangeException(nameof(year));
                if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
                if (!Enum.IsDefined(typeof(DayIntervalEnum), dayInterval)) throw new ArgumentOutOfRangeException(nameof(dayInterval));
                if (!Enum.IsDefined(typeof(DayEnum), day)) throw new ArgumentOutOfRangeException(nameof(day));

                // store first day of month
                var date = new DateTime(year, month, 1);
                var firstDayOfMonth = new DateTime(year, month, 1);

                // handle day to get to first day of this type
                DayOfWeek? dayOfWeek = null;
                switch (day)
                {
                    case DayEnum.Day:
                        // do nothing
                        break;
                    case DayEnum.WorkDay:
                        date = GetNextWorkDay(date, true);
                        break;
                    default:
                        dayOfWeek = GetDayOfWeek(day);
                        if (!dayOfWeek.HasValue) throw new ArgumentOutOfRangeException(nameof(day));
                        while (date.DayOfWeek != dayOfWeek.Value)
                        {
                            date = date.AddDays(1);
                        }
                        break;
                }

                // handle day + day-interval
                switch (day)
                {
                    case DayEnum.Day:
                        // handle day-interval
                        switch (dayInterval)
                        {
                            case DayIntervalEnum.First: return date;
                            case DayIntervalEnum.Seconds: return date.AddDays(1);
                            case DayIntervalEnum.Third: return date.AddDays(2);
                            case DayIntervalEnum.Fourth: return date.AddDays(3);
                            case DayIntervalEnum.Last: return date.AddMonths(1).AddDays(-1);
                            default:
                                throw new ArgumentOutOfRangeException(nameof(dayInterval));
                        }

                    case DayEnum.WorkDay:
                        // handle day-interval
                        switch (dayInterval)
                        {
                            case DayIntervalEnum.First: return date;
                            case DayIntervalEnum.Seconds: return GetNextWorkDay(date.AddDays(1), true);
                            case DayIntervalEnum.Third: return GetNextWorkDay(GetNextWorkDay(date.AddDays(1), true).AddDays(1), true);
                            case DayIntervalEnum.Fourth: return GetNextWorkDay(GetNextWorkDay(GetNextWorkDay(date.AddDays(1), true).AddDays(1), true).AddDays(1), true);
                            case DayIntervalEnum.Last: return GetPreviousWorkDay(firstDayOfMonth.AddMonths(1).AddDays(-1), true);
                            default:
                                throw new ArgumentOutOfRangeException(nameof(dayInterval));
                        }

                    default:
                        // handle day-interval
                        switch (dayInterval)
                        {
                            case DayIntervalEnum.First: return date;
                            case DayIntervalEnum.Seconds: return date.AddDays(7);
                            case DayIntervalEnum.Third: return date.AddDays(14);
                            case DayIntervalEnum.Fourth: return date.AddDays(21);
                            case DayIntervalEnum.Last: 
                                date = date.AddDays(28);
                                if (date.Month == month) return date;
                                else return date.AddDays(-7);
                            default:
                                throw new ArgumentOutOfRangeException(nameof(dayInterval));
                        }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Difference in Months (15.12.2023, SME)
        public static int GetMonthDiff(DateTime date1, DateTime date2)
        {
            if (date1 == date2) return 0;
         
            int factor, year1, year2, month1, month2, diff;
            if (date1 < date2)
            {
                factor = 1;
                year1 = date1.Year;
                month1 = date1.Month;
                year2 = date2.Year;
                month2 = date2.Month;
            }
            else
            {
                factor = -1;
                year1 = date2.Year;
                month1 = date2.Month;
                year2 = date1.Year;
                month2 = date1.Month;
            }

            diff = month2 - month1;
            if (year1 != year2)
            {
                diff += (year2 - year1) * 12;
            }

            return diff * factor;
        }

        // Get Caption (15.12.2023, SME)
        public static string GetCaption(object value)
        {
            // exit-handling
            if (value == null) return string.Empty;

            // handle caption-attribute
            CaptionAttribute caption = null;
            if (!value.GetType().IsEnum)
            {
                caption = value.GetType().GetCustomAttributes(false).OfType<CaptionAttribute>().FirstOrDefault();
            }
            else
            {
                var field = value.GetType().GetField(value.ToString());
                if (field != null)
                    caption = field.GetCustomAttributes(false).OfType<CaptionAttribute>().FirstOrDefault();
            }
            if (caption != null) return caption.Caption;

            // handle DayOfWeek-Enum
            if (value is DayOfWeek dayOfWeek)
            {
                switch (dayOfWeek)
                {
                    case DayOfWeek.Sunday: return "Sonntag";
                    case DayOfWeek.Monday: return "Montag";
                    case DayOfWeek.Tuesday: return "Dienstag";
                    case DayOfWeek.Wednesday: return "Mittwoch";
                    case DayOfWeek.Thursday: return "Donnerstag";
                    case DayOfWeek.Friday: return "Freitag";
                    case DayOfWeek.Saturday: return "Samstag";
                    default: return dayOfWeek.ToString();
                }
            }

            // return
            return value.ToString();
        }

        // Get Caption of Month (15.12.2023, SME)
        public static string GetMonthCaption(int month)
        {
            switch (month)
            {
                case 1: return "Januar";
                case 2: return "Februar";
                case 3: return "März";
                case 4: return "April";
                case 5: return "Mai";
                case 6: return "Juni";
                case 7: return "Juli";
                case 8: return "August";
                case 9: return "September";
                case 10: return "Oktober";
                case 11: return "November";
                case 12: return "Dezember";
                default:
                    throw new ArgumentOutOfRangeException(nameof(month));
            }
        }

        // FROM Definition-String (18.12.2023, SME)
        public static RecurrencePattern FromDefinitionString(string definitionString)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(definitionString)) throw new ArgumentNullException(nameof(definitionString));

                // declarations
                RecurrenceTypeEnum? type = null;
                int? interval = null;
                List<DayOfWeek> dayOfWeeks = new List<DayOfWeek>();
                DayIntervalEnum? dayInterval = null;
                DayEnum? day = null;
                int? dayOfMonth = null;
                int? monthOfYear = null;
                DateTime? startOn = null;
                DateTime? endOn = null;
                int? endAfterTimes = null;

                // store value-parts
                var valueParts = definitionString.Split(';');

                // loop throu value-parts
                foreach (var valuePart in valueParts)
                {
                    try
                    {
                        // store name + value
                        var valueAndPart = valuePart.Split('=');
                        if (valueAndPart.Length != 2) throw new Exception($"Ungültiger Definitions-String: {definitionString}");
                        var name = valueAndPart.First().ToUpper();
                        var value = valueAndPart.Last().ToLower();

                        // handle name
                        switch (name)
                        {
                            case "T":
                                // Type
                                foreach (var typeEnum in CoreFC.GetEnumValues<RecurrenceTypeEnum>())
                                {
                                    if (typeEnum.ToString().Substring(0, 1).ToLower() == value)
                                    {
                                        type = typeEnum; break;
                                    }
                                }
                                if (!type.HasValue)
                                {
                                    throw new Exception($"Ungültiger Definitions-String, weil Typ nicht erkannt: {definitionString}");
                                }
                                break;

                            case "I":
                                // Interval
                                if (int.TryParse(value, out int valueInt))
                                {
                                    if (valueInt <= 0) throw new Exception($"Ungültiger Definitions-String, weil Interval nicht gpültig ist: {definitionString}");
                                    interval = valueInt;
                                }
                                else
                                {
                                    throw new Exception($"Ungültiger Definitions-String, weil Intervall nicht erkannt: {definitionString}");
                                }
                                break;

                            case "DAYS":
                                // Days
                                var days = value.ToUpper().Split('+');
                                foreach (var dayOfWeek in CoreFC.GetEnumValues<DayOfWeek>())
                                {
                                    if (days.Contains(dayOfWeek.ToString().ToUpper().Substring(0, 2)))
                                    {
                                        dayOfWeeks.Add(dayOfWeek);
                                    }
                                }
                                break;

                            case "DAYINTERVAL":
                                // Day-Interval
                                foreach (var enumValue in CoreFC.GetEnumValues<DayIntervalEnum>())
                                {
                                    if (enumValue.ToString().ToLower() == value)
                                    {
                                        dayInterval = enumValue; break;
                                    }
                                }
                                if (!dayInterval.HasValue)
                                {
                                    throw new Exception($"Ungültiger Definitions-String, weil Tages-Intervall nicht erkannt: {definitionString}");
                                }
                                break;

                            case "DAY":
                                // Day
                                foreach (var enumValue in CoreFC.GetEnumValues<DayEnum>())
                                {
                                    if (enumValue.ToString().ToLower() == value)
                                    {
                                        day = enumValue; break;
                                    }
                                }
                                if (!day.HasValue)
                                {
                                    throw new Exception($"Ungültiger Definitions-String, weil Tag nicht erkannt: {day}");
                                }
                                break;

                            case "DAYOFMONTH":
                                // DayOfMonth
                                if (int.TryParse(value, out int valueInt2))
                                {
                                    if (valueInt2 < 1 || valueInt2 > 31) throw new Exception($"Ungültiger Definitions-String, weil Tag-von-Monat ungültig ist: {definitionString}");
                                    dayOfMonth = valueInt2;
                                }
                                else
                                {
                                    throw new Exception($"Ungültiger Definitions-String, weil Tag-von-Monat nicht erkannt: {definitionString}");
                                }
                                break;

                            case "MONTHOFYEAR":
                                // MonthOfYear
                                if (int.TryParse(value, out int valueInt3))
                                {
                                    if (monthOfYear < 1 || monthOfYear > 12) throw new Exception($"Ungültiger Definitions-String, weil Monat-von-Jahr ungültig ist: {definitionString}");
                                    monthOfYear = valueInt3;
                                }
                                else
                                {
                                    throw new Exception($"Ungültiger Definitions-String, weil Monat-von-Jahr nicht erkannt: {definitionString}");
                                }
                                break;

                            case "STARTON":
                                // StartOn
                                if (DateTime.TryParse(value, out DateTime valueStartOn))
                                {
                                    startOn = valueStartOn;
                                }
                                else
                                {
                                    throw new Exception($"Ungültiger Definitions-String, weil Start-Datum nicht erkannt: {definitionString}");
                                }
                                break;

                            case "ENDON":
                                // EndOn
                                if (DateTime.TryParse(value, out DateTime valueEndOn))
                                {
                                    endOn = valueEndOn;
                                }
                                else
                                {
                                    throw new Exception($"Ungültiger Definitions-String, weil End-Datum nicht erkannt: {definitionString}");
                                }
                                break;

                            case "ENDAFTERTIMES":
                                // EndAfterTimes
                                if (int.TryParse(value, out int valueEndAfterTimes))
                                {
                                    endAfterTimes = valueEndAfterTimes;
                                }
                                else
                                {
                                    throw new Exception($"Ungültiger Definitions-String, weil Endet-Nach nicht erkannt: {definitionString}");
                                }
                                break;

                            default:
                                throw new NotImplementedException($"Unbehandeler Recurrence-Pattern-Definitions-Teil: {name} = {value}");
                        }

                    }
                    catch (Exception ex)
                    {
                        CoreFC.ThrowError(ex);
                    }
                }

                // check mandatory properties
                if (!type.HasValue) throw new Exception($"Ungültiger Definitions-String, weil Typ nicht erkannt: {definitionString}");
                if (!interval.HasValue) throw new Exception($"Ungültiger Definitions-String, weil Intervall nicht erkannt: {definitionString}");

                // check type-depending properties
                switch (type.Value)
                {
                    case RecurrenceTypeEnum.Daily:
                        break;

                    case RecurrenceTypeEnum.Weekly:
                        if (!dayOfWeeks.Any()) throw new Exception($"Ungültiger Definitions-String, weil keine Wochentage gesetzt sind: {definitionString}");
                        break;

                    case RecurrenceTypeEnum.Monthly:
                        if (dayOfMonth.HasValue)
                        {
                            // Okay
                            break;
                        }
                        else if (dayInterval.HasValue && day.HasValue)
                        {
                            // Okay
                            break;
                        }
                        else
                        {
                            throw new Exception($"Ungültiger Definitions-String, weil keine Monats-Option gesetzt ist: {definitionString}");
                        }

                    case RecurrenceTypeEnum.Yearly:
                        if (!monthOfYear.HasValue)
                        {
                            throw new Exception($"Ungültiger Definitions-String, weil Monat-von-Jahr nicht gesetzt ist: {definitionString}");
                        }
                        else if (dayOfMonth.HasValue)
                        {
                            // Okay
                            break;
                        }
                        else if (dayInterval.HasValue && day.HasValue)
                        {
                            // Okay
                            break;
                        }
                        else
                        {
                            throw new Exception($"Ungültiger Definitions-String, weil keine Jahres-Option gesetzt ist: {definitionString}");
                        }

                    default:
                        throw new NotImplementedException($"Ungültiger Definitions-String, weil kein gültiger Typ erkannt wurde: {definitionString}");
                }

                // return
                var pattern = new RecurrencePattern(type.Value, interval.Value);
                pattern.StartOn = startOn;
                pattern.EndOn = endOn;
                pattern.EndAfterTimes = endAfterTimes;
                switch (type.Value)
                {
                    case RecurrenceTypeEnum.Daily: 
                        return pattern;

                    case RecurrenceTypeEnum.Weekly:
                        pattern.WeeklyDays = dayOfWeeks.ToArray();
                        return pattern;

                    case RecurrenceTypeEnum.Monthly:
                        if (dayOfMonth.HasValue)
                        {
                            pattern.DayOfMonth = dayOfMonth.Value;
                            return pattern;
                        }
                        else if (dayInterval.HasValue && day.HasValue)
                        {
                            pattern.DayInterval = dayInterval.Value;
                            pattern.Day = day.Value;
                            return pattern;
                        }
                        else
                        {
                            throw new Exception($"Ungültiger Definitions-String, weil keine Jahres-Option gesetzt ist: {definitionString}");
                        }
                        
                    case RecurrenceTypeEnum.Yearly:
                        if (dayOfMonth.HasValue)
                        {
                            pattern.DayOfMonth = dayOfMonth.Value;
                            pattern.MonthOfYear = monthOfYear.Value;
                            return pattern;
                        }
                        else if (dayInterval.HasValue && day.HasValue)
                        {
                            pattern.DayInterval = dayInterval.Value;
                            pattern.Day = day.Value;
                            pattern.MonthOfYear = monthOfYear.Value;
                            return pattern;
                        }
                        else
                        {
                            throw new Exception($"Ungültiger Definitions-String, weil keine Jahres-Option gesetzt ist: {definitionString}");
                        }

                    default:
                        throw new NotImplementedException($"Ungültiger Definitions-String, weil kein gültiger Typ erkannt wurde: {definitionString}");
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region General

        // New Instance (14.12.2023, SME)
        public RecurrencePattern(RecurrenceTypeEnum type, int interval)
        {
            // error-handling
            if (!Enum.IsDefined(typeof(RecurrenceTypeEnum), type)) throw new ArgumentOutOfRangeException(nameof(type));
            if (interval <= 0) throw new ArgumentOutOfRangeException(nameof(interval));

            // set properties
            Type = type;
            Interval = interval;
        }

        // Constants
        public static readonly DayOfWeek[] WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

        #endregion

        #region Enumerations

        // Recurrence-Types
        public enum RecurrenceTypeEnum
        {
            [Caption("Täglich")]
            Daily = 1,
            [Caption("Wöchentlich")]
            Weekly = 7,
            [Caption("Monatlich")]
            Monthly = 30,
            [Caption("Jährlich")]
            Yearly = 365
        }

        // Day-Intervals
        public enum DayIntervalEnum
        {
            [Caption("Erster")]
            First = 1,
            [Caption("Zweiter")]
            Seconds = 2,
            [Caption("Dritter")]
            Third = 3,
            [Caption("Vierter")]
            Fourth = 4,
            [Caption("Letzter")]
            Last = 5
        }

        // Days
        public enum DayEnum
        {
            [Caption("Montag")]
            Monday,
            [Caption("Dienstag")]
            Tuesday,
            [Caption("Mittwoch")]
            Wednesday,
            [Caption("Donnerstag")]
            Thursday,
            [Caption("Freitag")]
            Friday,
            [Caption("Samstag")]
            Saturday,
            [Caption("Sonntag")]
            Sunday,
            [Caption("Tag")]
            Day,
            [Caption("Arbeitstag")]
            WorkDay
        }

        #endregion

        #region Properties

        public RecurrenceTypeEnum Type { get; }
        public int Interval { get; }

        // Additional properties for specific recurrence types
        public DayOfWeek[] WeeklyDays { get; set; }
        public int? DayOfMonth { get; set; }
        public int? MonthOfYear { get; set; }
        public DayIntervalEnum? DayInterval { get; set; }
        public DayEnum? Day { get; set; }

        // Additional properties for Duration
        public DateTime? StartOn { get; set; }
        public DateTime? EndOn{ get; set; }
        public int? EndAfterTimes { get; set; }

        #endregion

        #region Methods

        // To Definition-String (18.12.2023, SME)
        public string ToDefinitionString()
        {
            try
            {
                // create string-builder
                var sb = new StringBuilder();

                // add type (first letter in upper case)
                sb.Append($"T={Type.ToString().Substring(0, 1).ToUpper()}");

                // add interval
                sb.Append($";I={Interval}");

                // add week-days
                var weekdays = GetOrderedWeekDays();
                if (weekdays.Any())
                {
                    sb.Append($";Days={string.Join("+",weekdays.Select(x => x.ToString().Substring(0, 2).ToUpper()))}");
                }

                // add day-interval
                if (DayInterval.HasValue)
                {
                    sb.Append($";DayInterval={DayInterval.Value}");
                }

                // add day-enum
                if (Day.HasValue)
                {
                    sb.Append($";Day={Day.Value}");
                }

                // add day of month
                if (DayOfMonth.HasValue)
                {
                    sb.Append($";DayOfMonth={DayOfMonth.Value}");
                }

                // add month of year
                if (MonthOfYear.HasValue)
                {
                    sb.Append($";MonthOfYear={MonthOfYear.Value}");
                }

                // add duration (04.01.2024, SME)
                if (StartOn.HasValue)
                {
                    sb.Append($";StartOn={StartOn.Value.ToString("yyyy-MM-dd")}");
                }
                if (EndOn.HasValue)
                {
                    sb.Append($";EndOn={EndOn.Value.ToString("yyyy-MM-dd")}");
                }
                else if (EndAfterTimes.HasValue)
                {
                    sb.Append($";EndAfterTimes={EndAfterTimes.Value}");
                }

                // return
                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Day-Interval-Caption for Caption (15.12.2023, SME)
        private string GetDayIntervalCaption(DayIntervalEnum dayInterval)
        {
            string text = GetCaption(dayInterval).ToLower();
            if (text.EndsWith("r"))
            {
                text = text.Substring(0, text.Length - 1) + "n";
            }
            return text;
        }

        // Get Caption (15.12.2023, SME)
        public string GetCaption()
        {
            switch (Type)
            {
                case RecurrenceTypeEnum.Daily:
                    if (Interval == 1) return "Täglich";
                    else return $"Jeden {Interval}. Tag";

                case RecurrenceTypeEnum.Weekly:
                    // check if all working-days
                    bool allWorkingDays = false;
                    if (WeeklyDays != null && WeeklyDays.Length == WorkingDays.Length)
                    {
                        allWorkingDays = true;
                        foreach (var workingDay in WorkingDays)
                        {
                            if (!WeeklyDays.Contains(workingDay))
                            {
                                allWorkingDays = false;
                                break;
                            }
                        }
                    }

                    // get ordered week-days
                    var weekdays = GetOrderedWeekDays();

                    if (Interval == 1)
                    {
                        if (allWorkingDays) return "Jeden Arbeitstag";
                        if (!weekdays.Any()) return "Wöchentlich am ?";
                        if (weekdays.Length == 7) return "Jedem Tag";
                        return "Jeden " + string.Join(" + ", weekdays.Select(x => GetCaption(x)));
                    }
                    else
                    {
                        if (allWorkingDays) return $"Jede {Interval}. Woche an jeden Arbeitstag";
                        if (!weekdays.Any()) return $"Jede {Interval}. Woche am ?";
                        if (weekdays.Length == 7) return $"Jede {Interval}. Woche an jedem Tag";
                        return $"Jede {Interval}. Woche am " + string.Join(" + ", weekdays.Select(x => GetCaption(x)));
                    }

                case RecurrenceTypeEnum.Monthly:
                    if (Interval == 1)
                    {
                        if (DayOfMonth.HasValue)
                        {
                            return $"Monatlich am {DayOfMonth}. Tag";
                        }
                        else if (DayInterval.HasValue && Day.HasValue)
                        {
                            return $"Monatlich am {GetDayIntervalCaption(DayInterval.Value)} {GetCaption(Day.Value)}";
                        }
                        else
                        {
                            return "Monatlich am ?";
                        }
                    }
                    else
                    {
                        if (DayOfMonth.HasValue)
                        {
                            return $"Jeden {Interval}. Monat am {DayOfMonth}. Tag";
                        }
                        else if (DayInterval.HasValue && Day.HasValue)
                        {
                            return $"Jeden {Interval}. Monat am {GetDayIntervalCaption(DayInterval.Value)} {GetCaption(Day.Value)}";
                        }
                        else
                        {
                            return $"Jeden {Interval}. Monat am ?";
                        }
                    }

                case RecurrenceTypeEnum.Yearly:
                    if (Interval == 1)
                    {
                        if (DayOfMonth.HasValue && MonthOfYear.HasValue)
                        {
                            return $"Jährlich am {DayOfMonth}. {GetMonthCaption(MonthOfYear.Value)}";
                        }
                        else if (DayInterval.HasValue && Day.HasValue && MonthOfYear.HasValue)
                        {
                            return $"Jährlich am {GetDayIntervalCaption(DayInterval.Value)} {GetCaption(Day.Value)} im {GetMonthCaption(MonthOfYear.Value)}";
                        }
                        else
                        {
                            return "Jährlich am ?";
                        }
                    }
                    else
                    {
                        if (DayOfMonth.HasValue && MonthOfYear.HasValue)
                        {
                            return $"Jedes {Interval}. Jahr am {DayOfMonth}. {GetMonthCaption(MonthOfYear.Value)}";
                        }
                        else if (DayInterval.HasValue && Day.HasValue && MonthOfYear.HasValue)
                        {
                            return $"Jedes {Interval}. Jahr am {GetDayIntervalCaption(DayInterval.Value)} {GetCaption(Day.Value)} im {GetMonthCaption(MonthOfYear.Value)}";
                        }
                        else
                        {
                            return $"Jedes {Interval}. Jahr am ?";
                        }
                    }

                default:
                    throw new NotImplementedException($"Unbehandelter Typ: {Type}");
            }
        }

        // Get ordered Week-Days (15.12.2023, SME)
        private DayOfWeek[] GetOrderedWeekDays()
        {
            var list = new List<DayOfWeek>();
            if (WeeklyDays != null && WeeklyDays.Any())
            {
                if (WeeklyDays.Contains(DayOfWeek.Monday)) list.Add(DayOfWeek.Monday);
                if (WeeklyDays.Contains(DayOfWeek.Tuesday)) list.Add(DayOfWeek.Tuesday);
                if (WeeklyDays.Contains(DayOfWeek.Wednesday)) list.Add(DayOfWeek.Wednesday);
                if (WeeklyDays.Contains(DayOfWeek.Thursday)) list.Add(DayOfWeek.Thursday);
                if (WeeklyDays.Contains(DayOfWeek.Friday)) list.Add(DayOfWeek.Friday);
                if (WeeklyDays.Contains(DayOfWeek.Saturday)) list.Add(DayOfWeek.Saturday);
                if (WeeklyDays.Contains(DayOfWeek.Sunday)) list.Add(DayOfWeek.Sunday);
            }
            return list.ToArray();
        }

        // Check if a date matches the recurrence pattern
        public bool IsDateMatching(DateTime dateToCheck, DateTime startDate)
        {
            switch (Type)
            {
                case RecurrenceTypeEnum.Daily:
                    return CheckDaily(dateToCheck, startDate);
                case RecurrenceTypeEnum.Weekly:
                    return CheckWeekly(dateToCheck, startDate);
                case RecurrenceTypeEnum.Monthly:
                    return CheckMonthly(dateToCheck, startDate);
                case RecurrenceTypeEnum.Yearly:
                    return CheckYearly(dateToCheck, startDate);
                default:
                    return false;
            }
        }

        // Get next matching Date (04.01.2024, SME)
        public DateTime GetNextMatchingDate(DateTime startDate)
        {
            DateTime dateToCheck = startDate.Date;
            while (true)
            {
                if (IsDateMatching(dateToCheck, startDate)) return dateToCheck;
                dateToCheck = dateToCheck.AddDays(1);
            }
        }

        // Check Daily
        private bool CheckDaily(DateTime dateToCheck, DateTime startDate)
        {
            TimeSpan difference = dateToCheck - startDate;
            return difference.Days % Interval == 0;
        }

        // Check Weekly
        private bool CheckWeekly(DateTime dateToCheck, DateTime startDate)
        {
            try
            {
                if (WeeklyDays == null || WeeklyDays.Length == 0)
                    return false;

                if (Interval != 1)
                {
                    TimeSpan difference = dateToCheck - startDate;
                    int weeks = difference.Days / 7;
                    if (weeks % Interval != 0) 
                        return false;
                    //if (difference.Days % (7 * Interval) != 0)
                    //    return false;
                }

                return Array.Exists(WeeklyDays, day => day == dateToCheck.DayOfWeek);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Check Monthly
        private bool CheckMonthly(DateTime dateToCheck, DateTime startDate)
        {
            try
            {
                // check interval
                if (Interval != 1)
                {
                    int months = GetMonthDiff(startDate, dateToCheck);
                    if (months < 0) months *= -1;
                    if (months % Interval != 0)
                        return false;
                }

                // check day of month
                if (DayOfMonth.HasValue)
                {
                    //return dateToCheck.Day == DayOfMonth && CheckDaily(dateToCheck, startDate);
                    if (dateToCheck.Day == DayOfMonth)
                        return true;
                    else
                        return false;
                }

                // check day-interval + day
                if (DayInterval.HasValue && Day.HasValue)
                {
                    // store day of week
                    var dayOfWeek = dateToCheck.DayOfWeek;

                    // check day
                    switch (Day.Value)
                    {
                        case DayEnum.Monday:
                            if (dayOfWeek != DayOfWeek.Monday) return false;
                            break;
                        case DayEnum.Tuesday:
                            if (dayOfWeek != DayOfWeek.Tuesday) return false;
                            break;
                        case DayEnum.Wednesday:
                            if (dayOfWeek != DayOfWeek.Wednesday) return false;
                            break;
                        case DayEnum.Thursday:
                            if (dayOfWeek != DayOfWeek.Thursday) return false;
                            break;
                        case DayEnum.Friday:
                            if (dayOfWeek != DayOfWeek.Friday) return false;
                            break;
                        case DayEnum.Saturday:
                            if (dayOfWeek != DayOfWeek.Saturday) return false;
                            break;
                        case DayEnum.Sunday:
                            if (dayOfWeek != DayOfWeek.Sunday) return false;
                            break;
                        case DayEnum.Day:
                            break;
                        case DayEnum.WorkDay:
                            if (dayOfWeek == DayOfWeek.Sunday || dayOfWeek == DayOfWeek.Saturday) return false;
                            break;
                        default:
                            return false;
                    }

                    // check day of month
                    var dayOfMonth = GetDayOfMonth(dateToCheck.Year, dateToCheck.Month, DayInterval.Value, Day.Value);
                    if (dayOfMonth.Date == dateToCheck.Date) 
                        return true;
                }

                // return
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Check Yearly
        private bool CheckYearly(DateTime dateToCheck, DateTime startDate)
        {
            try
            {
                // check interval
                if (Interval != 1)
                {
                    int years = startDate.Year - dateToCheck.Year;
                    if (years < 0) years *= -1;
                    if (years % Interval != 0)
                        return false;
                }

                // check month of year
                if (MonthOfYear.HasValue)
                {
                    if (dateToCheck.Month != MonthOfYear.Value) return false;
                }

                // check day of month + month of year
                if (DayOfMonth.HasValue && MonthOfYear.HasValue)
                {
                    return dateToCheck.Day == DayOfMonth.Value && dateToCheck.Month == MonthOfYear.Value;
                }

                // check day-interval + day
                if (DayInterval.HasValue && Day.HasValue)
                {
                    // store day of week
                    var dayOfWeek = dateToCheck.DayOfWeek;

                    // check day
                    switch (Day.Value)
                    {
                        case DayEnum.Monday:
                            if (dayOfWeek != DayOfWeek.Monday) return false;
                            break;
                        case DayEnum.Tuesday:
                            if (dayOfWeek != DayOfWeek.Tuesday) return false;
                            break;
                        case DayEnum.Wednesday:
                            if (dayOfWeek != DayOfWeek.Wednesday) return false;
                            break;
                        case DayEnum.Thursday:
                            if (dayOfWeek != DayOfWeek.Thursday) return false;
                            break;
                        case DayEnum.Friday:
                            if (dayOfWeek != DayOfWeek.Friday) return false;
                            break;
                        case DayEnum.Saturday:
                            if (dayOfWeek != DayOfWeek.Saturday) return false;
                            break;
                        case DayEnum.Sunday:
                            if (dayOfWeek != DayOfWeek.Sunday) return false;
                            break;
                        case DayEnum.Day:
                            break;
                        case DayEnum.WorkDay:
                            if (dayOfWeek == DayOfWeek.Sunday || dayOfWeek == DayOfWeek.Saturday) return false;
                            break;
                        default:
                            return false;
                    }

                    // check day of month
                    var dayOfMonth = GetDayOfMonth(dateToCheck.Year, dateToCheck.Month, DayInterval.Value, Day.Value);
                    if (dayOfMonth.Date == dateToCheck.Date)
                        return true;
                }

                // return
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
