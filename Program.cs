using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class project1
{
    static async Task<int> Main(string[] args)

    {
        var rootCommand = new RootCommand("אפליקציית CLI עם פקודת bundle");
        rootCommand.AddCommand(CreateBundleCommand());

        return await rootCommand.InvokeAsync(args);
    }

    static Command CreateBundleCommand()
    {
        var bundleCommand = new Command("bundle", "איחוד קבצי קוד לקובץ אחד")
        {
            new Option<List<string>>(
                "--language",
                description: "רשימת שפות תכנות. אם נבחר all, כל קבצי הקוד ייכללו.",
                getDefaultValue: () => new List<string> { "all" }),

            new Option<FileInfo>(
                "--output",
                "שם קובץ ה-bundle המיוצא. חובה לציין נתיב תקין."),

            new Option<bool>(
                "--note",
                "האם לרשום את מקור הקוד כהערה בקובץ ה-bundle."),

            new Option<string>(
                "--sort",
                () => "filename",
                "סדר העתקת קבצי הקוד, לפי א\"ב של שם הקובץ או לפי סוג הקוד (type)."),

            new Option<bool>(
                "--remove-empty-lines",
                "האם למחוק שורות ריקות."),

            new Option<string>(
                "--author",
                "רישום שם יוצר הקובץ.")
        };

        bundleCommand.Handler = CommandHandler.Create<List<string>, FileInfo, bool, string, bool, string>(
            (language, output, note, sort, removeEmptyLines, author) =>
            {
                if (output == null)
                {
                    Console.WriteLine("שגיאה: חובה לציין את נתיב הקובץ באמצעות --output.");
                    return;
                }

                try
                {
                    Directory.CreateDirectory(output.DirectoryName);

                    var supportedLanguages = language.Contains("all", StringComparer.OrdinalIgnoreCase)
                        ? new[] { "*.cs", "*.java", "*.py", "*.js" }
                        : language.Select(ext => $"*.{ext}").ToArray();

                    var codeFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
                        .Where(f => supportedLanguages.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    codeFiles = sort == "type"
                        ? codeFiles.OrderBy(f => Path.GetExtension(f)).ThenBy(f => Path.GetFileName(f)).ToList()
                        : codeFiles.OrderBy(f => Path.GetFileName(f)).ToList();

                    using (var writer = new StreamWriter(output.FullName))
                    {
                        if (!string.IsNullOrEmpty(author))
                        {
                            writer.WriteLine($"// Author: {author}");
                        }

                        foreach (var file in codeFiles)
                        {
                            if (note)
                            {
                                writer.WriteLine($"// מקור הקוד: {file}");
                            }

                            var lines = File.ReadAllLines(file);
                            if (removeEmptyLines)
                            {
                                lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                            }

                            foreach (var line in lines)
                            {
                                writer.WriteLine(line);
                            }

                            writer.WriteLine();
                        }
                    }

                    Console.WriteLine($"הקבצים נארזו בהצלחה לקובץ: {output.FullName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"שגיאה באריזת הקבצים: {ex.Message}");
                }
            });

        return bundleCommand;
    }
}