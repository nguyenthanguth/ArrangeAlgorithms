namespace ArrangeAlgorithms.Enums
{
    /// <summary>
    /// Represents the spatial location of a point relative to a geometric shape or boundary.
    /// </summary>
    public enum PointLocation
    {
        /// <summary>
        /// The point lies strictly inside the interior of the shape.
        /// </summary>
        Inside,

        /// <summary>
        /// The point lies outside the shape.
        /// </summary>
        OutSide,

        /// <summary>
        /// The point lies on the boundary (or edge / circumference) of the shape within tolerance.
        /// </summary>
        OnSide
    }
}
