using StreamKey.Shared.DTOs;

namespace StreamKey.Core.Abstractions;

public interface ICamoufoxService
{
    Task<string?> GetPageHtml(CamoufoxRequest request);
    Task<byte[]?> GetPageScreenshot(CamoufoxRequest request);
}