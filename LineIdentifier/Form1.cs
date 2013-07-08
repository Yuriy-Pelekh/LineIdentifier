using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LineIdentifier
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Змінна де зберігається вхідне зображення
        private Bitmap bmp;

        // Елементи (сегменти) лінії (ігрики в межах кожного ікса)
        private readonly List<List<int>> segments = new List<List<int>>();

        private void ConvertToBlackAndWhite()
        {
            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    var currentPixelColor = bmp.GetPixel(x, y);
                    var grayScaleColor = (currentPixelColor.R + currentPixelColor.G + currentPixelColor.B) / 3 > 200
                                             ? Color.White
                                             : Color.Black;
                    bmp.SetPixel(x, y, grayScaleColor);
                }
            }
        }

        private int GetLineStart()
        {
            var yCurrent = bmp.Height/2;

            while (!AreEqual(bmp.GetPixel(0, yCurrent), Color.Black))
            {
                yCurrent--;
            }

            return yCurrent;
        }

        // Пошук початкової верхньої точки сегменту для заданого ікса
        private int GetSegmentStart(int x, int y)
        {
            while (AreEqual(bmp.GetPixel(x, y), Color.Black))
            {
                y--;
            }

            y++;

            return y;
        }

        private int GetNextSegmentStart(int x, IList<int> currentSegment)
        {
            // Дивимося чи є чорні пікселі в наступному сегменті аналогічні до поточного,
            // якщо є - то повертаємо значення ігрика для заданого ікса
            foreach (var iSegment in currentSegment.Where(iSegment => AreEqual(bmp.GetPixel(x, iSegment), Color.Black)))
            {
                return GetSegmentStart(x, iSegment);
            }

            // Діагональний елемент зверху до першого елемента сегменту
            var y = currentSegment[0] - 1;

            // Якщо попередня перевірка не дала позитивного результату, то перевіряємо піксель по діагоналі до верхнього з поточного сегменту
            if (AreEqual(bmp.GetPixel(x, y), Color.Black))
            {
                return GetSegmentStart(x, y);
            }

            // Діагональний елемент до крайнього нижнього останнього елементу сегмента
            y = currentSegment[currentSegment.Count - 1] + 1;

            // Аналогічно для нижнього пікселя поточного сегменту
            if (AreEqual(bmp.GetPixel(x, y), Color.Black))
            {
                return GetSegmentStart(x, y);
            }

            // Якщо лінія обірвалася - повертаємо -1
            return -1;
        }

        // Порівнює два кольори
        private bool AreEqual(Color c1, Color c2)
        {
            return c1.R == c2.R && c1.G == c2.G && c1.B == c2.B;
        }

        // Колекція еталонних точок
        private readonly Collection<PointF> etalonPoints = new Collection<PointF>();
        
        // Колекція поточних точок
        private readonly Collection<PointF> currentPoints = new Collection<PointF>();

        private void buttonOpenText_Click(object sender, EventArgs e)
        {
            try
            {
                // Вибираємо файл з точками (текстовий)
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Відкриваємо файл для читання
                    using (var sr = new StreamReader(openFileDialog.FileName))
                    {
                        // Якщо це вже не вперше відкривається файл, то видалити попередні точки
                        if (etalonPoints.Count != 0)
                        {
                            etalonPoints.Clear();
                        }

                        // Зчитуємо весь файл в буфер
                        var buffer = sr.ReadToEnd();
                        
                        // Розбиваємо файл на масив рядків
                        var lines = buffer.Split('\n');

                        // Проходимося по кожному рядку, який розбиваємо по пробілах
                        foreach (var stringPoint in lines.Select(line => line.Split(' ')))
                        {
                            // Додаємо точки в колекцію еталонних точок
                            etalonPoints.Add(new PointF(float.Parse(stringPoint[0], CultureInfo.InvariantCulture),
                                                        float.Parse(stringPoint[1], CultureInfo.InvariantCulture)));
                        }

                        // Робимо кнопку вибору зображення активною (доступною)
                        buttonOpenImage.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                buttonOpenImage.Enabled = false;
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonOpenImage_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Аналогічно до текстового файлу
                    if (currentPoints.Count != 0)
                    {
                        currentPoints.Clear();
                    }

                    // Завантажуємо імедж в пам'ять в форматі BMP
                    var image = Image.FromFile(openFileDialog.FileName);
                    bmp = new Bitmap(image);

                    ConvertToBlackAndWhite();

                    // Пошук початку довільної лінії
                    var y = GetLineStart();

                    // Проходимося в ширину по зображенню (пошук лінії відносно знайденого початку)
                    for (var x = 0; x < bmp.Width; x++)
                    {
                        // Ініціалізуємо колекцію сегментів
                        var segment = new List<int>();

                        // Якщо це початок лінії (х==0)
                        y = x == 0
                            // то шукаємо початок сегменту
                                ? GetSegmentStart(x, y)
                            // якщо ні - то шукаємо початок наступного сегменту відносно попереднього
                                : segments.Count >= x ? GetNextSegmentStart(x, segments[x - 1]) : -1;

                        if (y < 0)
                        {
                            continue;
                        }

                        // Поки точки сегменту лінії (чорні)
                        while (AreEqual(bmp.GetPixel(segments.Count, y), Color.Black))
                        {
                            // Додаємо точки в колекцію точок сегмента
                            segment.Add(y);

                            // Переходимо на наступну потенційну точку сегменту
                            y++;
                        }

                        // Додаємо сегмент в колекцію  сегментів лінії
                        segments.Add(segment);
                    }

                    // Замальовуємо цілу лінію чи її середину (в 1 піксель)
                    var selectWholeLineOrCenter = true;

                    // Цикл проходження по всіх сегментах і замалювання відносно вибраного режиму
                    for (var x = 0; x < segments.Count; x++)
                    {
                        if (selectWholeLineOrCenter)
                        {
                            FillLine(x, Color.Red);
                        }
                        else
                        {
                            var segment = segments[x];
                            bmp.SetPixel(x, segment[segment.Count/2], Color.Red);
                        }
                    }

                    // Чи найдений початок неповторюваного фрагмента лінії (одиничний відрізок)
                    var isBeginFound = false;

                    // Чи найдений кінець неповторюваного фрагмента лінії (одиничний відрізок)
                    var isEndFound = false;

                    // Колекція точок одиничного відрізка
                    var points = new List<Point>();

                    // Напрямок пошуку (вверх чи вниз)
                    var direction = 0;

                    // Проходимося по всіх сегментах лінії починаючи з другогог
                    for (var x = 1; x < segments.Count; x++)
                    {
                        // Попередній сегмент
                        var previousSegment = segments[x - 1];

                        // Поточний сегмент
                        var currentSegment = segments[x];

                        // Точка середини попереднього сегмента
                        var previousPoint = previousSegment[previousSegment.Count/2];

                        // Точка середини поточного сегмента
                        var currentPoint = currentSegment[currentSegment.Count/2];

                        // Якщо точка попереднього сегмента нижче точки поточного
                        if (previousPoint > currentPoint)
                        {
                            // І попередній напрямок був не вниз
                            if (direction != -1)
                            {
                                // то присвоюємо напрямок вниз
                                direction = -1;
                                //bmp.SetPixel(x, currentPoint, Color.Yellow);

                                // Якщо початок не знайдено
                                if (!isBeginFound)
                                {
                                    // То встановлюємо початок шуканого сегменту
                                    isBeginFound = true;
                                }
                                    // Якщо початок знайдено, а кінець ще ні
                                else if (!isEndFound)
                                {
                                    // То значить ми знайшли кінець
                                    isEndFound = true;
                                }
                            }
                        }
                            // Якщо попередня точка сегменту вище точки наступного сегменту
                        else if (previousPoint < currentPoint)
                        {
                            // І напрямок не вверх
                            if (direction != 1)
                            {
                                // То встановлюємо напрямок вверх
                                direction = 1;
                            }
                        }

                        // Якщо знайдено початок і не знайдено кінець
                        if (isBeginFound && !isEndFound)
                        {
                            // То тсавимо в цьому місці жовту крапку
                            if (selectWholeLineOrCenter)
                            {
                                FillLine(x, Color.Yellow);
                            }
                            else
                            {
                                bmp.SetPixel(x, currentPoint, Color.Yellow);
                            }

                            // Додаємо поточну точку в колекцію точок одиничного відрізка (неповторюваний фрагмент)
                            points.Add(new Point {X = x, Y = currentPoint});
                        }
                    }

                    // Зберігаємо поточне (редаговане) зображення
                    bmp.Save(openFileDialog.FileName + ".bmp", ImageFormat.Bmp);

                    // Витягуємо першу точку з колекції еталонних точок
                    var etalonPoint = points.FirstOrDefault();

                    // Нормалізуємо точки відрізка відносно першої точки (початку координат)
                    var normalizePoints =
                        points.Select(point => new Point(point.X - etalonPoint.X, point.Y - etalonPoint.Y));
                    
                    // Шукаємо максимальний ігрик
                    var yMax = normalizePoints.Max(p => Math.Abs(p.Y));

                    // Проходимося по всіх нормальізованих точках
                    foreach (var point in normalizePoints)
                    {
                        // Приводимо нормалізовані точки до відповідного вигляду
                        currentPoints.Add(new PointF(point.X/180.0F, 1F/yMax*point.Y));
                    }

                    //listBox1.Items.Add(string.Format("{0}   {1}   {2}",
                    //                 point,
                    //                 Math.Round(point.X / 180.0, 4),
                    //                 Math.Round(1 / yMax * point.Y, 4)));

                    // Виводимо результуючу картинку на екран
                    pictureBox1.Image = bmp;
                }

                // Обчислюємо значення співпадіння двох ліній
                var result = Math.Abs(Math.Round(LineComparator.Compare(etalonPoints, currentPoints), 2));
                
                // Формуємо стрінг з результатом
                var resultString = string.Format("{0}%     {1}", result, result > 90 ? "Accept!" : "Reject!");
                
                // Виводимо результат в радок статусу
                toolStripStatusLabel.Text = string.Format("{0}     ->     {1}", DateTime.UtcNow, resultString);
                
                // Виводимо результат в меседж бокс
                MessageBox.Show(resultString);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Замальовує сегмент відповідним кольором
        private void FillLine(int x, Color color)
        {
            foreach (var segment in segments[x])
            {
                bmp.SetPixel(x, segment, color);
            }
        }
    }
}
