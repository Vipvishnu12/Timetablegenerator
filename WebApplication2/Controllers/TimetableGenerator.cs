using System;
using System.Collections.Generic;
using System.Linq;

namespace TimetableGA
{
    public class TimetableEngine
    {
        private static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri" };
        private const int HoursPerDay = 7;

        public class Subject
        {
            public string SubjectCode { get; set; }
            public string SubjectName { get; set; }
            public string SubjectType { get; set; } // "Theory", "Lab", "Embedded"
            public int Credit { get; set; }
            public string StaffAssigned { get; set; }
        }

        public class TimetableSlot
        {
            public string Day { get; set; }
            public Dictionary<int, string> HourlySlots { get; set; } = new();
        }

        public class Conflict
        {
            public Subject Subject { get; set; }
            public string Reason { get; set; }
        }

        public (List<TimetableSlot> timetable, List<Conflict> conflicts) Generate(
            List<Subject> subjects,
            Dictionary<string, Dictionary<string, HashSet<int>>> globalStaffAvailability)
        {
            int totalDays = Days.Length;
            var timetable = new TimetableSlot[totalDays];
            var conflicts = new List<Conflict>();

            // Initialize timetable structure
            for (int i = 0; i < totalDays; i++)
            {
                timetable[i] = new TimetableSlot { Day = Days[i] };
                foreach (int h in Enumerable.Range(1, HoursPerDay))
                {
                    timetable[i].HourlySlots[h] = "---";
                }
            }

            // ✅ Ensure all staff keys are initialized in globalStaffAvailability
            foreach (var subject in subjects)
            {
                string staff = subject.StaffAssigned;
                if (!globalStaffAvailability.ContainsKey(staff))
                {
                    globalStaffAvailability[staff] = Days.ToDictionary(day => day, day => new HashSet<int>());
                }
            }

            var rand = new Random();

            // ➤ Group subjects
            var labs = subjects.Where(s => s.SubjectType.ToLower() == "lab").ToList();
            var embeddeds = subjects.Where(s => s.SubjectType.ToLower() == "embedded").ToList();
            var theories = subjects.Except(labs).Except(embeddeds).ToList();

            // ➤ 1. Assign Lab subjects (4 continuous hours)
            foreach (var lab in labs)
            {
                bool placed = false;

                foreach (var dayIdx in Enumerable.Range(0, totalDays).OrderBy(_ => rand.Next()))
                {
                    for (int startHour = 1; startHour <= HoursPerDay - 3; startHour++) // Need 4 slots
                    {
                        bool allFree = Enumerable.Range(startHour, 4).All(h =>
                            timetable[dayIdx].HourlySlots[h] == "---" &&
                            !globalStaffAvailability[lab.StaffAssigned][Days[dayIdx]].Contains(h));

                        if (allFree)
                        {
                            foreach (int h in Enumerable.Range(startHour, 4))
                            {
                                timetable[dayIdx].HourlySlots[h] = $"{lab.SubjectCode} ({lab.StaffAssigned})";
                                globalStaffAvailability[lab.StaffAssigned][Days[dayIdx]].Add(h);
                            }
                            placed = true;
                            break;
                        }
                    }
                    if (placed) break;
                }

                if (!placed)
                {
                    conflicts.Add(new Conflict
                    {
                        Subject = lab,
                        Reason = $"❌ Could not allocate 4 continuous hours for lab: {lab.SubjectCode}"
                    });
                }
            }

            // ➤ 2. Assign Embedded subjects
            foreach (var embedded in embeddeds)
            {
                int theoryHours = 0;
                int labBlock = 0;

                foreach (var dayIdx in Enumerable.Range(0, totalDays).OrderBy(_ => rand.Next()))
                {
                    // Try 2 separate theory slots
                    for (int h = 1; h <= HoursPerDay; h++)
                    {
                        if (theoryHours < 2 &&
                            timetable[dayIdx].HourlySlots[h] == "---" &&
                            !globalStaffAvailability[embedded.StaffAssigned][Days[dayIdx]].Contains(h))
                        {
                            timetable[dayIdx].HourlySlots[h] = $"{embedded.SubjectCode} (Theory)";
                            globalStaffAvailability[embedded.StaffAssigned][Days[dayIdx]].Add(h);
                            theoryHours++;
                        }
                    }

                    // Try to find 2 continuous lab hours
                    for (int startHour = 1; startHour <= HoursPerDay - 1; startHour++)
                    {
                        bool free = Enumerable.Range(startHour, 2).All(h =>
                            timetable[dayIdx].HourlySlots[h] == "---" &&
                            !globalStaffAvailability[embedded.StaffAssigned][Days[dayIdx]].Contains(h));

                        if (labBlock == 0 && free)
                        {
                            foreach (int h in Enumerable.Range(startHour, 2))
                            {
                                timetable[dayIdx].HourlySlots[h] = $"{embedded.SubjectCode} (Lab)";
                                globalStaffAvailability[embedded.StaffAssigned][Days[dayIdx]].Add(h);
                            }
                            labBlock = 1;
                        }
                    }

                    if (theoryHours == 2 && labBlock == 1) break;
                }

                if (theoryHours < 2 || labBlock == 0)
                {
                    conflicts.Add(new Conflict
                    {
                        Subject = embedded,
                        Reason = $"❌ Could not place embedded subject fully (T={theoryHours}/2, L={labBlock}/1): {embedded.SubjectCode}"
                    });
                }
            }

            // ➤ 3. Assign Theory/normal subjects
            var remaining = new List<Subject>();
            foreach (var t in theories)
            {
                for (int i = 0; i < t.Credit; i++) remaining.Add(t);
            }

            foreach (var session in remaining.OrderBy(_ => rand.Next()))
            {
                bool placed = false;

                foreach (var dayIdx in Enumerable.Range(0, totalDays).OrderBy(_ => rand.Next()))
                {
                    for (int hour = 1; hour <= HoursPerDay; hour++)
                    {
                        string day = Days[dayIdx];
                        if (timetable[dayIdx].HourlySlots[hour] == "---" &&
                            !globalStaffAvailability[session.StaffAssigned][day].Contains(hour))
                        {
                            timetable[dayIdx].HourlySlots[hour] = $"{session.SubjectCode} ({session.StaffAssigned})";
                            globalStaffAvailability[session.StaffAssigned][day].Add(hour);
                            placed = true;
                            break;
                        }
                    }
                    if (placed) break;
                }

                if (!placed)
                {
                    conflicts.Add(new Conflict
                    {
                        Subject = session,
                        Reason = $"❌ Could not place theory subject: {session.SubjectCode}"
                    });
                }
            }

            return (timetable.ToList(), conflicts);
        }
    }
}
