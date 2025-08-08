using System.Globalization;
using System.Text.RegularExpressions;
using Serilog;

namespace StreamKey.Core.Extensions;

public static partial class PlaylistExtensions
{
    public static string RemoveAds(this string m3U8Content)
    {
        if (string.IsNullOrEmpty(m3U8Content)) return m3U8Content;

        try
        {
            return RemoveAdsFromPlaylist(m3U8Content);
        }
        catch (Exception e)
        {
            Log.Error(e, "Произошла ошибка при попытке удаления рекламы из плейлиста\n{Playlist}", m3U8Content);
        }

        return m3U8Content;
    }

    private static string RemoveAdsFromPlaylist(string m3U8Content)
    {
        var lines = m3U8Content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var resultLines = new List<string>();
        var currentDateTime = DateTime.MinValue;
        var lastSegmentWasContent = false;

        // --- Первый проход: Идентификация рекламных временных диапазонов (Date Ranges) ---
        var adDateRanges = (
                from line in lines
                where line.StartsWith("#EXT-X-DATERANGE") && IsAdDateRangeLine(line)
                select ParseDateRangeInfo(line))
            .ToList();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            // Обновляем текущее время плейлиста из тега #EXT-X-PROGRAM-DATE-TIME
            if (line.StartsWith("#EXT-X-PROGRAM-DATE-TIME:"))
            {
                DateTime.TryParse(line[25..].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out currentDateTime);
            }
            
            // Пропускаем теги, однозначно указывающие на рекламу
            if (line.StartsWith("#EXT-X-DATERANGE") && IsAdDateRangeLine(line))
            {
                continue; 
            }
            
            // Пропускаем теги предзагрузки Twitch, так как они часто связаны с рекламой
            if (line.StartsWith("#EXT-X-TWITCH-PREFETCH:"))
            {
                // Пропускаем и сам тег, и следующую за ним строку с URI
                i++; 
                continue;
            }
            
            // Обрабатываем информацию о сегменте
            if (line.StartsWith("#EXTINF:"))
            {
                // Определяем, является ли сегмент рекламным
                var segmentTitle = line.Contains(',') ? line.Split(',')[1] : "";
                var segmentDuration = ExtractDurationFromExtInf(line);

                if (IsSegmentInAdDateRange(currentDateTime, adDateRanges) || IsAdExtInf(segmentTitle))
                {
                    // Если сегмент рекламный, мы его пропускаем.
                    // Также пропускаем следующую строку, которая является URI этого сегмента.
                    i++;
                    lastSegmentWasContent = false;
                    continue;
                }
                    
                // Если это не реклама, обновляем время для следующего сегмента
                if (currentDateTime != DateTime.MinValue && segmentDuration > 0)
                {
                    currentDateTime = currentDateTime.AddSeconds(segmentDuration);
                }
            }
            
            // Обработка тегов разрыва (Discontinuity)
            if (line.StartsWith("#EXT-X-DISCONTINUITY"))
            {
                // Проверяем, нужно ли удалять этот тег.
                if (lastSegmentWasContent)
                {
                    continue; // Пропускаем лишний тег разрыва
                }
            }
            
            // Если строка не является URI рекламного сегмента, добавляем её в результат
            resultLines.Add(line);
                
            // Обновляем флаг, если последняя добавленная строка была не-комментарием (т.е. URI сегмента)
            if (!line.StartsWith('#'))
            {
                lastSegmentWasContent = true;
            }
            // Сбрасываем флаг, если это был тег, который может предшествовать рекламе
            else if (line.StartsWith("#EXT-X-DISCONTINUITY"))
            {
                lastSegmentWasContent = false;
            }
        }
        
        return string.Join("\n", resultLines);
    }
    
    private static bool IsAdExtInf(string extinfTitle)
    {
        return !string.IsNullOrEmpty(extinfTitle) &&
               (extinfTitle.Contains("Amazon", StringComparison.OrdinalIgnoreCase) ||
                AdSegment().IsMatch(extinfTitle));
    }
    
    private static bool IsSegmentInAdDateRange(DateTime segmentTime, List<AdDateRangeInfo> adDateRanges)
    {
        if (segmentTime == DateTime.MinValue || adDateRanges.Count == 0) return false;

        return adDateRanges.Any(range => range.Contains(segmentTime));
    }
        
    private static double ExtractDurationFromExtInf(string extinf)
    {
        var match = ExtInfDuration().Match(extinf);
        if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var duration))
        {
            return duration;
        }
        return 0;
    }

    private static bool IsAdDateRangeLine(string dateRangeLine)
    {
        return dateRangeLine.Contains("CLASS=\"twitch-stitched-ad\"") ||
               dateRangeLine.Contains("ID=\"stitched-ad-") ||
               TwitchAdAttribute().IsMatch(dateRangeLine);
    }

    private static AdDateRangeInfo ParseDateRangeInfo(string dateRangeLine)
    {
        var info = new AdDateRangeInfo
        {
            StartDate = ParseAttributeDateTime(dateRangeLine, "START-DATE")
        };

        var endDate = ParseAttributeDateTime(dateRangeLine, "END-DATE");
        if (endDate != DateTime.MinValue)
        {
            info.EndDate = endDate;
        }
        else
        {
            var duration = ParseAttributeDouble(dateRangeLine, "DURATION");
            if (duration.HasValue && info.StartDate != DateTime.MinValue)
            {
                info.EndDate = info.StartDate.AddSeconds(duration.Value);
            }
        }

        return info;
    }

    private static DateTime ParseAttributeDateTime(string line, string attributeName)
    {
        var match = Regex.Match(line, $"{attributeName}=\"([^\"]+)\"");
        if (match.Success && DateTime.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var result))
        {
            return result;
        }

        return DateTime.MinValue;
    }

    private static double? ParseAttributeDouble(string line, string attributeName)
    {
        var match = Regex.Match(line, $"{attributeName}=([^,]+)");
        if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture,
                out var result))
        {
            return result;
        }

        return null;
    }

    [GeneratedRegex(@"X-TV-TWITCH-AD-[\w-]+=", RegexOptions.IgnoreCase)]
    private static partial Regex TwitchAdAttribute();

    [GeneratedRegex(@"(?:amazon|twitch-ad|stitched-ad|/ads/)", RegexOptions.IgnoreCase)]
    private static partial Regex AdSegment();
    
    [GeneratedRegex(@"#EXTINF:(\d+(?:\.\d+)?)")]
    private static partial Regex ExtInfDuration();

    private class AdDateRangeInfo
    {
        public DateTime StartDate { get; set; } = DateTime.MinValue;
        public DateTime EndDate { get; set; } = DateTime.MinValue;

        public bool Contains(DateTime time)
        {
            if (StartDate == DateTime.MinValue || EndDate == DateTime.MinValue)
            {
                // Если диапазон неполный, но существует, считаем что реклама идет ~5 минут
                return time >= StartDate && time < StartDate.AddMinutes(5);
            }

            return time >= StartDate && time < EndDate;
        }
    }
}