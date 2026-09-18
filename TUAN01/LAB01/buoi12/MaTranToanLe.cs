// Buổi 12 - Bài tập cơ bản - Bài 3: Ma trận toàn lẻ
// File: MaTranToanLe.cs
// Kỹ thuật: cờ hiệu + thoát sớm ngay khi gặp phần tử chẵn đầu tiên.
using System;
using System.Globalization;

namespace NMLT.Buoi12
{
    class MaTranToanLe
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

        // Trả về false ngay khi gặp phần tử chẵn đầu tiên -> không cần duyệt hết mảng
        public static bool laToanLe(int[,] a)
        {
            for (int id = 0; id <= a.GetLength(0) - 1; id++)
                for (int ic = 0; ic <= a.GetLength(1) - 1; ic++)
                    if (a[id, ic] % 2 == 0)
                        return false;
            return true;
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            int[,] a = null;
            nhapMang(ref a);
            xuatMang(a);

            if (laToanLe(a))
                Console.WriteLine("Mang A toan le!");
            else
                Console.WriteLine("Mang A khong toan le.");
        }
    }
}
