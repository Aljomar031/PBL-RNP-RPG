using System;
using System.Globalization;
using UnityEngine;

public class SaveDateFormatter : MonoBehaviour
{
    public string FormatDate(string timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return "Empty";

        if (DateTimeOffset.TryParse(
                timestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset savedAt))
        {
            return savedAt
                .ToLocalTime()
                .ToString("MMM dd, yyyy — h:mm tt", CultureInfo.InvariantCulture);
        }

        return "Empty";
    }
}