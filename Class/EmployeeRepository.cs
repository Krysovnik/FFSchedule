using FFSchedule.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FFSchedule.Class;

public class EmployeeRepository
{
    private readonly string _databasePath;

    public EmployeeRepository(string databasePath)
    {
        _databasePath = Path.GetFullPath(databasePath);
    }

    public List<Employee> GetAllEmployees()
    {
        var optionsBuilder = new DbContextOptionsBuilder<FfsContext>();
        optionsBuilder.UseSqlite($"Data Source={_databasePath}");

        using var context = new FfsContext(optionsBuilder.Options);

        return context.Employees
            .Select(e => new Employee
            {
                EmId = e.EmId,
                EmLogin = e.EmLogin,
                EmPassword = e.EmPassword,
                EmFio = e.EmFio,
                RoId = e.RoId
            })
            .ToList();
    }
}
