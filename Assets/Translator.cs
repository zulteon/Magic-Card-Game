using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Translator
{
    private const string Separator = "|||";

    private static readonly Dictionary<string, string> translations = new();

    public static string CurrentLanguage { get; private set; } = "en";

    public static void LoadLanguage(string languageCode)
    {
        CurrentLanguage = languageCode;
        translations.Clear();

        // Az angol az eredeti nyelv, ezért nem kell fordítási fájl.
        if (languageCode == "en")
            return;

        string path = Path.Combine(
            Application.streamingAssetsPath,
            "Translations",
            $"{languageCode}.txt"
        );

        if (!File.Exists(path))
        {
            Debug.LogWarning(
                $"Translation file was not found: {path}"
            );

            return;
        }

        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("#"))
                continue;

            int separatorIndex = line.IndexOf(Separator);

            if (separatorIndex <= 0)
            {
                Debug.LogWarning(
                    $"Invalid translation line: {line}"
                );

                continue;
            }

            string originalText = line
                .Substring(0, separatorIndex)
                .Trim();

            string translatedText = line
                .Substring(separatorIndex + Separator.Length)
                .Trim()
                .Replace("\\n", "\n");

            if (!translations.TryAdd(originalText, translatedText))
            {
                Debug.LogWarning(
                    $"Duplicate translation: {originalText}"
                );
            }
        }

        Debug.Log(
            $"Loaded {translations.Count} translations for: {languageCode}"
        );
    }

    public static string Translate(string originalText)
    {
        if (string.IsNullOrEmpty(originalText))
            return originalText;

        if (CurrentLanguage == "en")
            return originalText;

        if (translations.TryGetValue(
                originalText,
                out string translatedText
            ))
        {
            return translatedText;
        }

        // Ha hiányzik a fordítás, az angol eredeti jelenik meg.
        return originalText;
    }
}
/*
 * private void Awake()
{
    Translator.LoadLanguage("hu");
}*/