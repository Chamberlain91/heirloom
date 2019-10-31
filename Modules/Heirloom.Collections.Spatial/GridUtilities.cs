﻿using System.Collections.Generic;

using Heirloom.Math;

namespace Heirloom.Collections.Spatial
{
    internal static class GridUtilities
    {
        internal static IEnumerable<IntVector> EnumerateNeighbors(IntVector co, GridNeighborType neighborType)
        {
            if (neighborType == GridNeighborType.Axis)
            {
                // Clockwise from three o'clock
                yield return (co.X + 1, co.Y + 0);
                yield return (co.X + 0, co.Y + 1);
                yield return (co.X - 1, co.Y + 0);
                yield return (co.X + 0, co.Y - 1);
            }
            else
            if (neighborType == GridNeighborType.Diagonal)
            {
                // Clockwise from half past one
                yield return (co.X + 1, co.Y - 1);
                yield return (co.X + 1, co.Y + 1);
                yield return (co.X - 1, co.Y + 1);
                yield return (co.X - 1, co.Y - 1);
            }
            else
            {
                // Clockwise from three o'clock
                yield return (co.X + 1, co.Y + 0);
                yield return (co.X + 1, co.Y + 1);
                yield return (co.X + 0, co.Y + 1);
                yield return (co.X - 1, co.Y + 1);
                yield return (co.X - 1, co.Y + 0);
                yield return (co.X - 1, co.Y - 1);
                yield return (co.X + 0, co.Y - 1);
                yield return (co.X + 1, co.Y - 1);
            }
        }
    }
}
