//using Google.OrTools.Sat;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace TimetableGA
//{
//    public class TimetableEngine
//    {
//        public static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri" };
//        const int Hours = 7, MaxPerDay = 4;

//        public class Subject
//        {
//            public string SubjectCode, SubjectName, SubjectType, StaffAssigned;
//            public int Credit;
//        }

//        public class Assignment
//        {
//            public int Day, Hour;
//            public string SubjectCode, StaffAssigned;
//        }

//        public class Solution
//        {
//            public bool IsSuccess;
//            public List<Assignment> Assignments = new();
//            public List<string> ConflictReasons = new();
//            internal object UnplacedSubjects;
//        }

//        public Solution Solve(
//            List<Subject> subjects,
//            Dictionary<string, Dictionary<int, List<int>>> forbidden)
//        {
//            var model = new CpModel();
//            int n = subjects.Count;
//            var x = new BoolVar[n, Days.Length, Hours];

//            for (int i = 0; i < n; i++)
//                for (int d = 0; d < Days.Length; d++)
//                    for (int h = 0; h < Hours; h++)
//                        x[i, d, h] = model.NewBoolVar($"x[{i},{d},{h}]");

//            // Each subject should occupy exactly its credit slots
//            for (int i = 0; i < n; i++)
//                model.Add(LinearExpr.Sum(
//                    Enumerable.Range(0, Days.Length)
//                        .SelectMany(d => Enumerable.Range(0, Hours)
//                        .Select(h => x[i, d, h]))
//                ) == subjects[i].Credit);

//            // At most one subject per time slot
//            for (int d = 0; d < Days.Length; d++)
//                for (int h = 0; h < Hours; h++)
//                    model.Add(LinearExpr.Sum(
//                        Enumerable.Range(0, n).Select(i => x[i, d, h])
//                    ) <= 1);

//            // Staff constraints (max per day and forbidden hours)
//            var byStaff = subjects.Select((s, i) => (s.StaffAssigned, i)).GroupBy(p => p.StaffAssigned);
//            foreach (var grp in byStaff)
//            {
//                string stf = grp.Key;
//                var idxs = grp.Select(p => p.i).ToList();

//                for (int d = 0; d < Days.Length; d++)
//                {
//                    // Max 4 hours per day
//                    model.Add(LinearExpr.Sum(
//                        idxs.Select(i => LinearExpr.Sum(
//                            Enumerable.Range(0, Hours).Select(h => x[i, d, h])
//                        ))
//                    ) <= MaxPerDay);

//                    // Forbidden slots
//                    if (forbidden.TryGetValue(stf, out var fd) && fd.TryGetValue(d, out var hrs))
//                    {
//                        foreach (var i in idxs)
//                            foreach (var h in hrs)
//                                model.Add(x[i, d, h] == 0);
//                    }
//                }
//            }

//            // Objective: maximize filled slots
//            model.Maximize(LinearExpr.Sum(x.Cast<BoolVar>()));

//            var solver = new CpSolver();
//            solver.StringParameters = "max_time_in_seconds:30";

//            var status = solver.Solve(model);

//            var sol = new Solution();
//            if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
//            {
//                sol.IsSuccess = false;
//                sol.ConflictReasons.Add($"Solver status: {status}");
//                return sol;
//            }

//            sol.IsSuccess = true;
//            for (int i = 0; i < n; i++)
//                for (int d = 0; d < Days.Length; d++)
//                    for (int h = 0; h < Hours; h++)
//                        if (solver.Value(x[i, d, h]) == 1)
//                            sol.Assignments.Add(new Assignment
//                            {
//                                Day = d,
//                                Hour = h,
//                                SubjectCode = subjects[i].SubjectCode,
//                                StaffAssigned = subjects[i].StaffAssigned
//                            });

//            return sol;
//        }
//    }
//}
