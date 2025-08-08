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
        // Разделяем по строкам (удаляем пустые строки)
        var lines = m3U8Content
            .Split(['\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r')) // убрать возможный CR
            .ToArray();

        var resultLines = new List<string>();
        var currentDateTime = DateTime.MinValue;
        var lastSegmentWasContent = false;

        // --- Первый проход: сбор DATERANGE (рекламных) --- 
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
                var dateStr = line["#EXT-X-PROGRAM-DATE-TIME:".Length..].Trim();
                if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                {
                    currentDateTime = dt;
                }
                else if (DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                {
                    currentDateTime = dto.UtcDateTime;
                }
                // добавляем в результат — PROGRAM-DATE-TIME полезно сохранить
                resultLines.Add(line);
                lastSegmentWasContent = false;
                continue;
            }

            // Пропускаем теги предзагрузки Twitch (вместе с URI)
            if (line.StartsWith("#EXT-X-TWITCH-PREFETCH:", StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug("Пропускаем TWITCH PREFETCH тег.");
                // пропускаем сам тег; следующий может быть URI (или нет) — безопасно пропустить одну строку
                if (i + 1 < lines.Length && !lines[i + 1].StartsWith('#')) i++;
                continue;
            }

            // Пропускаем сами DATERANGE теги, которые помечены как реклама
            if (line.StartsWith("#EXT-X-DATERANGE") && IsAdDateRangeLine(line))
            {
                Log.Debug("Пропускаем EXT-X-DATERANGE (реклама): {Line}", line);
                continue;
            }

            // Обработка #EXTINF (сегмент)
            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                var segmentDuration = ExtractDurationFromExtInf(line);
                var segmentTitle = line.Contains(',') ? line.Split(',', 2)[1] : "";

                // Получаем следующую строку — это ожидаемо URI сегмента
                var nextUri = (i + 1 < lines.Length) ? lines[i + 1].Trim() : null;

                // Определяем, рекламный ли сегмент:
                var inAdRangeByTime = IsSegmentInAdDateRange(currentDateTime, adDateRanges);
                var byExtInfTitle = IsAdExtInf(segmentTitle);
                var byUri = !string.IsNullOrEmpty(nextUri) && AdSegment().IsMatch(nextUri);

                var isAdSegment = inAdRangeByTime || byExtInfTitle || byUri;

                if (isAdSegment)
                {
                    Log.Debug("Пропускаем рекламный сегмент. reason: time={InTime}, title={TitleMatch}, uri={UriMatch}. EXTINF='{ExtInf}', URI='{Uri}'",
                        inAdRangeByTime, byExtInfTitle, byUri, line, nextUri ?? "<none>");

                    // Пропустить сам EXTINF и следующую строку (URI), если она существует
                    if (i + 1 < lines.Length && !lines[i + 1].StartsWith('#'))
                    {
                        i++; // пропускаем URI
                    }

                    // Если у нас есть метка времени, продвигаем её на длительность сегмента,
                    // чтобы корректно вычислять время следующих сегментов
                    if (currentDateTime != DateTime.MinValue && segmentDuration > 0)
                    {
                        currentDateTime = currentDateTime.AddSeconds(segmentDuration);
                    }

                    lastSegmentWasContent = false;
                    continue;
                }

                // Если это не реклама — сохраняем EXTINF (URI добавится на следующей итерации)
                resultLines.Add(line);

                // Обновляем время для следующего сегмента, если есть
                if (currentDateTime != DateTime.MinValue && segmentDuration > 0)
                {
                    currentDateTime = currentDateTime.AddSeconds(segmentDuration);
                }

                // не меняем lastSegmentWasContent здесь — URI будет помечать это
                continue;
            }

            // Обработка разрывов
            if (line.StartsWith("#EXT-X-DISCONTINUITY", StringComparison.OrdinalIgnoreCase))
            {
                // избегаем подряд идущих #EXT-X-DISCONTINUITY
                if (resultLines.Count > 0 && resultLines[^1].Equals("#EXT-X-DISCONTINUITY", StringComparison.OrdinalIgnoreCase))
                {
                    // уже есть разрыв — пропускаем
                    continue;
                }

                // Если прямо перед этим у нас не было контента (например мы удалили рекламу), то, возможно,
                // нет смысла добавлять разрыв — но в большинстве случаев безопаснее оставить один разрыв.
                resultLines.Add(line);
                lastSegmentWasContent = false;
                continue;
            }

            // URI сегмента или другие теги — добавляем в результат
            resultLines.Add(line);

            // если строка — URI (не комментарий), отмечаем, что последний добавленный сегмент — контент
            if (!line.StartsWith('#'))
            {
                lastSegmentWasContent = true;
            }
            else
            {
                // для других тегов — оставляем флаг нетронутым, кроме явных действий выше
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
        return dateRangeLine.Contains("CLASS=\"twitch-stitched-ad\"", StringComparison.OrdinalIgnoreCase) ||
               dateRangeLine.Contains("ID=\"stitched-ad-", StringComparison.OrdinalIgnoreCase) ||
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
        var match = Regex.Match(line, $"{attributeName}=\"([^\"]+)\"", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var s = match.Groups[1].Value;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
            {
                return result;
            }
            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            {
                return dto.UtcDateTime;
            }
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

    [GeneratedRegex(@"#EXTINF:(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex ExtInfDuration();

    private class AdDateRangeInfo
    {
        public DateTime StartDate { get; set; } = DateTime.MinValue;
        public DateTime EndDate { get; set; } = DateTime.MinValue;

        public bool Contains(DateTime time)
        {
            if (StartDate == DateTime.MinValue && EndDate == DateTime.MinValue) return false;

            if (StartDate == DateTime.MinValue || EndDate == DateTime.MinValue)
            {
                // Если диапазон неполный — делаем безопасное предположение: 5 минут от START (если есть)
                if (StartDate != DateTime.MinValue)
                    return time >= StartDate && time < StartDate.AddMinutes(5);
                return false;
            }

            return time >= StartDate && time < EndDate;
        }
    }
}
