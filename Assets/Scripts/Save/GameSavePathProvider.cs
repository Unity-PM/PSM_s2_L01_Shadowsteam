using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GameSavePathProvider
{
    const string SaveFolderName = "Shadowsteam";
    const string LegacyFolderName = "Shadowsteam_Mykyta";

    static readonly HashSet<string> migratedFiles = new HashSet<string>();

    public static string SaveDirectory
    {
        get
        {
            string directory = ResolveSaveDirectory();
            Directory.CreateDirectory(directory);
            return directory;
        }
    }

    public static string GetSavePath(string fileName)
    {
        string destinationPath = Path.Combine(SaveDirectory, fileName);
        TryMigrateLegacySave(fileName, destinationPath);
        return destinationPath;
    }

    static string ResolveSaveDirectory()
    {
        string unityPath = Application.persistentDataPath;
        DirectoryInfo parent = Directory.GetParent(unityPath);
        return parent != null ? Path.Combine(parent.FullName, SaveFolderName) : unityPath;
    }

    static void TryMigrateLegacySave(string fileName, string destinationPath)
    {
        if (string.IsNullOrEmpty(fileName) || migratedFiles.Contains(fileName) || File.Exists(destinationPath))
            return;

        migratedFiles.Add(fileName);

        foreach (string legacyDirectory in GetLegacyDirectories())
        {
            string sourcePath = Path.Combine(legacyDirectory, fileName);
            if (PathsMatch(sourcePath, destinationPath) || !File.Exists(sourcePath))
                continue;

            try
            {
                File.Move(sourcePath, destinationPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save migration failed for '{fileName}': {e.Message}");
            }

            return;
        }
    }

    static IEnumerable<string> GetLegacyDirectories()
    {
        string unityPath = Application.persistentDataPath;
        if (!PathsMatch(unityPath, SaveDirectory))
            yield return unityPath;

        DirectoryInfo parent = Directory.GetParent(unityPath);
        if (parent == null)
            yield break;

        string legacyPath = Path.Combine(parent.FullName, LegacyFolderName);
        if (!PathsMatch(legacyPath, SaveDirectory))
            yield return legacyPath;
    }

    static bool PathsMatch(string a, string b)
    {
        return string.Equals(
            Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
