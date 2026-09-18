/*
 * CHƯƠNG TRÌNH TÍNH GIAI THỪA
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập số nguyên n, tính và in giá trị n!.
 *
 * Ý tưởng: Dùng vòng lặp nhân lần lượt các số từ 1 đến n.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập n
 * 2. Đặt gt = 1
 * 3. Với i từ 1 đến n, tính gt = gt * i
 * 4. In gt
 * Kết thúc
 */

using System;

namespace NMLT.Buoi05
{
    class GiaiThua
    {
        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so n (1 <= n <= 12): ");
            int n = int.Parse(Console.ReadLine());
            long gt = 1;

            for (int i = 1; i <= n; i++)
            {
                gt *= i;
            }

            Console.Write("{0}! = 1", n);
            for (int i = 2; i <= n; i++)
                Console.Write(".{0}", i);
            Console.WriteLine(" = {0}.", gt);
            Console.Read();
        }
    }
}
