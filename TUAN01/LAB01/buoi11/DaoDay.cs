// Buổi 11 - Bài tập cơ bản - Bài 4: Đảo dãy
// File: DaoDay.cs
// Kỹ thuật: hai chỉ số chạy ngược chiều, hoán vị tại chỗ -> O(n), bộ nhớ phụ O(1).
// LƯU Ý BẪY: vòng lặp chỉ chạy đến GIỮA mảng (i < j).
//   Nếu duyệt hết mảng (i từ 0 đến n-1) thì mỗi cặp bị hoán vị hai lần
//   -> mảng trở về nguyên trạng. Đây là lỗi kinh điển của bài này.
using System;
using System.Globalization;

namespace NMLT.Buoi11
{
    class DaoDay
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

        public static void daoDay(int[] day)
        {
            int i = 0;
            int j = day.Length - 1;

            while (i < j)   // n lẻ: phần tử giữa đứng yên, đúng
            {
                int tmp = day[i];
                day[i] = day[j];
                day[j] = tmp;

                i++;
                j--;
            }
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            int[] a = null;
            nhapDaySo(ref a);

            Console.WriteLine("* Mang truoc dao:");
            inMangTraiSangPhai(a);

            daoDay(a);

            Console.WriteLine("* Mang sau khi dao:");
            inMangTraiSangPhai(a);
        }
    }
}
