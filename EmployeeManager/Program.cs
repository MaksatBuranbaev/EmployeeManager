using EmployeeManager.Services;
using EmployeeManager.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace EmployeeManager
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int mode) || mode < 1 || mode > 6)
            {
                Console.WriteLine("Необходимо указать режим работы приложения (1-6).");
                return;
            }

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddDbContext<AppDbContext>();
                    services.AddScoped<EmployeeService>();
                })
                .Build();

            using var scope = host.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<EmployeeService>();

            try
            {
                switch (mode)
                {
                    case 1:
                        await service.CreateTableAsync();
                        break;
                    case 2:
                        await AddEmployeeAsync(args, service);
                        break;
                    case 3:
                        await service.DisplayUniqueEmployeesAsync();
                        break;
                    case 4:
                        await service.BulkInsertEmployeesAsync();
                        break;
                    case 5:
                        await service.QueryEmployeesWithTimingAsync();
                        break;
                    case 6:
                        await service.OptimizeDatabaseAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private static async Task AddEmployeeAsync(string[] args, EmployeeService service)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Пример запуска: myApp 2 \"Ivanov Petr Sergeevich\" 2009-07-12 Male");
                return;
            }

            if (!DateTime.TryParse(args[2], out DateTime dob))
            {
                Console.WriteLine("Неверный формат даты рождения. Используйте YYYY-MM-DD.");
                return;
            }

            string fullName = args[1];
            string gender = args[3];
            await service.AddEmployeeAsync(fullName, dob, gender);
        }
    }
}
