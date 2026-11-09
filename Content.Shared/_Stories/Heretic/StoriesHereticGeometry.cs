using System.Numerics;

namespace Content.Shared._Stories.Heretic;

public static class StoriesHereticGeometry
{
    public static bool IsInCone(Vector2 origin, Vector2 facing, Vector2 target, float halfAngleRadians)
    {
        var direction = target - origin;
        return direction.LengthSquared() >= 0.01f
            && Vector2.Dot(Vector2.Normalize(direction), facing) >= MathF.Cos(halfAngleRadians);
    }
}
