using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Utilities.Editor
{
    /// <summary>
    /// Static utility class for exporting analytics data to CSV format.
    /// Handles file creation, data formatting, and encoding.
    /// </summary>
    public static class AnalyticsCsvExporter
    {
        /// <summary>
        /// Exports all line data to a timestamped CSV file.
        /// Format: SettingID, Mass, InkCost, IsOutlier, CreationTime, and all advanced metrics.
        /// </summary>
        /// <param name="data">List of LineData to export</param>
        public static void ExportToCSV(List<LineData> data)
        {
            if (data == null || data.Count == 0)
            {
                EditorUtility.DisplayDialog("Export Failed", "No data to export. Please refresh data first.", "OK");
                return;
            }

            // Generate timestamped filename
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"PhysicsAnalytics_{timestamp}.csv";

            // Get save path (default to project root)
            string defaultPath = Application.dataPath.Replace("/Assets", "");
            string filePath = EditorUtility.SaveFilePanel("Export Physics Analytics Data", defaultPath, fileName, "csv");

            if (string.IsNullOrEmpty(filePath))
            {
                return; // User cancelled
            }

            try
            {
                StringBuilder csv = new StringBuilder();

                // CSV Header (includes all advanced metrics)
                csv.AppendLine("SettingID,Mass,InkCost,IsOutlier,CreationTime,Length,PointCount,Straightness,COM_X,COM_Y,COM_Z,VelocityMagnitude,AngularVelocity,BoundsArea");

                // CSV Data (sorted by SettingID, then by CreationTime)
                var sortedData = data
                    .OrderBy(ld => ld.settingID)
                    .ThenBy(ld => ld.creationTime)
                    .ToList();

                foreach (LineData lineData in sortedData)
                {
                    // Format all fields including advanced metrics
                    csv.AppendLine(
                        $"{EscapeCSVField(lineData.settingID)}," +
                        $"{lineData.mass:F3}," +
                        $"{lineData.inkCost:F1}," +
                        $"{lineData.isOutlier}," +
                        $"{lineData.creationTime:F2}," +
                        $"{lineData.length:F3}," +
                        $"{lineData.pointCount}," +
                        $"{lineData.straightness:F3}," +
                        $"{lineData.centerOfMass.x:F3}," +
                        $"{lineData.centerOfMass.y:F3}," +
                        $"{lineData.centerOfMass.z:F3}," +
                        $"{lineData.velocityMagnitude:F3}," +
                        $"{lineData.angularVelocity:F3}," +
                        $"{lineData.boundsArea:F3}"
                    );
                }

                // Write to file with UTF-8 BOM encoding (so Excel opens special characters correctly)
                File.WriteAllText(filePath, csv.ToString(), new UTF8Encoding(true));

                EditorUtility.DisplayDialog("Export Successful",
                    $"Data exported to:\n{filePath}\n\n{data.Count} lines exported.", "OK");

                // Open the file location in file explorer
                EditorUtility.RevealInFinder(filePath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed",
                    $"Error exporting data:\n{ex.Message}", "OK");
                Debug.LogError($"AnalyticsCsvExporter: CSV export failed. {ex}");
            }
        }

        /// <summary>
        /// Escapes CSV field values to handle commas and quotes.
        /// </summary>
        /// <param name="field">The field value to escape</param>
        /// <returns>Escaped CSV field value</returns>
        private static string EscapeCSVField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }
    }
}

