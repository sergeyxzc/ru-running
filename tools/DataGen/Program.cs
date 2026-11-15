using System.Text;
using System.Text.Json;

namespace DataGen;

internal sealed record Rank(string Разряд, string Время);

internal sealed record Entry(string Дистанция, string Хронометраж, string? Круг, Rank[] Разряды);

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static int Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        if (args.Length > 2)
        {
            Console.Error.WriteLine("Слишком много аргументов.");
            PrintUsage();
            return 2;
        }

        string inputDir = args.Length > 0 ? args[0] : "data-sets";
        string outputPath = args.Length > 1 ? args[1] : Path.Combine("src", "data.js");

        try
        {
            return Generate(inputDir, outputPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка: {ex.Message}");
            return 1;
        }
    }

    private static int Generate(string inputDir, string outputPath)
    {
        if (!Directory.Exists(inputDir))
            throw new DirectoryNotFoundException($"каталог с исходными данными не найден: {Path.GetFullPath(inputDir)}");

        // Порядок блоков как в src/data.js и dataMapping в src/app.js: сначала женские, потом мужские.
        var files = Directory.GetFiles(inputDir, "*.json")
            .OrderBy(f => Path.GetFileNameWithoutExtension(f) switch
            {
                "woman-stadium" => 0,
                "man-stadium" => 1,
                _ => 2,
            })
            .ThenBy(f => f, StringComparer.Ordinal)
            .ToList();

        if (files.Count == 0)
            throw new FileNotFoundException($"в каталоге {Path.GetFullPath(inputDir)} нет JSON-файлов");

        var lines = new List<string>
        {
            "// ============================================",
            "// ДАННЫЕ НОРМАТИВОВ (отдельный файл)",
            "// ============================================",
            "// Сгенерировано tools/DataGen из data-sets/*.json — не редактировать вручную.",
            "",
        };

        for (int i = 0; i < files.Count; i++)
        {
            string file = files[i];
            string json = File.ReadAllText(file, Encoding.UTF8);
            Entry[] entries = JsonSerializer.Deserialize<Entry[]>(json, JsonOptions)
                ?? throw new InvalidDataException($"{Path.GetFileName(file)}: ожидался JSON-массив записей");

            Validate(entries, file);

            string varName = VariableName(file);
            int norms = entries.Sum(e => e.Разряды.Length);
            Console.WriteLine($"{Path.GetFileName(file)}: {entries.Length} записей, {norms} нормативов -> {varName}");

            lines.Add($"const {varName} = [");
            for (int e = 0; e < entries.Length; e++)
            {
                Entry entry = entries[e];
                lines.Add("  {");
                lines.Add($"    \"Дистанция\": {JsString(entry.Дистанция)},");
                lines.Add($"    \"Хронометраж\": {JsString(entry.Хронометраж)},");
                lines.Add($"    \"Круг\": {(entry.Круг is null ? "null" : JsString(entry.Круг))},");
                lines.Add("    \"Разряды\": [");
                for (int r = 0; r < entry.Разряды.Length; r++)
                {
                    Rank rank = entry.Разряды[r];
                    string comma = r < entry.Разряды.Length - 1 ? "," : "";
                    lines.Add($"      {{\"Разряд\": {JsString(rank.Разряд)}, \"Время\": {JsString(rank.Время)}}}{comma}");
                }
                lines.Add("    ]");
                lines.Add(e < entries.Length - 1 ? "  }," : "  }");
            }
            lines.Add("];");
            if (i < files.Count - 1)
                lines.Add("");
        }

        string content = string.Join("\r\n", lines) + "\r\n";
        string fullOutput = Path.GetFullPath(outputPath);

        if (File.Exists(fullOutput) && File.ReadAllText(fullOutput, Encoding.UTF8) == content)
        {
            Console.WriteLine($"{fullOutput}: без изменений.");
            return 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullOutput)!);
        File.WriteAllText(fullOutput, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Записан {fullOutput}");
        return 0;
    }

    private static void Validate(Entry[] entries, string file)
    {
        string fileName = Path.GetFileName(file);
        var seen = new HashSet<(string Дистанция, string Хронометраж, string? Круг)>();

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            string where = $"{fileName}, запись {i + 1} ({entry.Дистанция})";

            if (string.IsNullOrWhiteSpace(entry.Дистанция))
                throw new InvalidDataException($"{where}: пустая «Дистанция»");
            if (entry.Хронометраж is not ("ручной" or "авто"))
                throw new InvalidDataException($"{where}: «Хронометраж» должен быть «ручной» или «авто», получено «{entry.Хронометраж}»");
            if (entry.Круг is not null && string.IsNullOrWhiteSpace(entry.Круг))
                throw new InvalidDataException($"{where}: пустая «Круг» (ожидается null или значение вида «400м»)");
            if (entry.Разряды is null || entry.Разряды.Length == 0)
                throw new InvalidDataException($"{where}: пустой список «Разряды»");

            foreach (Rank rank in entry.Разряды)
            {
                if (string.IsNullOrWhiteSpace(rank.Разряд) || string.IsNullOrWhiteSpace(rank.Время))
                    throw new InvalidDataException($"{where}: пустые «Разряд» или «Время»");
            }

            if (!seen.Add((entry.Дистанция, entry.Хронометраж, entry.Круг)))
                throw new InvalidDataException($"{where}: дубликат ключа (Дистанция, Хронометраж, Круг)");
        }
    }

    private static string VariableName(string file)
    {
        // woman-stadium.json -> womanStadiumData
        string[] parts = Path.GetFileNameWithoutExtension(file)
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string name = parts[0].ToLowerInvariant()
            + string.Concat(parts.Skip(1).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
        return name + "Data";
    }

    private static string JsString(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\u2028': sb.Append("\\u2028"); break;
                case '\u2029': sb.Append("\\u2029"); break;
                default:
                    if (c < 0x20)
                        sb.Append($"\\u{(int)c:x4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            DataGen — генератор src/data.js из JSON-файлов каталога data-sets/.

            Использование (из корня проекта):
              dotnet run --project tools/DataGen [-- <каталогJSON> <выходнойJS>]

            По умолчанию: <каталогJSON> = data-sets, <выходнойJS> = src/data.js
            (пути относительны текущему каталогу).

            Каждый файл <имя>.json превращается в const <имяCamelCase>Data.
            Выходной файл перезаписывается только при изменении содержимого.
            """);
    }
}
