// Buổi 11 - Bài tập cơ bản - Bài 5: Trộn dãy
// File: TronDay.cs
// Trộn 2 dãy ĐÃ TĂNG DẦN thành 1 dãy tăng dần, KHÔNG dùng thuật toán sắp xếp.
// Kỹ thuật: merge hai con trỏ -> O(n + m).
//   (Nếu ghép rồi sắp xếp sẽ là O((n+m)^2) với interchange sort và vi phạm đề bài.)
// Điều kiện tiên quyết: cả hai dãy vào phải tăng dần. Chương trình KIỂM TRA
// điều kiện này thay vì mặc định tin người dùng -- nếu sai, kết quả merge sẽ sai.
using System;
using System.Globalization;

namespace NMLT.Buoi11
{
    class TronDay
    {
        const int MAX_N = 500;

        public static void nhapDaySo(string ten, ref int[] day)
        {
            int n;
            do
            {
                Console.Write("Moi ban nhap so luong phan tu day {0}: ", ten);
            } while (!int.TryParse(Console.ReadLine(), out n) || n <= 0 || n > MAX_N);

            day = new int[n];
            for (int i = 0; i <= n - 1; i++)
            {
                do
                {
                    Console.Write("{0}[{1}]: ", ten, i);
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

        public static bool laTangDan(int[] day)
        {
            for (int i = 0; i <= day.Length - 2; i++)
                if (day[i] > day[i + 1])
                    return false;
            return true;
        }

        // Merge chuẩn: mỗi bước lấy phần tử nhỏ hơn ở đầu 2 dãy còn lại
        public static int[] tron(int[] a, int[] b)
        {
            int[] c = new int[a.Length + b.Length];
            int i = 0, j = 0, k = 0;

            while (i < a.Length && j < b.Length)
            {
                if (a[i] <= b[j])       // <= giữ tính ỔN ĐỊNH: phần tử của a đứng trước khi bằng nhau
                    c[k++] = a[i++];
                else
                    c[k++] = b[j++];
            }

            // Vét phần đuôi còn lại: đúng 1 trong 2 vòng dưới đây chạy
            while (i < a.Length) c[k++] = a[i++];
            while (j < b.Length) c[k++] = b[j++];

            return c;
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            int[] a = null, b = null;

            nhapDaySo("A", ref a);
            nhapDaySo("B", ref b);

            Console.WriteLine("* Day A:");
            inMangTraiSangPhai(a);
            Console.WriteLine("* Day B:");
            inMangTraiSangPhai(b);

            if (!laTangDan(a) || !laTangDan(b))
            {
                Console.WriteLine("Loi: ca hai day vao phai tang dan thi thuat toan tron moi dung.");
                return;
            }

            int[] c = tron(a, b);

            Console.WriteLine("* Day C:");
            inMangTraiSangPhai(c);
        }
    }
}
