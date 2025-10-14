namespace StreamKey.Shared;

public static class Extensions
{
    public static string GenerateDeviceId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new char[32];
    
        for (var i = 0; i < 32; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
    
        return new string(result);
    }
}