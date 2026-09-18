// Buổi 12 - Bài tập cơ bản - Bài 1: Tổng các số chẵn, lẻ
// File: TongChanLeMang2C.cs
// Kỹ thuật: duyệt toàn mảng 2 chiều bằng 2 vòng for lồng nhau (theo dòng).
using System;
using System.Globalization;

namespace NMLT.Buoi12
{
    class TongChanLeMang2C
    {
        const int MAX_DIM = 100;

        public static void nhapMang(ref int[,] a)
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

            a = new int[nd, nc];
            for (int id = 0; id <= nd - 1; id++)
                for (int ic = 0; ic <= nc - 1; ic++)
                {
                    do
                    {
                        Console.Write("+ Nhap phan tu [{0}, {1}] = ", id, ic);
                    } while (!int.TryParse(Console.ReadLine(), out a[id, ic]));
                }
        }

        public static void xuatMang(int[,] a)
        {
            Console.WriteLine("Mang co {0} dong x {1} cot:", a.GetLength(0), a.GetLength(1));
            for (int id = 0; id <= a.GetLength(0) - 1; id++)
            {
                for (int ic = 0; ic <= a.GetLength(1) - 1; ic++)
                    Console.Write("{0,7}", a[id, ic]);
                Console.WriteLine();
            }
        }

        // Duyệt toàn mảng 1 lần, tách tổng chẵn/lẻ qua tham số ref
        public static void tongChanLe(int[,] a, ref long tongChan, ref long tongLe)
        {
            tongChan = 0;
            tongLe = 0;
            for (int id = 0; id <= a.GetLength(0) - 1; id++)
                for (int ic = 0; ic <= a.GetLength(1) - 1; ic++)
                {
                    if (a[id, ic] % 2 == 0)
                        tongChan += a[id, ic];
                    else
                        tongLe += a[id, ic];
                }
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            int[,] a = null;
            long tongChan = 0, tongLe = 0;

            nhapMang(ref a);
            xuatMang(a);

            tongChanLe(a, ref tongChan, ref tongLe);
            Console.WriteLine("Tong cac so chan la {0} va tong so le la {1}.", tongChan, tongLe);
        }
    }
}
