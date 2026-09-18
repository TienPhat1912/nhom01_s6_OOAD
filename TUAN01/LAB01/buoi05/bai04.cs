/*
 * CHƯƠNG TRÌNH TÌM SỐ TỰ NHIÊN NHỎ NHẤT
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập số thực a, tìm số nguyên dương n nhỏ nhất sao cho 1 + 1/2 + ... + 1/n > a.
 *
 * Ý tưởng: Tăng dần n, đồng thời cộng các nghịch đảo cho đến khi tổng vượt quá a.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập a
 * 2. Đặt S = 0, n = 0
 * 3. Khi S <= a, tăng n và cộng 1/n vào S
 * 4. In n và S
 * Kết thúc
 */

using System;

namespace NMLT.Buoi05
{
    class TimBeNhatThoa
    {
        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so thuc a: ");
            double a = double.Parse(Console.ReadLine());

            double tong = 0;
            int n = 0;

            while (tong <= a)
            {
                n++;
                tong += 1.0 / n;
            }

            Console.WriteLine("So n be nhat thoa man la: {0} (Tong S = {1:0.00})", n, tong);
            Console.Read();
        }
    }
}
