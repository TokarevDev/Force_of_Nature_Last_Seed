using System.Collections.Generic;

public interface IShotModifier
{
    void Apply(List<ShotSpawnData> shots, ShotContext context);
}