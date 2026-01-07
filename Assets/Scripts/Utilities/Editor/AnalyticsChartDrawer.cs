using System.Collections.Generic;
using System.Linq;
using Drawing.LineControl;
using UnityEditor;
using UnityEngine;

namespace Utilities.Editor
{
    /// <summary>
    /// Static utility class for drawing analytics charts in the Editor Window.
    /// Handles all chart visualization logic including mass distribution charts and timeline histograms.
    /// </summary>
    public static class AnalyticsChartDrawer
    {
        private const float CHART_PADDING = 5f;

        /// <summary>
        /// Draws a professional interactive bar chart showing mass distribution for a SettingID group.
        /// Features: Rounded container, grid lines, axis labels, gradient bars, hover tooltips, and click-to-select.
        /// Uses pre-calculated chart data for optimal performance.
        /// </summary>
        /// <param name="group">The SettingIDGroup containing chart data</param>
        /// <param name="windowWidth">Width of the editor window (for fallback rect calculation)</param>
        /// <param name="requestRepaint">Optional callback to request window repaint (for tooltip updates)</param>
        public static void DrawMassDistributionChart(SettingIDGroup group, float windowWidth, System.Action requestRepaint = null)
        {
            ChartData chartData = group.chartData;
            if (!chartData.isValid || chartData.normalizedHeights.Count == 0 || chartData.maxMass <= 0f)
                return;

            GUIStyle chartLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 4, 4)
            };
            EditorGUILayout.LabelField("Mass Distribution", chartLabelStyle);

            // Get chart rect with extra space for axis labels
            const float axisLabelWidth = 60f;
            const float chartPadding = 8f;
            const float chartHeight = 80f;
            const float labelGap = 4f; // Gap between label and chart

            // Reserve space for the chart using GetControlRect (properly integrated with GUILayout)
            // This automatically positions it after the label in the GUILayout flow
            Rect fullRect = EditorGUILayout.GetControlRect(false, chartHeight + chartPadding * 2 + labelGap, GUILayout.ExpandWidth(true));

            // Adjust the rect to add gap after label and reduce height accordingly
            fullRect.y += labelGap;
            fullRect.height = chartHeight + chartPadding * 2;

            // Chart container (rounded box)
            Rect containerRect = new Rect(
                fullRect.x + chartPadding,
                fullRect.y + chartPadding,
                fullRect.width - chartPadding * 2,
                chartHeight
            );

            // Chart drawing area (inside container, with space for axis labels)
            Rect chartRect = new Rect(
                containerRect.x + axisLabelWidth + chartPadding,
                containerRect.y + chartPadding,
                containerRect.width - axisLabelWidth - chartPadding * 2,
                containerRect.height - chartPadding * 2
            );

            // Only draw during Repaint event (GUILayoutUtility.GetRect was called during Layout)
            if (Event.current.type == EventType.Repaint)
            {
                // Draw rounded container background
                DrawRoundedBox(containerRect, GetBackgroundColor());

                // Draw grid lines
                DrawGridLines(chartRect, chartData.maxMass);

                // Draw axis labels
                DrawAxisLabels(chartRect, chartData.maxMass, axisLabelWidth);
            }

            // Reset hover state at start of each chart
            bool isHoveringThisChart = false;
            Line hoveredLineInThisChart = null;
            float hoveredMassInThisChart = 0f;
            Vector2 tooltipPosInThisChart = Vector2.zero;

            // Get mouse position relative to the chart (works in all events)
            Vector2 mousePos = Event.current.mousePosition;
            bool mouseInChart = chartRect.Contains(mousePos);

            // Use pre-calculated chart data
            int barCount = chartData.normalizedHeights.Count;
            float barWidth = chartRect.width / barCount;

            // Only draw during Repaint event
            if (Event.current.type == EventType.Repaint)
            {
                for (int i = 0; i < barCount; i++)
                {
                    float normalizedHeight = chartData.normalizedHeights[i];
                    float barHeight = normalizedHeight * (chartRect.height - CHART_PADDING * 2);

                    Rect barRect = new Rect(
                        chartRect.x + i * barWidth,
                        chartRect.y + chartRect.height - barHeight - CHART_PADDING,
                        barWidth - 1f,
                        barHeight
                    );

                    // Use pre-calculated color
                    Color baseColor = chartData.barColors[i];

                    // Draw bar with gradient
                    DrawGradientBar(barRect, baseColor);
                }

                // Draw average line
                if (group.averageMass > 0f && chartData.maxMass > 0f)
                {
                    float avgNormalized = group.averageMass / chartData.maxMass;
                    float avgY = chartRect.y + chartRect.height - (avgNormalized * (chartRect.height - CHART_PADDING * 2)) - CHART_PADDING;

                    EditorGUI.DrawRect(
                        new Rect(chartRect.x, avgY, chartRect.width, 1f),
                        new Color(1f, 1f, 0f, 0.8f) // Yellow line for average
                    );
                }
            }

            // Handle mouse interaction (works in all events) - using pre-calculated data
            if (mouseInChart)
            {
                for (int i = 0; i < barCount; i++)
                {
                    Line line = chartData.chartLines[i];
                    if (line == null)
                        continue;

                    try
                    {
                        float normalizedHeight = chartData.normalizedHeights[i];
                        float barHeight = normalizedHeight * (chartRect.height - CHART_PADDING * 2);

                        Rect barRect = new Rect(
                            chartRect.x + i * barWidth,
                            chartRect.y + chartRect.height - barHeight - CHART_PADDING,
                            barWidth - 1f,
                            barHeight
                        );

                        if (barRect.Contains(mousePos))
                        {
                            isHoveringThisChart = true;
                            hoveredLineInThisChart = line;

                            // Get mass from line or use pre-calculated tooltip
                            try
                            {
                                if (line.rigidBody != null)
                                {
                                    hoveredMassInThisChart = line.rigidBody.mass;
                                }
                                else
                                {
                                    // Fallback: extract from tooltip text
                                    string tooltipText = chartData.tooltipTexts[i];
                                    if (tooltipText.StartsWith("Mass: "))
                                    {
                                        string massStr = tooltipText.Substring(6);
                                        float.TryParse(massStr, out hoveredMassInThisChart);
                                    }
                                }
                            }
                            catch
                            {
                                hoveredMassInThisChart = 0f;
                            }

                            tooltipPosInThisChart = mousePos;

                            // Handle mouse click - select the Line GameObject
                            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                            {
                                try
                                {
                                    if (line.gameObject != null)
                                    {
                                        Selection.activeGameObject = line.gameObject;
                                        EditorGUIUtility.PingObject(line.gameObject);
                                        Event.current.Use(); // Consume the event to prevent other handlers
                                    }
                                }
                                catch
                                {
                                    // Line was destroyed, ignore
                                }
                            }

                            // Change cursor to indicate clickability
                            EditorGUIUtility.AddCursorRect(barRect, MouseCursor.Link);
                            break;
                        }
                    }
                    catch
                    {
                        // Skip invalid lines
                        continue;
                    }
                }

                // Draw tooltip on hover (during Repaint)
                if (Event.current.type == EventType.Repaint && isHoveringThisChart && hoveredLineInThisChart != null)
                {
                    DrawTooltip(tooltipPosInThisChart, hoveredMassInThisChart);
                }
            }

            // Request repaint on mouse move for smooth tooltip updates
            if (Event.current.type == EventType.MouseMove && mouseInChart && requestRepaint != null)
            {
                requestRepaint();
            }

            // Legend - use GUILayout for proper positioning
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(axisLabelWidth + chartPadding);

            GUIStyle legendStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                padding = new RectOffset(4, 4, 0, 0)
            };

            // Normal color indicator
            Rect normalRect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(normalRect, new Color(0.3f, 0.8f, 0.3f, 1f));
            }
            EditorGUILayout.LabelField("Normal", legendStyle, GUILayout.Width(50));

            // Outlier color indicator
            Rect outlierRect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(outlierRect, new Color(1f, 0.3f, 0.3f, 1f));
            }
            EditorGUILayout.LabelField("Outlier", legendStyle, GUILayout.Width(50));

            // Average line indicator
            Rect avgRect = GUILayoutUtility.GetRect(12, 2, GUILayout.Width(12));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(avgRect, new Color(1f, 1f, 0f, 1f));
            }
            EditorGUILayout.LabelField("Average", legendStyle, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws a histogram showing Total Mass created over Time (X-axis: Time, Y-axis: Total Mass).
        /// Helps identify difficulty/performance spikes at specific moments.
        /// </summary>
        /// <param name="timeBuckets">Grouped line snapshots by time buckets</param>
        /// <param name="bucketSize">Size of each time bucket in seconds</param>
        /// <param name="maxTime">Maximum time value for the histogram</param>
        /// <param name="windowWidth">Width of the editor window (for fallback rect calculation)</param>
        /// <param name="formatTimeCallback">Callback function to format time values for display</param>
        public static void DrawTimelineHistogram(
            List<System.Linq.IGrouping<int, LineSnapshot>> timeBuckets,
            float bucketSize,
            float maxTime,
            float windowWidth,
            System.Func<float, string> formatTimeCallback)
        {
            if (timeBuckets == null || timeBuckets.Count == 0)
                return;

            EditorGUILayout.BeginVertical("box");

            GUIStyle chartLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 4, 4)
            };
            EditorGUILayout.LabelField("Mass Over Time Histogram", chartLabelStyle);

            const float chartHeight = 120f;
            const float chartPadding = 8f;
            const float axisLabelWidth = 60f;
            const float labelGap = 4f; // Gap between label and chart

            // Use GetControlRect to properly reserve space in GUILayout
            // This automatically positions it after the label in the GUILayout flow
            Rect fullRect = EditorGUILayout.GetControlRect(false, chartHeight + chartPadding * 2 + labelGap, GUILayout.ExpandWidth(true));

            // Adjust the rect to add gap after label and reduce height accordingly
            fullRect.y += labelGap;
            fullRect.height = chartHeight + chartPadding * 2;

            // Chart container
            Rect containerRect = new Rect(
                fullRect.x + chartPadding,
                fullRect.y + chartPadding,
                fullRect.width - chartPadding * 2,
                chartHeight
            );

            // Chart drawing area
            Rect chartRect = new Rect(
                containerRect.x + axisLabelWidth + chartPadding,
                containerRect.y + chartPadding,
                containerRect.width - axisLabelWidth - chartPadding * 2,
                containerRect.height - chartPadding * 2
            );

            // Only draw during Repaint event (GUILayoutUtility.GetRect was called during Layout)
            if (Event.current.type == EventType.Repaint)
            {
                // Draw rounded container background
                DrawRoundedBox(containerRect, GetBackgroundColor());

                // Calculate max total mass for normalization
                float maxTotalMass = timeBuckets.Max(bucket => bucket.Sum(s => s.mass));
                if (maxTotalMass <= 0f) maxTotalMass = 1f; // Avoid division by zero

                // Draw grid lines
                DrawGridLines(chartRect, maxTotalMass, true); // true = vertical grid for histogram

                // Draw axis labels (Y-axis for mass)
                DrawHistogramAxisLabels(chartRect, maxTotalMass, axisLabelWidth);

                // Draw bars for each time bucket
                float barWidth = chartRect.width / timeBuckets.Count;
                int bucketIndex = 0;

                foreach (var bucket in timeBuckets)
                {
                    float totalMass = bucket.Sum(s => s.mass);
                    float normalizedHeight = totalMass / maxTotalMass;
                    float barHeight = normalizedHeight * chartRect.height;

                    Rect barRect = new Rect(
                        chartRect.x + bucketIndex * barWidth,
                        chartRect.y + chartRect.height - barHeight,
                        barWidth - 1f,
                        barHeight
                    );

                    // Color based on mass intensity (darker = more mass)
                    float intensity = normalizedHeight;
                    Color barColor = new Color(
                        0.3f + intensity * 0.5f,  // Green component
                        0.8f - intensity * 0.3f,  // Red component (less red for higher mass)
                        0.3f + intensity * 0.2f,   // Blue component
                        0.9f
                    );

                    DrawGradientBar(barRect, barColor);

                    // Draw time label below each bar
                    float startTime = bucket.Key * bucketSize;
                    GUIStyle timeLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 8,
                        alignment = TextAnchor.UpperCenter,
                        normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.3f, 0.3f, 0.3f) }
                    };
                    Rect timeLabelRect = new Rect(
                        barRect.x,
                        chartRect.yMax + 2f,
                        barRect.width,
                        12f
                    );

                    // Only show time label for every 3rd bar to avoid clutter
                    if (bucketIndex % 3 == 0 || bucketIndex == timeBuckets.Count - 1)
                    {
                        string timeLabel = formatTimeCallback != null ? formatTimeCallback(startTime) : startTime.ToString("F1");
                        GUI.Label(timeLabelRect, timeLabel, timeLabelStyle);
                    }

                    bucketIndex++;
                }

                // Draw X-axis label
                GUIStyle axisLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 9
                };
                Rect xAxisLabelRect = new Rect(
                    containerRect.x + axisLabelWidth,
                    containerRect.yMax - 15f,
                    chartRect.width,
                    12f
                );
                GUI.Label(xAxisLabelRect, "Time →", axisLabelStyle);
            }

            // Reserve space for time labels below the chart
            EditorGUILayout.Space(16f); // Space for time labels (12px height + 4px margin)

            EditorGUILayout.EndVertical();
        }

        #region Chart Helper Methods

        /// <summary>
        /// Gets background color based on Unity skin (Dark/Light).
        /// </summary>
        private static Color GetBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.15f, 0.15f, 0.15f, 1f)
                : new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        /// <summary>
        /// Draws a rounded box (simulated with multiple rectangles for smooth corners).
        /// </summary>
        private static void DrawRoundedBox(Rect rect, Color color)
        {
            // Main body
            EditorGUI.DrawRect(rect, color);

            // Draw border (simulated rounded effect)
            Color borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.3f, 0.3f, 1f)
                : new Color(0.5f, 0.5f, 0.5f, 1f);

            // Top border
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), borderColor);
            // Bottom border
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), borderColor);
            // Left border
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), borderColor);
            // Right border
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), borderColor);
        }

        /// <summary>
        /// Draws grid lines (horizontal for mass charts, vertical for time histograms).
        /// </summary>
        private static void DrawGridLines(Rect chartRect, float maxValue, bool isVertical = false)
        {
            Color gridColor = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.3f, 0.3f, 0.5f)
                : new Color(0.6f, 0.6f, 0.6f, 0.5f);

            if (isVertical)
            {
                // Vertical grid lines for histogram (time divisions)
                float[] percentages = { 0.25f, 0.5f, 0.75f };
                foreach (float percentage in percentages)
                {
                    float x = chartRect.x + (percentage * chartRect.width);
                    EditorGUI.DrawRect(
                        new Rect(x, chartRect.y, 1f, chartRect.height),
                        gridColor
                    );
                }
            }
            else
            {
                // Horizontal grid lines (mass divisions)
                float[] percentages = { 0.25f, 0.5f, 0.75f, 1.0f };
                foreach (float percentage in percentages)
                {
                    float y = chartRect.y + chartRect.height - (percentage * chartRect.height);
                    EditorGUI.DrawRect(
                        new Rect(chartRect.x, y, chartRect.width, 1f),
                        gridColor
                    );
                }
            }
        }

        /// <summary>
        /// Draws axis labels on the left side of the chart (0, Max/2, Max).
        /// </summary>
        private static void DrawAxisLabels(Rect chartRect, float maxMass, float labelWidth)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.3f, 0.3f, 0.3f) }
            };

            float[] values = { 0f, maxMass * 0.5f, maxMass };
            float[] percentages = { 0f, 0.5f, 1.0f };

            for (int i = 0; i < values.Length; i++)
            {
                float y = chartRect.y + chartRect.height - (percentages[i] * chartRect.height);
                Rect labelRect = new Rect(
                    chartRect.x - labelWidth,
                    y - 6f,
                    labelWidth - 4f,
                    12f
                );

                string labelText = values[i].ToString("F2");
                GUI.Label(labelRect, labelText, labelStyle);
            }
        }

        /// <summary>
        /// Draws Y-axis labels for the histogram (mass values).
        /// </summary>
        private static void DrawHistogramAxisLabels(Rect chartRect, float maxMass, float labelWidth)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.3f, 0.3f, 0.3f) }
            };

            float[] percentages = { 0f, 0.25f, 0.5f, 0.75f, 1.0f };
            float[] values = { 0f, maxMass * 0.25f, maxMass * 0.5f, maxMass * 0.75f, maxMass };

            for (int i = 0; i < values.Length; i++)
            {
                float y = chartRect.y + chartRect.height - (percentages[i] * chartRect.height);
                Rect labelRect = new Rect(
                    chartRect.x - labelWidth,
                    y - 6f,
                    labelWidth - 4f,
                    12f
                );

                string labelText = values[i].ToString("F1");
                GUI.Label(labelRect, labelText, labelStyle);
            }
        }

        /// <summary>
        /// Draws a bar with vertical gradient (darker at bottom, brighter at top) for 3D effect.
        /// </summary>
        private static void DrawGradientBar(Rect barRect, Color baseColor)
        {
            if (barRect.height <= 0f) return;

            const int gradientSteps = 8; // Number of gradient segments
            float stepHeight = barRect.height / gradientSteps;

            for (int i = 0; i < gradientSteps; i++)
            {
                float t = (float)i / gradientSteps; // 0 to 1 from bottom to top

                // Gradient: darker at bottom (t=0), brighter at top (t=1)
                float brightness = 0.6f + (t * 0.4f); // 60% to 100% brightness

                Color gradientColor = new Color(
                    baseColor.r * brightness,
                    baseColor.g * brightness,
                    baseColor.b * brightness,
                    baseColor.a
                );

                Rect segmentRect = new Rect(
                    barRect.x,
                    barRect.y + (i * stepHeight),
                    barRect.width,
                    stepHeight + 0.5f // Slight overlap to avoid gaps
                );

                EditorGUI.DrawRect(segmentRect, gradientColor);
            }

            // Add subtle highlight on top edge for 3D effect
            if (barRect.height > 2f)
            {
                Color highlightColor = new Color(
                    Mathf.Min(1f, baseColor.r + 0.15f),
                    Mathf.Min(1f, baseColor.g + 0.15f),
                    Mathf.Min(1f, baseColor.b + 0.15f),
                    0.8f
                );
                EditorGUI.DrawRect(
                    new Rect(barRect.x, barRect.y, barRect.width, 1f),
                    highlightColor
                );
            }
        }

        /// <summary>
        /// Draws a tooltip showing the mass value when hovering over a bar.
        /// </summary>
        private static void DrawTooltip(Vector2 position, float mass)
        {
            string tooltipText = $"Mass: {mass:F3}";
            GUIStyle tooltipStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 10,
                padding = new RectOffset(4, 4, 2, 2)
            };

            Vector2 textSize = tooltipStyle.CalcSize(new GUIContent(tooltipText));
            Rect tooltipRect = new Rect(
                position.x + 10f, // Offset to the right of cursor
                position.y - textSize.y - 5f, // Above cursor
                textSize.x + 8f,
                textSize.y + 4f
            );

            // Ensure tooltip stays within window bounds
            if (tooltipRect.xMax > position.x + 200f) // If too far right, show to the left
            {
                tooltipRect.x = position.x - tooltipRect.width - 10f;
            }
            if (tooltipRect.yMin < 0f) // If too high, show below cursor
            {
                tooltipRect.y = position.y + 5f;
            }

            // Draw tooltip background
            EditorGUI.DrawRect(tooltipRect, new Color(0.2f, 0.2f, 0.2f, 0.95f));

            // Draw tooltip border
            EditorGUI.DrawRect(new Rect(tooltipRect.x, tooltipRect.y, tooltipRect.width, 1f), Color.white);
            EditorGUI.DrawRect(new Rect(tooltipRect.x, tooltipRect.yMax - 1f, tooltipRect.width, 1f), Color.white);
            EditorGUI.DrawRect(new Rect(tooltipRect.x, tooltipRect.y, 1f, tooltipRect.height), Color.white);
            EditorGUI.DrawRect(new Rect(tooltipRect.xMax - 1f, tooltipRect.y, 1f, tooltipRect.height), Color.white);

            // Draw tooltip text
            GUI.Label(tooltipRect, tooltipText, tooltipStyle);
        }

        #endregion
    }
}

