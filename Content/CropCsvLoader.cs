// Content/CropCsvLoader.cs
using System.Globalization;
using AsciiFarm.Data;

namespace AsciiFarm.Content;

/// <summary>Читает справочную таблицу культур. Порядок столбцов произвольный, лишние — игнорируются.</summary>
public static class CropCsvLoader
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rye_winter"] = "Озимая рожь",
        ["wheat_spring"] = "Яровая пшеница",
        ["barley_spring"] = "Ячмень",
        ["oats_spring"] = "Овёс",
        ["millet_spring"] = "Просо",
        ["peas_field"] = "Горох",
        ["turnip_garden"] = "Репа",
        ["cabbage_garden"] = "Капуста",
        ["onion_garden"] = "Лук",
        ["flax_fiber"] = "Лён"
    };

    public static CropCatalog Load(string filePath) => Load(filePath, out _);

    public static CropCatalog Load(string filePath, out string? error)
    {
        error = null;
        var catalog = new CropCatalog();

        if (!File.Exists(filePath)) { error = $"Файл не найден: {filePath}"; catalog.LoadError = error; return catalog; }

        string[] lines;
        try { lines = File.ReadAllLines(filePath); }
        catch (Exception ex) { error = $"Не удалось прочитать: {ex.Message}"; catalog.LoadError = error; return catalog; }

        int headerIndex = -1;
        for (int i = 0; i < lines.Length && i < 20; i++)
            if (lines[i].ToLowerInvariant().Contains("crop_id")) { headerIndex = i; break; }

        if (headerIndex < 0) { error = "Не найдена строка заголовков (crop_id)"; catalog.LoadError = error; return catalog; }

        var header = Split(lines[headerIndex]);
        var col = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++) col[header[i]] = i;

        for (int line = headerIndex + 1; line < lines.Length; line++)
        {
            string text = lines[line].Trim();
            if (text.Length == 0 || text.StartsWith("#")) continue;
            var f = Split(text);

            string S(string key, string def = "") =>
                col.TryGetValue(key, out var i) && i < f.Count ? f[i] : def;
            float F(string key, float def = 0f) =>
                float.TryParse(S(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
            double D(string key, double def = 0d) =>
                double.TryParse(S(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

            string id = S("crop_id");
            if (id.Length == 0) continue;

            var crop = new CropDefinition
            {
                Id = id,
                Name = S("name", Names.TryGetValue(id, out var n) ? n : id),
                IsWinter = F("is_winter") > 0.5f,
                TBase = F("t_base"),
                TOptMin = F("t_opt_min"),
                TOptMax = F("t_opt_max"),
                TCritColdGround = F("t_crit_cold_ground"),
                TCritFrostFlower = F("t_crit_frost_flower"),
                TCritHeat = F("t_crit_heat"),
                GddVeg = D("gdd_veg"),
                GddFlower = D("gdd_flower"),
                GddEarlyHarvest = D("gdd_early_harvest"),
                GddHarvest = D("gdd_harvest"),
                GddSeedYear2 = D("gdd_seed_year2"),
                SoilMoistMin = F("soil_moist_min"),
                SoilMoistOptMin = F("soil_moist_opt_min"),
                SoilMoistOptMax = F("soil_moist_opt_max"),
                SoilMoistMax = F("soil_moist_max"),
                SoilMoistRipeningMax = F("soil_moist_ripening_max"),
                AirHumDiseaseMin = F("air_hum_disease_min"),
                SolarMjTarget = D("solar_mj_target"),
                Mode = ParseMode(S("harvest_mode")),
                SeedRatio = F("seed_ratio"),
                FertilityDelta = F("fertility_delta")
            };

            if (crop.GddHarvest <= 0 || crop.SolarMjTarget <= 0 || crop.SeedRatio <= 0) continue;
            catalog.Add(crop);
        }

        if (catalog.Count == 0) error = "Ни одна культура не распознана";
        catalog.LoadError = error;
        return catalog;
    }

    private static HarvestMode ParseMode(string s) => s.Trim().ToLowerInvariant() switch
    {
        "biennial" => HarvestMode.Biennial,
        "early_or_late" => HarvestMode.EarlyOrLate,
        _ => HarvestMode.GrainAndSeed
    };

    private static List<string> Split(string line) =>
        line.Split(',').Select(s => s.Trim().Trim('"')).ToList();
}