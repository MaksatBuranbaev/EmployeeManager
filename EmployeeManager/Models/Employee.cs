using EmployeeManager.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeManager.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } // "Фамилия Имя Отчество"

        [Column(TypeName = "date")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [StringLength(10)]
        public string Gender { get; set; }

        // Метод для расчёта возраста (полных лет)
        public int CalculateAge()
        {
            var today = DateTime.Today;
            int age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

        // Метод для отправки объекта в БД
        // Фактически не используется, т.к. не эффективен в выбранном мной подходе
        public void Save(AppDbContext context)
        {
            context.Employees.Add(this);
            context.SaveChanges();
        }
    }
}
