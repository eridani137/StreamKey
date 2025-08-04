namespace StreamKey.Application.Results;

public enum ErrorCode
{
    None = 0,
    NullValue = 1,
    StreamNotFound = 2,
    PlaylistNotReceived = 3,
    UnexpectedError = 4,
    Timeout = 5,
    
    ChannelNotFound = 6,
    ChannelAlreadyExists = 7,
}