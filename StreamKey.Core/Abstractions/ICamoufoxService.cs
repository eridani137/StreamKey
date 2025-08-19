using StreamKey.Core.DTOs;

namespace StreamKey.Core.Abstractions;

public interface ICamoufoxService
{
    Task<CamoufoxHtmlResponse?> GetPageHtml(CamoufoxRequest request);
    Task<byte[]?> GetPageScreenshot(CamoufoxRequest request);
}