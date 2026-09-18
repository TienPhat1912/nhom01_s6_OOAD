// Buổi 12 - Bài tập cơ bản - Bài 2: Đếm số trên biên
// File: DemSoTrenBienMang2C.cs
//
// LƯU Ý ERRATA: Ví dụ trong sách (ma trận 3x4, trang 89) in kết quả
// "5 so duong, 3 so am va 1 so khong" (tổng = 9), nhưng biên của ma trận
// 3x4 có đúng 2*4 + 2*(3-2) = 10 phần tử. Đếm tay lại từng phần tử biên
// cho ra 5 dương + 4 âm + 1 không = 10 (khớp tổng số phần tử biên).
// => Sách thiếu 1 số âm. Chương trình dưới đây cho kết quả đúng là 4 so am.
using System;
using System.Globalization;

namespace NMLT.Buoi12
{
    class DemSoTrenBienMang2C
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

        // Một ô (id, ic) thuộc biên <=> nó nằm ở dòng đầu/cuối hoặc cột đầu/cuối
        private static bool thuocBien(int id, int ic, int nd, int nc)
        {
            return id == 0 || id == nd - 1 || ic == 0 || ic == nc - 1;
        }

        public static void demSoTrenBien(double[,] a, ref int soDuong, ref int soAm, ref int soKhong)
        {
            soDuong = soAm = soKhong = 0;
            int nd = a.GetLength(0), nc = a.GetLength(1);

            for (int id = 0; id <= nd - 1; id++)
                for (int ic = 0; ic <= nc - 1; ic++)
                {
                    if (!thuocBien(id, ic, nd, nc))
                        continue;

                    if (Math.Abs(a[id, ic]) <= EPS)
                        soKhong++;
                    else if (a[id, ic] > 0)
                        soDuong++;
                    else
                        soAm++;
                }
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            double[,] a = null;
            int soDuong = 0, soAm = 0, soKhong = 0;

            nhapMang(ref a);
            xuatMang(a);

            demSoTrenBien(a, ref soDuong, ref soAm, ref soKhong);
            Console.WriteLine("Tren duong bien co {0} so duong, {1} so am va {2} so khong.",
                              soDuong, soAm, soKhong);
        }
    }
}
