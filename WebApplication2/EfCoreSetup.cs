using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;

namespace Timetablegenerator
{
    public class StaffData
    {
        public int Id { get; set; }
        public string StaffId { get; set; }
        public string Name { get; set; }
        public string Subject1 { get; set; }
        public string Subject2 { get; set; }
        public string Subject3 { get; set; }
        public string Block { get; set; }
        public string Department { get; set; }
        public string DepartmentId { get; set; }
    }

    public class SubjectData
    {
        public int Id { get; set; }
        public string SubCode { get; set; }
        public string SubjectName { get; set; }
        public string Year { get; set; }
        public string Sem { get; set; }
        public string Department { get; set; }
        public string DepartmentId { get; set; }
        public string SubjectType { get; set; }
        public int Credit { get; set; }
        public string SubjectId { get; set; }
    }

    public class SubjectRequest
    {
        public int Id { get; set; }
        public string SubCode { get; set; }
        public string SubjectName { get; set; }
        public string SubjectType { get; set; }
        public int Credit { get; set; }
        public string StaffAssigned { get; set; }
        public string Year { get; set; }
        public string Semester { get; set; }
        public string Section { get; set; }
        public string Department { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AssignedStaff { get; set; }
    }

    public class Admin
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Roll { get; set; }
    }

    public class ClassTimetable
    {
        public int Id { get; set; }
        public string FromDepartment { get; set; }
        public string ToDepartment { get; set; }
        public string Year { get; set; }
        public string Semester { get; set; }
        public string Section { get; set; }
        public string Day { get; set; }
        public int Hour { get; set; }
        public string SubjectCode { get; set; }
        public string StaffAssigned { get; set; }
        public string SubjectName { get; set; }
    }

    public class CrossDepartmentAssignr
    {
        public int Id { get; set; }
        public string FromDepartment { get; set; }
        public string ToDepartment { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public string Year { get; set; }
        public string Semester { get; set; }
        public string Section { get; set; }
        public DateTime AssignedAt { get; set; }
        public string AssignedStaff { get; set; }
    }

    public class StaffTimetable
    {
        public int Id { get; set; }
        public string StaffName { get; set; }
        public string Department { get; set; }
        public string Year { get; set; }
        public string Semester { get; set; }
        public string Section { get; set; }
        public string Day { get; set; }
        public int Hour { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public string StaffId { get; set; }
    }

    public class LabTimetable
    {
        public int Id { get; set; }
        public string LabId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public string StaffAssigned { get; set; }
        public string Department { get; set; }
        public string Year { get; set; }
        public string Semester { get; set; }
        public string Section { get; set; }
        public string Day { get; set; }
        public int Hour { get; set; }
    }

    //public class Labs
    //{
    //    [Key]
    //    public string LabId { get; set; }
    //    public string LabName { get; set; }
    //    public string Department { get; set; }
    //    public int Systems { get; set; }
    //}

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<StaffData> StaffData { get; set; }
        public DbSet<SubjectData> SubjectData { get; set; }
        public DbSet<SubjectRequest> SubjectRequest { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<ClassTimetable> ClassTimetables { get; set; }
        public DbSet<CrossDepartmentAssignr> CrossDepartmentAssignr { get; set; }
        public DbSet<StaffTimetable> StaffTimetables { get; set; }
        public DbSet<LabTimetable> LabTimetables { get; set; }
      //  public DbSet<Labs> Labs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Admin>().HasData(new Admin
            {
                Id = "2",
                Username = "ADMIN",
                Password = "admin@123",
                Roll = "Admin"
            });
        }
    }
}
