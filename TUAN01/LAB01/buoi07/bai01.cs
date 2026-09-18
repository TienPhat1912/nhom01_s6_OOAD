/*
 * CHƯƠNG TRÌNH KIỂM TRA SỐ HOÀN HẢO BẰNG HÀM
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập n, kiểm tra n có phải là số hoàn hảo hay không bằng một hàm riêng.
 *
 * Ý tưởng: Viết hàm tính tổng các ước dương thực sự và trả về kết quả so sánh với n.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập n
 * 2. Gọi hàm LaSoHoanHao(n)
 * 3. Trong hàm, tính tổng các ước của n và kiểm tra tổng bằng n
 * 4. In kết luận
 * Kết thúc
 */

using System;

namespace NMLT.Buoi07
{
    class KiemTraSoHoanHao
    {
        public static bool LaSoHoanHao(int n)
        {
            if (n <= 0) return false;
            int tong = 0;
            for (int i = 1; i <= n / 2; i++)
            {
                if (n % i == 0) tong += i;
            }
            return tong == n;
        }

        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap n: ");
            int n = int.Parse(Console.ReadLine());

            if (LaSoHoanHao(n))
                Console.WriteLine("{0} la so hoan hao.", n);
            else
                Console.WriteLine("{0} khong la so hoan hao.", n);

            Console.Read();
        }
    }
}
