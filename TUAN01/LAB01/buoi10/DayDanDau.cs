// Buổi 10 - Bài tập cơ bản - Bài 3: Dãy đan dấu
// File: DayDanDau.cs
// Dãy đan dấu <=> MỌI cặp kề nhau đều trái dấu.
// Kỹ thuật: đặt cờ hiệu, thoát sớm ngay khi phát hiện phản ví dụ.
using System;
using System.Globalization;

namespace NMLT.Buoi10
{
    class DayDanDau
    {
        const int MAX_N = 500;
        const double EPS = 1e-9;

        public static void nhapDaySo(ref double[] day)
        {
            int n;
            do
            {
                Console.Write("Moi ban nhap so luong phan tu: ");
            } while (!int.TryParse(Console.ReadLine(), out n) || n <= 0 || n > MAX_N);

            day = new double[n];
            for (int i = 0; i <= n - 1; i++)
            {
                do
                {
                    Console.Write("Phan tu {0}: ", i);
                } while (!double.TryParse(Console.ReadLine(), NumberStyles.Float,
                                          CultureInfo.InvariantCulture, out day[i]));
            }
        }

        public static void inMangTraiSangPhai(double[] day)
        {
            Console.Write("Day so co {0} phan tu:", day.Length);
            for (int i = 0; i <= day.Length - 1; i++)
                Console.Write(" {0}", day[i].ToString("G", CultureInfo.InvariantCulture));
            Console.WriteLine();
        }

        private static int dau(double x)
        {
            if (Math.Abs(x) <= EPS) return 0;
            return x < 0 ? -1 : 1;
        }

        public static bool laDanDau(double[] day)
        {
            // Dãy 1 phần tử: quy ước là đan dấu nếu phần tử đó khác 0
            if (day.Length == 1)
                return dau(day[0]) != 0;

            bool coHieu = true;   // giả sử dãy đan dấu
            for (int i = 0; i <= day.Length - 2 && coHieu; i++)
            {
                if (dau(day[i]) * dau(day[i + 1]) >= 0)   // cùng dấu hoặc có phần tử 0
                    coHieu = false;
            }
            return coHieu;
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            double[] a = null;
            nhapDaySo(ref a);
            inMangTraiSangPhai(a);

            if (laDanDau(a))
                Console.WriteLine("Day so tren la day dan dau.");
            else
                Console.WriteLine("Day so tren khong la day dan dau.");
        }
    }
}
