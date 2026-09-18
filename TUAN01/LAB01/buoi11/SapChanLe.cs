// Buổi 11 - Bài tập cơ bản - Bài 1: Sắp chẵn lẻ
// File: SapChanLe.cs
// Yêu cầu: số chẵn ở trước và tăng dần, số lẻ ở sau và giảm dần.
// Kỹ thuật: TỔNG QUÁT HÓA SẮP XẾP THEO NGHỊCH THẾ.
//   Thay vì viết 3 vòng sắp xếp riêng, ta chỉ định nghĩa lại quan hệ thứ tự
//   truocHon(x, y) = "x phải đứng trước y", rồi dùng đúng khung interchange sort.
using System;
using System.Globalization;

namespace NMLT.Buoi11
{
    class SapChanLe
    {
        const int MAX_N = 500;

        public static void nhapDaySo(ref int[] day)
        {
            int n;
            do
            {
                Console.Write("Moi ban nhap so luong phan tu: ");
            } while (!int.TryParse(Console.ReadLine(), out n) || n <= 0 || n > MAX_N);

            day = new int[n];
            for (int i = 0; i <= n - 1; i++)
            {
                do
                {
                    Console.Write("Phan tu {0}: ", i);
                } while (!int.TryParse(Console.ReadLine(), out day[i]));
            }
        }

        public static void inMangTraiSangPhai(int[] day)
        {
            Console.Write("Day so co {0} phan tu:", day.Length);
            for (int i = 0; i <= day.Length - 1; i++)
                Console.Write(" {0}", day[i]);
            Console.WriteLine();
        }

        // LƯU Ý: dùng % 2 != 0, KHÔNG dùng % 2 == 1.
        // Trong C#, (-3) % 2 == -1 nên điều kiện "% 2 == 1" bỏ sót mọi số lẻ ÂM.
        private static bool laChan(int x)
        {
            return x % 2 == 0;
        }

        // Quan hệ thứ tự mong muốn: x đứng trước y ?
        private static bool truocHon(int x, int y)
        {
            bool xChan = laChan(x), yChan = laChan(y);

            if (xChan != yChan)
                return xChan;            // chẵn luôn đứng trước lẻ

            if (xChan)
                return x < y;            // hai số chẵn: tăng dần
            else
                return x > y;            // hai số lẻ: giảm dần
        }

        // Interchange sort tổng quát theo quan hệ truocHon. O(n^2), n <= 500 nên chấp nhận được.
        public static void sapChanLe(int[] day)
        {
            for (int i = 0; i <= day.Length - 2; i++)
            {
                for (int j = i + 1; j <= day.Length - 1; j++)
                {
                    if (truocHon(day[j], day[i]))   // phát hiện nghịch thế
                    {
                        int tmp = day[i];
                        day[i] = day[j];
                        day[j] = tmp;
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            int[] a = null;
            nhapDaySo(ref a);

            Console.WriteLine("* Mang truoc khi sap xep:");
            inMangTraiSangPhai(a);

            sapChanLe(a);

            Console.WriteLine("* Mang sau khi sap xep:");
            inMangTraiSangPhai(a);
        }
    }
}
