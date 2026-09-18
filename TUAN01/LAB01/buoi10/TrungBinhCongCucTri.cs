// Buổi 10 - Bài tập cơ bản - Bài 1: Trung bình cộng phần tử cực trị
// File: TrungBinhCongCucTri.cs
// Định nghĩa cực trị (theo sách): phần tử a[i] với 0 < i < n-1 thỏa
//     (a[i] >= a[i-1] && a[i] >= a[i+1])  ||  (a[i] <= a[i-1] && a[i] <= a[i+1])
// Hai phần tử đầu/cuối KHÔNG được xét vì thiếu một phía lân cận.
using System;
using System.Globalization;

namespace NMLT.Buoi10
{
    class TrungBinhCongCucTri
    {
        const int MAX_N = 500;

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

        private static bool laCucTri(double[] day, int i)
        {
            if (i <= 0 || i >= day.Length - 1)   // biên: không xét
                return false;
            bool cucDai = day[i] >= day[i - 1] && day[i] >= day[i + 1];
            bool cucTieu = day[i] <= day[i - 1] && day[i] <= day[i + 1];
            return cucDai || cucTieu;
        }

        // Duyệt bộ ba kề nhau (i-1, i, i+1); trả về số lượng cực trị, tổng qua ref
        public static int thongKeCucTri(double[] day, ref double tong)
        {
            int dem = 0;
            tong = 0;
            for (int i = 1; i <= day.Length - 2; i++)
            {
                if (laCucTri(day, i))
                {
                    dem++;
                    tong += day[i];
                }
            }
            return dem;
        }

        public static void lietKeCucTri(double[] day)
        {
            for (int i = 1; i <= day.Length - 2; i++)
                if (laCucTri(day, i))
                    Console.Write(" {0}", day[i].ToString("G", CultureInfo.InvariantCulture));
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            double[] a = null;
            double tong = 0;

            nhapDaySo(ref a);
            inMangTraiSangPhai(a);

            int dem = thongKeCucTri(a, ref tong);

            if (dem == 0)   // chặn chia cho 0
            {
                Console.WriteLine("Day so khong co phan tu cuc tri.");
                return;
            }

            Console.Write("Day so co {0} phan tu cuc tri:", dem);
            lietKeCucTri(a);
            Console.WriteLine();
            Console.WriteLine("Tong cac phan tu cuc tri co trong day so: {0}.",
                              tong.ToString("G", CultureInfo.InvariantCulture));
            Console.WriteLine("Trung binh cong cac phan tu cuc tri: {0}.",
                              (tong / dem).ToString("F2", CultureInfo.InvariantCulture));
        }
    }
}
