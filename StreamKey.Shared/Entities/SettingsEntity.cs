using System.ComponentModel.DataAnnotations;

namespace StreamKey.Shared.Entities;

public class SettingsEntity : BaseEntity
{
    [MaxLength(256)] public required string Key { get; set; }
    [MaxLength(10000)] public required string Value { get; set; }
}