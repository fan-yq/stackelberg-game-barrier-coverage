namespace Aircraft.Core.ConvexHull;

/// <summary>
/// 可能是point在hull内部，也可能是DiskSample在radius范围内无法采样
/// </summary>
/// <param name="message"></param>
public abstract class HullIntersectionException(string message) : Exception(message) {
    public abstract ConvexHullF[] IntersctionHulls { get; }
}
