/*
 * CHƯƠNG TRÌNH LIỆT KÊ SỐ HOÀN HẢO
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập n, liệt kê các số hoàn hảo trong đoạn [1, n] theo thứ tự giảm dần.
 *
 * Ý tưởng: Duyệt từ n về 1, dùng hàm LaSoHoanHao để kiểm tra từng số.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập n
 * 2. Với i từ n giảm đến 1, kiểm tra i có hoàn hảo không
 * 3. Nếu có, in i
 * Kết thúc
 */

using System;

namespace NMLT.Buoi08
{
    class LietKeSoHoanHao
    {
        public static bool LaSoHoanHao(int n)
        {
            if (n <= 0) return false;
            int tong = 0;
            for (int i = 1; i <= n / 2; i++)
                if (n % i == 0) tong += i;
            return tong == n;
        }

        public static void LietKeHoanHaoGiam(int n)
        {
            Console.Write("Cac so hoan hao trong doan [1, {0}]: ", n);
            for (int i = n; i >= 1; i--)
            {
                if (LaSoHoanHao(i))
                    Console.Write("{0} ", i);
            }
            Console.WriteLine();
        }

        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so n: ");
            int n = int.Parse(Console.ReadLine());

            LietKeHoanHaoGiam(n);
            Console.Read();
        }
    }
}
