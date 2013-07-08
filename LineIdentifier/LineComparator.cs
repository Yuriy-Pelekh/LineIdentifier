using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace LineIdentifier
{
    // Для порівняння ліній
    public class LineComparator
    {
        // Допустима похибка при порівнянні точок ліній
        private const double Epsilon = 0.001;

        // Порівнює два набори точок (двох ліній)
        public static double Compare(Collection<PointF> etalonPoints,Collection<PointF> currentPoints)
        {
            // Чисельник (формули)
            var top = 0.0;
            
            // Знаменник еталонного значення (формули)
            var bottomEtalon = 0.0;

            // Знаменник поточного значення (формули)
            var bottomCurrent = 0.0;

            // Визначення членів формули (підрахунок)
            foreach (var etalonPoint in etalonPoints)
            {
                var currentPoint = currentPoints.FirstOrDefault(p => Math.Abs(etalonPoint.X - p.X) < Epsilon);

                top += etalonPoint.Y * currentPoint.Y;
                bottomEtalon += Math.Pow(etalonPoint.Y, 2);
                bottomCurrent += Math.Pow(currentPoint.Y, 2);
            }

            // Обчислюємо формулу і повертаємо результат
            return top/Math.Sqrt(bottomEtalon)/Math.Sqrt(bottomCurrent)*100;
        }
    }
}
