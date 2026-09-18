/*
 * CHƯƠNG TRÌNH TÍNH SỐ PI
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập sai số epsilon, tính gần đúng số pi bằng chuỗi Leibniz.
 *
 * Ý tưởng: Dùng hàm TinhPi để cộng các số hạng luân phiên 4/(2i+1) khi số hạng còn lớn hơn hoặc bằng epsilon.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập epsilon
 * 2. Đặt pi = 0, i = 0 và dấu đầu tiên là dương
 * 3. Khi 4/(2i+1) >= epsilon, cộng số hạng có dấu tương ứng và đổi dấu
 * 4. In pi và sai số thực tế
 * Kết thúc
 */

using System;

namespace NMLT.Buoi07
{
    class TinhSoPi
    {
        public static double TinhPi(double epsilon)
        {
            double pi = 0;
            double term = 1.0;
            int i = 0;
            int dau = 1;

            while (4.0 / (2 * i + 1) >= epsilon)
            {
                pi += dau * (4.0 / (2 * i + 1));
                dau = -dau;
                i++;
            }
            return pi;
        }

        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap do sai so epsilon: ");
            double epsilon = double.Parse(Console.ReadLine());

            double pi = TinhPi(epsilon);
            Console.WriteLine("So pi tinh den do chinh xac {0} la {1}.", epsilon, pi);
            Console.WriteLine("Do sai so thuc te la {0}.", Math.Abs(pi - Math.PI));

            Console.Read();
        }
    }
}
