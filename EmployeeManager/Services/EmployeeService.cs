using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using EmployeeManager.Data;
using EmployeeManager.Models;
using System.Linq;

namespace EmployeeManager.Services
{
    public class EmployeeService
    {
        private readonly AppDbContext _context;
        private readonly Random _random = new Random();
        private readonly string[] _firstNames = { "James", "John", "Robert", "Michael", "William", "David", "Richard", "Charles", "Joseph", "Thomas" };
        private readonly string[] _lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        private readonly string[] _middleNames = { "James", "John", "Robert", "Michael", "William", "David", "Richard", "Charles", "Joseph", "Thomas" };
        private readonly string[] _genders = { "Male", "Female" };
        private readonly string[] _specialLastNames = { "Foster", "Franklin", "Fletcher", "Frazier", "Ferguson", "Floyd", "Finley", "Fields", "Farmer", "Frost" };

        public EmployeeService(AppDbContext context) => _context = context;

        // Режим 1: Создание и миграция базы данных
        public async Task CreateTableAsync()
        {
            await _context.Database.MigrateAsync();
            Console.WriteLine("База данных готова.");
        }

        // Режим 2: Добавление одного сотрудника
        public async Task AddEmployeeAsync(string fullName, DateTime dob, string gender)
        {
            var employee = new Employee
            {
                FullName = fullName,
                DateOfBirth = dob,
                Gender = gender
            };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            Console.WriteLine("Сотрудник добавлен.");
        }

        // Режим 3: Вывод уникальных сотрудников
        public async Task DisplayUniqueEmployeesAsync(int limit = 1000)
        {
            string sqlQuery = @"
                SELECT DISTINCT ON (""FullName"", ""DateOfBirth"") *
                FROM ""Employees""
                ORDER BY ""FullName"", ""DateOfBirth"", ""Id""
                LIMIT {0}";

            var uniqueEmployees = _context.Employees
                .FromSqlRaw(sqlQuery, limit)
                .AsNoTracking()
                .AsAsyncEnumerable();

            await foreach (var emp in uniqueEmployees)
            {
                Console.WriteLine($"ФИО: {emp.FullName}, Дата рождения: {emp.DateOfBirth:yyyy-MM-dd}, Пол: {emp.Gender}, Возраст: {emp.CalculateAge()}");
            }
        }


        // Режим 4: Массовое добавление сотрудников
        public async Task BulkInsertEmployeesAsync(int total = 1_000_000, int specific = 100)
        {
            Console.WriteLine("Начало массового добавления сотрудников...");
            var employees = new List<Employee>(total);

            // Генерация случайных сотрудников
            for (int i = 0; i < total - specific; i++)
            {
                employees.Add(new Employee
                {
                    FullName = $"{_lastNames[_random.Next(_lastNames.Length)]} {_firstNames[_random.Next(_firstNames.Length)]} {_middleNames[_random.Next(_middleNames.Length)]}",
                    DateOfBirth = RandomDate(new DateTime(1950, 1, 1), new DateTime(2000, 12, 31)),
                    Gender = _genders[_random.Next(_genders.Length)]
                });

                if ((i + 1) % 100_000 == 0)
                    Console.WriteLine($"{i + 1} сотрудников сгенерировано...");
            }

            // Генерация специфических сотрудников
            for (int i = 0; i < specific; i++)
            {
                employees.Add(new Employee
                {
                    FullName = $"{_specialLastNames[_random.Next(_specialLastNames.Length)]} {_firstNames[_random.Next(_firstNames.Length)]} {_middleNames[_random.Next(_middleNames.Length)]}",
                    DateOfBirth = RandomDate(new DateTime(1950, 1, 1), new DateTime(2000, 12, 31)),
                    Gender = "Male"
                });
            }

            // Пакетная вставка
            const int batchSize = 100_000;
            for (int i = 0; i < employees.Count; i += batchSize)
            {
                var batch = employees.Skip(i).Take(batchSize);
                await _context.Employees.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
                Console.WriteLine($"{i + batchSize} сотрудников добавлено.");
            }

            Console.WriteLine("Массовое добавление завершено.");
        }

        // Режим 5: Выборка по критерию с замером времени
        public async Task QueryEmployeesWithTimingAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            var result = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Gender == "Male" && EF.Functions.ILike(e.FullName, "F%"))
                .ToListAsync();

            stopwatch.Stop();

            foreach (var emp in result)
            {
                Console.WriteLine($"ФИО: {emp.FullName}, Дата рождения: {emp.DateOfBirth:yyyy-MM-dd}, Пол: {emp.Gender}, Возраст: {emp.CalculateAge()}");
            }

            Console.WriteLine($"Время выполнения запроса: {stopwatch.ElapsedMilliseconds} мс");
        }

        // Режим 6: Оптимизация базы данных
        public async Task OptimizeDatabaseAsync()
        {
            Console.WriteLine("Оптимизация базы данных...");

            // Добавление комбинированного индекса
            await _context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS idx_gender_fullname ON \"Employees\" (\"Gender\", \"FullName\");");

            // Добавление отдельных индексов
            await _context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS idx_gender ON \"Employees\" (\"Gender\");");
            await _context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS idx_fullname ON \"Employees\" (\"FullName\");");

            // Создание частичного индекса для Gender = 'Male'
            await _context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS idx_male_fullname ON \"Employees\" (\"FullName\") WHERE \"Gender\" = 'Male';");

            // Вакуум и анализ таблицы
            await _context.Database.ExecuteSqlRawAsync("VACUUM ANALYZE \"Employees\";");

            Console.WriteLine("Оптимизация завершена.");

            var stopwatch = Stopwatch.StartNew();

            var result = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Gender == "Male" && EF.Functions.ILike(e.FullName, "F%"))
                .ToListAsync();

            stopwatch.Stop();

            foreach (var emp in result)
            {
                Console.WriteLine($"ФИО: {emp.FullName}, Дата рождения: {emp.DateOfBirth:yyyy-MM-dd}, Пол: {emp.Gender}, Возраст: {emp.CalculateAge()}");
            }

            Console.WriteLine($"Время выполнения запроса: {stopwatch.ElapsedMilliseconds} мс");
        }


        // Вспомогательный метод для генерации случайной даты
        private DateTime RandomDate(DateTime start, DateTime end) => start.AddDays(_random.Next((end - start).Days));
    }
}
