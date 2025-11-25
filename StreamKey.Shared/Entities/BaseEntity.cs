using System.ComponentModel.DataAnnotations;

namespace StreamKey.Shared.Entities;

public class BaseEntity
{
    
}

public class BaseGuidEntity : BaseEntity
{
    [Key] public Guid Id { get; set; }
}

public class BaseIntEntity : BaseEntity
{
    [Key] public int Id { get; set; }
}

public class BaseLongEntity : BaseEntity
{
    [Key] public long Id { get; set; }
}