// Buổi 11 - Bài tập cơ bản - Bài 3: Thêm chẵn lẻ
// File: ThemChanLe.cs
// Đề bài chỉ nói "thêm 1 giá trị lẻ" mà KHÔNG nói giá trị nào.
// Suy ra quy tắc từ ví dụ trong sách:
//     2 4 3 4 2 8 2 5  ->  2 [3] 4 3 4 [5] 2 [3] 8 [9] 2 5
//     chèn sau 2 -> 3 ;  sau 4 -> 5 ;  sau 2 -> 3 ;  sau 8 -> 9
//     => giá trị chèn = a[i] + 1  (a[i] chẵn nên a[i]+1 luôn lẻ)
// Kỹ thuật: KHÔNG chèn tại chỗ. Chèn tại chỗ phải dời đuôi mảng mỗi lần -> O(n^2)
//   và phải cẩn thận không xét lại phần tử vừa chèn. Ở đây ta duyệt 1 lượt và
//   ghi thẳng sang mảng kết quả -> O(n).
using System;
using System.Globalization;

namespace NMLT.Buoi11
{
    class ThemChanLe
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

        private static bool laChan(int x)
        {
            return x % 2 == 0;
        }

        // Số phần tử chèn tối đa là n-1 (mọi phần tử đều chẵn) -> mảng kết quả <= 2n-1
        public static void themChanLe(ref int[] day)
        {
            int n = day.Length;
            int[] kq = new int[2 * n];   // cấp phát dư 1 ô, an toàn với n = 0
            int k = 0;

            for (int i = 0; i <= n - 1; i++)
            {
                kq[k++] = day[i];

                // Nếu (a[i], a[i+1]) là cặp kề nhau cùng chẵn -> chèn giá trị lẻ vào giữa
                if (i < n - 1 && laChan(day[i]) && laChan(day[i + 1]))
                    kq[k++] = day[i] + 1;
            }

            Array.Resize(ref kq, k);
            day = kq;
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            int[] a = null;
            nhapDaySo(ref a);

            Console.WriteLine("* Mang truoc them:");
            inMangTraiSangPhai(a);

            themChanLe(ref a);

            Console.WriteLine("* Mang sau khi them:");
            inMangTraiSangPhai(a);
        }
    }
}
