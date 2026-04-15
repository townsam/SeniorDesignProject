using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class HandoverLogTools
{
    private const string HandoverFileName = "README_HANDOVER.md";

    [MenuItem("Tools/Handover/Append Entry Template")]
    public static void AppendEntryTemplate()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            EditorUtility.DisplayDialog("Handover Log", "Could not resolve project root.", "OK");
            return;
        }

        string logPath = Path.Combine(projectRoot, HandoverFileName);
        string dateHeader = $"## {DateTime.Now:yyyy-MM-dd}";
        string entryTime = DateTime.Now.ToString("HH:mm");

        string content = File.Exists(logPath)
            ? File.ReadAllText(logPath)
            : "# SeniorDesign — Handover Log\n\nThis file is an append-only engineering handover log.\n";

        if (!content.Contains(dateHeader))
        {
            if (!content.EndsWith("\n"))
            {
                content += "\n";
            }

            content += $"\n## {DateTime.Now:yyyy-MM-dd}\n";
        }

        int sectionStart = content.IndexOf(dateHeader, StringComparison.Ordinal);
        int sectionEnd = content.IndexOf("\n## ", sectionStart + dateHeader.Length, StringComparison.Ordinal);
        if (sectionEnd < 0)
        {
            sectionEnd = content.Length;
        }

        string sectionText = content.Substring(sectionStart, sectionEnd - sectionStart);
        var matches = Regex.Matches(sectionText, @"^### Entry (\d{3})", RegexOptions.Multiline);
        int nextEntryNumber = 1;

        if (matches.Count > 0)
        {
            nextEntryNumber = matches
                .Cast<Match>()
                .Select(match => int.TryParse(match.Groups[1].Value, out int parsed) ? parsed : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        string entryBlock =
            $"\n\n### Entry {nextEntryNumber:000} — {entryTime}\n" +
            "**Summary**\n" +
            "- ...\n\n" +
            "**Changes**\n" +
            "- ...\n\n" +
            "**Files Updated**\n" +
            "- ...\n\n" +
            "**Validation**\n" +
            "- ...\n\n" +
            "**Next Steps**\n" +
            "- ...\n\n" +
            "**Blockers / Risks**\n" +
            "- None\n";

        string updatedContent = content.Insert(sectionEnd, entryBlock);
        File.WriteAllText(logPath, updatedContent.Replace("\n", Environment.NewLine));

        AssetDatabase.Refresh();
        InternalEditorUtility.OpenFileAtLineExternal(logPath, 1);

        EditorUtility.DisplayDialog(
            "Handover Log",
            $"Added Entry {nextEntryNumber:000} for {DateTime.Now:yyyy-MM-dd}.",
            "OK"
        );
    }
}
