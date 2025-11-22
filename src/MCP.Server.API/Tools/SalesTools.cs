using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;

/// <summary>
/// MCP tools for sales simulation.
/// Provides simulated sales data for any requested month.
/// </summary>
internal class SalesTools
{
    private static readonly Random _random = new Random();

    [McpServerTool]
    [Description("Simula las ventas del mes solicitado. Genera datos de ventas diarias con estadísticas como total, promedio, mejor día y peor día. Ejemplo: 'enero', 'febrero 2024', 'marzo', '12' (para diciembre)")]
    public string GetMonthlySales(
        [Description("Nombre del mes (ej: 'enero', 'febrero', 'marzo') o número del mes (1-12). Puede incluir el año, por ejemplo: 'enero 2024' o 'febrero 2025'. Si no se especifica el año, se usa el año actual.")] string month)
    {
        try
        {
            // Parsear el mes y año
            var (monthNumber, year) = ParseMonthAndYear(month);
            
            if (monthNumber < 1 || monthNumber > 12)
            {
                return $"Error: El mes debe estar entre 1 y 12, o ser un nombre válido de mes (enero, febrero, etc.).";
            }

            // Obtener el número de días del mes
            int daysInMonth = DateTime.DaysInMonth(year, monthNumber);
            
            // Generar ventas diarias simuladas
            var dailySales = new List<DailySale>();
            double baseSales = 1000 + _random.NextDouble() * 2000; // Base entre 1000 y 3000
            
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, monthNumber, day);
                
                // Simular variaciones: fines de semana tienen menos ventas, algunos días tienen picos
                double multiplier = 1.0;
                
                // Fines de semana: 30-50% menos ventas
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    multiplier = 0.5 + _random.NextDouble() * 0.2; // 0.5 a 0.7
                }
                // Días de semana: variación normal
                else
                {
                    multiplier = 0.8 + _random.NextDouble() * 0.4; // 0.8 a 1.2
                }
                
                // Ocasionalmente hay días con ventas excepcionales (10% de probabilidad)
                if (_random.NextDouble() < 0.1)
                {
                    multiplier *= 1.5 + _random.NextDouble(); // 1.5x a 2.5x
                }
                
                double sales = baseSales * multiplier;
                dailySales.Add(new DailySale
                {
                    Date = date,
                    Sales = Math.Round(sales, 2),
                    DayOfWeek = date.DayOfWeek.ToString()
                });
            }

            // Calcular estadísticas
            double totalSales = dailySales.Sum(d => d.Sales);
            double averageSales = dailySales.Average(d => d.Sales);
            var bestDay = dailySales.OrderByDescending(d => d.Sales).First();
            var worstDay = dailySales.OrderBy(d => d.Sales).First();
            
            // Días con ventas por encima del promedio
            int daysAboveAverage = dailySales.Count(d => d.Sales > averageSales);
            
            // Formatear la respuesta
            string monthName = new DateTime(year, monthNumber, 1).ToString("MMMM", new CultureInfo("es-ES"));
            monthName = char.ToUpper(monthName[0]) + monthName.Substring(1);
            
            string result = $"📊 Ventas Simuladas - {monthName} {year}\n\n";
            result += $"💰 Total de ventas: ${totalSales:N2}\n";
            result += $"📈 Promedio diario: ${averageSales:N2}\n";
            result += $"📅 Días del mes: {daysInMonth}\n";
            result += $"✅ Días por encima del promedio: {daysAboveAverage}\n\n";
            
            result += $"🏆 Mejor día: {bestDay.Date:dd/MM/yyyy} ({bestDay.DayOfWeek})\n";
            result += $"   Ventas: ${bestDay.Sales:N2}\n\n";
            
            result += $"📉 Peor día: {worstDay.Date:dd/MM/yyyy} ({worstDay.DayOfWeek})\n";
            result += $"   Ventas: ${worstDay.Sales:N2}\n\n";
            
            // Resumen por semana
            result += "📋 Resumen semanal:\n";
            var weeklyGroups = dailySales
                .GroupBy(d => GetWeekNumber(d.Date))
                .OrderBy(g => g.Key);
            
            foreach (var week in weeklyGroups)
            {
                double weekTotal = week.Sum(d => d.Sales);
                double weekAverage = week.Average(d => d.Sales);
                result += $"   Semana {week.Key}: Total ${weekTotal:N2} | Promedio ${weekAverage:N2}\n";
            }

            return result;
        }
        catch (Exception ex)
        {
            return $"Error al generar las ventas simuladas: {ex.Message}";
        }
    }

    private static (int monthNumber, int year) ParseMonthAndYear(string input)
    {
        input = input.Trim().ToLower();
        int year = DateTime.Now.Year;
        int monthNumber = 0;

        // Diccionario de nombres de meses en español
        var monthNames = new Dictionary<string, int>
        {
            { "enero", 1 }, { "febrero", 2 }, { "marzo", 3 }, { "abril", 4 },
            { "mayo", 5 }, { "junio", 6 }, { "julio", 7 }, { "agosto", 8 },
            { "septiembre", 9 }, { "octubre", 10 }, { "noviembre", 11 }, { "diciembre", 12 }
        };

        // Intentar extraer el año si está presente
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length > 1)
        {
            // Hay año especificado
            if (int.TryParse(parts[1], out int parsedYear) && parsedYear >= 2000 && parsedYear <= 2100)
            {
                year = parsedYear;
            }
            input = parts[0];
        }

        // Intentar parsear como número
        if (int.TryParse(input, out int monthNum))
        {
            monthNumber = monthNum;
        }
        // Intentar parsear como nombre de mes
        else if (monthNames.TryGetValue(input, out int monthFromName))
        {
            monthNumber = monthFromName;
        }
        else
        {
            throw new ArgumentException($"No se pudo reconocer el mes: {input}");
        }

        return (monthNumber, year);
    }

    private static int GetWeekNumber(DateTime date)
    {
        var culture = new CultureInfo("es-ES");
        var calendar = culture.Calendar;
        int week = calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return week;
    }

    private class DailySale
    {
        public DateTime Date { get; set; }
        public double Sales { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
    }
}


