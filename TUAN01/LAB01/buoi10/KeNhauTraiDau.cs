// Buổi 10 - Bài tập cơ bản - Bài 2: Kề nhau trái dấu
// File: KeNhauTraiDau.cs
// Đếm số CẶP kề nhau (i, i+1) mà a[i] và a[i+1] trái dấu.
// Quy ước: 0 không mang dấu -> cặp chứa 0 KHÔNG được tính là trái dấu.
using System;
using System.Globalization;

namespace NMLT.Buoi10
{
    class KeNhauTraiDau
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

        // -1 / 0 / +1 ; dùng EPS thay cho so sánh trực tiếp với 0
        private static int dau(double x)
        {
            if (Math.Abs(x) <= EPS) return 0;
            return x < 0 ? -1 : 1;
        }

        // Duyệt các cặp kề nhau (i, i+1)
        public static void demKeNhauTraiDau(double[] day, ref int dem)
        {
            dem = 0;
            for (int i = 0; i <= day.Length - 2; i++)
            {
                int d1 = dau(day[i]), d2 = dau(day[i + 1]);
                if (d1 * d2 < 0)   // tích âm <=> khác dấu và cả hai khác 0
                    dem++;
            }
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            double[] a = null;
            int kq = 0;

            nhapDaySo(ref a);
            inMangTraiSangPhai(a);

            demKeNhauTraiDau(a, ref kq);
            Console.WriteLine("So luong cac cap ke nhau ma trai dau: {0}.", kq);
        }
    }
}
