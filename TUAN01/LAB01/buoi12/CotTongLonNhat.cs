// Buổi 12 - Bài tập cơ bản - Bài 5: Liệt kê cột có tổng lớn nhất
// File: CotTongLonNhat.cs
// Kỹ thuật: duyệt THEO CỘT để tính tổng từng cột (mảng phụ 1 chiều),
//   sau đó liệt kê TẤT CẢ các cột đạt giá trị lớn nhất (có thể nhiều cột hòa nhau).
using System;
using System.Globalization;

namespace NMLT.Buoi12
{
    class CotTongLonNhat
    {
        const int MAX_DIM = 100;
        const double EPS = 1e-9;

        public static void nhapMang(ref double[,] a)
        {
            int nd, nc;
            do
            {
                Console.Write("Moi ban nhap so dong: ");
            } while (!int.TryParse(Console.ReadLine(), out nd) || nd <= 0 || nd > MAX_DIM);
            do
            {
                Console.Write("Moi ban nhap so cot: ");
            } while (!int.TryParse(Console.ReadLine(), out nc) || nc <= 0 || nc > MAX_DIM);

            a = new double[nd, nc];
            for (int id = 0; id <= nd - 1; id++)
                for (int ic = 0; ic <= nc - 1; ic++)
                {
                    do
                    {
                        Console.Write("+ Nhap phan tu [{0}, {1}] = ", id, ic);
                    } while (!double.TryParse(Console.ReadLine(), NumberStyles.Float,
                                              CultureInfo.InvariantCulture, out a[id, ic]));
                }
        }

        public static void xuatMang(double[,] a)
        {
            Console.WriteLine("Mang co {0} dong x {1} cot:", a.GetLength(0), a.GetLength(1));
            for (int id = 0; id <= a.GetLength(0) - 1; id++)
            {
                for (int ic = 0; ic <= a.GetLength(1) - 1; ic++)
                    Console.Write("{0,7}", a[id, ic].ToString("G", CultureInfo.InvariantCulture));
                Console.WriteLine();
            }
        }

        // Duyệt theo cột: với mỗi cột ic, cộng dồn theo dòng id -> mảng tổng từng cột
        public static double[] tongTungCot(double[,] a)
        {
            int nd = a.GetLength(0), nc = a.GetLength(1);
            double[] tong = new double[nc];

            for (int ic = 0; ic <= nc - 1; ic++)
            {
                tong[ic] = 0;
                for (int id = 0; id <= nd - 1; id++)
                    tong[ic] += a[id, ic];
            }
            return tong;
        }

        // Liệt kê chỉ số các cột đạt tổng lớn nhất (xử lý hòa: có thể nhiều cột)
        public static int[] cotTongLonNhat(double[] tong)
        {
            double max = tong[0];
            for (int ic = 1; ic <= tong.Length - 1; ic++)
                if (tong[ic] > max)
                    max = tong[ic];

            int[] kq = new int[tong.Length];
            int dem = 0;
            for (int ic = 0; ic <= tong.Length - 1; ic++)
                if (Math.Abs(tong[ic] - max) <= EPS)
                    kq[dem++] = ic;

            Array.Resize(ref kq, dem);
            return kq;
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            double[,] a = null;
            nhapMang(ref a);
            xuatMang(a);

            double[] tong = tongTungCot(a);
            int[] dsCot = cotTongLonNhat(tong);

            Console.Write("Cac cot co tong lon nhat:");
            for (int i = 0; i <= dsCot.Length - 1; i++)
                Console.Write(" {0}", dsCot[i]);
            Console.WriteLine();
        }
    }
}
