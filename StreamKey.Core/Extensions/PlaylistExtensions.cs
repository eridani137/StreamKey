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
        // Разделяем по строкам и очищаем
        var lines = m3U8Content
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToArray();

        var resultLines = new List<string>();
        var currentDateTime = DateTime.MinValue;
        var lastAddedLine = "";
        var segmentsRemoved = 0;

        // Собираем все рекламные диапазоны времени
        var adDateRanges = ExtractAdDateRanges(lines);

        Log.Debug("Найдено {Count} рекламных диапазонов времени", adDateRanges.Count);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Обновляем текущее время из тега PROGRAM-DATE-TIME
            if (line.StartsWith("#EXT-X-PROGRAM-DATE-TIME:"))
            {
                currentDateTime = ParseProgramDateTime(line);
                AddLineToResult(line);
                continue;
            }

            // Удаляем теги предзагрузки Twitch
            if (IsTwitchPrefetchTag(line))
            {
                Log.Debug("Пропускаем Twitch PREFETCH тег: {Line}", line);
                // Пропускаем следующую строку, если это URI
                if (i + 1 < lines.Length && !lines[i + 1].StartsWith('#'))
                {
                    i++;
                }

                continue;
            }

            // Удаляем рекламные DATERANGE теги
            if (IsAdDateRangeTag(line))
            {
                Log.Debug("Удаляем рекламный DATERANGE: {Line}", line);
                continue;
            }

            // Обработка сегментов (#EXTINF + URI)
            if (line.StartsWith("#EXTINF:"))
            {
                var segmentInfo = ParseExtInfSegment(line, i < lines.Length - 1 ? lines[i + 1] : null);

                if (IsAdSegment(segmentInfo, currentDateTime, adDateRanges))
                {
                    Log.Debug("Удаляем рекламный сегмент: {Duration}s, URI: {Uri}",
                        segmentInfo.Duration, segmentInfo.Uri ?? "<none>");

                    segmentsRemoved++;

                    // Пропускаем EXTINF и URI
                    if (segmentInfo.Uri != null) i++; // Пропускаем следующую строку с URI

                    // Обновляем время для корректного определения следующих сегментов
                    UpdateCurrentTime(segmentInfo.Duration);
                    continue;
                }

                // Добавляем контентный сегмент
                AddLineToResult(line);
                
                if (segmentInfo.Uri != null) AddLineToResult(segmentInfo.Uri);
                
                UpdateCurrentTime(segmentInfo.Duration);
                continue;
            }

            // Обработка разрывов - избегаем дублирования
            if (line.StartsWith("#EXT-X-DISCONTINUITY"))
            {
                if (!IsLastLineDiscontinuity())
                {
                    AddLineToResult(line);
                }

                continue;
            }

            // Все остальные строки добавляем
            AddLineToResult(line);
        }

        Log.Debug("Удалено {Count} рекламных сегментов", segmentsRemoved);
        return string.Join("\n", resultLines);


        void AddLineToResult(string lineToAdd)
        {
            resultLines.Add(lineToAdd);
            lastAddedLine = lineToAdd;
        }

        void UpdateCurrentTime(double duration)
        {
            if (currentDateTime != DateTime.MinValue && duration > 0)
            {
                currentDateTime = currentDateTime.AddSeconds(duration);
            }
        }

        bool IsLastLineDiscontinuity()
        {
            return lastAddedLine.Equals("#EXT-X-DISCONTINUITY", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static List<AdDateRangeInfo> ExtractAdDateRanges(string[] lines)
    {
        return lines
            .Where(IsAdDateRangeTag)
            .Select(ParseDateRangeInfo)
            .Where(range => range.IsValid)
            .ToList();
    }

    private static DateTime ParseProgramDateTime(string line)
    {
        var dateStr = line["#EXT-X-PROGRAM-DATE-TIME:".Length..].Trim();

        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
        {
            return dt;
        }

        if (DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        {
            return dto.UtcDateTime;
        }

        Log.Warning("Не удалось распарсить дату: {DateString}", dateStr);
        return DateTime.MinValue;
    }

    private static bool IsTwitchPrefetchTag(string line)
    {
        return line.StartsWith("#EXT-X-TWITCH-PREFETCH:", StringComparison.OrdinalIgnoreCase) ||
               line.StartsWith("#EXT-X-TWITCH-LIVE-PREFETCH:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAdDateRangeTag(string line)
    {
        if (!line.StartsWith("#EXT-X-DATERANGE")) return false;

        return line.Contains("CLASS=\"twitch-stitched-ad\"", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("CLASS=\"twitch-ads\"", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("ID=\"stitched-ad-", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("X-TV-TWITCH-AD-", StringComparison.OrdinalIgnoreCase) ||
               TwitchAdAttribute().IsMatch(line);
    }

    private static ExtInfSegmentInfo ParseExtInfSegment(string extinf, string? nextLine)
    {
        var duration = ExtractDurationFromExtInf(extinf);
        var title = extinf.Contains(',') ? extinf.Split(',', 2)[1].Trim() : "";
        var uri = nextLine != null && !nextLine.StartsWith('#') ? nextLine : null;

        return new ExtInfSegmentInfo
        {
            Duration = duration,
            Title = title,
            Uri = uri
        };
    }

    private static bool IsAdSegment(ExtInfSegmentInfo segment, DateTime currentTime, List<AdDateRangeInfo> adRanges)
    {
        // Проверка по временным диапазонам
        if (IsSegmentInAdDateRange(currentTime, adRanges))
        {
            return true;
        }

        // Проверка по заголовку EXTINF
        if (IsAdByTitle(segment.Title))
        {
            return true;
        }

        // Проверка по URI
        if (!string.IsNullOrEmpty(segment.Uri) && IsAdByUri(segment.Uri))
        {
            return true;
        }

        return false;
    }

    private static bool IsAdByTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return false;

        return title.Contains("Amazon", StringComparison.OrdinalIgnoreCase) ||
               title.Contains("advertisement", StringComparison.OrdinalIgnoreCase) ||
               title.Contains("commercial", StringComparison.OrdinalIgnoreCase) ||
               AdContentPattern().IsMatch(title);
    }

    private static bool IsAdByUri(string uri)
    {
        return AdSegmentPattern().IsMatch(uri) ||
               uri.Contains("/ads/", StringComparison.OrdinalIgnoreCase) ||
               uri.Contains("advertisement", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSegmentInAdDateRange(DateTime segmentTime, List<AdDateRangeInfo> adDateRanges)
    {
        if (segmentTime == DateTime.MinValue || !adDateRanges.Any())
            return false;

        return adDateRanges.Any(range => range.Contains(segmentTime));
    }

    private static double ExtractDurationFromExtInf(string extinf)
    {
        var match = ExtInfDurationPattern().Match(extinf);
        if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture,
                out var duration))
        {
            return duration;
        }

        return 0;
    }

    private static AdDateRangeInfo ParseDateRangeInfo(string dateRangeLine)
    {
        var info = new AdDateRangeInfo();

        // Парсим START-DATE
        info.StartDate = ParseAttributeDateTime(dateRangeLine, "START-DATE");

        // Парсим END-DATE или вычисляем через DURATION
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
        var pattern = $"""
                       {Regex.Escape(attributeName)}="([^"]+)"
                       """;
        var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);

        if (!match.Success) return DateTime.MinValue;

        var dateString = match.Groups[1].Value;

        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
            return result;

        if (DateTimeOffset.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var dto))
            return dto.UtcDateTime;

        return DateTime.MinValue;
    }

    private static double? ParseAttributeDouble(string line, string attributeName)
    {
        var pattern = $@"{Regex.Escape(attributeName)}=([^,\s]+)";
        var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);

        if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture,
                out var result))
            return result;

        return null;
    }


    [GeneratedRegex(@"X-TV-TWITCH-AD-[\w-]+=", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TwitchAdAttribute();

    [GeneratedRegex(@"(?:amazon|twitch-ad|stitched-ad|doubleclick|googleads)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AdSegmentPattern();

    [GeneratedRegex(@"(?:ad|advertisement|commercial|sponsor)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AdContentPattern();

    [GeneratedRegex(@"#EXTINF:(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExtInfDurationPattern();
    
    [GeneratedRegex(@"FRAME-RATE=(\d+(\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex FrameRatePattern();

    private record ExtInfSegmentInfo
    {
        public double Duration { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Uri { get; init; }
    }

    private class AdDateRangeInfo
    {
        public DateTime StartDate { get; set; } = DateTime.MinValue;
        public DateTime EndDate { get; set; } = DateTime.MinValue;

        public bool IsValid => StartDate != DateTime.MinValue;

        public bool Contains(DateTime time)
        {
            if (!IsValid) return false;

            // Если нет END-DATE, предполагаем стандартную длительность рекламы
            var endTime = EndDate != DateTime.MinValue ? EndDate : StartDate.AddSeconds(30);

            return time >= StartDate && time < endTime;
        }
    }
}