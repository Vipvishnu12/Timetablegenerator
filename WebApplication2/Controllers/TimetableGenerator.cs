//// TimetableEngine.cs - Modified to match improved logic
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using static YourNamespace.Controllers.CrossDepartmentAssignmentsController;

//namespace TimetableGA
//{
//    public class TimetableEngine
//    {
//        private static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri" };
//        private const int HoursPerDay = 7;
//        private readonly Random random = new();

//        public class Subject
//        {
//            public string SubjectCode { get; set; }
//            public string SubjectName { get; set; }
//            public string SubjectType { get; set; }
//            public int Credit { get; set; }
//            public string StaffAssigned { get; set; }
//            public string LabId { get; set; }
//        }

//        public class TimetableSlot
//        {
//            public string Day { get; set; }
//            public Dictionary<int, string> HourlySlots { get; set; } = new();
//        }

//        public class Gene
//        {
//            public string SubjectCode;
//            public string StaffAssigned;
//            public string LabId;
//            public string Day;
//            public int StartHour;
//            public int Duration;
//            public bool IsLabBlock;
//        }

//        public class Chromosome
//        {
//            public List<Gene> Genes = new List<Gene>();
//            public int FitnessScore;
//        }

//        private List<Subject> Subjects;
//        private Dictionary<string, Dictionary<string, HashSet<int>>> StaffAvailability;
//        private Dictionary<string, Dictionary<string, HashSet<int>>> LabAvailability;

//        public int PopulationSize = 100;
//        public int MaxGenerations = 300;
//        public double MutationRate = 0.1;

//        public void Initialize(List<Subject> subjects,
//                               Dictionary<string, Dictionary<string, HashSet<int>>> staffAvailability,
//                               Dictionary<string, Dictionary<string, HashSet<int>>> labAvailability)
//        {
//            Subjects = subjects;
//            StaffAvailability = staffAvailability;
//            LabAvailability = labAvailability;
//        }

//        private Chromosome CreateRandomChromosome()
//        {
//            var chromosome = new Chromosome();

//            foreach (var subject in Subjects)
//            {
//                string type = subject.SubjectType?.ToLower() ?? "theory";

//                if (type == "lab")
//                {
//                    string day = Days[random.Next(Days.Length)];
//                    int startHour = random.Next(1, HoursPerDay - 4 + 2);
//                    chromosome.Genes.Add(new Gene
//                    {
//                        SubjectCode = subject.SubjectCode,
//                        StaffAssigned = subject.StaffAssigned,
//                        LabId = subject.LabId,
//                        Day = day,
//                        StartHour = startHour,
//                        Duration = 4,
//                        IsLabBlock = true
//                    });
//                }
//                else if (type == "embedded")
//                {
//                    for (int i = 0; i < 2; i++)
//                    {
//                        chromosome.Genes.Add(new Gene
//                        {
//                            SubjectCode = subject.SubjectCode,
//                            StaffAssigned = subject.StaffAssigned,
//                            LabId = null,
//                            Day = Days[random.Next(Days.Length)],
//                            StartHour = random.Next(1, HoursPerDay + 1),
//                            Duration = 1,
//                            IsLabBlock = false
//                        });
//                    }

//                    string labDay = Days[random.Next(Days.Length)];
//                    int labStartHour = random.Next(1, HoursPerDay - 2 + 2);
//                    chromosome.Genes.Add(new Gene
//                    {
//                        SubjectCode = subject.SubjectCode,
//                        StaffAssigned = subject.StaffAssigned,
//                        LabId = subject.LabId,
//                        Day = labDay,
//                        StartHour = labStartHour,
//                        Duration = 2,
//                        IsLabBlock = true
//                    });
//                }
//                else
//                {
//                    for (int i = 0; i < subject.Credit; i++)
//                    {
//                        chromosome.Genes.Add(new Gene
//                        {
//                            SubjectCode = subject.SubjectCode,
//                            StaffAssigned = subject.StaffAssigned,
//                            LabId = null,
//                            Day = Days[random.Next(Days.Length)],
//                            StartHour = random.Next(1, HoursPerDay + 1),
//                            Duration = 1,
//                            IsLabBlock = false
//                        });
//                    }
//                }
//            }

//            return chromosome;
//        }

//        private int CalculateFitness(Chromosome chromosome)
//        {
//            int penalty = 0;

//            // Fix for CS8754: Specify the target type explicitly for 'new()'
//            var staffSchedule = new Dictionary<string, Dictionary<string, HashSet<int>>>();
//            // Fix for CS8754: Specify the target type explicitly for 'new()'  
//            var labSchedule = new Dictionary<string, Dictionary<string, HashSet<int>>>();

//            foreach (var gene in chromosome.Genes)
//            {
//                if (!staffSchedule.ContainsKey(gene.StaffAssigned))
//                    staffSchedule[gene.StaffAssigned] = Days.ToDictionary(d => d, d => new HashSet<int>());

//                if (!string.IsNullOrWhiteSpace(gene.LabId) && !labSchedule.ContainsKey(gene.LabId))
//                    labSchedule[gene.LabId] = Days.ToDictionary(d => d, d => new HashSet<int>());

//                for (int hour = gene.StartHour; hour < gene.StartHour + gene.Duration; hour++)
//                {
//                    if (hour > HoursPerDay)
//                    {
//                        penalty += 20;
//                        continue;
//                    }

//                    if (StaffAvailability.TryGetValue(gene.StaffAssigned, out var staffDay))
//                        if (staffDay.TryGetValue(gene.Day, out var hours) && hours.Contains(hour))
//                            penalty += 10;

//                    if (!staffSchedule[gene.StaffAssigned][gene.Day].Add(hour))
//                        penalty += 10;

//                    if (!string.IsNullOrWhiteSpace(gene.LabId))
//                    {
//                        if (LabAvailability.TryGetValue(gene.LabId, out var labDay))
//                            if (labDay.TryGetValue(gene.Day, out var labHours) && labHours.Contains(hour))
//                                penalty += 10;

//                        if (!labSchedule[gene.LabId][gene.Day].Add(hour))
//                            penalty += 10;
//                    }
//                }
//            }

//            return -penalty;
//        }

//        private Chromosome TournamentSelection(List<Chromosome> population)
//        {
//            var candidates = new List<Chromosome>();
//            for (int i = 0; i < 5; i++)
//                candidates.Add(population[random.Next(population.Count)]);
//            return candidates.OrderByDescending(c => c.FitnessScore).First();
//        }

//        private (Chromosome, Chromosome) Crossover(Chromosome p1, Chromosome p2)
//        {
//            int cut = random.Next(1, p1.Genes.Count - 1);
//            var c1 = new Chromosome { Genes = p1.Genes.Take(cut).Concat(p2.Genes.Skip(cut)).ToList() };
//            var c2 = new Chromosome { Genes = p2.Genes.Take(cut).Concat(p1.Genes.Skip(cut)).ToList() };
//            return (c1, c2);
//        }

//        private void Mutate(Chromosome chromosome)
//        {
//            foreach (var gene in chromosome.Genes)
//            {
//                if (random.NextDouble() < MutationRate)
//                {
//                    gene.Day = Days[random.Next(Days.Length)];
//                    gene.StartHour = random.Next(1, HoursPerDay - gene.Duration + 2);
//                }
//            }
//        }

//        public (List<TimetableSlot> timetable, List<Conflict> conflicts, Chromosome bestChromosome) GenerateGA()
//        {
//            var population = Enumerable.Range(0, PopulationSize)
//                .Select(_ => CreateRandomChromosome())
//                .ToList();

//            population.ForEach(c => c.FitnessScore = CalculateFitness(c));

//            for (int gen = 0; gen < MaxGenerations; gen++)
//            {
//                var nextGen = new List<Chromosome>();
//                while (nextGen.Count < PopulationSize)
//                {
//                    var p1 = TournamentSelection(population);
//                    var p2 = TournamentSelection(population);

//                    var (c1, c2) = Crossover(p1, p2);
//                    Mutate(c1);
//                    Mutate(c2);
//                    c1.FitnessScore = CalculateFitness(c1);
//                    c2.FitnessScore = CalculateFitness(c2);
//                    nextGen.Add(c1);
//                    nextGen.Add(c2);
//                }

//                population = nextGen.OrderByDescending(c => c.FitnessScore).Take(PopulationSize).ToList();
//                if (population[0].FitnessScore == 0) break;
//            }

//            var best = population.OrderByDescending(c => c.FitnessScore).First();
//            return (ConvertToTimetable(best.Genes), ExtractConflicts(best), best);
//        }

//        private List<TimetableSlot> ConvertToTimetable(List<Gene> genes)
//        {
//            var timetable = Days.Select(day => new TimetableSlot
//            {
//                Day = day,
//                HourlySlots = Enumerable.Range(1, HoursPerDay).ToDictionary(h => h, _ => "---")
//            }).ToList();

//            foreach (var gene in genes)
//            {
//                var slot = timetable.First(t => t.Day == gene.Day);
//                for (int h = gene.StartHour; h < gene.StartHour + gene.Duration && h <= HoursPerDay; h++)
//                    slot.HourlySlots[h] = $"{gene.SubjectCode} ({gene.StaffAssigned})";
//            }

//            return timetable;
//        }

//        private List<Conflict> ExtractConflicts(Chromosome chromosome)
//        {
//            var conflicts = new List<Conflict>();
//            if (chromosome.FitnessScore < 0)
//                conflicts.Add(new Conflict { Reason = "Schedule contains conflicts (staff or lab double bookings or unavailable)." });
//            return conflicts;
//        }

//        public class Conflict
//        {
//            public Subject Subject { get; set; } = null;
//            public string Reason { get; set; }
//        }
//    }
//}
 



using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using static YourNamespace.Controllers.CrossDepartmentAssignmentsController;

namespace TimetableGA
{
    public class TimetableEngine
    {
        private static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri" };
        private const int HoursPerDay = 7;
        private readonly Random random = new();

        public class Subject
        {
            public string SubjectCode { get; set; }
            public string SubjectName { get; set; }
            public string SubjectType { get; set; } // "Theory", "Lab", "Embedded"
            public int Credit { get; set; }
            public string StaffAssigned { get; set; }
            public string LabId { get; set; }
        }

        public class TimetableSlot
        {
            public string Day { get; set; }
            public Dictionary<int, string> HourlySlots { get; set; } = new();
        }

        public class Gene
        {
            public string SubjectCode;
            public string StaffAssigned;
            public string LabId;
            public string Day;
            public int StartHour;    // Continuous block start hour
            public int Duration;     // Number of continuous hours
            public bool IsLabBlock; // True if this gene represents a lab block (4 hours for lab, 2 hours lab part of embedded)
        }

        public class Chromosome
        {
            public List<Gene> Genes = new List<Gene>();
            public int FitnessScore;
        }

        private List<Subject> Subjects;
        private Dictionary<string, Dictionary<string, HashSet<int>>> StaffAvailability;
        private Dictionary<string, Dictionary<string, HashSet<int>>> LabAvailability;

        public int PopulationSize = 100;
        public int MaxGenerations = 300;
        public double MutationRate = 0.1;

        public void Initialize(List<Subject> subjects,
                               Dictionary<string, Dictionary<string, HashSet<int>>> staffAvailability,
                               Dictionary<string, Dictionary<string, HashSet<int>>> labAvailability)
        {
            Subjects = subjects;
            StaffAvailability = staffAvailability;
            LabAvailability = labAvailability;
        }

        private Chromosome CreateRandomChromosome()
        {
            var chromosome = new Chromosome();

            foreach (var subject in Subjects)
            {
                var type = subject.SubjectType?.ToLower() ?? "theory";

                if (type == "lab")
                {
                    string day = Days[random.Next(Days.Length)];
                    int startHour = random.Next(1, HoursPerDay - 4 + 2); // 4 continuous hours
                    chromosome.Genes.Add(new Gene
                    {
                        SubjectCode = subject.SubjectCode,
                        StaffAssigned = subject.StaffAssigned,
                        LabId = subject.LabId,
                        Day = day,
                        StartHour = startHour,
                        Duration = 4,
                        IsLabBlock = true
                    });
                }
                else if (type == "embedded")
                {
                    // 2 theory hours (non-lab), 1 hour each, can be separate and non-continuous
                    for (int i = 0; i < 2; i++)
                    {
                        chromosome.Genes.Add(new Gene
                        {
                            SubjectCode = subject.SubjectCode,
                            StaffAssigned = subject.StaffAssigned,
                            LabId = null,
                            Day = Days[random.Next(Days.Length)],
                            StartHour = random.Next(1, HoursPerDay + 1),
                            Duration = 1,
                            IsLabBlock = false
                        });
                    }

                    // 2 continuous lab hours (lab part)
                    {
                        string labDay = Days[random.Next(Days.Length)];
                        int labStartHour = random.Next(1, HoursPerDay - 2 + 2);
                        chromosome.Genes.Add(new Gene
                        {
                            SubjectCode = subject.SubjectCode,
                            StaffAssigned = subject.StaffAssigned,
                            LabId = subject.LabId,
                            Day = labDay,
                            StartHour = labStartHour,
                            Duration = 2,
                            IsLabBlock = true
                        });
                    }
                }
                else
                {
                    // Theory subjects: one gene per credit hour, duration=1
                    for (int i = 0; i < subject.Credit; i++)
                    {
                        chromosome.Genes.Add(new Gene
                        {
                            SubjectCode = subject.SubjectCode,
                            StaffAssigned = subject.StaffAssigned,
                            LabId = null,
                            Day = Days[random.Next(Days.Length)],
                            StartHour = random.Next(1, HoursPerDay + 1),
                            Duration = 1,
                            IsLabBlock = false
                        });
                    }
                }
            }

            return chromosome;
        }

        // Fitness and other GA methods remain similar but check whole continuous blocks

        private int CalculateFitness(Chromosome chromosome)
        {
            int penalty = 0;

            var staffSchedule = new Dictionary<string, Dictionary<string, HashSet<int>>>();
            var labSchedule = new Dictionary<string, Dictionary<string, HashSet<int>>>();

            foreach (var gene in chromosome.Genes)
            {
                if (!staffSchedule.ContainsKey(gene.StaffAssigned))
                    staffSchedule[gene.StaffAssigned] = Days.ToDictionary(d => d, d => new HashSet<int>());

                if (!string.IsNullOrWhiteSpace(gene.LabId) && !labSchedule.ContainsKey(gene.LabId))
                    labSchedule[gene.LabId] = Days.ToDictionary(d => d, d => new HashSet<int>());

                for (int hour = gene.StartHour; hour < gene.StartHour + gene.Duration; hour++)
                {
                    if (hour > HoursPerDay)
                    {
                        penalty += 20; // Out of timetable hours
                        continue;
                    }

                    if (StaffAvailability.TryGetValue(gene.StaffAssigned, out var staffDay))
                    {
                        if (staffDay.ContainsKey(gene.Day) && staffDay[gene.Day].Contains(hour))
                            penalty += 10;
                    }
                    if (staffSchedule[gene.StaffAssigned][gene.Day].Contains(hour))
                        penalty += 10;
                    else
                        staffSchedule[gene.StaffAssigned][gene.Day].Add(hour);

                    if (!string.IsNullOrWhiteSpace(gene.LabId))
                    {
                        if (LabAvailability.TryGetValue(gene.LabId, out var labDay))
                        {
                            if (labDay.ContainsKey(gene.Day) && labDay[gene.Day].Contains(hour))
                                penalty += 10;
                        }
                        if (labSchedule[gene.LabId][gene.Day].Contains(hour))
                            penalty += 10;
                        else
                            labSchedule[gene.LabId][gene.Day].Add(hour);
                    }
                }
            }

            return -penalty;
        }

        private Chromosome TournamentSelection(List<Chromosome> population)
        {
            int tournamentSize = 5;
            var candidates = new List<Chromosome>();
            for (int i = 0; i < tournamentSize; i++)
                candidates.Add(population[random.Next(population.Count)]);
            return candidates.OrderByDescending(c => c.FitnessScore).First();
        }

        private (Chromosome, Chromosome) Crossover(Chromosome p1, Chromosome p2)
        {
            int cut = random.Next(1, p1.Genes.Count - 1);
            var c1Genes = p1.Genes.Take(cut).Concat(p2.Genes.Skip(cut)).ToList();
            var c2Genes = p2.Genes.Take(cut).Concat(p1.Genes.Skip(cut)).ToList();
            return (new Chromosome { Genes = c1Genes }, new Chromosome { Genes = c2Genes });
        }

        private void Mutate(Chromosome chromosome)
        {
            foreach (var gene in chromosome.Genes)
            {
                if (random.NextDouble() < MutationRate)
                {
                    if (random.NextDouble() < 0.5)
                        gene.Day = Days[random.Next(Days.Length)];
                    else
                    {
                        int maxStart = HoursPerDay - gene.Duration + 1;
                        gene.StartHour = random.Next(1, maxStart + 1);
                    }
                }
            }
        }

        public (List<TimetableSlot> timetable, List<Conflict> conflicts, Chromosome bestChromosome) GenerateGA()
        {
            var population = new List<Chromosome>();

            for (int i = 0; i < PopulationSize; i++)
            {
                var chrom = CreateRandomChromosome();
                chrom.FitnessScore = CalculateFitness(chrom);
                population.Add(chrom);
            }

            for (int gen = 0; gen < MaxGenerations; gen++)
            {
                var nextPop = new List<Chromosome>();
                while (nextPop.Count < PopulationSize)
                {
                    var p1 = TournamentSelection(population);
                    var p2 = TournamentSelection(population);

                    var (c1, c2) = Crossover(p1, p2);

                    Mutate(c1);
                    Mutate(c2);

                    c1.FitnessScore = CalculateFitness(c1);
                    c2.FitnessScore = CalculateFitness(c2);

                    nextPop.Add(c1);
                    nextPop.Add(c2);
                }
                population = nextPop.OrderByDescending(c => c.FitnessScore).Take(PopulationSize).ToList();

                if (population[0].FitnessScore == 0)
                    break;
            }

            var best = population.OrderByDescending(c => c.FitnessScore).First();
            return (ConvertToTimetable(best.Genes), ExtractConflicts(best), best);
        }

        private List<TimetableSlot> ConvertToTimetable(List<Gene> genes)
        {
            var timetable = Days.Select(day => new TimetableSlot
            {
                Day = day,
                HourlySlots = Enumerable.Range(1, HoursPerDay).ToDictionary(h => h, _ => "---")
            }).ToList();

            foreach (var gene in genes)
            {
                var slot = timetable.First(t => t.Day == gene.Day);
                for (int h = gene.StartHour; h < gene.StartHour + gene.Duration; h++)
                {
                    if (h <= HoursPerDay)
                        slot.HourlySlots[h] = $"{gene.SubjectCode} ({gene.StaffAssigned})";
                }
            }

            return timetable;
        }

        private List<Conflict> ExtractConflicts(Chromosome chromosome)
        {
            var conflicts = new List<Conflict>();
            if (chromosome.FitnessScore < 0)
            {
                conflicts.Add(new Conflict
                {
                    Reason = "Schedule contains conflicts (staff or lab double bookings or unavailable)."
                });
            }
            return conflicts;
        }

        public class Conflict
        {
            public Subject Subject { get; set; } = null;
            public string Reason { get; set; }
        }
    }
}




