using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

/// <summary>
/// MCP tools for weather information.
/// Provides weather forecast and current temperature for any location.
/// Uses Open-Meteo API (completely free, no API key required).
/// </summary>
internal class WeatherTools
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string GeocodingApiUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string WeatherApiUrl = "https://api.open-meteo.com/v1/forecast";

    [McpServerTool]
    [Description("Obtiene el pronóstico del tiempo y la temperatura actual para un lugar específico. Ejemplo: 'Corrientes Argentina', 'Buenos Aires', 'Madrid España'. No requiere configuración de API key.")]
    public async Task<string> GetWeather(
        [Description("Nombre del lugar o ciudad. Puede incluir el país para mayor precisión. Ejemplo: 'Corrientes Argentina', 'Buenos Aires', 'Madrid España'")] string location)
    {
        try
        {
            // Paso 1: Obtener coordenadas geográficas del lugar
            string geocodingUrl = $"{GeocodingApiUrl}?name={Uri.EscapeDataString(location)}&count=1&language=es&format=json";
            
            HttpResponseMessage geocodingResponse = await _httpClient.GetAsync(geocodingUrl);
            
            if (!geocodingResponse.IsSuccessStatusCode)
            {
                return $"Error al buscar el lugar '{location}': {geocodingResponse.StatusCode} - {geocodingResponse.ReasonPhrase}";
            }

            string geocodingJson = await geocodingResponse.Content.ReadAsStringAsync();
            var geocodingData = JsonSerializer.Deserialize<GeocodingResponse>(geocodingJson);

            if (geocodingData == null || geocodingData.Results == null || geocodingData.Results.Length == 0)
            {
                return $"No se pudo encontrar el lugar '{location}'. " +
                       "Por favor, verifica que el nombre del lugar sea correcto e intenta incluir el país si es necesario. " +
                       "Ejemplo: 'Corrientes Argentina', 'Buenos Aires Argentina'";
            }

            var place = geocodingData.Results[0];
            double latitude = place.Latitude;
            double longitude = place.Longitude;
            string placeName = place.Name;
            string? country = place.Country;
            string? admin1 = place.Admin1; // Estado/Provincia

            // Paso 2: Obtener el clima usando las coordenadas
            string weatherUrl = $"{WeatherApiUrl}?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m,wind_direction_10m,pressure_msl&hourly=temperature_2m,weather_code&daily=temperature_2m_max,temperature_2m_min,weather_code&timezone=auto&forecast_days=1";
            
            HttpResponseMessage weatherResponse = await _httpClient.GetAsync(weatherUrl);

            if (!weatherResponse.IsSuccessStatusCode)
            {
                return $"Error al obtener el clima: {weatherResponse.StatusCode} - {weatherResponse.ReasonPhrase}";
            }

            string weatherJson = await weatherResponse.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<WeatherResponse>(weatherJson);

            if (weatherData == null || weatherData.Current == null)
            {
                return "Error: No se pudo procesar la respuesta de la API del clima.";
            }

            // Formatear la respuesta
            string result = $"🌤️ Clima en {placeName}";
            if (!string.IsNullOrEmpty(admin1))
            {
                result += $", {admin1}";
            }
            if (!string.IsNullOrEmpty(country))
            {
                result += $", {country}";
            }
            result += "\n\n";

            result += $"🌡️ Temperatura actual: {weatherData.Current.Temperature2m:F1}°C\n";
            result += $"🌡️ Sensación térmica: {weatherData.Current.ApparentTemperature:F1}°C\n";
            
            if (weatherData.Daily != null && weatherData.Daily.Temperature2mMax != null && weatherData.Daily.Temperature2mMax.Length > 0)
            {
                result += $"📊 Temperatura máxima: {weatherData.Daily.Temperature2mMax[0]:F1}°C\n";
            }
            if (weatherData.Daily != null && weatherData.Daily.Temperature2mMin != null && weatherData.Daily.Temperature2mMin.Length > 0)
            {
                result += $"📊 Temperatura mínima: {weatherData.Daily.Temperature2mMin[0]:F1}°C\n";
            }
            
            result += $"💧 Humedad: {weatherData.Current.RelativeHumidity2m}%\n";
            result += $"🌬️ Viento: {weatherData.Current.WindSpeed10m:F1} km/h";
            
            if (weatherData.Current.WindDirection10m.HasValue)
            {
                string windDirection = GetWindDirection(weatherData.Current.WindDirection10m.Value);
                result += $" ({windDirection})";
            }
            
            result += $"\n☁️ Condiciones: {GetWeatherDescription(weatherData.Current.WeatherCode)}\n";
            
            if (weatherData.Current.PressureMsl.HasValue)
            {
                result += $"📊 Presión atmosférica: {weatherData.Current.PressureMsl.Value:F1} hPa\n";
            }

            return result;
        }
        catch (Exception ex)
        {
            return $"Error al obtener el clima: {ex.Message}";
        }
    }

    private static string GetWeatherDescription(int weatherCode)
    {
        // Códigos de clima de WMO (World Meteorological Organization)
        return weatherCode switch
        {
            0 => "Cielo despejado",
            1 => "Mayormente despejado",
            2 => "Parcialmente nublado",
            3 => "Nublado",
            45 => "Niebla",
            48 => "Niebla con escarcha",
            51 => "Llovizna ligera",
            53 => "Llovizna moderada",
            55 => "Llovizna densa",
            56 => "Llovizna helada ligera",
            57 => "Llovizna helada densa",
            61 => "Lluvia ligera",
            63 => "Lluvia moderada",
            65 => "Lluvia intensa",
            66 => "Lluvia helada ligera",
            67 => "Lluvia helada intensa",
            71 => "Nieve ligera",
            73 => "Nieve moderada",
            75 => "Nieve intensa",
            77 => "Granizo",
            80 => "Chubascos ligeros",
            81 => "Chubascos moderados",
            82 => "Chubascos intensos",
            85 => "Chubascos de nieve ligeros",
            86 => "Chubascos de nieve intensos",
            95 => "Tormenta",
            96 => "Tormenta con granizo ligero",
            99 => "Tormenta con granizo intenso",
            _ => "Condiciones desconocidas"
        };
    }

    private static string GetWindDirection(double degrees)
    {
        string[] directions = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
        int index = (int)Math.Round(degrees / 22.5) % 16;
        return directions[index];
    }

    // Clases para deserializar las respuestas JSON
    private class GeocodingResponse
    {
        public GeocodingResult[]? Results { get; set; }
    }

    private class GeocodingResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }
        public string? FeatureCode { get; set; }
        public string? CountryCode { get; set; }
        public string? Admin1 { get; set; }
        public string? Admin2 { get; set; }
        public string? Admin3 { get; set; }
        public string? Admin4 { get; set; }
        public string? Timezone { get; set; }
        public int Population { get; set; }
        public string? Country { get; set; }
        public string? CountryId { get; set; }
    }

    private class WeatherResponse
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double GenerationTimeMs { get; set; }
        public int UtcOffsetSeconds { get; set; }
        public string? Timezone { get; set; }
        public string? TimezoneAbbreviation { get; set; }
        public double Elevation { get; set; }
        public CurrentWeather? Current { get; set; }
        public DailyWeather? Daily { get; set; }
        public HourlyWeather? Hourly { get; set; }
    }

    private class CurrentWeather
    {
        public string? Time { get; set; }
        public int Interval { get; set; }
        public double Temperature2m { get; set; }
        public double ApparentTemperature { get; set; }
        public int RelativeHumidity2m { get; set; }
        public int WeatherCode { get; set; }
        public double WindSpeed10m { get; set; }
        public int? WindDirection10m { get; set; }
        public double? PressureMsl { get; set; }
    }

    private class DailyWeather
    {
        public string[]? Time { get; set; }
        public double[]? Temperature2mMax { get; set; }
        public double[]? Temperature2mMin { get; set; }
        public int[]? WeatherCode { get; set; }
    }

    private class HourlyWeather
    {
        public string[]? Time { get; set; }
        public double[]? Temperature2m { get; set; }
        public int[]? WeatherCode { get; set; }
    }
}
